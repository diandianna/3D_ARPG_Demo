using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using Random = UnityEngine.Random;


public class SocketMgr : MonoBehaviour
{
    private static SocketMgr instance;
    public static SocketMgr Instance => instance;

    ConcurrentQueue<string> _msgQueue = new ConcurrentQueue<string>();
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

    int damage;
    int enemyIndex;
    // Update is called once per frame
    void Update()
    {
        //按R键重新连接
        if (Input.GetKeyDown(KeyCode.R)&&!socket.Connected)
        {
            TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
            t.SetTipsText("tag: 尝试重连中");
            Connect();
        }
        //主线程处理消息
        while(_msgQueue.TryDequeue(out string msg))
        {
            //Debug.Log(msg);

           // 在主线程处理场景切换等 Unity API 调用
            if (msg == "登录成功")
            {
                SceneLoaderMgr.Instance.targetSceneName = "MainScene";
               SceneManager.LoadScene("LoadingScene");
            }
            else if (msg == "登录失败")
            {
                TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
                t.SetTipsText("tag: 登录失败，请检查账号密码是否正确");
            }
             if (msg == "注册成功")
            {
                UIMgr.Instance.ShowPanel<LoginPanel>();
                RegisterPanel t = UIMgr.Instance.GetPanel<RegisterPanel>() as RegisterPanel;
                t.FillRegisterInfo();
                UIMgr.Instance.HidePanel<RegisterPanel>();
            }
            else if (msg == "注册失败")
            {
                TipsPanel t = UIMgr.Instance.ShowPanel<TipsPanel>() as TipsPanel;
                t.SetTipsText("tag: 注册失败，用户名已存在");
            }
             if(msg == "攻击消息")
            {
                Debug.Log("服务器返回攻击消息");
                float randomX = Random.Range(-1.5f, 1.5f);
                Vector3 vector3 = GameDataMgr.Instance.monsters[enemyIndex].transform.position + transform.position + transform.right * randomX + transform.up * 1.8f;
                DamageFontMgr.Instance.ShowDamageFont(vector3,damage,WorldCanvasTrans,enemyIndex);
                //print("造成伤害: " + damage);

                CountingMgr.Instance.AtkCounting();
            }
            if (msg.StartsWith("同步玩家位置"))
            {
                var parts = msg.Split('|');
                string acountName = parts[1];
                float x = float.Parse(parts[2]);
                float y = float.Parse(parts[3]);
                float z = float.Parse(parts[4]);
                float yrot = float.Parse(parts[5]);
                SyncMgr.Instance.OnRecvPos(acountName,new Vector3(x, y, z));
                SyncMgr.Instance.OnRecvRot(acountName,yrot);
            }

            if (msg.StartsWith("创建其他玩家"))
            {
                var parts = msg.Split('|');
                string acountName = parts[1];
                float x = float.Parse(parts[2]);
                float y = float.Parse(parts[3]);
                float z = float.Parse(parts[4]);
                float yrot = float.Parse(parts[5]);
                SyncMgr.Instance.OnRecvPos(acountName,new Vector3(x, y, z));
                SyncMgr.Instance.OnRecvRot(acountName,yrot);
                SyncMgr.Instance.CreateVisitor(acountName ,new Vector3(x, y, z));
            }

             if (msg.StartsWith("创建房间"))
            {
                var parts = msg.Split('|');
                SyncMgr.Instance.roomId = int.Parse(parts[1]);
                SyncMgr.Instance.isHost = true;
                Debug.Log("未找到匹配房间，已自动创建房间");
                SyncMgr.Instance.StartSyncLoop(socket);
            }
            else if (msg.StartsWith("加入房间成功"))
            {
                GameDataMgr.Instance.ClearAllMonster();

                var parts = msg.Split('|');
                SyncMgr.Instance.roomId = int.Parse(parts[1]);
                Debug.Log("已成功加入匹配房间");

                SyncMgr.Instance.visitors.Add(GameDataMgr.Instance.acountName, GameDataMgr.Instance.mainCharacter);

                SyncMgr.Instance.StartSyncLoop(socket);
            }

            if (msg == "怪物快照结束")
            {
                MonsterSyncMgr.Instance.OnSnapShotEnd();
            }
            else if (msg.StartsWith("怪物位置同步"))
            {
                var parts = msg.Split('|');
                int id =int.Parse(parts[1]);
                int type =int.Parse(parts[2]);
                float x = float.Parse(parts[3]);
                float y = float.Parse(parts[4]);
                float z =float.Parse(parts[5]);
                float yaw =float.Parse(parts[6]);
                int hp =int.Parse(parts[7]);
                int state =int.Parse(parts[8]);
                  MonsterSyncMgr.Instance.OnRecvMonster(id, type, new Vector3(x, y, z), yaw, hp, state);
            }
            if (msg.StartsWith("附身怪物同步"))
            {
                Debug.Log("访客控制怪物同步");
                var parts = msg.Split('|');
                int id = int.Parse(parts[1]);
                float x = float.Parse(parts[2]);
                float y = float.Parse(parts[3]);
                float z = float.Parse(parts[4]);
                float yaw = float.Parse(parts[5]);
                //var m = GameDataMgr.Instance.GetMonsterByID(id);
                //if (m != null)
                //{
                //    m.transform.position = new Vector3(x, y, z);
                //    m.transform.eulerAngles = new Vector3(0, yaw, 0);
                //}
                MonsterSyncMgr.Instance.OnRecvPossessionMonster(id, new Vector3(x, y, z), yaw);
            }
            if (msg.StartsWith("成功附身"))
            {
                var parts = msg.Split('|');
                int id = int.Parse(parts[1]);

                StartCoroutine(SendPossessStateLoop(id));
            }
            if (msg == ("附身失败"))
            {
                BulletTimeMgr.Instance.RollbackPossession();
            }
            if (msg.StartsWith("附身请求"))
            {
                var parts = msg.Split('|');
                int roomid = int.Parse(parts[1]);
                int strlen = int.Parse(parts[2]);
                string name = parts[3];
                int id = int.Parse(parts[4]);
                int flag = int.Parse(parts[5]);
                int allLen = int.Parse(parts[6]);

                int ok = 0;
                if (flag == 1)
                {
                    if (!GameDataMgr.Instance.possessedMonsters.Contains(GameDataMgr.Instance.GetMonsterByID(id)))
                    {
                        //发PossessOK消息
                        ok = 1;
                        GameDataMgr.Instance.possessedMonsters.Add(GameDataMgr.Instance.GetMonsterByID(id));
                    }
                }
                else
                {
                    if (GameDataMgr.Instance.possessedMonsters.Contains(GameDataMgr.Instance.GetMonsterByID(id)))
                    {
                        //发PossessOK消息
                        ok = 1;
                        GameDataMgr.Instance.possessedMonsters.Remove(GameDataMgr.Instance.GetMonsterByID(id));
                    }
                }
                byte[] response = new byte[allLen + 4];
                int index = 0;
                BitConverter.GetBytes(allLen + 4).CopyTo(response, index);index += 4;
                BitConverter.GetBytes(102).CopyTo(response, index);index += 4;
                BitConverter.GetBytes(roomid).CopyTo(response, index);index += 4;
                BitConverter.GetBytes(strlen).CopyTo(response, index);index += 4;
                Encoding.UTF8.GetBytes(name).CopyTo(response, index); index += strlen;
                BitConverter.GetBytes(id).CopyTo(response, index);index += 4;
                BitConverter.GetBytes(flag).CopyTo(response, index);index += 4;
                BitConverter.GetBytes(ok).CopyTo(response, index);
                socket.Send(response);
            }
            if (msg.StartsWith("单人受伤命中"))
            {
                var parts = msg.Split('|');
                int dead = int.Parse(parts[1]);
                int hp = int.Parse(parts[2]);
                GameDataMgr.Instance.playerData.hp = hp;
                if (dead == 1) GameDataMgr.Instance.mainCharacter.GetComponent<Animator>().SetBool("Death", true);
            }
            if (msg.StartsWith("联机受伤命中"))
            {
                var parts = msg.Split('|');
                int dead = int.Parse(parts[1]);
                int hp = int.Parse(parts[2]);
                string name = parts[3];
                if (name == GameDataMgr.Instance.acountName)
                {
                    if (dead == 1)
                    {
                        SyncMgr.Instance.visitors[name].GetComponent<Animator>().SetBool("Death", true);
                    }
                    else
                    {
                        GameDataMgr.Instance.playerData.hp = hp;
                        SyncMgr.Instance.visitors[name].GetComponent<Animator>().SetTrigger("Hurt");
                    }
                }
                else
                {
                    if (dead == 1)
                        SyncMgr.Instance.visitors[name].GetComponent<Animator>().SetBool("Death", true);
                    else SyncMgr.Instance.visitors[name].GetComponent<Animator>().SetTrigger("Hurt");
                }
            }
            if (msg.StartsWith("联机攻击命中"))
            {
                var parts = msg.Split('|');
                int monsterid = int.Parse(parts[1]);
                int damage = int.Parse(parts[2]);
                string name2 = parts[3];
                if (SyncMgr.Instance.isHost)
                {
                    // 房主：权威扣血（DamageTaken 里顺带播受击动画）
                    GameObject monster = GameDataMgr.Instance.GetMonsterByID(monsterid);
                    if (monster != null)
                        monster.GetComponent<MonsterData>().DamageTaken(damage);
                }
                else
                {
                    // 访客：只播受击动画，血量靠快照 type100 同步
                    if (MonsterSyncMgr.Instance.monsters.TryGetValue(monsterid, out var m))
                        m.GetComponent<Animator>().SetTrigger("Hurt");
                }
            }
            if (msg.StartsWith("同步玩家动画"))
            {
                var parts = msg.Split('|');
                int animationId = int.Parse(parts[1]);
                int combo = int.Parse(parts[2]);
                string name = parts[3];

                if (name == GameDataMgr.Instance.acountName) return;
                Animator animator = SyncMgr.Instance.visitors[name].GetComponent<Animator>();
                if (animator == null) return;
                switch (animationId)
                {
                    case 0:
                        {
                            animator.SetInteger("ComboStep", combo);
                            animator.SetTrigger("CanAttack");
                        }
                        break;
                    case 1:
                        {
                            animator.SetTrigger("Hurt");
                        }
                        break;
                }
            }
            if (msg.StartsWith("同步怪物动画"))
            {
                var parts = msg.Split('|');
                int animationId = int.Parse(parts[1]);
                int combo = int.Parse(parts[2]);
                int monsterid = int.Parse(parts[3]);
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                float posX = float.Parse(parts[4], inv);
                float posY = float.Parse(parts[5], inv);
                float posZ = float.Parse(parts[6], inv);
                float yaw = float.Parse(parts[7], inv);
                string name = parts[8];

                if (name == GameDataMgr.Instance.acountName) return;
                // 访客端怪物在 MonsterSyncMgr 字典里（GameDataMgr 只存房主本地的），用 TryGetValue 取
                Debug.Log($"[怪物动画同步] 收到 动画id={animationId} combo={combo} monsterid={monsterid} name={name} 我的名字={GameDataMgr.Instance.acountName}");
                if (!MonsterSyncMgr.Instance.monsters.TryGetValue(monsterid, out GameObject monster) || monster == null)
                {
                    Debug.LogWarning($"[怪物动画同步] 找不到怪物 monsterid={monsterid}，字典里有的key={string.Join(",", MonsterSyncMgr.Instance.monsters.Keys)}");
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
                if (isBossAnim && (animationId == 0 || animationId == 3 || animationId == 4))
                {
                    monster.transform.position = new Vector3(posX, posY, posZ);
                    monster.transform.rotation = Quaternion.Euler(0, yaw, 0);
                }
                switch (animationId)
                {
                    case 0:
                        {
                            animator.SetInteger("ComboStep", combo);
                            animator.SetTrigger("CanAttack");
                            if (monster.GetComponent<BossData>() != null)
                            {
                                animator.SetFloat("SpeedX", 0);   // Boss 攻击时停住，移动归零
                                animator.SetFloat("SpeedY", 0);
                                string weights = "";
                                for (int li = 0; li < animator.layerCount; li++)
                                    weights += $"L{li}权={animator.GetLayerWeight(li)} ";
                                Debug.Log($"[怪物动画同步] 已触发Boss攻击 combo={combo} monsterid={monsterid} 怪物={monster.name} 层数={animator.layerCount} {weights} L1状态={animator.GetCurrentAnimatorStateInfo(1).fullPathHash} CanAttack参数={animator.GetBool("CanAttack")}");
                            }
                        }
                        break;
                    case 1:
                        animator.SetTrigger("Hurt");
                        break;
                    case 2:
                        {
                            animator.SetInteger("ComboStep", -1);
                        }
                        break;
                    case 3:
                        {
                            // 转阶段（Boss）：播 Rage + 同步 Phase
                            animator.CrossFade("Rage", 0.1f);
                            BossData bossData = monster.GetComponent<BossData>();
                            if (bossData != null) bossData.Phase ++;
                            animator.SetBool("Special", true);
                        }
                        break;
                    case 4:
                        {
                            // Boss 移动方向同步：combo = (moveX+1)*3 + (moveY+1)，moveX/moveY ∈ -1/0/1
                            if (monster.GetComponent<BossData>() != null)
                            {
                                int moveX = combo / 3 - 1;
                                int moveY = combo % 3 - 1;
                                animator.SetFloat("SpeedX", moveX);
                                animator.SetFloat("SpeedY", moveY);
                            }
                        }
                        break;
                }
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
                _msgQueue.Enqueue("连接成功");



                await ReceiveLoop();
            }
            catch (SocketException ex)
            {
                _msgQueue.Enqueue($"连接失败: {ex.Message}");
            }
        });
    }

