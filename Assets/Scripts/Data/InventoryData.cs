using System;
using System.Collections.Generic;

/// <summary>
/// 背包：记录"有哪些道具、各多少个"。
/// 当前存客户端本地（买完记进来），后续想跨设备/防作弊再把它挂到服务器 PlayerData 上。
/// Add / TryRemove 是唯一增减入口，保证数量不会变成负数。
/// </summary>
[Serializable]
public class InventoryData
{
    public List<InventoryItem> items = new List<InventoryItem>();

    public void Add(int itemId, int count = 1)
    {
        var item = items.Find(i => i.itemId == itemId);
        if (item == null) items.Add(new InventoryItem { itemId = itemId, count = count });  // 没有就新建
        else item.count += count;                                                            // 有就累加
    }

    public int GetCount(int itemId)
    {
        var item = items.Find(i => i.itemId == itemId);
        return item == null ? 0 : item.count;
    }

    public bool TryRemove(int itemId, int count = 1)
    {
        var item = items.Find(i => i.itemId == itemId);
        if (item == null || item.count < count) return false;   // 数量不够，失败
        item.count -= count;
        if (item.count == 0) items.Remove(item);                // 减到 0 就删掉这条
        return true;
    }
}

[Serializable]
public class InventoryItem
{
    public int itemId;
    public int count;
}
