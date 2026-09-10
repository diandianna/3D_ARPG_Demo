using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;


public class SocketMgr : MonoBehaviour
{
    private static SocketMgr instance;
    public static SocketMgr Instance => instance;

    // 后台线程(ReceiveLoop)解析好的消息，主线程 Update 里统一消费。
    // 之前这里放的是"拼好的字符串"，主线程还要再 Split/Parse 一遍；现在直接放强类型消息对象。
    ConcurrentQueue<object> _msgQueue = new ConcurrentQueue<object>();
    Socket socket;

    public Transform WorldCanvasTrans;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        Connect();
    }

    void Update()
    {
        // 按 R 键重新连接
        if (Input.GetKeyDown(KeyCode.R) && !socket.Connected)
        {
            TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
            t.SetTipsText("tag: 尝试重连中");
            Connect();
        }

        // 主线程处理后台线程塞进来的消息
        while (_msgQueue.TryDequeue(out object msg))
        {
            switch (msg)
            {
                case ConnectMsg c:
                    if (!c.ok) Debug.LogWarning("[Socket] 连接失败或已断开，按 R 重连");
                    break;

                case LoginMsg l:
                    if (l.ok)
                    {
                        SceneLoaderMgr.Instance.targetSceneName = "MainScene";
                        SceneManager.LoadScene("LoadingScene");
                    }
                    else
                    {
                        TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
                        t.SetTipsText("tag: 登录失败，请检查账号密码是否正确");
                    }
                    break;

                case RegisterMsg r:
                    if (r.ok)
                    {
                        UIMgr.Instance.ShowPanel<LoginPanel>();
                        RegisterPanel rp = UIMgr.Instance.GetPanel<RegisterPanel>() as RegisterPanel;
                        rp.FillRegisterInfo();
                        UIMgr.Instance.HidePanel<RegisterPanel>();
                    }
                    else
                    {
                        TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
                        t.SetTipsText("tag: 注册失败，用户名已存在");
                    }
                    break;

                case AtkMsg a:
                    {
                        Debug.Log("服务器返回攻击消息");
                        float randomX = Random.Range(-1.5f, 1.5f);
                        Vector3 pos = GameDataMgr.Instance.monsters[a.enemyIndex].transform.position
                                      + transform.position + transform.right * randomX + transform.up * 1.8f;
                        DamageFontMgr.Instance.ShowDamageFont(pos, a.damage, WorldCanvasTrans, a.enemyIndex);
                        CountingMgr.Instance.AtkCounting();
                    }
                    break;

                case PlayerSyncMsg p:
                    // 已存在访客 → 只同步；不存在 → 同步并创建
                    SyncMgr.Instance.OnRecvPos(p.account, p.pos);
                    SyncMgr.Instance.OnRecvRot(p.account, p.yaw);
                    if (!SyncMgr.Instance.visitors.ContainsKey(p.account))
                        SyncMgr.Instance.CreateVisitor(p.account, p.pos);
                    break;

                case CreateRoomMsg cr:
                    SyncMgr.Instance.roomId = cr.roomId;
                    SyncMgr.Instance.isHost = true;
                    Debug.Log("未找到匹配房间，已自动创建房间");
                    SyncMgr.Instance.StartSyncLoop(socket);
                    break;

                case JoinRoomMsg jr:
                    GameDataMgr.Instance.ClearAllMonster();
                    SyncMgr.Instance.roomId = jr.roomId;
                    Debug.Log("已成功加入匹配房间");
                    SyncMgr.Instance.visitors.Add(GameDataMgr.Instance.acountName, GameDataMgr.Instance.mainCharacter);
                    SyncMgr.Instance.StartSyncLoop(socket);
                    break;

                case MonsterSyncMsg m:
                    MonsterSyncMgr.Instance.OnRecvMonster(m.id, m.type, m.pos, m.yaw, m.hp, m.state);
                    break;

                case MonsterSnapEndMsg _:
                    MonsterSyncMgr.Instance.OnSnapShotEnd();
                    break;

                case PossessionMonsterMsg pm:
                    Debug.Log("访客控制怪物同步");
                    MonsterSyncMgr.Instance.OnRecvPossessionMonster(pm.id, pm.pos, pm.yaw);
                    break;

                case PossessSuccessMsg ps:
                    StartCoroutine(SendPossessStateLoop(ps.id));
                    break;

                case PossessFailMsg _:
                    BulletTimeMgr.Instance.RollbackPossession();
                    break;

                case PossessReqMsg preq:
                    HandlePossessRequest(preq);
                    break;

                case SingleHurtMsg sh:
                    GameDataMgr.Instance.playerData.hp = sh.hp;
                    if (sh.dead == 1) GameDataMgr.Instance.mainCharacter.GetComponent<Animator>().SetBool("Death", true);
                    break;

                case VisitorHurtMsg vh:
                    if (vh.name == GameDataMgr.Instance.acountName)
                    {
                        if (vh.dead == 1)
                            SyncMgr.Instance.visitors[vh.name].GetComponent<Animator>().SetBool("Death", true);
                        else
                        {
                            GameDataMgr.Instance.playerData.hp = vh.hp;
                            SyncMgr.Instance.visitors[vh.name].GetComponent<Animator>().SetTrigger("Hurt");
                        }
                    }
                    else
                    {
                        if (vh.dead == 1)
                            SyncMgr.Instance.visitors[vh.name].GetComponent<Animator>().SetBool("Death", true);
                        else
                            SyncMgr.Instance.visitors[vh.name].GetComponent<Animator>().SetTrigger("Hurt");
                    }
                    break;

                case VisitorAtkMsg va:
                    if (SyncMgr.Instance.isHost)
                    {
                        // 房主：权威扣血（DamageTaken 里顺带播受击动画）
                        GameObject monster = GameDataMgr.Instance.GetMonsterByID(va.monsterId);
                        if (monster != null)
                            monster.GetComponent<MonsterData>().DamageTaken(va.damage);
                    }
                    else
                    {
                        // 访客：只播受击动画，血量靠快照 type100 同步
                        if (MonsterSyncMgr.Instance.monsters.TryGetValue(va.monsterId, out var mm))
                            mm.GetComponent<Animator>().SetTrigger("Hurt");
                    }
                    break;

                case PlayerAnimMsg panim:
                    if (panim.name == GameDataMgr.Instance.acountName) break;
                    Animator panimAnimator = SyncMgr.Instance.visitors[panim.name].GetComponent<Animator>();
                    if (panimAnimator == null) break;
                    switch (panim.animId)
                    {
                        case 0:
                            panimAnimator.SetInteger("ComboStep", panim.combo);
                            panimAnimator.SetTrigger("CanAttack");
                            break;
                        case 1:
                            panimAnimator.SetTrigger("Hurt");
                            break;
                    }
                    break;

                case MonsterAnimMsg manim:
                    HandleMonsterAnim(manim);
                    break;
            }
        }
    }

    void Connect()
    {
        Task.Run(async () =>
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8080));
                _msgQueue.Enqueue(new ConnectMsg { ok = true });

                await ReceiveLoop();
            }
            catch (SocketException ex)
            {
                _msgQueue.Enqueue(new ConnectMsg { ok = false });
                Debug.LogWarning($"[Socket] 连接失败: {ex.Message}");
            }
        });
    }

    async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024];

        // 接收缓冲区（核心！）
        byte[] _recvBuffer = new byte[1024];
        // 当前正在接收的消息的长度（-1 表示还没读到长度头）
        int _currentMsgLength = -1;

        // 心跳：每 2 秒发一次
        _ = Task.Run(() =>
        {
            while (true)
            {
                Thread.Sleep(2000);
                SendHeartbeat();
            }
        });

        while (socket.Connected)
        {
            int len = await Task.Factory.FromAsync<int>(
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                socket.EndReceive);
            if (len == 0) break;

            int index = 0;

            if (_currentMsgLength == -1)// 说明现在的数据是从数据头开始的
            {
                int AllLen = BitConverter.ToInt32(buffer, index);
                while (true)
                {
                    if (AllLen < 8 || AllLen > 1024 * 64)
                    {
                        Debug.LogError($"非法长度: {AllLen}，断开");
                        socket.Close();
                        return;
                    }
                    if (len > AllLen)// 黏包
                    {
                        index += 4;// 排除长度数据头
                        await ProcessMessages(socket, index, buffer);
                        index += AllLen - 4;
                        len -= AllLen;
                        if (len <= 0)
                        {
                            _currentMsgLength = -1; break;
                        }
                        AllLen = BitConverter.ToInt32(buffer, index);
                    }
                    else if (len < AllLen)// 分包
                    {
                        Array.Copy(buffer, index, _recvBuffer, 0, len);
                        _currentMsgLength = len;
                        break;
                    }
                    else// 整包
                    {
                        index += 4;
                        await ProcessMessages(socket, index, buffer);
                        _currentMsgLength = -1;
                        break;
                    }
                }
            }
            else
            {
                int AllLen = BitConverter.ToInt32(_recvBuffer);
                while (true)
                {
                    if (AllLen < 8 || AllLen > 1024 * 64)
                    {
                        Debug.LogError($"非法长度: {AllLen}，断开");
                        socket.Close();
                        return;
                    }
                    if (len + _currentMsgLength > AllLen)// 黏包
                    {
                        Array.Copy(buffer, index, _recvBuffer, _currentMsgLength, AllLen - _currentMsgLength);
                        index += 4;
                        await ProcessMessages(socket, 4, _recvBuffer);
                        index += AllLen - 4;
                        len -= (AllLen - _currentMsgLength);
                        if (len <= 0)
                        {
                            _currentMsgLength = -1; break;
                        }
                        AllLen = BitConverter.ToInt32(buffer, index - _currentMsgLength);
                        if (_currentMsgLength != 0)
                        {
                            index -= _currentMsgLength;
                            _currentMsgLength = 0;
                        }
                    }
                    else if (len + _currentMsgLength < AllLen)// 分包
                    {
                        Array.Copy(buffer, 0, _recvBuffer, _currentMsgLength, len);
                        _currentMsgLength += len;
                        break;
                    }
                    else// 整包
                    {
                        Array.Copy(buffer, 0, _recvBuffer, _currentMsgLength, AllLen - _currentMsgLength);
                        index += 4;
                        await ProcessMessages(socket, index, _recvBuffer);
                        _currentMsgLength = -1;
                        break;
                    }
                }
            }
        }
        _msgQueue.Enqueue(new ConnectMsg { ok = false });
    }

    public void Send(int type, byte[] bytedata)
    {
        if (socket == null || !socket.Connected)
        {
            Debug.LogWarning("未连接到服务器");
            return;
        }

        byte[] buffer;
        if (bytedata == null)
        {
            buffer = new byte[4 + 4];
            BitConverter.GetBytes(4 + 4).CopyTo(buffer, 0);
            BitConverter.GetBytes(type).CopyTo(buffer, 4);
        }
        else
        {
            buffer = new byte[4 + 4 + bytedata.Length];
            BitConverter.GetBytes(4 + 4 + bytedata.Length).CopyTo(buffer, 0);
            BitConverter.GetBytes(type).CopyTo(buffer, 4);
            bytedata.CopyTo(buffer, 8);
        }

        Task.Run(async () =>
        {
            await Task.Factory.FromAsync<int>(
                socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                socket.EndSend);
        });
    }

    public async Task SendLogin_Register(AcountData acountData, int type)
    {
        byte[] buffer = new byte[1024];
        int index = 4;
        if (type > 1) return;

        // 数据类型
        BitConverter.GetBytes(type).CopyTo(buffer, index);
        index += sizeof(int);

        // 账号长度 + 账号
        byte[] accountBytes = Encoding.UTF8.GetBytes(acountData.acountName);
        BitConverter.GetBytes(accountBytes.Length).CopyTo(buffer, index);
        index += sizeof(int);
        accountBytes.CopyTo(buffer, index);
        index += accountBytes.Length;

        // 密码长度 + 密码
        byte[] pwdBytes = Encoding.UTF8.GetBytes(acountData.password);
        BitConverter.GetBytes(pwdBytes.Length).CopyTo(buffer, index);
        index += sizeof(int);
        pwdBytes.CopyTo(buffer, index);
        index += pwdBytes.Length;

        // 4 总长 + 4 类型 + 4 账号长度 + 账号 + 4 密码长度 + 密码
        BitConverter.GetBytes(4 + 4 + 4 + accountBytes.Length + 4 + pwdBytes.Length).CopyTo(buffer, 0);

        print("发送登录/注册请求");
        await Task.Factory.FromAsync<int>(
            socket.BeginSend(buffer, 0, index, SocketFlags.None, null, null),
            socket.EndSend);
    }

    private void OnDestroy()
    {
        socket.Close();
        _msgQueue.Clear();
    }

    // 用于在其他脚本中检查 socket 是否连接
    public bool Check()
    {
        return socket != null && socket.Connected;
    }

    async Task ProcessMessages(Socket socket, int index, byte[] buffer)
    {
        MsgType msgType = (MsgType)BitConverter.ToInt32(buffer, index);
        index += 4;

        switch (msgType)
        {
            case MsgType.Login:
                if (BitConverter.ToInt32(buffer, index) == 1)
                {
                    _msgQueue.Enqueue(new LoginMsg { ok = true });
                    // 登录成功则字节数组里必定还有 PlayerData 数据，直接更新
                    GameDataMgr.Instance.playerData.UpdataPlayerData(buffer);
                }
                else
                {
                    _msgQueue.Enqueue(new LoginMsg { ok = false });
                }
                break;

            case MsgType.Register:
                _msgQueue.Enqueue(new RegisterMsg { ok = BitConverter.ToInt32(buffer, index) == 1 });
                break;

            case MsgType.Atk:
                {
                    int damage = BitConverter.ToInt32(buffer, index); index += 4;
                    int enemyIndex = BitConverter.ToInt32(buffer, index); index += 4;
                    _msgQueue.Enqueue(new AtkMsg { damage = damage, enemyIndex = enemyIndex });
                }
                break;

            case MsgType.Hurt:// 单人模式受伤
                {
                    int hp = BitConverter.ToInt32(buffer, index); index += 4;
                    int dead = BitConverter.ToInt32(buffer, index); index += 4;
                    _msgQueue.Enqueue(new SingleHurtMsg { hp = hp, dead = dead });
                }
                break;

            case MsgType.VisitorHurt:// 多人联机模式受伤
                {
                    int hp = BitConverter.ToInt32(buffer, index); index += 4;
                    int dead = BitConverter.ToInt32(buffer, index); index += 4;
                    int nameLen = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, nameLen); index += nameLen;
                    _msgQueue.Enqueue(new VisitorHurtMsg { hp = hp, dead = dead, name = name });
                }
                break;

            case MsgType.VisitorAtk:
                {
                    int monsterId = BitConverter.ToInt32(buffer, index); index += 4;
                    int damage = BitConverter.ToInt32(buffer, index); index += 4;
                    int nameLen = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, nameLen); index += nameLen;
                    _msgQueue.Enqueue(new VisitorAtkMsg { monsterId = monsterId, damage = damage, name = name });
                }
                break;

            case MsgType.MultiSync:
                {
                    index += 4;// roomid（这里不用）
                    float x = BitConverter.ToSingle(buffer, index); index += 4;
                    float y = BitConverter.ToSingle(buffer, index); index += 4;
                    float z = BitConverter.ToSingle(buffer, index); index += 4;
                    float yrot = BitConverter.ToSingle(buffer, index); index += 4;
                    int len = BitConverter.ToInt32(buffer, index); index += 4;
                    string acountName = Encoding.UTF8.GetString(buffer, index, len); index += len;
                    // 是否新访客的判断放到主线程做（这里不碰 SyncMgr 字典，避免跨线程读写）
                    _msgQueue.Enqueue(new PlayerSyncMsg { account = acountName, pos = new Vector3(x, y, z), yaw = yrot });
                }
                break;

            case MsgType.Multiplayer:
                {
                    int result = BitConverter.ToInt32(buffer, index); index += 4;
                    int roomId = BitConverter.ToInt32(buffer, index); index += 4;
                    if (result == 0)
                        _msgQueue.Enqueue(new CreateRoomMsg { roomId = roomId });
                    else if (result == 1)
                        _msgQueue.Enqueue(new JoinRoomMsg { roomId = roomId });
                }
                break;

            case MsgType.MonsterSync:
                {
                    index += 4;// roomid
                    int monsterCount = BitConverter.ToInt32(buffer, index); index += 4;
                    for (int i = 0; i < monsterCount; i++)
                    {
                        int monsterId = BitConverter.ToInt32(buffer, index); index += 4;
                        int monsterType = BitConverter.ToInt32(buffer, index); index += 4;
                        float x = BitConverter.ToSingle(buffer, index); index += 4;
                        float y = BitConverter.ToSingle(buffer, index); index += 4;
                        float z = BitConverter.ToSingle(buffer, index); index += 4;
                        float yaw = BitConverter.ToSingle(buffer, index); index += 4;
                        int hp = BitConverter.ToInt32(buffer, index); index += 4;
                        int state = BitConverter.ToInt32(buffer, index); index += 4;
                        _msgQueue.Enqueue(new MonsterSyncMsg { id = monsterId, type = monsterType, pos = new Vector3(x, y, z), yaw = yaw, hp = hp, state = state });
                    }
                    _msgQueue.Enqueue(new MonsterSnapEndMsg());
                }
                break;

            case MsgType.Possess:
                {
                    int allLen = BitConverter.ToInt32(buffer, index - 8);// 本消息的长度头，回包时复用
                    int roomId = BitConverter.ToInt32(buffer, index); index += 4;
                    int nameLen = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, nameLen); index += nameLen;
                    int id = BitConverter.ToInt32(buffer, index); index += 4;
                    int flag = BitConverter.ToInt32(buffer, index); index += 4;
                    _msgQueue.Enqueue(new PossessReqMsg { roomId = roomId, name = name, id = id, flag = flag, allLen = allLen });
                }
                break;

            case MsgType.PossessOK:
                {
                    index += 4;// roomid
                    int nameLen = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, nameLen); index += nameLen;
                    int id = BitConverter.ToInt32(buffer, index); index += 4;
                    int flag = BitConverter.ToInt32(buffer, index); index += 4;
                    int result = BitConverter.ToInt32(buffer, index); index += 4;
                    if (flag == 1)
                    {
                        if (result == 1)
                            _msgQueue.Enqueue(new PossessSuccessMsg { id = id });
                        else
                        {
                            Debug.Log("附身失败：已被他人附身");
                            _msgQueue.Enqueue(new PossessFailMsg());
                        }
                    }
                    else
                    {
                        Debug.Log(result == 1 ? "解除成功" : "解除失败");
                    }
                }
                break;

            case MsgType.PossessState:
                {
                    index += 4;// roomid
                    int monsterId = BitConverter.ToInt32(buffer, index); index += 4;
                    float x = BitConverter.ToSingle(buffer, index); index += 4;
                    float y = BitConverter.ToSingle(buffer, index); index += 4;
                    float z = BitConverter.ToSingle(buffer, index); index += 4;
                    float yaw = BitConverter.ToSingle(buffer, index); index += 4;
                    _msgQueue.Enqueue(new PossessionMonsterMsg { id = monsterId, pos = new Vector3(x, y, z), yaw = yaw });
                }
                break;

            case MsgType.PlayerAnimation:
                {
                    int animationId = BitConverter.ToInt32(buffer, index); index += 4;
                    int combo = BitConverter.ToInt32(buffer, index); index += 4;
                    int len = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, len); index += len;
                    _msgQueue.Enqueue(new PlayerAnimMsg { animId = animationId, combo = combo, name = name });
                }
                break;

            case MsgType.MonsterAnimation:
                {
                    int animationId = BitConverter.ToInt32(buffer, index); index += 4;
                    int combo = BitConverter.ToInt32(buffer, index); index += 4;
                    float posX = BitConverter.ToSingle(buffer, index); index += 4;
                    float posY = BitConverter.ToSingle(buffer, index); index += 4;
                    float posZ = BitConverter.ToSingle(buffer, index); index += 4;
                    float yaw = BitConverter.ToSingle(buffer, index); index += 4;
                    int monsterId = BitConverter.ToInt32(buffer, index); index += 4;
                    int len = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, len); index += len;
                    _msgQueue.Enqueue(new MonsterAnimMsg { animId = animationId, combo = combo, pos = new Vector3(posX, posY, posZ), yaw = yaw, monsterId = monsterId, name = name });
                }
                break;
        }
    }

    // 附身请求：房主本地判定是否可附身，然后回一个 PossessOK（复用原请求字节 + 追加 ok 标志）
    void HandlePossessRequest(PossessReqMsg r)
    {
        int ok = 0;
        GameObject target = GameDataMgr.Instance.GetMonsterByID(r.id);
        if (r.flag == 1)
        {
            if (!GameDataMgr.Instance.possessedMonsters.Contains(target))
            {
                ok = 1;
                GameDataMgr.Instance.possessedMonsters.Add(target);
            }
        }
        else
        {
            if (GameDataMgr.Instance.possessedMonsters.Contains(target))
            {
                ok = 1;
                GameDataMgr.Instance.possessedMonsters.Remove(target);
            }
        }

        byte[] nameBytes = Encoding.UTF8.GetBytes(r.name);
        byte[] response = new byte[r.allLen + 4];
        int idx = 0;
        BitConverter.GetBytes(r.allLen + 4).CopyTo(response, idx); idx += 4;
        BitConverter.GetBytes(102).CopyTo(response, idx); idx += 4;
        BitConverter.GetBytes(r.roomId).CopyTo(response, idx); idx += 4;
        BitConverter.GetBytes(nameBytes.Length).CopyTo(response, idx); idx += 4;
        nameBytes.CopyTo(response, idx); idx += nameBytes.Length;
        BitConverter.GetBytes(r.id).CopyTo(response, idx); idx += 4;
        BitConverter.GetBytes(r.flag).CopyTo(response, idx); idx += 4;
        BitConverter.GetBytes(ok).CopyTo(response, idx);
        socket.Send(response);
    }

    // 怪物动画同步：动画切换点对齐，Boss 位置由根运动驱动，两端播同一段动画落点天然一致
    void HandleMonsterAnim(MonsterAnimMsg ma)
    {
        if (ma.name == GameDataMgr.Instance.acountName) return;

        // 访客端怪物在 MonsterSyncMgr 字典里（GameDataMgr 只存房主本地的），用 TryGetValue 取
        if (!MonsterSyncMgr.Instance.monsters.TryGetValue(ma.monsterId, out GameObject monster) || monster == null)
        {
            Debug.LogWarning($"[怪物动画同步] 找不到怪物 monsterid={ma.monsterId}");
            return;
        }
        Animator animator = monster.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"[怪物动画同步] 怪物 {monster.name} 上没有 Animator 组件");
            return;
        }

        // 动画切换点对齐：Boss 位置由根运动驱动，两端播同一段动画从同一起点开始，
        // 落点就天然一致。所以收到攻击/转阶段/转向消息时，先把位置+朝向瞬移到
        // 房主切动画那一刻的起点，之后完全信任根运动（MonsterSyncMgr 不再做位置纠正）。
        bool isBossAnim = monster.GetComponent<BossData>() != null;
        if (isBossAnim && (ma.animId == 0 || ma.animId == 3 || ma.animId == 4))
        {
            monster.transform.position = ma.pos;
            monster.transform.rotation = Quaternion.Euler(0, ma.yaw, 0);
        }

        switch (ma.animId)
        {
            case 0:
                {
                    animator.SetInteger("ComboStep", ma.combo);
                    animator.SetTrigger("CanAttack");
                    if (monster.GetComponent<BossData>() != null)
                    {
                        animator.SetFloat("SpeedX", 0);   // Boss 攻击时停住，移动归零
                        animator.SetFloat("SpeedY", 0);
                    }
                }
                break;
            case 1:
                animator.SetTrigger("Hurt");
                break;
            case 2:
                animator.SetInteger("ComboStep", -1);
                break;
            case 3:
                {
                    // 转阶段（Boss）：播 Rage + 同步 Phase
                    animator.CrossFade("Rage", 0.1f);
                    BossData bossData = monster.GetComponent<BossData>();
                    if (bossData != null) bossData.Phase++;
                    animator.SetBool("Special", true);
                }
                break;
            case 4:
                {
                    // Boss 移动方向同步：combo = (moveX+1)*3 + (moveY+1)，moveX/moveY ∈ -1/0/1
                    if (monster.GetComponent<BossData>() != null)
                    {
                        int moveX = ma.combo / 3 - 1;
                        int moveY = ma.combo % 3 - 1;
                        animator.SetFloat("SpeedX", moveX);
                        animator.SetFloat("SpeedY", moveY);
                    }
                }
                break;
        }
    }

    public void SendHeartbeat()
    {
        Send(7, null);
    }

    IEnumerator SendPossessStateLoop(int monsterId)
    {
        if (!MonsterSyncMgr.Instance.monsters.TryGetValue(monsterId, out var m)) yield break;
        while (GameDataMgr.Instance.isPossession && m != null)
        {
            byte[] buf = new byte[24];
            BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(buf, 0);
            BitConverter.GetBytes(monsterId).CopyTo(buf, 4);
            BitConverter.GetBytes(m.transform.position.x).CopyTo(buf, 8);
            BitConverter.GetBytes(m.transform.position.y).CopyTo(buf, 12);
            BitConverter.GetBytes(m.transform.position.z).CopyTo(buf, 16);
            BitConverter.GetBytes(m.transform.eulerAngles.y).CopyTo(buf, 20);
            SocketMgr.Instance.Send(103, buf);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void SendPlayerAnimation(int animationId, int combo)
    {
        if (!SyncMgr.Instance.isRoom) return;
        byte[] buffer = new byte[12];
        BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(buffer, 0);
        BitConverter.GetBytes(animationId).CopyTo(buffer, 4);
        BitConverter.GetBytes(combo).CopyTo(buffer, 8);
        SocketMgr.instance.Send(200, buffer);
    }

    public void SendMonsterAnimation(int animationId, int combo, int monsterid, Vector3 pos, float yaw)
    {
        if (!SyncMgr.Instance.isRoom) return;
        // 布局：roomId + animationId + combo + posX/Y/Z + yaw + monsterid（32 字节）
        // pos/yaw 是"切动画那一刻"的起点：访客收到后对齐到这个起点再播同一段动画，
        // 之后位置完全信任根运动（两端根运动确定，落点天然一致），不再靠每帧快照纠正。
        byte[] buffer = new byte[32];
        BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(buffer, 0);
        BitConverter.GetBytes(animationId).CopyTo(buffer, 4);
        BitConverter.GetBytes(combo).CopyTo(buffer, 8);
        BitConverter.GetBytes(pos.x).CopyTo(buffer, 12);
        BitConverter.GetBytes(pos.y).CopyTo(buffer, 16);
        BitConverter.GetBytes(pos.z).CopyTo(buffer, 20);
        BitConverter.GetBytes(yaw).CopyTo(buffer, 24);
        BitConverter.GetBytes(monsterid).CopyTo(buffer, 28);
        SocketMgr.instance.Send(201, buffer);
    }
}


// ===== 网络消息对象：后台线程解析好值，主线程直接用字段，不再字符串 Split/Parse =====

sealed class ConnectMsg           { public bool ok; }
sealed class LoginMsg             { public bool ok; }
sealed class RegisterMsg          { public bool ok; }
sealed class AtkMsg               { public int damage; public int enemyIndex; }
sealed class SingleHurtMsg        { public int hp; public int dead; }
sealed class VisitorHurtMsg       { public int hp; public int dead; public string name; }
sealed class VisitorAtkMsg        { public int monsterId; public int damage; public string name; }
sealed class PlayerSyncMsg        { public string account; public Vector3 pos; public float yaw; }
sealed class CreateRoomMsg        { public int roomId; }
sealed class JoinRoomMsg          { public int roomId; }
sealed class MonsterSyncMsg       { public int id; public int type; public Vector3 pos; public float yaw; public int hp; public int state; }
sealed class MonsterSnapEndMsg    { }
sealed class PossessionMonsterMsg { public int id; public Vector3 pos; public float yaw; }
sealed class PossessSuccessMsg    { public int id; }
sealed class PossessFailMsg       { }
sealed class PossessReqMsg        { public int roomId; public string name; public int id; public int flag; public int allLen; }
sealed class PlayerAnimMsg        { public int animId; public int combo; public string name; }
sealed class MonsterAnimMsg       { public int animId; public int combo; public int monsterId; public Vector3 pos; public float yaw; public string name; }


/* type 说明（与服务器约定）
 * 0 登录消息
 * 1 注册消息
 * 2 更新消息
 * 7 心跳消息
 */
