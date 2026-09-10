using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossData : MonsterData
{
    [Tooltip("Boss当前阶段")]
    public int Phase = 1;
    [Tooltip("Boss总共阶段数")]
    int maxPhases = 2;
    public bool phaseTransitioning;
    [Header("Boss最大血量")]
    public int MaxHp = 1000;
    private void Awake()
    {
        monsterType = "Boss";
        monsterTypeid = 3;
        maxhp = MaxHp;
    }
    public override void DamageTaken(int damage)
    {
        if (hp <= 0) return;
        ChangeHp(-damage);
        GetComponent<Animator>().SetTrigger("Hurt");                          // 房主本地播         
        if (GameDataMgr.Instance.isPossession && GameDataMgr.Instance.possessionCharacter == gameObject)
        {
            PossessionCosts.Instance.AddOverDamage(damage * 0.3f);
        }

        deathLog.damageTaken += damage;
        if (hp <= 0 && Phase < maxPhases) 
        {
            hp = maxhp;
            Phase++;
            gameObject.GetComponent<BossAuto>().animator.CrossFade("Rage", 0.1f);
            SocketMgr.Instance.SendMonsterAnimation(3, Phase, monsterid, transform.position, transform.eulerAngles.y);  // 同步转阶段给访客
            phaseTransitioning = true;          // 短暂无敌 ~1s
            return;
        }
        else if (hp <= 0)
        {
            DropResult dropResult = DeathLogMgr.Instance.ProcessMonsterDeath(deathLog);
            gameObject.GetComponent<BossAuto>().OnDeath();
            print(dropResult.ToString());
            print(deathLog.ToString());
        }
    }
}
