using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterData : MonoBehaviour
{
    public int hp=100;
    public int maxhp=100;
    public int atkNum=20;
    public int monsterid = 0;
    public DeathLog deathLog = new DeathLog();
    public string monsterType = "Normal";
    public int monsterTypeid = 1;
    void Start()
    {
        deathLog.Init(DeathLogMgr.Instance.GetNextMonsterId(), monsterType);
        if (SyncMgr.Instance.isRoom && !SyncMgr.Instance.isHost) return;
        monsterid = GameDataMgr.Instance.nextMonsterid++;
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void DamageTaken(int damage)
    {
        if (hp <= 0) return;
        ChangeHp(-damage);
        GetComponent<Animator>().SetTrigger("Hurt");                          // 房主本地播         
        if (GameDataMgr.Instance.isPossession && GameDataMgr.Instance.possessionCharacter == gameObject)
        {
            PossessionCosts.Instance.AddOverDamage(damage * 0.3f);
        }

        deathLog.damageTaken += damage;
        if(hp <= 0)
        {
            //StartCoroutine(BulletTimeMgr.Instance.Slow_Motion(0.3f, 1.5f));
            if(GameDataMgr.Instance.isPossession&&GameDataMgr.Instance.possessionCharacter == gameObject)
            {
                StartCoroutine(BulletTimeMgr.Instance.BackSphereMove());
                //PossessionCosts.Instance.OnPossessEnd();
            }


            DropResult dropResult = DeathLogMgr.Instance.ProcessMonsterDeath(deathLog);
            gameObject.GetComponent<Auto>().OnDeath();
            print(dropResult.ToString());
            print(deathLog.ToString());
        }
        
    }

    public void ChangeHp(int num)
    {
        this.hp += num;
    }

}
