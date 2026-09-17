using System;
using UnityEngine;

/// <summary>
/// 玩家数据 / 商店 / 任务 的服务器同步统一入口。
///
/// 为什么单独建这个类：商店、养成、任务、金币……都要"改完数据同步服务器"。
/// 如果各系统自己拼字节调 SocketMgr.Send，协议一改就要到处改。收拢到这里，
/// 业务层只调 SyncPlayerData() / SendShopBuy() / SendTaskClaim()，换协议只改这一个文件。
///
/// 消息通用格式（和服务器约定，所有消息都一样）：
///   [4字节 总长度][4字节 消息类型][payload...]
///   总长度 = 4(长度自己) + 4(类型) + payload 字节数。
///   调 SocketMgr.Instance.Send(type, data) 时，前面 8 字节头会自动补上，这里只拼 payload。
/// </summary>
public class PlayerSyncMgr : MonoBehaviour
{
    public static PlayerSyncMgr Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 全量同步：把本地 PlayerData 的 44 字节整个发过去，服务器原样覆盖存。
    /// 沿用现有 Update=2 消息（和杀怪掉钱是同一个通道）。
    /// 缺点：客户端权威——服务器不校验，客户端说多少就是多少。demo 够用，但不是防作弊。
    /// </summary>
    public void SyncPlayerData()
    {
        if (!SocketMgr.Instance.Check()) return;   // 没连上就不发，避免空 socket 报错
        SocketMgr.Instance.Send(2, GameDataMgr.Instance.playerData.GetPlayerDataBytes());
    }

    /// <summary>
    /// 商店购买（服务器权威）。
    /// payload 布局：[4 itemId]，只发 id——价格和扣款都由服务器查表决定，客户端无法改价格。
    /// </summary>
    public void SendShopBuy(int itemId)
    {
        if (!SocketMgr.Instance.Check())   // 没连上服务器就发不出去，先拦住并提示
        {
            Debug.LogWarning("[Shop] 未连接服务器，购买请求未发出");
            return;
        }
        byte[] data = new byte[4];
        BitConverter.GetBytes(itemId).CopyTo(data, 0);
        SocketMgr.Instance.Send(8, data);   // type=8 = ShopBuy
        Debug.Log($"[Shop] 已发送购买请求 itemId={itemId}");
    }

    /// <summary>
    /// 任务领奖（服务器权威）。
    /// payload 布局：[4 taskId]，奖励金币/经验由服务器查奖励表发放。
    /// </summary>
    public void SendTaskClaim(int taskId)
    {
        byte[] data = new byte[4];
        BitConverter.GetBytes(taskId).CopyTo(data, 0);
        SocketMgr.Instance.Send(9, data);   // type=9 = TaskClaim
    }

    // ============ 下面是"服务器回包"的处理 ============
    // 服务器回包在 SocketMgr 的后台接收线程里解析，再通过消息队列
    // 抛回 Unity 主线程调下面这两个方法（不能在后台线程直接改 Unity 对象）。

    /// <summary>
    /// 商店购买结果。服务器回包布局：
    ///   [4 总长][4 type=8][4 result][4 itemId][4 新金币]
    /// result=1 成功（用服务器返回的权威金币覆盖本地），0 失败（钱不够/商品不存在）。
    /// </summary>
    public void OnShopBuyResult(int result, int itemId, int newMoney)
    {
        if (result == 1)
        {
            GameDataMgr.Instance.playerData.money1 = newMoney;   // 金币以服务器为准，防止本地乱改
            ShopMgr.Instance?.OnBuySuccess(itemId);
        }
        else Debug.LogWarning($"[Shop] 购买失败 itemId={itemId}");
    }

    /// <summary>
    /// 任务领奖结果。服务器回包布局：
    ///   [4 总长][4 type=9][4 result][4 taskId][4 新金币][4 新经验]
    /// </summary>
    public void OnTaskClaimResult(int result, int taskId, int newMoney, int newExp)
    {
        if (result == 1)
        {
            GameDataMgr.Instance.playerData.money1 = newMoney;
            GameDataMgr.Instance.playerData.levelNum = newExp;
        }
        else Debug.LogWarning($"[Task] 领奖失败 taskId={taskId}");
    }
}
