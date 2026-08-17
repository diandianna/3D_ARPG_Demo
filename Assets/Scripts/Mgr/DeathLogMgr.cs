using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 死亡日志管理器 —— 负责：
/// 1. 分配怪物唯一ID
/// 2. 根据 DeathLog 动态计算掉落（非固定概率表）
/// 3. 归档所有死亡日志供调试/分析
///
/// 掉落公式说明：
///   gold  = 基值 × 威胁倍率 × 击杀倍率 + 追回被偷金币 × 1.5
///   exp   = 基值 × 威胁倍率 × 存活时间倍率
///   fragments = 基值 + 附身加成 + 截杀加成
///
/// 威胁倍率基于怪物造成的伤害（damageDealt）：
///   >100 伤害 → 1.5x（高危怪物，掉落更好）
///   >50  伤害 → 1.2x
///   否则      → 1.0x
///
/// 这体现了"怪物越危险、掉落越好"的设计理念：
/// 玩家承担的风险越大，回报越高。
/// </summary>
public class DeathLogMgr : MonoBehaviour
{
    public static DeathLogMgr Instance { get; private set; }

    // ====== 怪物ID分配 ======
    private int nextMonsterId = 0;

    // ====== 死亡日志归档（调试/分析用） ======
    private List<DeathLog> deathArchive = new List<DeathLog>();

    // ====== 掉落基表（按怪物类型） ======
    // 不在 ScriptableObject 中配置，直接硬编码——等配表系统上线后再迁移
    private Dictionary<string, DropResult> baseDropTable;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        InitDropTable();
    }

    /// <summary>初始化掉落基表</summary>
    private void InitDropTable()
    {
        baseDropTable = new Dictionary<string, DropResult>
        {
            ["Normal"]     = new DropResult { gold = 10, exp = 15, soulFragments = 1,  debugSummary = "" },
            ["Elite"]      = new DropResult { gold = 30, exp = 40, soulFragments = 3,  debugSummary = "" },
            ["Boss"]       = new DropResult { gold = 100, exp = 150, soulFragments = 10, debugSummary = "" },
            ["Interceptor"] = new DropResult { gold = 25, exp = 50, soulFragments = 5,  debugSummary = "" },
        };
    }

    // ============================================================
    // 公共接口
    // ============================================================

    /// <summary>分配下一个怪物ID</summary>
    public int GetNextMonsterId()
    {
        return nextMonsterId++;
    }

    /// <summary>根据死亡日志动态计算掉落</summary>
    public DropResult CalculateDrops(DeathLog log)
    {
        // 1. 查基表
        if (!baseDropTable.TryGetValue(log.monsterType, out DropResult drop))
        {
            drop = baseDropTable["Normal"];
        }

        // 2. 威胁倍率：怪物造成的伤害越高，掉落越好
        float threatMult = 1f;
        if (log.damageDealt > 100f)
            threatMult = 1.5f;
        else if (log.damageDealt > 50f)
            threatMult = 1.2f;

        // 3. 存活时间倍率：活得越久，经验越多（代表怪物"老练"）
        float timeMult = 1f;
        float alive = log.AliveTime;
        if (alive > 120f)
            timeMult = 1.3f;
        else if (alive > 60f)
            timeMult = 1.1f;

        // 4. 击杀玩家倍率：高风险 → 高回报
        float killMult = 1f + log.playersKilled * 1.0f;

        // 5. 计算各项掉落
        int gold = Mathf.RoundToInt(drop.gold * threatMult * killMult);

        // 追回被偷金币 +50% 利息
        if (log.goldStolen > 0)
        {
            int recoveredGold = Mathf.RoundToInt(log.goldStolen * 1.5f);
            gold += recoveredGold;
        }

        int exp = Mathf.RoundToInt(drop.exp * threatMult * timeMult);

        int fragments = drop.soulFragments;
        if (log.wasPossessed)
            fragments += 1;          // 附身过的怪物残留灵魂精华
        fragments += log.interceptCount; // 截杀灵体次数 = 额外碎片

        // 6. 构建调试摘要
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append($"威胁x{threatMult:F1} ");
        sb.Append($"时间x{timeMult:F1} ");
        if (killMult > 1f) sb.Append($"击杀x{killMult:F1} ");
        if (log.wasPossessed) sb.Append("[附身+1碎片] ");
        if (log.interceptCount > 0) sb.Append($"[截杀+{log.interceptCount}碎片] ");
        if (log.goldStolen > 0) sb.Append($"[追回{log.goldStolen}G+利息] ");

        return new DropResult
        {
            gold = gold,
            exp = exp,
            soulFragments = fragments,
            debugSummary = sb.ToString().TrimEnd()
        };
    }

    /// <summary>获取已归档的死亡日志数量（调试用）</summary>
    public int ArchiveCount => deathArchive.Count;

    /// <summary>获取最近一条死亡日志（调试用）</summary>
    public DeathLog? GetLatestDeathLog()
    {
        if (deathArchive.Count == 0) return null;
        return deathArchive[deathArchive.Count - 1];
    }

    // ============================================================
    // 内部方法
    // ============================================================

    /// <summary>归档一条死亡日志</summary>
    public void Archive(DeathLog log)
    {
        deathArchive.Add(log);
        Debug.Log($"[DeathLogMgr] 归档 #{log.monsterId}: {log}");
    }

    /// <summary>处理怪物死亡：计算掉落 → 发放奖励 → 归档日志</summary>
    /// <returns>最终的 DropResult</returns>
    public DropResult ProcessMonsterDeath(DeathLog log)
    {
        log.MarkDeath();

        DropResult result = CalculateDrops(log);

        // 发放奖励到各系统
         if (SoulFragmentMgr.Instance != null && result.soulFragments > 0)
        {
            SoulFragmentMgr.Instance.AddFragments(result.soulFragments);
        }
        // TODO: 金币和经验暂不发放（等背包/经验系统上线后接入）
        GameDataMgr.Instance.playerData.AddMoney(result.gold,0);// += result.gold;
        GameDataMgr.Instance.playerData.AddExp(result.exp);// += result.exp;

        Archive(log);

        Debug.Log($"[DeathLogMgr] 怪物 #{log.monsterId}({log.monsterType}) 死亡 → {result} | {result.debugSummary}");

        return result;
    }
}
