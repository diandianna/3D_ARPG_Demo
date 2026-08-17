using System.Collections;
using System.Collections.Generic;
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
        if (triggerCollider != null)
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
        if (!GameDataMgr.Instance.playerIsAtking && CompareTag("PlayerAtk")) return;
        if(!GameDataMgr.Instance.monsterIsAtking.ContainsKey(other.gameObject) && CompareTag("MonsterAtk")) return;

        auto = other.GetComponent<Auto>();
        Debug.Log($"auto={(auto != null ? auto.name : "NULL")}, other.tag={other.tag}, myTag={tag}");
        
        if (CompareTag("PlayerAtk")&&other.CompareTag("Monster"))
        {
            if(hitList.Contains(other.gameObject))
            {
                Debug.Log("已经命中过该怪物，忽略");
                return;
            }
            hitList.Add(other.gameObject);
            Debug.Log("PlayerAtk触发了怪物");
            auto.OnHurt();
        }
        else if(CompareTag("MonsterAtk") && other.CompareTag("Player"))
        {
            if (hitList.Contains(other.gameObject))
            {
                Debug.Log("已经命中过该玩家，忽略");
                return;
            }
            hitList.Add(other.gameObject);
            Debug.Log("MonsterAtk触发了玩家");
            GameDataMgr.Instance.playerData.ChangeHp(10);//怪物攻击玩家，扣血10 (暂时写死测试)
        }
    }

    public void ClearHitList()
    {
        hitList.Clear();
    }
}
