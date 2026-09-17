using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任务面板 / 追踪栏：显示当前任务名、描述、进度、领奖按钮。
/// 预制体要放在 Resources/Prefabs/Panel/TaskPanel（UIMgr 按名字加载）。
///
/// 刷新机制：订阅 TaskMgr.OnTaskChanged 事件，任务一变化（接取/推进/完成）就自动刷新，
/// 不需要每帧去读。OnEnable 订阅、OnDisable 退订，防止面板销毁后还被触发。
/// </summary>
public class TaskPanel : BasePanel
{
    public Text taskNameText;
    public Text taskDescText;
    public Text progressText;
    public Button claimButton;    // "领取奖励"按钮，只有任务达成时才显示

    private void OnEnable()
    {
        if (TaskMgr.Instance != null) TaskMgr.Instance.OnTaskChanged += Refresh;   // 订阅
        claimButton.onClick.AddListener(OnClaimClick);   // 代码里绑领奖按钮，不用 Inspector 拖 OnClick
        Refresh();
    }
    private void OnDisable()
    {
        if (TaskMgr.Instance != null) TaskMgr.Instance.OnTaskChanged -= Refresh;   // 退订
        claimButton.onClick.RemoveListener(OnClaimClick);   // 退订按钮，防止重复绑定
    }

    public void Refresh(TaskConfig task = null)
    {
        // task 为 null 时读当前任务（事件回调会传最新任务进来，这里也能手动刷新）
        var current = task ?? (TaskMgr.Instance != null ? TaskMgr.Instance.CurrentTask : null);
        if (current == null)
        {
            taskNameText.text = "暂无任务";
            taskDescText.text = "";
            progressText.text = "";
            claimButton.gameObject.SetActive(false);
            return;
        }

        taskNameText.text = current.taskName;
        taskDescText.text = current.description;

        int cur = TaskMgr.Instance.GetProgress(current.id);
        progressText.text = $"{cur} / {current.targetCount}";   // 如 "3 / 5"

        // 只有"状态真正是已完成"才显示领奖按钮。
        // 不能用 cur >= targetCount 判断：万一任务 targetCount 填成 0，按钮会提前显示，
        // 但 ClaimReward 里判的是 state == Completed，两者对不上，点了也没反应。
        bool done = TaskMgr.Instance != null && TaskMgr.Instance.IsCompleted(current.id);
        claimButton.gameObject.SetActive(done);
    }

    // 绑定到领奖按钮的点击事件
    public void OnClaimClick() => TaskMgr.Instance.ClaimReward();

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = 1;
            UIMgr.Instance.HidePanel<TaskPanel>();
        }
    }
}
