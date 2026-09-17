using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务系统：接取 → 追踪进度 → 达成 → 领奖 → 接下一环（引导链）。
///
/// 进度怎么进来：各系统在"发生某件事"时调 TaskMgr 的 OnXxx 入口，
/// 例如怪物死亡时调 OnMonsterKilled。TaskMgr 只对"当前任务的匹配目标"累加，
/// 目标不匹配（比如当前任务要杀怪、你却收集了东西）就忽略。
///
/// 领奖怎么同步：奖励金币/经验由服务器发（服务器有 taskRewards 奖励表），
/// 客户端只发 taskId，服务器查表、加钱、回权威数据。防止客户端自己改奖励。
/// </summary>
public class TaskMgr : MonoBehaviour
{
    public static TaskMgr Instance { get; private set; }

    [Header("任务配置（拖入所有 TaskConfig 资产）")]
    [SerializeField] private List<TaskConfig> taskConfigs = new List<TaskConfig>();

    // configDict：id → 配置，方便按 id 查。progressDict：id → 运行时进度。
    private Dictionary<int, TaskConfig> configDict = new Dictionary<int, TaskConfig>();
    private Dictionary<int, TaskProgress> progressDict = new Dictionary<int, TaskProgress>();

    public TaskConfig CurrentTask { get; private set; }   // 当前引导任务（引导链每次只做一个）
    public System.Action<TaskConfig> OnTaskChanged;       // 任务变化时广播，UI 订阅刷新

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        foreach (var cfg in taskConfigs) configDict[cfg.id] = cfg;   // 建索引
    }

    private void Start()
    {
        // 引导入口：开场自动接第一个任务（也可改成 NPC 对话触发，调 AcceptTask）
        if (taskConfigs.Count > 0) AcceptTask(taskConfigs[0].id);
    }

    public TaskConfig GetConfig(int taskId) => configDict.TryGetValue(taskId, out var c) ? c : null;

    /// <summary>接取任务。进行中/待领奖的任务重复接会被忽略。</summary>
    public void AcceptTask(int taskId)
    {
        if (!configDict.ContainsKey(taskId)) return;   // 没这个任务
        if (!progressDict.ContainsKey(taskId))
            progressDict[taskId] = new TaskProgress { taskId = taskId, state = TaskState.NotAccepted };

        var p = progressDict[taskId];
        if (p.state == TaskState.InProgress || p.state == TaskState.Completed) return;  // 已接/待领奖不重复接
        p.state = TaskState.InProgress;
        CurrentTask = configDict[taskId];
        OnTaskChanged?.Invoke(CurrentTask);   // 通知 UI 刷新
    }

    // ---- 进度入口：各系统在对应时机调用（外部 hook 点）----
    public void OnMonsterKilled(string monsterType) => Progress(TaskObjective.KillMonster, 1);
    public void OnItemCollected(int itemId)          => Progress(TaskObjective.CollectItem, 1);
    public void OnReachArea()                        => Progress(TaskObjective.ReachArea, 1);
    public void OnTalkNpc()                          => Progress(TaskObjective.TalkNpc, 1);
    public void OnLevelUp()                          => Progress(TaskObjective.LevelUp, 1);

    /// <summary>推进当前任务进度。只有目标类型匹配才 +1，满了标记 Completed。</summary>
    private void Progress(TaskObjective obj, int add)
    {
        if (CurrentTask == null || CurrentTask.objective != obj) return;   // 没任务 / 目标类型不匹配
        if (!progressDict.TryGetValue(CurrentTask.id, out var p) || p.state != TaskState.InProgress) return;

        p.currentCount += add;
        if (p.currentCount >= CurrentTask.targetCount)
        {
            p.state = TaskState.Completed;   // 达成，等玩家点领奖
            Debug.Log($"[Task] 任务 {CurrentTask.taskName} 达成，可领奖");
        }
        OnTaskChanged?.Invoke(CurrentTask);
    }

    /// <summary>领奖：发 taskId 给服务器发奖励，然后自动接下一环。</summary>
    public void ClaimReward()
    {
        if (CurrentTask == null) return;
        if (!progressDict.TryGetValue(CurrentTask.id, out var p) || p.state != TaskState.Completed) return;  // 没达成不能领

        p.state = TaskState.Claimed;
        PlayerSyncMgr.Instance.SendTaskClaim(CurrentTask.id);   // 奖励由服务器发

        int next = CurrentTask.nextTaskId;
        if (next > 0 && configDict.ContainsKey(next)) AcceptTask(next);  // 有下一环就接
        else CurrentTask = null;                                          // 否则引导结束

        OnTaskChanged?.Invoke(CurrentTask);
    }

    public int GetProgress(int taskId) => progressDict.TryGetValue(taskId, out var p) ? p.currentCount : 0;

    /// <summary>任务是否已达成（待领奖）。面板显示领奖按钮、和 ClaimReward 的判据保持一致。</summary>
    public bool IsCompleted(int taskId) => progressDict.TryGetValue(taskId, out var p) && p.state == TaskState.Completed;
}
