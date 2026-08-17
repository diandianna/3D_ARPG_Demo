using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterData : MonoBehaviour
{
    public int hp=100;
    public int maxhp=100;
    public int atkNum=5;
    public int monsterid = 0;
    public DeathLog deathLog = new DeathLog();
    public string monsterType = "Normal";

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

    public void DamageTaken(int damage)
    {
        ChangeHp(-damage);
        

        deathLog.damageTaken += damage;
        if(hp <= 0)
        {
            StartCoroutine(BulletTimeMgr.Instance.Slow_Motion(0.3f, 1.5f));

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