    async Task ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        //DateTime lastHeartbeat = DateTime.UtcNow;

        // 接收缓冲区（核心！）
        byte[] _recvBuffer = new byte[1024];        //MemoryStream _recvBuffer = new MemoryStream();
        // 当前正在接收的消息的长度（-1表示还没读到长度头）
        int _currentMsgLength = -1;

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

            int AllLen;
            if (_currentMsgLength == -1)//说明现在的数据是从数据头开始的
            {

                AllLen = BitConverter.ToInt32(buffer, index);
                while (true)
                {
                    if (AllLen < 8 || AllLen > 1024 * 64)
                    {
                        Debug.LogError($"非法长度: {AllLen}，断开");
                        socket.Close();
                        return;
                    }
                    if (len > AllLen)//黏包
                    {
                        index += 4;//排除长度数据头
                        await ProcessMessages(socket,index, buffer);
                        index += AllLen - 4;
                        len -= AllLen;
                        if (len <= 0)
                        {
                            _currentMsgLength = -1; break;
                        }
                        AllLen = BitConverter.ToInt32(buffer, index);
                    }
                    else if (len < AllLen)//分包
                    {
                        Array.Copy(buffer, index, _recvBuffer, 0, len);
                        _currentMsgLength = len;
                        break;
                    }
                    else//整包
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
                AllLen = BitConverter.ToInt32(_recvBuffer);
                while (true)
                {
                    if (AllLen < 8 || AllLen > 1024 * 64)
                    {
                        Debug.LogError($"非法长度: {AllLen}，断开");
                        socket.Close();
                        return;
                    }
                    if (len + _currentMsgLength > AllLen)//黏包
                    {
                        Array.Copy(buffer, index, _recvBuffer, _currentMsgLength, AllLen - _currentMsgLength);
                        index += 4;//排除长度数据头
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
                    else if (len + _currentMsgLength < AllLen)//分包
                    {
                        Array.Copy(buffer, 0, _recvBuffer, _currentMsgLength, len);
                        if (_currentMsgLength == 0)
                        {
                            _currentMsgLength = len;
                        }
                        else _currentMsgLength += len;
                        break;
                    }
                    else//整包
                    {
                        Array.Copy(buffer, 0, _recvBuffer, _currentMsgLength, AllLen - _currentMsgLength);
                        index += 4;
                        await ProcessMessages(socket, index, _recvBuffer);
                        _currentMsgLength = -1;
                        break;
                    }
                }
            }

            
            //int type = BitConverter.ToInt32(buffer, index);
            //index += 4;
            //switch (type)
            //{
            //    case 0:if(BitConverter.ToInt32(buffer, index) == 1)
            //        {
            //            _msgQueue.Enqueue("登录成功");
            //            //登录成功则字节数组必定还有消息（PlayerData数据）
            //            GameDataMgr.Instance.playerData.UpdataPlayerData(buffer);
            //        }
            //        else
            //        {
            //            _msgQueue.Enqueue("登录失败");
            //        }
            //        break;
            //    case 1:
            //        if (BitConverter.ToInt32(buffer, index) == 1)
            //        {
            //            _msgQueue.Enqueue("注册成功");
            //        }
            //        else
            //        {
            //            _msgQueue.Enqueue("注册失败");
            //        }
            //        break;
            //}

            //string msg = Encoding.UTF8.GetString(buffer, 0, len);
            //_msgQueue.Enqueue($"收到服务器:{msg}");
        }
        _msgQueue.Enqueue("服务器断开连接");
    }

