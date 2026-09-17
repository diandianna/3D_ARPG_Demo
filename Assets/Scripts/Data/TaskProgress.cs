using System;

/// <summary>
/// 单个任务的运行时进度（玩家玩到哪了）。
///
/// 状态机只有一条线，不会乱：
///   未接取 NotAccepted → 进行中 InProgress → 已达成待领奖 Completed → 已领奖 Claimed
/// 状态切换都发生在 TaskMgr 里：AcceptTask 接取，Progress 推进到 Completed，ClaimReward 到 Claimed。
/// </summary>
[Serializable]
public class TaskProgress
{
    public int taskId;
    public int currentCount;   // 当前进度（如任务要杀 5 只，杀了 3 只这里就是 3）
    public TaskState state;
}

public enum TaskState
{
    NotAccepted,   // 未接取
    InProgress,    // 进行中
    Completed,     // 目标达成，待领奖
    Claimed        // 已领奖
}
