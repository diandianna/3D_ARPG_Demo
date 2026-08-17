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
    NavMeshAgent agent;
    public Animator animator;

    public Image hpImg;
    public GameObject hpbkImg;

    public int hp;

    public float hurtDeltaTime = 0.3f;
    public float hurtTime;

    public float atkDeltatime = 0.3f;
    public bool skilled = false;
    public float randSkillTime = 0;
    public float randLorRMove = 0;
    public float skillt = 0;
    public float atkt = 0;
    public int AtkCount = -1;

    public List<GameObject> hurtobj = new List<GameObject>();

    private void Awake()
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

    private void Update()
    {

        hp = monsterData.hp;

        if (SyncMgr.Instance.isRoom && !SyncMgr.Instance.isHost)
            return;

        if (!BulletTimeMgr.Instance.sphereIsMoving)
        {
            hpbkImg.gameObject.transform.LookAt(Camera.main.transform.position);
        }

        if (GameDataMgr.Instance.IsCurrentControl(gameObject))
        {
            //停止nav自动寻路
            agent.isStopped = true;
            animator.SetInteger("LorRMove", 0);
            Debug.Log("当前控制的对象是怪物，AI不执行");
            
            return;
        }
        else agent.isStopped = false;

        if (monsterData.hp <= 0) return;

        hpImg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1.5f * monsterData.hp / monsterData.maxhp);

        if (targetPos == null) { OnIdle(); transform.LookAt(targetPos); return;}
        if (Vector3.Distance(transform.position, targetPos.position) > 2.8f){
            OnChase();
            transform.LookAt(targetPos);
            return;
        }
        OnAttack();



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

    }

    public virtual void OnChase()
    { 
       AtkCount = 0;
       agent.speed = 3f;
       animator.SetFloat("Speed", 1);
       agent.SetDestination(targetPos.position);
        
    }

    public virtual void OnAttack()
    {
        if (skilled) { randSkillTime = Random.Range(8, 21.8f); }
        if (Time.time - skillt > randSkillTime)
        {
            print("释放技能");
            skillt = Time.time;

        }

        if (Vector3.Distance(transform.position, targetPos.position) < 2.8f)
        {
            randLorRMove = Random.Range(-1, 1);
            if (randLorRMove > 0)
            {
                if (AtkCount == -1)
                    transform.Translate(Vector3.right * Time.deltaTime * 2f);
                animator.SetInteger("LorRMove", 1);
            }
            else if (randLorRMove < 0)
            {
                if (AtkCount == -1)
                    transform.Translate(Vector3.right * Time.deltaTime * -2f);
                animator.SetInteger("LorRMove", -1);
            }


            if (Time.time - atkt > atkDeltatime)
            {
                animator.SetInteger("ComboStep", ++AtkCount);
                animator.SetTrigger("CanAttack");
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
        print("敌人受伤");
        animator.SetTrigger("Hurt");
        SocketMgr.Instance.Send(5, BitConverter.GetBytes(GameDataMgr.Instance.monsters.IndexOf(gameObject)));
    }
    public virtual void OnDeath()
    {
        print("敌人死亡");
        animator.SetBool("Death", true);

        StartCoroutine(ClearObj(3f));
    }

    public virtual void OnSkill()
    {

    }
    //private void OnTriggerEnter(Collider other)
    //{
    //    if (monsterData.hp <= 0) return;

    //    if (other.CompareTag("PlayerAtk"))
    //    {
    //        if (Time.time - hurtTime >= hurtDeltaTime)
    //        {
    //            if (hurtobj.Contains(other.gameObject)) return;
    //            hurtTime = Time.time;
    //            hurtobj.Add(other.gameObject);
    //            OnHurt();
    //        }
               
    //    }
    //}

    public IEnumerator ClearObj(float time)
    {

        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
