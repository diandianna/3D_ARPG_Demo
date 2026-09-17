using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 养成面板：显示剩余属性点 + 四个属性当前值 + 四个加点按钮。
/// 预制体放 Resources/Prefabs/Panel/GrowthPanel。
/// </summary>
public class GrowthPanel : BasePanel
{
    public Text levelText, expText;            // 等级 / 当前经验(还差多少升级)
    public Text atkText, defText, hpText, critText;   // 攻/防/血/暴击当前值

    private void OnEnable() => Refresh();   // 每次打开面板刷新一次

    public void Refresh()
    {
        PlayerData d = GameDataMgr.Instance.playerData;
        int need = 100 * d.level;   // 和 GrowthMgr.AddExp 的曲线对齐（100*level）
        levelText.text = d.level.ToString();
        expText.text = d.levelNum + " / " + need;
        atkText.text = d.atkNum.ToString();
        defText.text = d.DefNum.ToString();
        hpText.text = d.MaxHp.ToString();
        critText.text = (d.CritRate * 100).ToString("0") + "%";   // 暴击率转成百分比显示
    }

    // 下面四个是加点按钮的点击事件，加完刷新一次让数字更新
    public void OnAddAtk()  { GrowthMgr.Instance.AddAttribute(GrowthAttribute.Atk);      Refresh(); }
    public void OnAddDef()  { GrowthMgr.Instance.AddAttribute(GrowthAttribute.Def);      Refresh(); }
    public void OnAddHp()   { GrowthMgr.Instance.AddAttribute(GrowthAttribute.MaxHp);    Refresh(); }
    public void OnAddCrit() { GrowthMgr.Instance.AddAttribute(GrowthAttribute.CritRate); Refresh(); }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = 1;   // 关面板恢复游戏速度（打开时暂停了）
            UIMgr.Instance.HidePanel<GrowthPanel>();
        }
    }
}
