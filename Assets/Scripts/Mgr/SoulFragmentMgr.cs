using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 灵魂碎片管理器 —— 独立于 HP/MP 的消耗性货币。
///
/// 获取途径：击杀怪物（根据 DeathLog 动态掉落）、完成战斗
/// 消费途径：刷新技能CD / 激活灵魂护盾 / 恢复理智 / 升级天赋
///
/// 设计原则：
/// - TryConsume 是唯一扣减入口，保证碎片不会变负数
/// - 所有消费方法返回 bool，调用方根据结果决定后续逻辑
/// - 当前理智/天赋系统未上线，消费方法留好接口 + TODO
/// </summary>
public class SoulFragmentMgr : MonoBehaviour
{
    public static SoulFragmentMgr Instance { get; private set; }

    // ====== 碎片数量 ======
    [SerializeField] private int fragments = 0;
    public int Fragments => fragments;

    // ====== 消费定价 ======
    public const int COST_REFRESH_CD    = 3;   // 刷新一个技能CD
    public const int COST_SOUL_SHIELD   = 5;   // 激活灵魂护盾（附身时保护本体）
    public const int COST_RESTORE_SANITY = 2;  // 恢复1点理智
    public const int COST_UPGRADE_TALENT = 10; // 升级1级天赋

    // ====== 护盾状态 ======
    public bool soulShieldActive { get; private set; } = false;

    // ====== 调试：最近消费记录 ======
    private Queue<string> usageHistory = new Queue<string>();
    private const int MAX_HISTORY = 20;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ============================================================
    // 碎片增减
    // ============================================================

    /// <summary>增加碎片（怪物死亡掉落时调用）</summary>
    public void AddFragments(int count)
    {
        if (count <= 0) return;
        fragments += count;
        GameDataMgr.Instance.playerData.AddMoney(0, fragments);
        Debug.Log($"[SoulFragment] +{count} 碎片 (当前: {fragments})");
    }

    /// <summary>尝试消耗碎片。唯一扣减入口，保证不超扣。</summary>
    /// <returns>true = 消耗成功，false = 碎片不足</returns>
    public bool TryConsume(int cost)
    {
        if (cost <= 0) return true;

        if (fragments >= cost)
        {
            fragments -= cost;
            return true;
        }

        Debug.Log($"[SoulFragment] 碎片不足! 需要 {cost}, 当前 {fragments}");
        return false;
    }

    // ============================================================
    // 消费接口
    // ============================================================

    /// <summary>消耗碎片刷新指定技能槽的CD</summary>
    /// <param name="skillSlot">技能槽位: 1/2/3 = 小技能, 4 = R大招</param>
    /// <returns>是否成功刷新</returns>
    public bool RefreshSkillCD(int skillSlot)
    {
        if (!TryConsume(COST_REFRESH_CD))
            return false;

        RecordUsage($"刷新技能CD (槽位{skillSlot})");

        // TODO: 等技能系统有 CD 数据结构后，在此处清零对应技能的冷却
        // 当前技能系统（LU_Attack.ShowSkill）没有 CD 概念，留好接口
        Debug.Log($"[SoulFragment] 技能槽 {skillSlot} CD 已刷新");

        return true;
    }

    /// <summary>激活灵魂护盾 —— 附身期间本体受到伤害时优先扣护盾而非HP</summary>
    /// <returns>是否激活成功</returns>
    public bool ActivateSoulShield()
    {
        if (!TryConsume(COST_SOUL_SHIELD))
            return false;

        soulShieldActive = true;
        RecordUsage("激活灵魂护盾");

        // TODO: 附身伤害反弹逻辑（四大代价之"灵魂过载"）中检查此标记
        // 在伤害结算处: if (SoulFragmentMgr.Instance.soulShieldActive) { 护盾吸收伤害; }
        Debug.Log("[SoulFragment] 灵魂护盾已激活 —— 附身时本体受到伤害将由护盾吸收");

        return true;
    }

    /// <summary>护盾被击破时由伤害系统调用</summary>
    public void BreakSoulShield()
    {
        soulShieldActive = false;
        Debug.Log("[SoulFragment] 灵魂护盾已破碎!");
    }

    /// <summary>消耗碎片恢复理智值</summary>
    /// <param name="amount">要恢复的理智点数</param>
    /// <returns>是否恢复成功</returns>
    public bool RestoreSanity(int amount)
    {
        if (amount <= 0) return false;

        int cost = amount * COST_RESTORE_SANITY;
        if (!TryConsume(cost))
            return false;

        RecordUsage($"恢复理智 +{amount}");

        // TODO: 等理智系统（精神污染）上线后，在此处增加理智值
        // GameDataMgr.Instance.playerData.sanity += amount;
        Debug.Log($"[SoulFragment] 理智 +{amount}");

        return true;
    }

    /// <summary>消耗碎片升级天赋</summary>
    /// <param name="talentId">天赋ID</param>
    /// <returns>是否升级成功</returns>
    public bool UpgradeTalent(int talentId)
    {
        if (!TryConsume(COST_UPGRADE_TALENT))
            return false;

        RecordUsage($"升级天赋 #{talentId}");

        // TODO: 等天赋树系统上线后，在此处调用天赋升级逻辑
        // TalentTreeMgr.Instance.Upgrade(talentId);
        Debug.Log($"[SoulFragment] 天赋 #{talentId} 已升级");

        return true;
    }

    // ============================================================
    // 调试
    // ============================================================

    private void RecordUsage(string description)
    {
        usageHistory.Enqueue($"[{Time.time:F1}s] {description} (剩余: {fragments})");
        if (usageHistory.Count > MAX_HISTORY)
            usageHistory.Dequeue();
    }

    /// <summary>获取最近消费记录（调试面板用）</summary>
    public string[] GetUsageHistory()
    {
        return usageHistory.ToArray();
    }
}
