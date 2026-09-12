# 辛吉勒姆 Boss + 玩家闪避 设计

> 生成 2026-08-23。原则：**讲得清 > 写得全**。demo 目标是能跑通核心闭环 + 讲清设计决策，不是复刻黑魂。

## 0. 核心设计要点

- **Boss 用 FSM**：状态少（7 个）够用；行为一多、要复用子树、要策划可视化配置时上**行为树**（Halo / UE 默认）。BT 本质是把 FSM 状态封装成树节点，便于层级化和复用。
- **霸体 + 韧性**：防无限连。霸体在放技能时免疫硬直；韧性决定平时挨打会不会被打断，韧性打空才进大硬直。
- **闪避无敌帧**：攻防对等是动作游戏地基，没有闪避就没法谈 Boss 强度。

---

## 1. 三层设计地图（按顺序做，不跳层）

| 层 | 玩家侧 | Boss 侧 |
|---|---|---|
| **① 地基（必须先做）** | 闪避 + 无敌帧（0.2~0.4s） | FSM 7 状态 + 受击硬直 |
| **② 核心（demo 亮点）** | 极限闪避 → 子弹时间（复用 BulletTimeMgr） | 技能表（距离+CD+权重）+ 霸体/韧性 |
| **③ 加分（时间够再加）** | 闪避反击 | followUp 连招 + 阶段脚本 + 麻痹机制 |

> 到第 ② 层结束就是完整动作闭环；第 ③ 层不做不亏。

---

## 2. Boss FSM（7 个逻辑状态，代码层）

```
Idle ──▶ Chase(追人) ──▶ Combat(战斗) ──▶ Hurt(受击) ──▶ Death
              ▲              │  ▲  │
              │        Rage  │  │  │  Paralysis(韧性打空)
              └──────────────┘  │  ▼
                        (狂暴/麻痹打断)  Paralysis_Start→Loop→End
```

- `Idle`：待机 Stand01
- `Chase`：Walk_F 追人；AM_Move_L/R 左右绕走（strafe）
- `Combat`：内部是**技能选择器**，不是状态
- `Hurt`：轻受击 / 重受击
- `Paralysis`：麻痹三连（韧性打空后的大硬直）
- `Rage`：狂暴转场（阶段切换播一次）
- `Death`：死亡

**关键**：Animator（Mecanim）管"播哪个 clip + 过渡"，C# 管"Boss 怎么想"。移动混合（Idle/Walk/Run）可用 Animator 图 + `SetFloat`；攻击/连段/麻痹/狂暴这些离散决策全放代码，用 `CrossFade` 切动画，别画进 Animator 图。

---

## 3. Boss 技能表（Combat 状态内部，数据驱动）

```csharp
SkillConfig {
    string id;            // "attack01"
    string animName;      // "AM_Attack01"
    int    phase;         // 1/2/3 属于哪个阶段
    float  minDist;       // 距离环下限（米）
    float  maxDist;       // 距离环上限
    float  cooldown;      // 冷却（秒）
    int    weight;        // 加权随机权重（越大越常出）
    string comboNext;     // 连段下一招 id（空 = 无）
    string followUp;      // 强制后续技能 id（空 = 随机）——"放了 A 必接 B"
    bool   interruptible; // 前摇可被麻痹/受击打断
    bool   isCharge;      // 冲锋类（先强制面向目标）
}
```

**技能选择逻辑（两级）**：
```
1. 先查 lastSkill.followUp —— 非空直接播 followUp（"绝对接这招"），跳过随机
2. 否则按 phase + 距离 + CD 过滤 → 按 weight 随机挑 → CrossFade 播
3. 播完记 lastSkill = 刚放的
```

**固定循环/阶段脚本**：`List<string> p2Script = {A,B,C,D};` + 指针，放完一次 index++，到末尾循环或停。

**实例**：

| animName | phase | dist | cd | weight | followUp | 说明 |
|---|---|---|---|---|---|---|
| AM_Attack01 | 1 | 0~2 | 0 | 30 | AM_Attack02_1 | 普攻起手 |
| AM_Attack02_1 | 1 | 0~2 | 0 | 30 | AM_Attack03 | 连段 |
| AM_Attack03 | 1 | 0~2 | 2 | 20 | — | 收尾 |
| AM_Attack07 | 2 | 2~8 | 6 | 25 | — | 冲锋重击(isCharge) |
| AM_Attack09 | 2 | 0~4 | 8 | 30 | — | AOE |
| AM_Attack16 | 3 | 3~12 | 5 | 30 | — | 光球弹道 |

