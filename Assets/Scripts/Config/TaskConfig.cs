using UnityEngine;

/// <summary>
/// 任务配置（ScriptableObject 资产）。
/// 创建方式：Project 窗口右键 → Create → ARPG/Task。
///
/// 引导流程怎么做：多个任务用 nextTaskId 串成一条链——
/// 任务 1 完成领奖 → 自动接 nextTaskId 指向的任务 2 → …（nextTaskId = -1 表示最后一个）。
/// 这样开场玩家被一条任务链带着走，就是"引导流程"。
/// </summary>
[CreateAssetMenu(menuName = "ARPG/Task", fileName = "Task_")]
public class TaskConfig : ScriptableObject
{
    public int id;
    public string taskName;
    [TextArea] public string description;

    [Header("目标")]
    public TaskObjective objective;   // 达成条件类型（击杀/收集/到达/对话/升级）
    public int targetCount;           // 要达成多少数量（如击杀 5 只）

    [Header("奖励（仅 UI 显示，实际发放以服务器 taskRewards 表为准）")]
    public int rewardGold;            // 这里只是给玩家看的数字，真正发多少由服务器定
    public int rewardExp;

    [Header("引导链")]
    public int nextTaskId = -1;       // 完成后解锁的下一个任务 id，-1 表示末端
}

public enum TaskObjective
{
    KillMonster,    // 击杀
    CollectItem,    // 收集
    ReachArea,      // 到达区域
    TalkNpc,        // 对话
    LevelUp         // 升级
}
