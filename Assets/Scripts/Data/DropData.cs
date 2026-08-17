/// <summary>
/// 掉落结果 —— DeathLogMgr.CalculateDrops() 的返回值。
/// 包含金币、经验、灵魂碎片三个维度。
/// </summary>
[System.Serializable]
public struct DropResult
{
    public int gold;            // 金币掉落
    public int exp;             // 经验掉落
    public int soulFragments;   // 灵魂碎片掉落
    public string debugSummary; // 调试用：掉落计算过程说明

    /// <summary>空掉落（怪物被销毁但无奖励，如GM清理）</summary>
    public static DropResult None => new DropResult
    {
        gold = 0,
        exp = 0,
        soulFragments = 0,
        debugSummary = "无掉落"
    };

    public bool HasDrops => gold > 0 || exp > 0 || soulFragments > 0;

    public override string ToString()
    {
        return $"[掉落] {gold}金币 {exp}经验 {soulFragments}碎片";
    }
}
