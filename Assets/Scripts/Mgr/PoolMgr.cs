using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池：按 prefab 复用 GameObject。
/// 契约：池里的实例永远 inactive、挂在池根下；Get 取出后由调用方 SetActive(true)，
///       Release 负责 SetActive(false) + 挂回池根 + 入队。
/// 用法：
///   PoolMgr.Instance.Warm(prefab, n);          // 预热，避免首次播放卡顿
///   GameObject go = PoolMgr.Instance.Get(prefab);   // 取
///   PoolMgr.Instance.Release(prefab, go);           // 还
/// </summary>
public class PoolMgr : MonoBehaviour
{
    private static PoolMgr instance;
    public static PoolMgr Instance => instance;

    // prefab -> 空闲实例队列
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
    private Transform root;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        root = new GameObject("_Pool").transform;
        root.SetParent(transform);
    }

    /// <summary>预热：预先实例化 count 个实例压进池。</summary>
    public void Warm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        for (int i = 0; i < count; i++)
            Release(prefab, Create(prefab));
    }

    /// <summary>取一个实例（池里没有就新建）。返回的是 inactive 实例，调用方 SetActive(true) 后使用。</summary>
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null) return null;
        if (!pools.TryGetValue(prefab, out var queue) || queue.Count == 0)
            return Create(prefab);
        return queue.Dequeue();
    }

    /// <summary>归还实例（SetActive(false) 并挂回池根）。</summary>
    public void Release(GameObject prefab, GameObject go)
    {
        if (prefab == null || go == null) return;
        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }
        go.SetActive(false);
        go.transform.SetParent(root);
        queue.Enqueue(go);
    }

    private GameObject Create(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, root);
        go.name = prefab.name;
        go.SetActive(false); // 新建也保持 inactive，统一由调用方激活
        return go;
    }
}
