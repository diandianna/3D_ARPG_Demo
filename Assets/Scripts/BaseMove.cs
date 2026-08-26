using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseMove : MonoBehaviour
{
    public Animator animator;
    public PlayerCamera playerCamera;

    float horizontal=0;
    float vertical=0;

    float stopDelay = 0.1f;
    Coroutine stopCoroutine;

    public float Movespeed;

    public float rotateSpeed;


    void Start()
    {
        animator = GetComponent<Animator>();
        playerCamera = Camera.main.GetComponent<PlayerCamera>();
        StartCoroutine(waitBorn());
    }

    void Update()
    {
        //GameDataMgr.Instance.playerPos = gameObject.transform.position;
        //GameDataMgr.Instance.playerRot = gameObject.transform.eulerAngles;
        if (GameDataMgr.Instance.playerHurting) return;
        if (GameDataMgr.Instance.playerDeath)
        {
            horizontal = 0;
            vertical = 0;
            return;
        }
        if(BulletTimeMgr.Instance.sphereIsMoving)return;
        if (GameDataMgr.Instance.IsCurrentControl(gameObject) == false)return;


        if (Input.GetKeyDown(KeyCode.P))
        {
            Time.timeScale = Time.timeScale <= 0.3 ? 1 : 0.3f;
        }

        // 攻击期间不读输入、不旋转、不设动画参数
        // isAttacking 由 LU_Attack.ClearAttackState()（Animation Event）置 false
        if (!GameDataMgr.Instance.borned || GetComponent<LU_Attack>().isAttacking)
            return;

        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        Rotate();
        SetMoveAnimation();  // 只设 Animator 参数，不平移
    }

    /// <summary>
    /// 在 LateUpdate 中执行平移。
    /// Unity 执行顺序：Update → Animation Update（Animator 处理状态切换）→ LateUpdate
    /// 所以 LateUpdate 里 Animator 已经处理完本帧的 Speed 参数变化，
    /// Base Layer 已经进入或正在过渡到 Run 状态，此时平移不会出现"先滑步后播动画"。
    /// </summary>
    private void LateUpdate()
    {
        if (GameDataMgr.Instance.playerDeath) return;
        if(GameDataMgr.Instance.playerHurting) return;
        if (GameDataMgr.Instance.IsCurrentControl(gameObject) == false)
        {
            return;
        }

        if (!GameDataMgr.Instance.borned ||GetComponent<LU_Attack>().isAttacking)
            return;

        DoTranslate();
    }

    public void SetSpeed(float speed)
    {
        Movespeed = speed;
    }

    public void SetCameraSpeed(float speed)
    {
        playerCamera.rotateSpeed = speed;
    }

    IEnumerator waitBorn()
    {
        yield return new WaitForSeconds(2f);
        GameDataMgr.Instance.borned = true;
    }

    /// <summary>
    /// 只设置 Animator 的 Speed 参数，告诉状态机"该跑步了"或"该待机了"。
    ///
    /// Speed = 1f：Stand→Run（条件 Speed > 0）触发，进入跑步动画；
    ///             Run→Run_End（条件 Speed < 1）不触发，保持跑步不被打断。
    /// Speed = 0f：Run→Run_End（条件 Speed < 1）触发，播完停步动画后回 Stand。
    ///
    /// 真正的位移在 LateUpdate 的 DoTranslate() 中执行。
    /// </summary>
    public void SetMoveAnimation()
    {
        if (GameDataMgr.Instance.playerDeath)
        {
            // 只播死亡动画自带的位移（播完就归零），不再叠加其他
            transform.position += animator.deltaPosition;
            return;
        }
        if (horizontal != 0 || vertical != 0)
        {
            // 1f 满足 Stand→Run（Speed > 0），同时不满足 Run→Run_End（Speed < 1），
            // 角色保持跑步状态不被打断。
            animator.SetFloat("Speed", 1f);

            if(stopCoroutine != null)
            {
                StopCoroutine(stopCoroutine);
                stopCoroutine = null;
            }
        }
        else
        {
            if(stopCoroutine == null)
            {
                stopCoroutine = StartCoroutine("DelayStop");
            }
            
        }
    }

    /// <summary>
    /// 在 Animator 处理完本帧状态之后再平移角色。
    /// 三道闸门防止滑步：
    ///
    /// 闸门1 — Atk Layer（第1层）还有动画 clip 在播？
    ///   ClearAttackState() 把 isAttacking 置 false 之后，Atk Layer 的攻击状态
    ///   可能还在做退出过渡（exit transition），攻击动画 clip 仍在播放。
    ///   此时不平移，等 Atk Layer 彻底回到空状态（New State）再放行。
    ///
    /// 闸门2 — Base Layer（第0层）既不在跑动状态、也不在过渡中？
    ///   说明 Animator 还没开始切跑步动画（Stand→Run 过渡还没启动），
    ///   此时不平移，等过渡开始。
    ///
    /// 闸门3 — 正在 Stand→Run 过渡中？
    ///   过渡混合期间动画还没完全变成跑步，如果满速平移就会出现滑步。
    ///   这里按过渡进度（normalizedTime 0→1）等比缩放移动速度，
    ///   让角色随着动画混合逐渐加速，而非瞬间满速滑出去。
    /// </summary>
    private void DoTranslate()
    {
        // 没输入就不动
        if (horizontal == 0 && vertical == 0)
            return;

        // ====== 闸门1：Atk Layer（第1层）是否已清空？ ======
        // GetCurrentAnimatorClipInfo 返回当前正在播放的动画 clip 列表。
        // 如果 Atk Layer 上还有攻击动画 clip（包括退出过渡期间），Length > 0。
        // 只有攻击动画彻底播完、Atk Layer 回到空 New State 时，Length 才为 0。
        if (animator.GetCurrentAnimatorClipInfo(1).Length > 0)
            return;

        // ====== 闸门2：Base Layer（第0层）是否处于移动相关状态？ ======
        AnimatorStateInfo baseState = animator.GetCurrentAnimatorStateInfo(0);
        bool inRunState = IsRunState(baseState);
        bool inBaseTransition = animator.IsInTransition(0);

        // 既不在跑步/冲刺等移动状态，也不在过渡中 → 动画还没准备好，不平移
        if (!inRunState && !inBaseTransition)
            return;

        // ====== 闸门3：Stand→Run 过渡中，按进度等比缩放移速 ======
        // 过渡期间动画在 Stand 和 Run 之间混合（TransitionDuration 通常 0.25s），
        // 如果满速平移，角色会在这 0.25s 内"滑"出去而动画还没切到跑步。
        // 用过渡进度 normalizedTime（0→1）乘以移速，实现平滑加速。
        if (inBaseTransition && !inRunState)
        {
            AnimatorTransitionInfo trans = animator.GetAnimatorTransitionInfo(0);
            float t = Mathf.Clamp01(trans.normalizedTime);
            transform.position += MoveBlocked(transform.forward * Movespeed * t * Time.deltaTime);
        }
        else
        {
            // 已经在 Run 等移动状态中 → 正常满速平移
            transform.position += MoveBlocked(transform.forward * Movespeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 判断 Base Layer 当前状态是否为"可移动"状态（跑步/冲刺相关）。
    /// 只有这些状态下才允许平移，Stand/Idle 状态下不允许。
    /// </summary>
    private bool IsRunState(AnimatorStateInfo state)
    {
        return state.IsName("Run") || state.IsName("Run_Start") ||
               state.IsName("Sprint") || state.IsName("Sprint_B") ||
               state.IsName("RunLeanL") || state.IsName("RunLeanR");
    }

    /// <summary>
    /// 根据相机方向计算目标朝向并球形插值旋转。
    /// 只在 Update 中执行（不等 LateUpdate），因为旋转纯粹是视觉表现，
    /// 不依赖 Animator 状态切换。
    /// </summary>
    public void Rotate()
    {
        // 死区：摇杆几乎在中心时不旋转，避免微小漂移导致角色抖动
        if (Mathf.Abs(vertical) < 0.1f && Mathf.Abs(horizontal) < 0.1f)
        {
            return;
        }

        // 获取相机的世界空间方向
        Vector3 cameraForword = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // 剔除 Y 轴分量，投影到水平面（XZ 平面）
        // 防止摄像机俯仰角导致角色朝上/朝下旋转
        cameraForword.y = 0;
        cameraRight.y = 0;
        cameraForword.Normalize();
        cameraRight.Normalize();

        // 合成相机相对方向的移动向量
        // vertical   = W/S 或摇杆上下 → 沿相机前方
        // horizontal = A/D 或摇杆左右 → 沿相机右方
        Vector3 moveDir = cameraForword * vertical + cameraRight * horizontal;

        // 方向向量 → 四元数旋转 → Slerp 平滑过渡
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }

    IEnumerator DelayStop()
    {
        yield return new WaitForSeconds(stopDelay);
        
        animator.SetFloat("Speed", 0f);
        StopCoroutine(stopCoroutine);
    }

    Vector3 MoveBlocked(Vector3 delta)
    {
        PushOutOfMonsters();

        if (delta.sqrMagnitude < 0.0001f) return delta;

        Vector3 bottom = transform.position + Vector3.up * 0.2f;
        Vector3 top = transform.position + Vector3.up * 1.4f;
        float step = delta.magnitude;

        if(Physics.CapsuleCast(bottom,top,0.4f,delta.normalized,
            out RaycastHit hit, step+0.2f , ~0, QueryTriggerInteraction.Ignore))
        {
            float allowed = Mathf.Max(0, hit.distance - 0.05f);
            return delta.normalized * Mathf.Min(step, allowed);
        }
        return delta;

    }

    private void OnAnimatorMove()
    {
        if (!GameDataMgr.Instance.IsCurrentControl(gameObject))
        {
            transform.position += animator.deltaPosition;
            transform.rotation *= animator.deltaRotation;
        }
        else
        {
            transform.position += MoveBlocked(animator.deltaPosition);
        }
    }

    void PushOutOfMonsters()
    {
        Vector3 bottom = transform.position + Vector3.up * 0.2f;
        Vector3 top = transform.position + Vector3.up * 1.4f;

        Collider[] cols =Physics.OverlapCapsule(bottom,top,0.4f,~0,QueryTriggerInteraction.Ignore);
        foreach(Collider c in cols)
        {
            if (c.transform == transform) continue;
            if (c.transform.IsChildOf(transform)) continue;
            if (c.isTrigger) continue;

            Vector3 bodyCenter = transform.position + Vector3.up * 0.8f;
            Vector3 closest = c.ClosestPoint(bodyCenter);
            Vector3 pushDir = bodyCenter - closest;
            float distance = pushDir.magnitude;
            if(distance >0.0001f && distance < 0.4f)
            {
                pushDir.y = 0;
                transform.position += pushDir.normalized * (0.4f - distance);
            }
        }
    }
}
