using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;
public class BossAuto :Auto
{
    //1-7
    //8
    //9-18
    //18-19 18-20 18-21


    public BossData bossData;


    public float moveDeltaTime = 6f;
    public float moveTime = 0f;
    public float randComboTime = 5;
    public int combo = 0;
    public int lastCombo = 0;

    public bool isAttacking = false;

    int moveX;
    int moveY;
    int lastSyncMoveX = -999;   // 上次同步给访客的移动方向（-999 强制首帧同步）
    int lastSyncMoveY = -999;

    // ==== 冲刺攻击 BoxCast 检测 ====
    public Vector3 dashBoxHalfExtents = new Vector3(0.8f, 1.2f, 2f);   // 攻击盒半尺寸（宽、高、冲刺方向长），Inspector 可调
    public float dashCastExtra = 3f;                                   // 每帧位移之外多往前扫的长度，防超快穿透

    private bool dashActive = false;                                   // 当前是否在冲刺攻击
    private HashSet<GameObject> dashHitSet = new HashSet<GameObject>(); // 本次冲刺已命中的目标


    public override void Awake()
    {
        bossData = gameObject.AddComponent<BossData>();
        GameDataMgr.Instance.monsters.Add(gameObject);
        monsterData = bossData;
    }
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
       // agent.acceleration = 10f;
        atkt = Time.time;
        hurtTime = Time.time;

