using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class AtkTrigger : MonoBehaviour
{

    //命中列表，防止重复命中
    [SerializeField]
    private List<GameObject> hitList = new List<GameObject>();

    public float hitedTime = -0.5f;
    public float canHitTime = 0.5f;

    private Collider triggerCollider;

    void Start()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null && CompareTag("PlayerAtk"))
            triggerCollider.enabled = false; // 默认关闭，由动画事件开启
    }

    // Update 作为兜底：防止动画事件漏配导致命中列表永不清理
    void Update()
    {
        if (Time.time - hitedTime >= canHitTime)
        {
            hitList.Clear();
            hitedTime = Time.time;
        }
    }

    /// <summary>
    /// 由 Animation Event 调用：攻击帧开始时开启碰撞体，同时清空命中列表
    /// </summary>


    Auto auto;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter触发! other={other.name}, playerIsAtking={GameDataMgr.Instance.playerIsAtking}, hitList.Count={hitList.Count}");
        ////自己是玩家攻击tag但是玩家并没有在攻击
        //if (!GameDataMgr.Instance.playerIsAtking && CompareTag("PlayerAtk")) return;
        if (CompareTag("MonsterAtk"))//怪物攻击
        {
            Auto owner = GetComponentInParent<Auto>();
            //owner不为空（有怪物ai组件）并且玩家处在附身状态 并且附身的角色就是owner的obj则为true
            bool possessed = owner!=null && GameDataMgr.Instance.isPossession && GameDataMgr.Instance.possessionCharacter == owner.gameObject;
            if (possessed )
            {
                if (!GameDataMgr.Instance.playerIsAtking) return;//玩家没有进行攻击
            }
            //没有怪物组件或者已经对该对象攻击过
            else if (owner ==null || !GameDataMgr.Instance.monsterIsAtking.Contains(owner.gameObject)) return;
        }


        auto = other.GetComponentInParent<Auto>();
        Debug.Log($"auto={(auto != null ? auto.name : "NULL")}, other.tag={other.tag}, myTag={tag}");

        Monster_PlayerHits(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // 附身怪打普通怪（先接触后攻击）
        if (CompareTag("MonsterAtk")&&other.CompareTag("Monster")&&
            GameDataMgr.Instance.isPossession&&
            GameDataMgr.Instance.possessionCharacter==gameObject.GetComponentInParent<Auto>().gameObject&&
            GameDataMgr.Instance.playerIsAtking)
        {
            Monster_PlayerHits(other);
            return;
        }
        // 普通怪打附身怪（先接触后攻击）—— 补这条
        if (CompareTag("MonsterAtk") && other.CompareTag("Monster") &&
            GameDataMgr.Instance.isPossession &&
            GameDataMgr.Instance.possessionCharacter == other.transform.GetComponentInParent<Auto>().gameObject &&
            GameDataMgr.Instance.monsterIsAtking.Contains(gameObject.GetComponentInParent<Auto>().gameObject))
        {
            Monster_PlayerHits(other);
            return;
        }
        
        if (!CompareTag("MonsterAtk") || !other.CompareTag("Player")) return;
        if (CompareTag("MonsterAtk"))
        {
            Auto owner = GetComponentInParent<Auto>();
            // 访客端 Boss 的 Auto/BossAuto 已被销毁，攻击判定由房主权威处理，这里直接跳过
            if (owner == null || !GameDataMgr.Instance.monsterIsAtking.Contains(owner.gameObject)) return;
        }

        Monster_PlayerHits(other);
    }

    public void ClearHitList()
    {
        hitList.Clear();
    }

    void Monster_PlayerHits(Collider other)
    {
        Auto hitAuto = other.GetComponentInParent<Auto>();
        if (hitAuto == null)
        {
            hitAuto = other.GetComponentInParent<BossAuto>();
        }
        //玩家攻击怪物
        if (CompareTag("PlayerAtk") && other.CompareTag("Monster"))
        {
            if (hitList.Contains(other.gameObject))
            {
                Debug.Log("已经命中过该怪物，忽略");
                return;
            }
            hitList.Add(other.gameObject);
            Debug.Log("PlayerAtk触发了怪物");
            if (SyncMgr.Instance.isRoom && !SyncMgr.Instance.isHost)
            {
                byte[] namebyte = Encoding.UTF8.GetBytes(GameDataMgr.Instance.acountName);
                byte[] response = new byte[4 + 4 + 4 + namebyte.Length];
                BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(response, 0);
                BitConverter.GetBytes(other.gameObject.GetComponent<MonsterData>().monsterid).CopyTo(response, 4);
                BitConverter.GetBytes(namebyte.Length).CopyTo(response, 8);
                namebyte.CopyTo(response, 12);
                SocketMgr.Instance.Send(97, response);

            }
            else hitAuto.OnHurt();
        }
        //怪物攻击玩家
        else if (CompareTag("MonsterAtk") && other.CompareTag("Player"))
        {
            if (hitList.Contains(other.gameObject))
            {
                Debug.Log("已经命中过该玩家，忽略");
                return;
            }
            hitList.Add(other.gameObject);
            Debug.Log("MonsterAtk触发了玩家");

            other.gameObject.GetComponent<Animator>().SetTrigger("Hurt");
            MonsterData Data = transform.gameObject.GetComponentInParent<MonsterData>();
            int reallyDamage = Data.atkNum-
                (GameDataMgr.Instance.mainCharacter==other.gameObject? GameDataMgr.Instance.playerData.DefNum : 0);//待完善
            print(reallyDamage);
            if (SyncMgr.Instance.isRoom&&SyncMgr.Instance.visitors.ContainsValue(other.gameObject)&&!SyncMgr.Instance.isHost)
            {
                //联机状态发VisitorHurt
                string name = null;
                foreach(var v in SyncMgr.Instance.visitors)
                {
                    if (v.Value == other.gameObject)
                    {
                        name = v.Key;
                        break;
                    }
                }
                if (name == null) return;
                byte[] namebyte = Encoding.UTF8.GetBytes(name);
                byte[] response = new byte[4+4+4+namebyte.Length];
                BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(response, 0);
                BitConverter.GetBytes(reallyDamage).CopyTo(response, 4);
                BitConverter.GetBytes(namebyte.Length).CopyTo(response, 8);
                namebyte.CopyTo(response, 12);
                SocketMgr.Instance.Send(96, response);
            }
            else //单人发Hurt
            {
                byte[] response = new byte[4];
                BitConverter.GetBytes(reallyDamage).CopyTo(response, 0);
                SocketMgr.Instance.Send(95, response);
                //GameDataMgr.Instance.playerData.ChangeHp(-reallyDamage);
            }
        }
        //被附身怪物攻击怪物
        else if(CompareTag("MonsterAtk") && other.CompareTag("Monster") &&
            GameDataMgr.Instance.isPossession && GameDataMgr.Instance.possessionCharacter==transform.GetComponentInParent<Auto>().gameObject)
        {
            if (hitList.Contains(other.gameObject))
            {
                Debug.Log("已经命中过该怪物，忽略");
                return;
            }
            hitList.Add(other.gameObject);
            hitAuto.OnHurt();
            hitAuto.hateTarget = transform.GetComponentInParent<Auto>().transform;
        }
        //怪物攻击被附身怪物
        else if (CompareTag("MonsterAtk") && other.CompareTag("Monster") &&
            GameDataMgr.Instance.isPossession && GameDataMgr.Instance.possessionCharacter == other.transform.GetComponentInParent<Auto>().gameObject)
        {
            if (hitList.Contains(other.gameObject))
            {
                Debug.Log("已经命中过该怪物，忽略");
                return;
            }
            Debug.Log($"普通怪打附身怪生效: {other.name}, atkNum={transform.GetComponentInParent<MonsterData>().atkNum}");
            hitList.Add(other.gameObject);
            hitAuto.OnHurt();
        }
    }
}
