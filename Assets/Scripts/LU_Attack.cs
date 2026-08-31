using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LU_Attack : MonoBehaviour
{

    public float attackDelataTime = 0.25f; // 攻击间隔时间

    public float attackTime;

    public int ComboStep = 0;

    public bool hasComboArea = false;
    public bool isAttacking = false;

    public float UltimateEnergy = 0f;

    public GameObject VFXAttack;
    public GameObject VFXAttackAround;

    public Transform SpawnAttack1;
    public Transform SpawnAttack2_1;
    public Transform SpawnAttack2_2;

    public bool lastIsAttacking = false;
    Animator animator;

    
    private Coroutine comboCoroutine;
    void Start()
    {
        animator = GetComponent<Animator>();
        if(animator == null)
        {
            animator =gameObject.AddComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameDataMgr.Instance.borned)
        {
            return;
        }

        print(isAttacking);

        if (GameDataMgr.Instance.IsCurrentControl(gameObject) == false)
        {
            return;
        }

        if (isAttacking != lastIsAttacking)
        {
            lastIsAttacking=isAttacking;

            animator.SetBool("IsAttacking", isAttacking);
        }

        ShowSkill();
        if (Input.GetMouseButtonDown(1))
        {
            animator.SetBool("Dodge", true);
            ClearAttackState();
            if (comboCoroutine != null) { StopCoroutine(comboCoroutine); comboCoroutine = null; }
            hasComboArea = false;
            isAttacking = false;
            GameDataMgr.Instance.playerIsAtking = false;
            ComboStep = 0;
            animator.SetInteger("ComboStep", 0);
            DisableTrigger();
        }
        else
        {
            animator.SetBool("Dodge", false);
        }

        if (!hasComboArea)
        {
            ComboStep = 0;
            if (Input.GetMouseButtonDown(0))
            {
                attackTime = Time.time;
                //if (ComboStep >= 4)
                //    ComboStep = 1;
                //else
                //    ComboStep++;
                animator.SetInteger("ComboStep", ComboStep);
                isAttacking = true;
                GameDataMgr.Instance.playerIsAtking = isAttacking;
                animator.SetTrigger("CanAttack");
                SocketMgr.Instance.SendPlayerAnimation(0, ComboStep);


                // ResetState();
                comboCoroutine = StartCoroutine(AttackComboArea());
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if(UltimateEnergy >= 100f)
            {
                animator.SetTrigger("Ultimate");
                isAttacking = true;
                GameDataMgr.Instance.playerIsAtking = isAttacking;
                UltimateEnergy = 0f;
            }
        }
    }

    //连接协程
    IEnumerator AttackComboArea()
    {
        hasComboArea = true;
        float time = Time.time;
        while (true)
        {
            if(time - attackTime > attackDelataTime && time - attackTime < attackDelataTime + 2f)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    //ResetState();
                    AtkTrigger atkTrigger = triggerCollider.gameObject.GetComponentInChildren<AtkTrigger>();
                    atkTrigger.ClearHitList();
                    attackTime = Time.time;
                    if (ComboStep >= 5)
                        ComboStep = 0;
                    else
                        ComboStep++;
                    animator.SetInteger("ComboStep", ComboStep);
                    isAttacking = true;
                    GameDataMgr.Instance.playerIsAtking = isAttacking;
                    animator.SetTrigger("CanAttack");
                    SocketMgr.Instance.SendPlayerAnimation(0, ComboStep);
                }
            }
            if(time - attackTime > attackDelataTime + 2f)
            {
                break;
            }
            time = Time.time;
            yield return null;
        }
        hasComboArea = false;
        ClearAttackState();
        //isAttacking = false;
        //animator.SetFloat("Speed", 0f);
        animator.SetInteger("ComboStep", 0);
    }

    public void ClearAttackState()
    {
        isAttacking = false;
        GameDataMgr.Instance.playerIsAtking = isAttacking;
        if(hasComboArea)
        {
            ComboStep = -1;
        }
        else
            ComboStep = 0;
        //animator.SetFloat("Speed", 0f);
    }

    //攻击特效
    public void Spawn(int num)
    {
        GameObject vfxobj = null;
        switch (num)
        {
            case 1:
                vfxobj = Instantiate(VFXAttack, SpawnAttack1.transform.position, SpawnAttack1.transform.rotation);
                break;
            case 2:
                vfxobj = Instantiate(VFXAttack, SpawnAttack1.transform.position, SpawnAttack2_1.transform.rotation);
                break;
            case 3:
                vfxobj = Instantiate(VFXAttack, SpawnAttack1.transform.position, SpawnAttack2_2.transform.rotation);
                break;
        }
        if(vfxobj != null)
            Destroy(vfxobj, 2f);
    }

    //环绕攻击特效
    public void SpawnAround(int num)
    {
        GameObject vfxobj = null;
        switch (num)
        {
            case 1:
                vfxobj = Instantiate(VFXAttackAround, SpawnAttack1.transform.position, SpawnAttack1.transform.rotation);
                break;
            case 2:
                vfxobj = Instantiate(VFXAttackAround, SpawnAttack1.transform.position, SpawnAttack2_1.transform.rotation);
                break;
            case 3:
                vfxobj = Instantiate(VFXAttackAround, SpawnAttack1.transform.position, SpawnAttack2_2.transform.rotation);
                break;
        }
        if (vfxobj != null)
            Destroy(vfxobj, 2f);
    }

    public void ClearSkillState()
    {
        animator.SetInteger("Skill", 0);

    }

    //攻击,闪避时重置一些状态（速度等）
    public void ResetState()
    {
        //animator.SetFloat("Speed", 0f);
        animator.SetBool("Dodge", false);
        ClearAttackState();
    }

    public void ShowSkill()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            animator.SetInteger("Skill", 1);
            return;
            //animator.SetInteger("Skill", 0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            animator.SetInteger("Skill", 2);
            return;
            //animator.SetInteger("Skill", 0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            animator.SetInteger("Skill", 3);
            return;
            //animator.SetInteger("Skill", 0);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            animator.SetTrigger("Ultimate");
            return;
        }
    }

    public Collider triggerCollider;
    public void EnableTrigger()
    {
        AudioManager.Instance.PlayAttack(transform.position);
        AtkTrigger atkTrigger = triggerCollider.gameObject.GetComponentInChildren<AtkTrigger>();
        atkTrigger.ClearHitList();
        atkTrigger.hitedTime = Time.time;
        if (triggerCollider != null)
            triggerCollider.enabled = true;
        Debug.Log("AtkTrigger: 开启碰撞体，清空命中列表");
    }

    /// <summary>
    /// 由 Animation Event 调用：攻击帧结束时关闭碰撞体
    /// </summary>
    public void DisableTrigger()
    {
        if (triggerCollider != null)
            triggerCollider.enabled = false;
        Debug.Log("AtkTrigger: 关闭碰撞体");
    }

    public void hurtingStart()
    {
        GameDataMgr.Instance.playerHurting = true;
    }
    public void hurtingEnd()
    {
        GameDataMgr.Instance.playerHurting = false;
    }
}