        moveTime = Time.time;
    }

    protected override void Update()
    {
        if (SyncMgr.Instance.isRoom && !SyncMgr.Instance.isHost)
            return;

        if (!BulletTimeMgr.Instance.sphereIsMoving)
        {
            hpbkImg.gameObject.transform.LookAt(Camera.main.transform.position);
        }

        if (GameDataMgr.Instance.IsCurrentControl(gameObject) || GameDataMgr.Instance.possessedMonsters.Contains(gameObject))
        {
            //停止nav自动寻路
            //agent.isStopped = true;
            return;
        }
        else //agent.isStopped = false;

        if (bossData.hp <= 0) return;

        hpImg.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1.5f * bossData.hp / bossData.maxhp);

        curTarget = hateTarget != null ? hateTarget : targetPos;
        if (curTarget == null)
        {
            OnIdle();
            return;
        }
        if (Vector3.Distance(transform.position, curTarget.position) > 3f && !isAttacking)
        {
            OnChase();
            transform.LookAt(curTarget);
        }

        OnAttack();
    }

    public override void OnIdle()
    {
        // 不发"停止信号"(ComboStep=-1)。攻击动画由 exit-time 自然结束，不需要停信号复位。
        // 停信号会在攻击消息后的下一帧把访客端 ComboStep 覆盖成 -1，导致 Atk Layer 内部
        // ComboStep==N 路由永远匹配不上，访客端卡在空的 Level1 状态、看不到攻击动画。
        // 房主端本来就不吃这个停信号（OnChase/OnIdle 只发给访客），去掉后访客端与房主端行为对齐。
    }
    public override void OnChase()
    {
        // 同上：不发停止信号，避免下一帧覆盖访客端刚收到的攻击 ComboStep。
        combo = 0;
        //agent.speed = 3f;
        //agent.SetDestination(curTarget.position);
        transform.LookAt(curTarget);
        if(Time.time - moveTime> moveDeltaTime)
        {
           moveX = Random.Range(-1, 2);
           moveY = Random.Range(-1, 2);
           moveTime = Time.time;
           moveDeltaTime=Random.Range(3, 8);
        }

        if (Vector3.Distance(transform.position, curTarget.position) < 5f) moveY = -1;
        animator.SetFloat("SpeedX", moveX);
        animator.SetFloat("SpeedY", moveY);

        // 移动方向变化时同步给访客（SpeedX/SpeedY 二维混合树，moveX/moveY ∈ -1/0/1）
        if (moveX != lastSyncMoveX || moveY != lastSyncMoveY)
        {
            SocketMgr.Instance.SendMonsterAnimation(4, (moveX + 1) * 3 + (moveY + 1), bossData.monsterid, transform.position, transform.eulerAngles.y);
            lastSyncMoveX = moveX;
            lastSyncMoveY = moveY;
        }
    }
    public override void OnAttack()
    {
        if (Vector3.Distance(transform.position, curTarget.position) < 15f)
        {
            if (bossData.Phase == 1) combo = Random.Range(1, 8);
            if(bossData.Phase == 2) combo = Random.Range(9, 19);

            if (Time.time - atkt > randComboTime)
            {
                animator.SetInteger("ComboStep", combo);
                animator.SetTrigger("CanAttack");
                animator.SetFloat("SpeedX", 0);
                animator.SetFloat("SpeedY", 0);
                lastSyncMoveX = -999;  // 攻击时移动归零，攻击结束后强制重发移动方向
                lastSyncMoveY = -999;
                SocketMgr.Instance.SendMonsterAnimation(0, combo, bossData.monsterid, transform.position, transform.eulerAngles.y);
                Debug.Log($"[Boss房主] 发攻击动画 combo={combo} monsterid={bossData.monsterid}");
                atkt = Time.time;
                randComboTime = Random.Range(8f, 15f);
            }

            //agent.speed = 0f;
        }
        else if(Vector3.Distance(transform.position, curTarget.position) >= 15f)
        {
            if (Time.time - atkt > randComboTime)
            {
                combo = Random.Range(2, 4);
                animator.SetInteger("ComboStep", combo);
                animator.SetTrigger("CanAttack");
                animator.SetFloat("SpeedX", 0);
                animator.SetFloat("SpeedY", 0);
                lastSyncMoveX = -999;  // 攻击时移动归零，攻击结束后强制重发移动方向
                lastSyncMoveY = -999;
                SocketMgr.Instance.SendMonsterAnimation(0, combo, bossData.monsterid, transform.position, transform.eulerAngles.y);
                Debug.Log($"[Boss房主] 发攻击动画 combo={combo} monsterid={bossData.monsterid}");
                atkt = Time.time;
                randComboTime = Random.Range(8f, 15f);
            }
        }
    }
    public override void OnHurt()
    {
        print("敌人受伤");
        animator.SetTrigger("Hurt");
        SocketMgr.Instance.Send(5, BitConverter.GetBytes(GameDataMgr.Instance.monsters.IndexOf(gameObject)));
    }

    public override void OnDeath()
    {
        print("敌人死亡");
        animator.SetBool("Death", true);

        StartCoroutine(ClearObj(3f));
    }


    private void OnAnimatorMove()
    {
        Vector3 prevPos = transform.position;
        transform.position += animator.deltaPosition;
        transform.rotation *= animator.deltaRotation;

        if (dashActive)
            DashHitCheck(prevPos, transform.position);
    }

    public void MarkAttackingState()
    {
        isAttacking = true;
        if (!GameDataMgr.Instance.monsterIsAtking.Contains(gameObject))
            GameDataMgr.Instance.monsterIsAtking.Add(gameObject);
    }
    public void MarkAttackingStateEnd()
    {
        isAttacking = false;
        if (GameDataMgr.Instance.monsterIsAtking.Contains(gameObject))
            GameDataMgr.Instance.monsterIsAtking.Remove(gameObject);
    }

    // ==== 冲刺攻击：BoxCast 检测（防高速穿透）====

    // 冲刺动画开始：由动画事件调用
    public void MarkDashStart()
    {
        dashActive = true;
        dashHitSet.Clear();
    }

    // 冲刺动画结束：由动画事件调用
    public void MarkDashEnd()
    {
        dashActive = false;
        dashHitSet.Clear();
    }

    // 从上一帧位置扫到当前帧位置，检测路径上的玩家
    private void DashHitCheck(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist < 0.001f) return;    // 这一帧没移动就不查
        dir /= dist;

        RaycastHit[] hits = Physics.BoxCastAll(
            from,
            dashBoxHalfExtents,
            dir,
            Quaternion.LookRotation(dir),
            dist + dashCastExtra,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide   // 玩家身上是触发器，强制命中触发器
        );

        foreach (RaycastHit hit in hits)
        {
            GameObject obj = hit.collider.gameObject;
            if (obj.CompareTag("Player") && !dashHitSet.Contains(obj))
            {
                dashHitSet.Add(obj);
                OnDashHitPlayer(obj);
            }
        }
    }

    // 冲刺命中玩家：播受击 + 算伤害 + 发包（逻辑与 AtkTrigger 怪物→玩家一致）
    private void OnDashHitPlayer(GameObject player)
    {
        Animator playerAnim = player.GetComponent<Animator>();
        if (playerAnim != null) playerAnim.SetTrigger("Hurt");

        int reallyDamage = bossData.atkNum -
            (GameDataMgr.Instance.mainCharacter == player ? GameDataMgr.Instance.playerData.DefNum : 0);

        if (SyncMgr.Instance.visitors.ContainsValue(player))
        {
            // 联机：发 VisitorHurt(96)
            string name = null;
            foreach (var v in SyncMgr.Instance.visitors)
            {
                if (v.Value == player) { name = v.Key; break; }
            }
            if (name == null) return;

            byte[] namebyte = Encoding.UTF8.GetBytes(name);
            byte[] response = new byte[4 + 4 + 4 + namebyte.Length];
            BitConverter.GetBytes(SyncMgr.Instance.roomId).CopyTo(response, 0);
            BitConverter.GetBytes(reallyDamage).CopyTo(response, 4);
            BitConverter.GetBytes(namebyte.Length).CopyTo(response, 8);
            namebyte.CopyTo(response, 12);
            SocketMgr.Instance.Send(96, response);
        }
        else
        {
            // 单人：发 Hurt(95)
            byte[] response = new byte[4];
            BitConverter.GetBytes(reallyDamage).CopyTo(response, 0);
            SocketMgr.Instance.Send(95, response);
        }
    }

    public void ChangePhase()
    {
        animator.SetBool("Special", true);
    }
}
