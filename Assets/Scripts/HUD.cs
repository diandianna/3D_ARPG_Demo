using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Image hp;
    public Text soulIntegrity;
    public Text sanity;
    public Text BulletSeconds;

    private void Update()
    {
        hp.fillAmount = (GameDataMgr.Instance.playerData.hp+30) / 1.0f /
                        GameDataMgr.Instance.playerData.MaxHp;
        soulIntegrity.text = GameDataMgr.Instance.soulIntegrity.ToString();
        sanity.text = GameDataMgr.Instance.sanity.ToString("F0");
        BulletSeconds.text = BulletTimeMgr.Instance.BulletSeconds.ToString("F1");
    }
}
