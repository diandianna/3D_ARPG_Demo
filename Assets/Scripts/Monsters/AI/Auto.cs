using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Auto : MonoBehaviour
{
    public MonsterData monsterData;
    public Transform targetPos;
    public NavMeshAgent agent;
    public Animator animator;

    public Image hpImg;
    public GameObject hpbkImg;

    public float hurtDeltaTime = 0.3f;
    public float hurtTime;

    public float atkDeltatime = 0.3f;
    public bool skilled = false;
    public float randSkillTime = 0;
    public float randLorRMove = 0;
    public float skillt = 0;
    public float atkt = 0;
    public int AtkCount = -1;

    public Transform hateTarget;   // 仇恨目标，优先级高于 targetPos（被附身怪打中后转移过来）
    public Transform curTarget;   // 本帧真正追的目标

    public List<GameObject> hurtobj = new List<GameObject>();

    public virtual void Awake()
    {
        monsterData = gameObject.AddComponent<MonsterData>();
        GameDataMgr.Instance.monsters.Add(gameObject);

    }
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.acceleration = 10f;
        randSkillTime = Random.Range(2, 7.8f);
        skillt=Time.time;
        atkt = Time.time;
        hurtTime = Time.time;
        randLorRMove = Random.Range(-1,1);


    }

    protected virtual void Update()
    {
        if (SyncMgr.Instance.isRoom && !SyncMgr.Instance.isHost)
            return;

        if (!BulletTimeMgr.Instance.sphereIsMoving)
        {
            hpbkImg.gameObject.transform.LookAt(Camera.main.transform.position);
        }

        if (GameDataMgr.Instance.IsCurrentControl(gameObject)||GameDataMgr.Instance.possessedMonsters.Contains(gameObject))
        {
            //停止nav自动寻路
            agent.isStopped = true;
            animator.SetInteger("LorRMove", 0);
            return;
        }
        else agent.isStopped = false;

        if (monsterData.hp <= 0) return;

        hpImg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1.5f * monsterData.hp / monsterData.maxhp);

        curTarget = hateTarget != null ? hateTarget : targetPos;
        if (curTarget == null)
        {
            OnIdle();
            return;
        }
        if (Vector3.Distance(transform.position, curTarget.position) > 2.8f){
            OnChase();
            transform.LookAt(curTarget);
            return;
        }
        else OnAttack();



    }

    //private void OnTriggerStay(Collider other)
    //{
    //    if(other.CompareTag("Player"))
    //    {
    //        Debug.Log("发现玩家");
    //        targetPos = other.gameObject.transform;
    //    }
    //}

    public virtual void OnIdle()
    {
        if (AtkCount != -1)                                    // 之前正在攻击
            SocketMgr.Instance.SendMonsterAnimation(2, -1, monsterData.monsterid, transform.position, transform.eulerAngles.y);  // 停止信号
    }

    public virtual void OnChase()
    {
        if (AtkCount != -1)                                    // 之前正在攻击
            SocketMgr.Instance.SendMonsterAnimation(2, -1, monsterData.monsterid, transform.position, transform.eulerAngles.y);  // 停止信号
        AtkCount = 0;
       agent.speed = 20f;
       animator.SetFloat("Speed", 1);
       agent.SetDestination(curTarget.position);
       animator.SetInteger("LorRMove", 0);
    }

    public virtual void OnAttack()
    {
        if (skilled) { randSkillTime = Random.Range(8, 21.8f); }
        if (Time.time - skillt > randSkillTime)
        {
            print("释放技能");
            skillt = Time.time;
            if (!GameDataMgr.Instance.monsterIsAtking.Contains(gameObject))
                GameDataMgr.Instance.monsterIsAtking.Add(gameObject);
        }

        if (Vector3.Distance(transform.position, curTarget.position) < 2.8f)
        {
            randLorRMove = Random.Range(-1, 1);
            if (randLorRMove > 0)
            {
                if (AtkCount == -1)
                    transform.Translate(Vector3.right * Time.deltaTime * 2f);
                animator.SetInteger("LorRMove", 1);
                if (GameDataMgr.Instance.monsterIsAtking.Contains(gameObject))
                    GameDataMgr.Instance.monsterIsAtking.Remove(gameObject);
            }
            else if (randLorRMove < 0)
            {
                if (AtkCount == -1)
                    transform.Translate(Vector3.right * Time.deltaTime * -2f);
                animator.SetInteger("LorRMove", -1);
                if (GameDataMgr.Instance.monsterIsAtking.Contains(gameObject))
                    GameDataMgr.Instance.monsterIsAtking.Remove(gameObject);
            }


            if (Time.time - atkt > atkDeltatime)
            {
                animator.SetInteger("ComboStep", ++AtkCount);
                animator.SetTrigger("CanAttack");
                SocketMgr.Instance.SendMonsterAnimation(0, AtkCount,monsterData.monsterid, transform.position, transform.eulerAngles.y);
                atkt = Time.time;
                atkDeltatime = Random.Range(0.5f, 1.8f);
            }

            agent.speed = 0f;
            animator.SetFloat("Speed", 0);


            if (AtkCount >= 6) AtkCount = -1;
        }
        

    }
    public virtual void OnHurt()
    {
        AudioManager.Instance.PlayHurt(transform.position);
        print("敌人受伤");
        animator.SetTrigger("Hurt");
        SocketMgr.Instance.Send(5, BitConverter.GetBytes(GameDataMgr.Instance.monsters.IndexOf(gameObject)));
    }

    public virtual void OnDeath()
    {
        AudioManager.Instance.PlayDeath(transform.position);
        print("敌人死亡");
        animator.SetBool("Death", true);

        StartCoroutine(ClearObj(3f));
    }

    public virtual void OnSkill()
    {

    }

    public IEnumerator ClearObj(float time)
    {

        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

    public void MonsterAttack_Start()
    {
        if (!GameDataMgr.Instance.monsterIsAtking.Contains(gameObject))
            GameDataMgr.Instance.monsterIsAtking.Add(gameObject);
    }
    public void MonsterAttack_End()
    {
       if(GameDataMgr.Instance.monsterIsAtking.Contains(gameObject))
            GameDataMgr.Instance.monsterIsAtking.Remove(gameObject);
    }
}
