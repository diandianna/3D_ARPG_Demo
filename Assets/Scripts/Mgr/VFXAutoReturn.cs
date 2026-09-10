using UnityEngine;

/// <summary>
/// 特效自动回收：由 VFXMgr 自动挂到每个池化特效根节点上，不用手动加。
/// 生命周期用 OnEnable/OnDisable 驱动，而不是 Start——
/// 池复用走 SetActive(true) 不会重跑 Start，但会重跑 OnEnable。
/// </summary>
public class VFXAutoReturn : MonoBehaviour
{
    [HideInInspector] public string key;      // 回收时查注册表用
    [HideInInspector] public float life = 1f; // 播放时长（秒）

    private float endTime;
    private bool returning;

    void OnEnable()
    {
        returning = false;
        endTime = Time.time + life;

        // 重新播放所有粒子：池复用 SetActive(true) 不一定自动重播，这里显式重启。
        var pss = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            pss[i].Clear();   // 清残留粒子
            pss[i].time = 0f; // 关键：模拟时钟归零，否则复用快进
            pss[i].Play();
        }
        // 重置动画状态，让带 Animator 的特效从头播。
        var anim = GetComponentInChildren<Animator>(true);
        if (anim != null) { anim.Rebind(); anim.Update(0f); }
    }

    void Update()
    {
        if (returning || Time.time < endTime) return;
        returning = true;
        if (VFXMgr.Instance != null)
            VFXMgr.Instance.Release(key, gameObject);
    }

    void OnDisable()
    {
        // 停掉并清空粒子，避免下次 SetActive(true) 时旧粒子残留。
        var pss = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
            pss[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
