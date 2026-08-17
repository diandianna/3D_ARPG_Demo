using UnityEngine;

/// <summary>
/// 怪物死亡日志 —— 记录怪物从出生到死亡的完整行为履历。
/// 死亡时由 DeathLogMgr 根据履历动态计算掉落，而非固定概率表。
///
/// 设计原则：
/// - struct 避免 GC 分配
/// - 所有字段在 Init() 中清零，死亡时 MarkDeath() 封存
/// - 各系统在怪物存活期间写入对应字段（伤害系统写 damageDealt/damageTaken，附身系统写 wasPossessed 等）
/// </summary>
[System.Serializable]
public struct DeathLog
{
    // ====== 基础信息 ======
    public int monsterId;           // 怪物实例唯一ID（由 DeathLogMgr 分配）
    public string monsterType;      // 怪物类型名："Normal"/"Elite"/"Boss"/"Interceptor"
    public float spawnTime;         // 出生时间戳（Time.time）
    public float deathTime;         // 死亡时间戳，0 表示还活着

    // ====== 行为履历 ======
    public float damageDealt;       // 对玩家造成的总伤害
    public float damageTaken;       // 受到的总伤害（衡量承伤能力）
    public int goldStolen;          // 偷了多少金币
    public bool wasPossessed;       // 是否被玩家附身过
    public int trapsTriggered;      // 踩了多少陷阱
    public int playersKilled;       // 击杀玩家次数
    public int interceptCount;      // 截杀灵体次数（仅 Interceptor 类型有意义）

    // ====== 计算属性 ======
    public float AliveTime => deathTime > 0
        ? deathTime - spawnTime
        : Time.time - spawnTime;

    /// <summary>怪物出生时调用，初始化所有字段</summary>
    public void Init(int id, string type)
    {
        monsterId = id;
        monsterType = type;
        spawnTime = Time.time;
        deathTime = 0;

        damageDealt = 0;
        damageTaken = 0;
        goldStolen = 0;
        wasPossessed = false;
        trapsTriggered = 0;
        playersKilled = 0;
        interceptCount = 0;
    }

    /// <summary>怪物死亡时调用，封存死亡时间</summary>
    public void MarkDeath()
    {
        deathTime = Time.time;
    }

    /// <summary>调试用摘要</summary>
    public override string ToString()
    {
        return $"[DeathLog #{monsterId}] {monsterType} | " +
               $"存活 {AliveTime:F1}s | " +
               $"造成伤害 {damageDealt:F0} | 受到伤害 {damageTaken:F0} | " +
               $"偷金 {goldStolen} | 附身 {wasPossessed} | 截杀 {interceptCount} | " +
               $"陷阱 {trapsTriggered} | 杀玩家 {playersKilled}";
    }
}
