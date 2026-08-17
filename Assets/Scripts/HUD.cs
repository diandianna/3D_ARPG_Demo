using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD : MonoBehaviour
{
    public Image hp;
    private void Update()
    {
        hp.fillAmount = (GameDataMgr.Instance.playerData.hp+30) / 1.0f /
                        GameDataMgr.Instance.playerData.MaxHp;

    }
}
