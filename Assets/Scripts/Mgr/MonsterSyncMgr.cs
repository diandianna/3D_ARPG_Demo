using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Antlr3.Runtime.Tree.TreeWizard;

public class MonsterSyncMgr : MonoBehaviour
{
    public static MonsterSyncMgr Instance { get; private set; }

    public Dictionary<int, GameObject> monsters = new Dictionary<int, GameObject>();
    Dictionary<int, Vector3> targetPos = new Dictionary<int, Vector3>();
    Dictionary<int, float> targetYaw = new Dictionary<int, float>();
    Dictionary<int, Vector3> lastPos = new Dictionary<int, Vector3>();
    HashSet<int> seeded = new HashSet<int>();
    HashSet<int> dying = new HashSet<int>();
    Dictionary<int ,GameObject> hpObj = new Dictionary<int ,GameObject>();
    Dictionary<int ,Image> hpImg = new Dictionary<int , Image>();
    void Start()
    {
        Instance = this;
    }
    

    void Update()
    {
        float k = 1f - Mathf.Exp(-10f * Time.deltaTime); // 帧率无关的平滑系数，永远不到 1

        foreach (var kv in monsters)
        {
            int id = kv.Key;
            GameObject obj = kv.Value;
            if (dying.Contains(id)) continue;
            if (GameDataMgr.Instance.isPossession && GameDataMgr.Instance.possessionCharacter == obj) continue;
            if(hpObj.TryGetValue(id, out GameObject hpobj))
            {
                hpObj[id].transform.LookAt(Camera.main.transform);
            }

            MonsterData md = obj.GetComponent<MonsterData>();
            bool isBoss = md != null && md.monsterTypeid == 3;

            if (targetPos.TryGetValue(id, out Vector3 tp))
            {
                if (!isBoss)
                {
                    if ((obj.transform.position - tp).sqrMagnitude < 0.035f * 0.035f)
                    {
                        obj.transform.position = tp;
                    }
                    else obj.transform.position = Vector3.Lerp(obj.transform.position, tp, k);
                }
                // Boss：不做任何位置纠正。位置由根运动 + 动画切换点对齐（收 201 消息时
                // 瞬移到房主切动画那一刻的起点）驱动；两端根运动确定，落点天然一致。
                // 快照只兜底 Boss 刚被创建时的初始落点（CreateMonster 用快照位置生成）。
            }

            if (targetYaw.TryGetValue(id, out float yaw))
            {
                if (isBoss)
                {
                    // 方案C：Boss 根运动按"当前朝向"位移，朝向用 Slerp 慢跟会有滞后，
                    // 导致水平漂移/落点偏。Boss 朝向直接对齐房主 yaw，根运动方向才对。
                    if (!monsters[id].GetComponent<BossAnimEventStub>().isAttacking)
                         obj.transform.rotation = Quaternion.Euler(0, yaw, 0);
                }
                else
                {
                    obj.transform.rotation = Quaternion.Slerp(obj.transform.rotation, Quaternion.Euler(0, yaw, 0), k);
                }
            }

            Animator animator = obj.GetComponent<Animator>();
            if (animator == null) continue;

            if (isBoss) continue;  // Boss 的移动/攻击动画由房主同步消息驱动，这里不反推

            float speed = 0;
            if (lastPos.TryGetValue(id, out Vector3 prev))
                speed = (obj.transform.position - prev).magnitude / Time.deltaTime;
            lastPos[id] = obj.transform.position;
            animator.SetFloat("Speed", speed > 0.08f ? 1f : 0f);
        }
    }
    
    public void OnRecvPossessionMonster(int id,Vector3 pos, float yaw)
    {
        targetPos[id] = pos;
        targetYaw[id] = yaw;
    }


