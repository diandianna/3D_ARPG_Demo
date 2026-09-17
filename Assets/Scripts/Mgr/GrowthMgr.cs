using UnityEngine;

/// <summary>
/// 养成系统：打怪得经验 → 升级给属性点 → 加点（攻/防/血/暴击）。
///
/// 数据在哪：属性和等级都在 GameDataMgr.Instance.playerData 里（那 44 字节里就有），
/// 改完调 PlayerSyncMgr.SyncPlayerData() 全量同步到服务器。
/// 属性点 attributePoints 是客户端本地记的，暂时没上服务器。
/// </summary>
public class GrowthMgr : MonoBehaviour
{
    public static GrowthMgr Instance { get; private set; }

    public int attributePoints;    // 可分配属性点（升级时增加，加点时消耗）

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 加经验并判断升级。曲线先写死：升一级需要 100 * 当前等级 经验，每级给 3 点。
    /// 用 while 而不是 if：防止一次加很多经验时连升几级。
    /// </summary>
    public void AddExp(int exp)
    {
        PlayerData data = GameDataMgr.Instance.playerData;
        data.AddExp(exp);   // AddExp 把经验累加到 levelNum

        int need = 10 * data.level;
        while (data.levelNum >= need)
        {
            // ↓ 升级自动加数值（数字你定）
            data.level++;
            data.atkNum += 2;
            data.DefNum += 1;
            data.MaxHp += 20;
            data.hp = data.MaxHp;          // 升级回满血，可选
            need = 10 * data.level;
            TaskMgr.Instance?.OnLevelUp();   // 顺便通知任务系统（"升到 N 级"这类目标）
        }
        PlayerSyncMgr.Instance.SyncPlayerData();   // 等级变了，同步服务器
    }

    /// <summary>加点。消耗 1 属性点改对应属性，再同步。返回 false 表示没点了。</summary>
    public bool AddAttribute(GrowthAttribute attr)
    {
        if (attributePoints <= 0) return false;   // 没点了
        PlayerData data = GameDataMgr.Instance.playerData;

        switch (attr)
        {
            case GrowthAttribute.Atk:      data.atkNum++;          break;   // 攻击 +1
            case GrowthAttribute.Def:      data.DefNum++;          break;   // 防御 +1
            case GrowthAttribute.MaxHp:    data.MaxHp += 10;       break;   // 血量 +10
            case GrowthAttribute.CritRate: data.CritRate += 0.01f; break;   // 暴击率 +1%
        }
        attributePoints--;
        PlayerSyncMgr.Instance.SyncPlayerData();
        return true;
    }
}

public enum GrowthAttribute { Atk, Def, MaxHp, CritRate }
