using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特效管理器：集中管理所有战斗特效（玩家 + 敌人通用）。
/// 视觉归这里管，伤害判定归 VFXDamage 管，两者分离、互不耦合。
///
/// 两类特效两种方式：
///   - poolable=true（纯粒子特效）：走对象池，复用、零 GC。
///   - poolable=false（带 Start/自毁脚本的 ScriptBased 特效）：走 Instantiate + Destroy，
///     让特效自带的脚本按"活一次死一次"原生方式跑（Start 每次重新跑、子物体随根销毁清掉）。
///
/// 用法：
///   1. 把 VFXMgr 挂到常驻对象上（和 AudioManager 同一个 GameObject 即可）。
///   2. Inspector 的 entries 里拖特效、起 key；ScriptBased 特效勾掉 poolable 并手动填 duration。
///   3. 调用：
///        VFXMgr.Instance.Play("hit_normal", pos, rot);             // 世界坐标
///        VFXMgr.Instance.Play("buff_aura", parent, localPos, rot); // 跟随
/// </summary>
public class VFXMgr : MonoBehaviour
{
    private static VFXMgr instance;
    public static VFXMgr Instance => instance;

    [Header("特效注册表：把要用的特效拖进来（玩家/敌人共用）")]
    public List<VFXEntry> entries = new List<VFXEntry>();

    private Dictionary<string, VFXEntry> registry = new Dictionary<string, VFXEntry>();

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildRegistry();
        PrewarmAll();
    }

    void BuildRegistry()
    {
        registry.Clear();
        foreach (var e in entries)
        {
            if (e == null || e.prefab == null) continue;
            if (string.IsNullOrEmpty(e.key))
            {
                Debug.LogWarning("[VFXMgr] 有条目没填 key，已跳过", e.prefab);
                continue;
            }
            if (registry.ContainsKey(e.key))
            {
                Debug.LogWarning($"[VFXMgr] key 重复：{e.key}，已跳过", e.prefab);
                continue;
            }
            if (e.duration <= 0f) e.duration = DetectDuration(e.prefab);
            registry[e.key] = e;
        }
    }

    void PrewarmAll()
    {
        foreach (var kv in registry)
        {
            var e = kv.Value;
            if (!e.poolable) continue; // 不池化的不用预热
            for (int i = 0; i < e.prewarm; i++)
            {
                GameObject go = PoolMgr.Instance.Get(e.prefab);
                EnsureAutoReturn(go, e.key, e.duration);
                PoolMgr.Instance.Release(e.prefab, go);
            }
        }
    }

    // ===== 播放：世界坐标（爆炸 / AOE / 命中）=====
    public GameObject Play(string key, Vector3 position, Quaternion rotation)
    {
        VFXEntry e = GetEntry(key);
        if (e == null) return null;

        GameObject go;
        if (e.poolable)
        {
            go = PoolMgr.Instance.Get(e.prefab);
            EnsureAutoReturn(go, e.key, e.duration);
            go.transform.SetParent(null);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(e.prefab, position, rotation);
            Destroy(go, e.duration); // 到点销毁，子物体一起清掉
        }
        return go;
    }

    // ===== 播放：世界坐标、默认朝向（命中特效常用）=====
    public GameObject Play(string key, Vector3 position)
    {
        return Play(key, position, Quaternion.identity);
    }

    // ===== 播放：跟随（挂在 parent 身上，localPos/localRot 为相对偏移）=====
    public GameObject Play(string key, Transform parent, Vector3 localPos, Quaternion localRot)
    {
        VFXEntry e = GetEntry(key);
        if (e == null) return null;

        GameObject go;
        if (e.poolable)
        {
            go = PoolMgr.Instance.Get(e.prefab);
            EnsureAutoReturn(go, e.key, e.duration);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.SetActive(true);
        }
        else
        {
            go = Instantiate(e.prefab, parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            Destroy(go, e.duration);
        }
        return go;
    }

    // ===== 手动回收（提前结束；不池化的特效会直接销毁）=====
    public void Release(string key, GameObject go)
    {
        if (go == null) return;
        if (!registry.TryGetValue(key, out var e)) return;
        if (!e.poolable) { Destroy(go); return; }
        PoolMgr.Instance.Release(e.prefab, go);
    }

    private VFXEntry GetEntry(string key)
    {
        if (!registry.TryGetValue(key, out var e))
        {
            Debug.LogWarning($"[VFXMgr] 找不到特效 key：{key}");
            return null;
        }
        return e;
    }

    // 保证实例上挂了 VFXAutoReturn 并设好 key/时长。
    // 注意：池实例此时是 inactive 的，AddComponent 不会触发 OnEnable，
    //       所以先把 key/life 设好，等 SetActive(true) 时 OnEnable 才带着正确时长启动。
    private void EnsureAutoReturn(GameObject go, string key, float duration)
    {
        var auto = go.GetComponent<VFXAutoReturn>();
        if (auto == null) auto = go.AddComponent<VFXAutoReturn>();
        auto.key = key;
        auto.life = duration;
    }

    // 自动推断播放时长：取所有 ParticleSystem 里 (主时长 + 粒子生命) 的最大值（启发式，够用）。
    // 带脚本的 ScriptBased 特效总时长算不准，请手动填 duration。
    private float DetectDuration(GameObject prefab)
    {
        float max = 3f;
        var pss = prefab.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            float d = ps.main.duration + ps.main.startLifetime.constantMax;
            if (d > max) max = d;
        }
        return max;
    }
}

[System.Serializable]
public class VFXEntry
{
    public string key;                                // 调用名（唯一）
    public GameObject prefab;                         // 特效 prefab
    [Tooltip("播放时长(秒)；0=自动按粒子时长推断。带脚本的特效请手动填。")]
    [Range(0f, 30f)] public float duration = 0f;      // 播放时长
    [Tooltip("预热数量（仅 poolable=true 时生效）")]
    [Range(0, 8)] public int prewarm = 1;             // 预热数量
    [Tooltip("是否进对象池。纯粒子特效=true；带 Start/自毁脚本的 ScriptBased 特效=false。")]
    public bool poolable = true;                      // 是否池化
}