    public void OnRecvMonster(int id, int type, Vector3 pos, float yaw, int hp, int state)
    {
        Debug.Log($"[怪物快照] OnRecvMonster id={id} type={type} hp={hp} 当前字典count={monsters.Count}");
        seeded.Add(id);

        if (hp <= 0)
        {
            MarkDead(id);
            return;
        }

        targetPos[id] = pos;
        targetYaw[id] = yaw;
        if(monsters.TryGetValue(id,out GameObject obj) && obj != null)
        {
            MonsterData monster = obj.GetComponent<MonsterData>();
            if (monster != null)
            {
                monster.hp = hp;
                hpImg[id].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1.5f * hp / 100);
            }
            return;
        }
        CreateMonster(id, type, pos, hp, state);
    }

    public void CreateMonster(int id,int type, Vector3 vector3,int hp,int state)
    {
        if (monsters.ContainsKey(id))
        {
            return;
        }
        string monsterType = null;
        GameObject obj = null;
        switch (type)
        {
            case 1:
                {
                    monsterType = "Normal";
                    obj = Resources.Load<GameObject>("Prefabs/Character/LanAI");
                }
                break;
            case 2:
                {
                    monsterType = "Elite";
                    obj = Resources.Load<GameObject>("Prefabs/Character/ChangeYu");
                }
                break;
            case 3:
                {
                    monsterType = "Boss";
                    obj = Resources.Load<GameObject>("Prefabs/Character/Boss");
                }
                break;
        }
        if (obj == null || monsterType == null)
        {
            Debug.LogWarning($"[怪物快照] CreateMonster 加载失败 id={id} type={type}");
            return;
        }
        Debug.Log($"[怪物快照] CreateMonster 创建 id={id} type={type} 预制体={obj.name}");
        GameObject monster = GameObject.Instantiate(obj, vector3, Quaternion.identity);
        monster.transform.SetParent(SocketMgr.Instance.WorldCanvasTrans,true);
        Auto auto = monster.GetComponent<Auto>();
       if(type==3) auto = monster.GetComponent<BossAuto>();

        // Boss 靠 root motion 移动（访客端销毁 BossAuto 后由 Unity 自动应用根运动，
        // 与房主 OnAnimatorMove 等价），位置交给根运动驱动，本类只做大漂移纠正。
        if (type == 3)
        {
            // 访客端 Boss 位置由根运动驱动（与房主一致：攻击/走路的动画自带位移）。
            // 保持根运动开启，只把刚体改成 kinematic，根运动/漂移纠正写 transform.position 时才不会和动态刚体打架。
            Animator bossAnim = monster.GetComponent<Animator>();
            if (bossAnim != null) bossAnim.applyRootMotion = true;

            Rigidbody rb = monster.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        if (auto != null)
        {
            hpImg.Add(id, auto.hpImg);
            hpObj.Add(id, auto.hpbkImg);
            Destroy(auto);

            // Boss 攻击动画带动画事件，BossAuto 销毁后没有接收器会刷 "has no receiver"，
            // 挂一个空壳接收器接住（伤害/冲刺/转阶段由房主权威处理，访客端只播动画）
            if (type == 3)
                monster.AddComponent<BossAnimEventStub>();
        }
        monster.GetComponent<MonsterData>().monsterid = id;
        monster.GetComponent<MonsterData>().monsterType = monsterType;
        monster.GetComponent<MonsterData>().monsterTypeid = type;
        monster.GetComponent<MonsterData>().hp = hp;
        hpImg[id].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1.5f * hp / 100) ;
        monsters.Add(id, monster);

        //monsters[id] = vector3;

        //StartCoroutine(LerpPos(name));
        //StartCoroutine(LerpRot(name));
    }

    public void OnSnapShotEnd()
    {
        List<int> dead = new List<int>();
        foreach (int id in monsters.Keys)
            if(!seeded.Contains(id)) 
                dead.Add(id);
        foreach(int id in dead)
            MarkDead(id);
        seeded.Clear();
    }

    void MarkDead(int id)
    {
        if (dying.Contains(id)) return;
        if (!monsters.TryGetValue(id, out GameObject obj) || obj == null) return;
        dying.Add(id);
        if (obj == GameDataMgr.Instance.possessionCharacter)
            StartCoroutine(BulletTimeMgr.Instance.BackSphereMove());
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null) animator.SetBool("Death", true);
        StartCoroutine(DestroyMonster(id, 3f));
    }

    IEnumerator DestroyMonster(int id, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (monsters.TryGetValue(id, out GameObject obj) && obj != null)
            Destroy(obj);
        monsters.Remove(id);
        targetPos.Remove(id);
        targetYaw.Remove(id);
        lastPos.Remove(id);
        hpImg.Remove(id);
        hpObj.Remove(id);
        dying.Remove(id);
    }
}