    //public void Send(string msg)
    //{
    //    if(socket == null || !socket.Connected)
    //    {
    //        _msgQueue.Enqueue("未连接到服务器");
    //        return;
    //    }
    //    Task.Run(async () =>
    //    {
    //        byte[] data = Encoding.UTF8.GetBytes(msg);
    //        await socket.SendAsync(data, SocketFlags.None);
    //    });
    //}

    public void Send(int type,byte[] bytedata)
    {
        if (socket == null || !socket.Connected)
        {
            _msgQueue.Enqueue("未连接到服务器");
            return;
        }
        byte[] buffer;
        if (bytedata == null)
        {
            buffer = new byte[4 + 4];
            BitConverter.GetBytes(4 + 4).CopyTo(buffer, 0);
            BitConverter.GetBytes(type).CopyTo(buffer, 4);
            Task.Run(async () =>
            {
                await Task.Factory.FromAsync<int>(
                    socket.BeginSend(buffer, 0, 4 + 4, SocketFlags.None, null, null),
                    socket.EndSend
                    );
            });
            return;
        }


        buffer = new byte[4+4+bytedata.Length];

        BitConverter.GetBytes(4+4+bytedata.Length).CopyTo(buffer, 0);
        BitConverter.GetBytes(type).CopyTo(buffer, 4);

        bytedata.CopyTo(buffer, 8);

        Task.Run(async () =>
        {
            await Task.Factory.FromAsync<int>(
                socket.BeginSend(buffer, 0, 4+4 + bytedata.Length, SocketFlags.None, null, null),
                socket.EndSend
                );
        });
    }

