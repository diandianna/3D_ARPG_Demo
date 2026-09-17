using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店面板：ScrollView 里生成商品条目，点击购买。
/// 预制体放 Resources/Prefabs/Panel/ShopPanel。
///
/// 条目生成方式：遍历 ShopMgr.GetShopItems()，每个商品 Instantiate 一个 itemPrefab
/// 到 ScrollView 的 Content 下，再把名字/价格/图标填进去、给购买按钮绑上购买事件。
///
/// itemPrefab（单个商品条目）子节点的固定命名，缺哪个就少显示哪个（不会报错）：
///   "Name"  -> Text   （商品名，建议有）
///   "Price" -> Text   （价格，建议有）
///   "Icon"  -> Image  （图标，可没有）
///   "Buy"   -> Button （购买按钮，必须有，否则点不了买不了）
/// 注意：这几个节点必须是 itemPrefab 的"直接子节点"（transform.Find 只找一层）。
/// </summary>
public class ShopPanel : BasePanel
{
    public Transform content;        // ScrollView 的 Content 节点（商品条目的父节点）
    public GameObject itemPrefab;    // 单个商品条目预制体（含名字/价格/图标/购买按钮）

    private const string NAME_NODE  = "Name";
    private const string PRICE_NODE = "Price";
    private const string ICON_NODE  = "Icon";
    private const string BUY_NODE   = "Buy";

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (content == null || itemPrefab == null) return;

        // 清空上一次生成的商品条目（itemPrefab 现在是独立预制体，不在 Content 下，放心全删）
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        List<ItemConfig> items = ShopMgr.Instance.GetShopItems();
        foreach (var item in items)
        {
            GameObject go = Instantiate(itemPrefab, content);
            go.SetActive(true);   // 保险：即使预制体是失活的，克隆也强制激活
            FillItem(go, item);
        }
    }

    /// <summary>
    /// 把单个商品的 名字/价格/图标 填进条目，并给购买按钮绑上购买事件。
    /// 每个节点都判空：条目里没有某个节点就跳过，不报错。
    /// </summary>
    private void FillItem(GameObject go, ItemConfig item)
    {
        Text nameText = Find<Text>(go, NAME_NODE);
        if (nameText != null) nameText.text = item.itemName;

        Text priceText = Find<Text>(go, PRICE_NODE);
        if (priceText != null) priceText.text = item.price.ToString();

        Image iconImg = Find<Image>(go, ICON_NODE);
        if (iconImg != null) iconImg.sprite = item.icon;

        Button buyBtn = Find<Button>(go, BUY_NODE);
        if (buyBtn != null)
        {
            int id = item.id;   // 存局部变量，闭包里引用的 id 才不会随循环变到最后一项
            buyBtn.onClick.AddListener(() => ShopMgr.Instance.Buy(id));
        }
        else
        {
            // 找不到购买按钮就提示，不然点了没反应还不知道为啥
            Debug.LogWarning($"[Shop] 条目缺购买按钮：itemPrefab 下要有名为 \"{BUY_NODE}\" 的直接子节点（Button）");
        }
    }

    // 按名字找直接子节点上的组件，找不到（节点不存在 / 没该组件）返回 null
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
            UIMgr.Instance.HidePanel<ShopPanel>();
        }
    }
}