---

## 4. Boss 霸体 / 韧性（防无限连）

```csharp
float maxPoise = 100;         // 韧性上限
float poise;                  // 当前韧性（被打就减）
float poiseRecoverDelay = 3;  // 多久没被打后开始回韧性
bool  isSuperArmor;           // 霸体：放技能前摇/后摇/起身后 = true
int   hitCount;               // 连续受击次数
int   retaliateAt = 3;        // 打几次就反击
string retaliateSkill;        // 反击放哪招（如 "AM_Attack09"）
```

**受击判断逻辑**：
```
1. Boss 在霸体（放技能中）→ 只扣血，不播受击、不打断
2. 否则扣韧性：
   - 韧性 > 0  → 播轻受击（AM_Behit_S_L），动作不打断
   - 韧性 <= 0 → 播大硬直 Paralysis_Start→Loop（玩家输出窗口）
3. 连续受击 hitCount ≥ retaliateAt → 霸体 + 放反击技（AOE 弹开）
```

> 辛吉勒姆的 `Paralysis_Start→Loop→End` 就是"韧性打空后的大硬直"，天生配好。

---

## 5. 玩家闪避系统（当前缺口，地基）

闪避动画用 **Animation Event 打窗口**，不用代码算时间：

| Event | 位置 | 方法 |
|---|---|---|
| `PerfectDodgeStart` | ~25% | `inPerfectWindow = true` |
| `PerfectDodgeEnd` | ~75% | `inPerfectWindow = false` |
| `DodgeEnd`（可选） | 100% | `isDodging = false` |

```
受击判断：
if (isDodging) {
    if (inPerfectWindow) → 极限闪避：不受伤 + BulletTimeMgr 慢动作(0.5~1s) + 飘字"极限闪避"
    else                 → 普通无敌：不受伤，无奖励
} else → 正常受伤
```

- 窗口宽窄直接拖 Event 位置调。
- **坑**：Event 方法必须写在 Animator 所在 GameObject 的脚本上（挂玩家身上的 PlayerController/BaseMove 里写 `public void PerfectDodgeStart()`）。
- 极限闪避成功后 → 设"可反击"标记 → 攻击键变反击技（第三层闪避反击）。
- 更硬核版：真正的 just-frame dodge 是"攻击前摇命中瞬间按键闪避"；demo 用"无敌帧内被攻击"版足够。

---

## 6. 辛吉勒姆最小动画集（150 → ~18，其余移出 Assets）

| 状态 | 文件 |
|---|---|
| Idle | `Stand01` |
| Chase | `Walk_F`、`AM_Move_L`、`AM_Move_R` |
| Combat P1 | `AM_Attack01`、`AM_Attack02_1`、`AM_Attack03` |
| Combat P2 | `AM_Attack07`、`AM_Attack09`、`AM_Attack13` |
| Combat P3 | `AM_Attack16` |
| Hurt | `AM_Behit_S_L`、`AM_Behit_B_L` |
| Paralysis | `Paralysis_Start`、`Paralysis_Loop`、`Paralysis_End` |
| Rage | `Rage` |
| Death | `AM_Death` |

**移出判断**：所有 `_SEQ1/_SEQ2/_Child` 变体、`_1/_2/_3` 连段变体、`Walk_B/L/R`、`Stand2_*` 7 个变体、10 种 `Death_*`、15 种 `Behit_*`——全不要，只留上表。

> ⚠️ 150 个 FBX 每个都内嵌完整模型，全导进 Unity Library 会爆到几十 G。先按上表砍到 ~18 个再导入。

---

## 7. 落地顺序（砍到能喘气）

1. **玩家闪避 + 无敌帧**（半天，地基）
2. **Boss FSM 7 状态 + 受击硬直**（1 天，复用 Auto.cs 改）
3. **极限闪避 → 子弹时间**（半天，接线 BulletTimeMgr）
4. **Boss 技能表 + 霸体韧性**（1 天）

第 3 步结束 = 能打、能躲、躲得好看的动作闭环。followUp / 麻痹 / 闪避反击全是不做不亏的加分项。
