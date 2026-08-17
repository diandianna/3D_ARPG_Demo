using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextPanel : BasePanel
{
    public Button add1;
    public Button add2;
    public Button add3;
    public Button add4;
    public Button sure;

    private void Start()
    {
        add1.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.playerData.atkNum += 20;
        });
        add2.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.playerData.DefNum += 20;
        });
        add3.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.playerData.CritRate += 20;
        });
        add4.onClick.AddListener(() =>
        {
            GameDataMgr.Instance.playerData.CritDamage += 20;
        });
        sure.onClick.AddListener(() =>
        {
            PlayerData t = GameDataMgr.Instance.playerData;
            SocketMgr.Instance.Send(2,t.GetPlayerDataBytes());
        });
    }
}
