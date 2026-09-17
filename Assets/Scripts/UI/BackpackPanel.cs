using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板：把 ShopMgr.inventory 里买到的道具列出来（名字 + 图标 + 数量）。
/// 预制体放 Resources/Prefabs/Panel/BackpackPanel（UIMgr 按类名加载）。
///
/// 数据从哪来：不自己存，每次打开都读 ShopMgr.Instance.inventory 这个"唯一数据源"。
/// 商店买完 → ShopMgr.OnBuySuccess → inventory.Add；背包打开 → 读 inventory 显示。
/// 所以只要买的动作对了，背包自然就有，不需要在两者之间再做任何同步。
///
/// itemPrefab（单个背包条目）子节点的固定命名（和 ShopPanel 同套路，缺哪个少显示哪个）：
///   "Name"  -> Text   （道具名）
///   "Icon"  -> Image  （图标）
///   "Count" -> Text   （数量，显示成 "×3"）
/// 注意：必须是 itemPrefab 的"直接子节点"（transform.Find 只找一层）。
/// </summary>
public class BackpackPanel : BasePanel
{
    public Transform content;        // ScrollView 的 Content（条目的父节点）
    public GameObject itemPrefab;    // 单个背包条目预制体（含 Name/Icon/Count 直接子节点）

    private const string NAME_NODE  = "Name";
    private const string ICON_NODE  = "Icon";
    private const string COUNT_NODE = "Count";
    private const string USE_NODE = "Use";  

    // 用 OnEnable 而不是 Start：面板每次打开都要刷新一次（背包可能刚买过东西变了）
    private void OnEnable() => Refresh();

    public void Refresh()
    {
        if (content == null || itemPrefab == null) return;

        // 清空上一次生成的条目
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // inventory.items 是 List<InventoryItem>（itemId + count），直接遍历
        foreach (var entry in ShopMgr.Instance.inventory.items)
        {
            GameObject go = Instantiate(itemPrefab, content);
            go.SetActive(true);   // 保险：预制体失活时克隆也失活，这里强制激活
            FillItem(go, entry.itemId, entry.count);
        }
    }

    /// <summary>把单个道具的名字/图标/数量填进条目。</summary>
    private void FillItem(GameObject go, int itemId, int count)
    {
        // 按 id 查道具配置拿名字/图标。demo 里道具都来自商店，所以从 ShopMgr 的
        // 上架列表里查就够了；将来道具来源变多，再抽一个独立的 ItemConfig 表。
        ItemConfig cfg = ShopMgr.Instance.GetShopItems().Find(i => i.id == itemId);

        Text nameText = Find<Text>(go, NAME_NODE);
        if (nameText != null) nameText.text = cfg != null ? cfg.itemName : $"道具 {itemId}";

        Text countText = Find<Text>(go, COUNT_NODE);
        if (countText != null) countText.text = $"×{count}";

        Image iconImg = Find<Image>(go, ICON_NODE);
        if (iconImg != null) iconImg.sprite = cfg != null ? cfg.icon : null;   // 没图标就清空，别显示旧的

        Button useBtn = Find<Button>(go, USE_NODE);
        if (useBtn != null)
        {
            if (cfg != null && cfg.type == ItemType.Consumable)
            {
                int id = itemId;                       // itemId 已是本方法参数，天然每调用一份，无闭包陷阱
                useBtn.gameObject.SetActive(true);
                useBtn.onClick.AddListener(() => {
                    ShopMgr.Instance.UseItem(id);
                    Refresh();                         // 用掉后重建列表，数量立刻变
                });
            }
            else
            {
                useBtn.gameObject.SetActive(false);    // 装备/材料不显示"使用"按钮
            }
        }
    }

    // 按名字找直接子节点上的组件，找不到返回 null（和 ShopPanel 里那个 helper 一样）
    private T Find<T>(GameObject go, string name) where T : Component
    {
        Transform t = go.transform.Find(name);
        return t != null ? t.GetComponent<T>() : null;
    }

    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Time.timeScale = 1;   // 关面板恢复游戏速度（打开时暂停了）
            UIMgr.Instance.HidePanel<BackpackPanel>();
        }
    }
}
