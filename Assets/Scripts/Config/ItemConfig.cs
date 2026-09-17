using UnityEngine;

/// <summary>
/// 商店道具配置（ScriptableObject 资产）。
/// 创建方式：Project 窗口右键 → Create → ARPG/Item。
///
/// 为什么用 ScriptableObject：道具的"数值"（名字/价格/图标）由策划在编辑器里填，
/// 和"逻辑"（ShopMgr 怎么买）分离，改数值不用改代码。
///
/// 注意 id：服务器不认识这个资产，它查的是自己硬编码的 id→价格 表（shopPrices），
/// 所以这里的 id 必须和服务器那边对齐，否则买的时候服务器会说"商品不存在"。
/// </summary>
[CreateAssetMenu(menuName = "ARPG/Item", fileName = "Item_")]
public class ItemConfig : ScriptableObject
{
    public int id;
    public string itemName;
    [TextArea] public string description;   // 多行描述，编辑器里显示成文本框
    public Sprite icon;

    public ItemType type = ItemType.Consumable;   // 消耗品 / 装备 / 材料
    public int price;                             // 金币价（仅 UI 显示，真实扣款看服务器）

    [Header("效果数值")]
    public int effectValue;                       // 回血/加攻等，含义按 type 定
}

public enum ItemType { Consumable, Equipment, Material }
