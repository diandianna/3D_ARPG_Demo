using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店：上架商品列表 + 购买。
///
/// 为什么客户端只发 itemId、不自己算价格扣钱：
/// 如果客户端自己扣钱，玩家改内存就能免费买。发 itemId 让服务器查价格表扣钱，
/// 就是"服务器权威"，防作弊。背包 inventory 目前是客户端本地记的（买完记进来）。
/// </summary>
public class ShopMgr : MonoBehaviour
{
    public static ShopMgr Instance { get; private set; }

    [Header("上架商品（拖入 ItemConfig）")]
    [SerializeField] private List<ItemConfig> shopItems = new List<ItemConfig>();

    public InventoryData inventory = new InventoryData();   // 背包（买的道具进这里）

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public List<ItemConfig> GetShopItems() => shopItems;   // UI 遍历这个列表生成商品条目

    /// <summary>购买入口。只发 itemId，结果通过 OnBuySuccess 回来（由 PlayerSyncMgr 回调）。</summary>
    public void Buy(int itemId)
    {
        if (!shopItems.Exists(i => i.id == itemId))
        {
            Debug.LogWarning($"[Shop] 商品 {itemId} 不在上架列表");
            return;
        }
        PlayerSyncMgr.Instance.SendShopBuy(itemId);
    }

    /// <summary>服务器确认购买成功后回调，本地把道具记进背包。</summary>
    public void OnBuySuccess(int itemId)
    {
        inventory.Add(itemId);
        Debug.Log($"[Shop] 购买成功 itemId={itemId}");
    }

    /// <summary>使用背包里的消耗品。demo 先只做一种效果：回血（effectValue = 回血量）。</summary>
    public void UseItem(int itemId)
    {
        ItemConfig cfg = shopItems.Find(i => i.id == itemId);
        if (cfg == null) { Debug.LogWarning($"[Item] 道具 {itemId} 不在上架列表"); return; }
        if (cfg.type != ItemType.Consumable) { Debug.Log($"[Item] {cfg.itemName} 不是消耗品，不能使用"); return; }
        if (inventory.GetCount(itemId) <= 0) { Debug.Log($"[Item] 背包里没有 {cfg.itemName}"); return; }

        // 回血：ChangeHp 内部已 clamp 到 MaxHp，不会溢出
        GameDataMgr.Instance.playerData.ChangeHp(cfg.effectValue);
        inventory.TryRemove(itemId, 1);   // 用掉一个

        // 要不要把新 hp 同步服务器（联机/存档才需要），demo 单机先注释掉：
         PlayerSyncMgr.Instance.SyncPlayerData();

        Debug.Log($"[Item] 使用 {cfg.itemName}，回血 {cfg.effectValue}");
    }
}