    public async Task SendLogin_Register(AcountData acountData,int type)
    {
        byte[] buffer = new byte[1024];
        int index = 4;
        if (type > 1) return;
        //数据类型
        BitConverter.GetBytes(type).CopyTo(buffer, index);
        index += sizeof(int);
        //账号长度+账号
        byte[] accountBytes = Encoding.UTF8.GetBytes(acountData.acountName);
        BitConverter.GetBytes(accountBytes.Length).CopyTo(buffer, index);
        index += sizeof(int);

        accountBytes.CopyTo(buffer, index);
        index += accountBytes.Length;

        //密码长度+密码
        byte[] pwdBytes = Encoding.UTF8.GetBytes(acountData.password);
        BitConverter.GetBytes(pwdBytes.Length).CopyTo(buffer, index);
        index += sizeof(int);
        pwdBytes.CopyTo(buffer, index);
        index += pwdBytes.Length;

        //4存储总长度+4类型+4存储账号长度+账号长度+4存储密码长度+密码长度
        BitConverter.GetBytes(4+4 +4+accountBytes.Length+4+pwdBytes.Length).CopyTo(buffer, 0);

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

    //用于在其他脚本中检查socket是否连接
    public bool Check()
    {
        return socket != null && socket.Connected;
    }

    public  async Task ProcessMessages(Socket socket, int index , byte[] buffer)
    {
        int type = BitConverter.ToInt32(buffer, index);
        MsgType msgType = (MsgType)type;
        index += 4;
        byte[] response;
        switch (msgType)
        {
            case MsgType.Login:
                if (BitConverter.ToInt32(buffer, index) == 1)
                {
                    _msgQueue.Enqueue("登录成功");
                    //登录成功则字节数组必定还有消息（PlayerData数据）
                    GameDataMgr.Instance.playerData.UpdataPlayerData(buffer);
                }
                else
                {
                    _msgQueue.Enqueue("登录失败");
                }
                break;
            case MsgType.Register:
                if (BitConverter.ToInt32(buffer, index) == 1)
                {
                    _msgQueue.Enqueue("注册成功");
                }
                else
                {
                    _msgQueue.Enqueue("注册失败");
                }
                break;
            case MsgType.Atk:
                {
                    //print("收到攻击消息");
                    damage = BitConverter.ToInt32(buffer, index);
                    //print("伤害值: " + damage);
                    index += 4;
                    enemyIndex = BitConverter.ToInt32(buffer, index);
                    _msgQueue.Enqueue($"攻击消息");

                }
                break;
            case MsgType.Hurt://单人模式受伤
                {
                    int hp = BitConverter.ToInt32(buffer, index);index += 4;
                    int dead = BitConverter.ToInt32(buffer, index);index += 4;

                    _msgQueue.Enqueue($"单人受伤命中|{dead}|{hp}");
                }
                break;
            case MsgType.VisitorHurt://多人联机模式受伤
                {
                    int hp = BitConverter.ToInt32(buffer, index); index += 4;
                    int dead = BitConverter.ToInt32(buffer, index); index += 4;
                    int nameLen = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, nameLen); index += nameLen;

                    _msgQueue.Enqueue($"联机受伤命中|{dead}|{hp}|{name}");
                }
                break;
            case MsgType.VisitorAtk:
                {
                    int monsterid = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int damage = BitConverter.ToInt32(buffer, index);index += 4;
                    int nameLen = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, nameLen); index += nameLen;
                    

                    _msgQueue.Enqueue($"联机攻击命中|{monsterid}|{damage}|{name}");
                }
                break;
            case MsgType.MultiSync:
                {
                    //print("收到多人同步消息");
                    int roomid = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    float x = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float y = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float z = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float yrot = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    int len = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    string acountName = Encoding.UTF8.GetString(buffer, index, len);
                    if (SyncMgr.Instance.visitors.ContainsKey(acountName))
                    {
                        _msgQueue.Enqueue($"同步玩家位置|{acountName}|{x}|{y}|{z}|{yrot}");
                    }
                    else
                    {
                        //print("创建其他玩家");
                        _msgQueue.Enqueue($"创建其他玩家|{acountName}|{x}|{y}|{z}|{yrot}");
                    }
                }
                break;
            case MsgType.Multiplayer:
                {
                    //print("收到联机房间消息");
                    int result = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int roomid = BitConverter.ToInt32(buffer, index);
                    if(result== 0)
                    {
                        _msgQueue.Enqueue($"创建房间|{roomid}");
                    }
                    else if(result== 1)
                    {
                        _msgQueue.Enqueue($"加入房间成功|{roomid}" );
                    }
                }
                break;
            case MsgType.MonsterSync:
                {
                    int roomid = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int monsterCount = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    for(int i = 0; i < monsterCount; i++)
                    {
                        int monsterId = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        int monsterType = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        float x = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        float y = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        float z = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        float yaw = BitConverter.ToSingle(buffer, index);
                        index += 4;
                        int hp = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        int state = BitConverter.ToInt32(buffer, index);
                        index += 4;
                        _msgQueue.Enqueue($"怪物位置同步|{monsterId}|{monsterType}|{x}|{y}|{z}|{yaw}|{hp}|{state}");
                    }
                    _msgQueue.Enqueue($"怪物快照结束");
                }
                break;
            case MsgType.Possess:
                {
                    int msgStart = index-8;
                    int allLen = BitConverter.ToInt32(buffer, msgStart);
                    int roomid = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int strlen = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, strlen);
                    index += strlen;
                    int id = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int flag = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    _msgQueue.Enqueue($"附身请求|{roomid}|{strlen}|{name}|{id}|{flag}|{allLen}|{msgStart}");

                }
                break;
            case MsgType.PossessOK:
                {
                    int roomid = BitConverter.ToInt32(buffer, index); index += 4;
                    int strlen = BitConverter.ToInt32(buffer, index); index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, strlen); index += strlen;
                    int id = BitConverter.ToInt32(buffer, index); index += 4;
                    int flag = BitConverter.ToInt32(buffer, index); index += 4;
                    int result = BitConverter.ToInt32(buffer, index); index += 4;
                    if(flag == 1)
                    {
                        if (result == 1)
                        {
                            _msgQueue.Enqueue($"成功附身|{id}");

                        }
                        else
                        {
                            Debug.Log("附身失败：已被他人附身"); /* 本地弹回 */
                            _msgQueue.Enqueue("附身失败");
                        }
                    }
                    else
                    {
                        if (result == 1)
                        {
                            Debug.Log("解除成功");
                        }
                        else
                        {
                            Debug.Log("解除失败");
                        }
                    }

                }
                break;
            case MsgType.PossessState:
                {
                    int roomid = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int monsterid = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    float x = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float y = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float z = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float yaw = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    _msgQueue.Enqueue($"附身怪物同步|{monsterid}|{x}|{y}|{z}|{yaw}");
                }
                break;

            case MsgType.PlayerAnimation:
                {
                    int animationId = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int combo = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int len = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index,len);

                    _msgQueue.Enqueue($"同步玩家动画|{animationId}|{combo}|{name}");
                }
                break;
            case MsgType.MonsterAnimation:
                {
                    int animationId = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int combo = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    float posX = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float posY = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float posZ = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    float yaw = BitConverter.ToSingle(buffer, index);
                    index += 4;
                    int monsterid = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    int len = BitConverter.ToInt32(buffer, index);
                    index += 4;
                    string name = Encoding.UTF8.GetString(buffer, index, len);

                    _msgQueue.Enqueue($"同步怪物动画|{animationId}|{combo}|{monsterid}|{posX}|{posY}|{posZ}|{yaw}|{name}");
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
        if(!MonsterSyncMgr.Instance.monsters.TryGetValue(monsterId,out var m))yield break;
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

    public void SendPlayerAnimation(int animationId,int combo)
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


/*type
 *0登录消息
 *1注册消息
 *2更新消息
 *7心跳消息
 */
