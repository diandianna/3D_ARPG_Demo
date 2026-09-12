# 3D ARPG Demo

Unity 做的 3D 动作 RPG 练习项目。有角色控制、战斗、成长系统，还有一套自研 TCP 二进制协议 + C# 服务器，支持多人联机。

> 为了控制仓库体积，大图贴图、UI 图片（PNG/TGA）和 3ds Max 源文件没提交，本地是完整的。仓库里主要是代码、场景、Prefab、动画控制器、FBX 模型。

## 主要系统

- **连招**：Animator 状态机 + ComboStep 参数驱动 5 段连招，动画事件控制伤害判定帧，命中盒去重避免重复判定
- **网络**：`[4字节总长][4字节类型][数据]` 二进制协议，处理粘包/分包 + 心跳；客户端权威位置同步 + 指数平滑插值
- **怪物 AI**：FSM（Idle/Chase/Attack/Hurt/Dead）+ NavMeshAgent 寻路；Boss 多阶段（转场回血、Rage 变身、霸体韧性）
- **子弹时间**：BulletTimeMgr，慢动作
- **附身**：子弹时间 → 灵体飞行选敌 → 附身操控怪物，带灵魂完整度 / 理智值

## 功能

- 账号：注册 / 登录，服务器存玩家数据
- 角色：相机相对移动、连招、硬直、受击反馈
- 战斗：攻击判定、伤害飘字、怪物 AI
- 记录：死亡日志、灵魂碎片
- 联机：多人房间、位置同步

## 技术栈

| 端 | 技术 |
|---|---|
| 客户端 | Unity 2022.3.54f1c1、C# |
| 服务器 | C# / .NET 8、原生 Socket（TCP） |

## 运行

### 客户端

1. 用 Unity Hub 打开项目（Unity 2022.3.54f1c1）
2. 打开 `Assets/Scenes/BeginScene.unity`
3. Play

### 服务器

服务器是独立项目，不在这个仓库里：

```
D:\develop\My_Course\C++&C\3DArpgServer
```

```bash
cd 3DArpgServer
dotnet run
```

监听 `127.0.0.1:8080`，跨机器联机要改成局域网 IP。

## 目录

```
Assets/Scripts/
├── Base/        # 基础类（BaseMove 等）
├── Data/        # 数据类
├── Mgr/         # 管理器（GameDataMgr、SocketMgr、SyncMgr 等）
├── Monsters/    # 怪物与 AI
├── Scene/       # 场景逻辑
├── TextScript/  # 文本相关
└── UI/          # UI 面板
```

## 联机协议

- 消息格式：`[4字节总长][4字节类型][数据...]`
- 消息类型：登录 / 注册 / 更新 / 攻击 / 移动同步 / 心跳 / 多人同步 / 多人联机
- 网络模型：客户端权威，服务器只转发
