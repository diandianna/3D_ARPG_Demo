using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritInterceptor : Auto
{
    public float interceptRange = 5f;
    public float buffDuration = 15f;
    float buffEndTime;
    public bool hasBuff;


    protected override void Update()
    {
        // Buff 过期恢复：拦截范围+50% 到期还原（攻速/移速 buff 未接，见 Auto 注释）
        if (hasBuff && Time.time > buffEndTime)
        {
            hasBuff = false;
        }

        if (BulletTimeMgr.Instance.sphereIsMoving && BulletTimeMgr.Instance.currentSphere != null)
        {
            float range = hasBuff ? interceptRange * 1.5f : interceptRange;
            if(Vector3.Distance(transform.position, BulletTimeMgr.Instance.currentSphere.transform.position) < range)
            {
                GrabSpirit();
            }
        }
        base.Update();
    }

    void GrabSpirit()
    {
        BulletTimeMgr.Instance.OnIntercepted();
        GameDataMgr.Instance.soulIntegrity = Mathf.Max(0, GameDataMgr.Instance.soulIntegrity - 30);
        int overDmg = (int)(GameDataMgr.Instance.playerData.MaxHp * 0.5f);
        GameDataMgr.Instance.playerData.ChangeHp(-overDmg);

        hasBuff = true; buffEndTime = Time.time + buffDuration;
    }
}
