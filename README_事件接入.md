# 事件 / 外部代码接入说明

第 0 层（避难所 1 区）只负责「探索 + 两扇门」，**棋盘和事件玩法不在这里**。目前两扇门：1F 东墙的 `门·事件一`（一开始就能进）、楼梯下暗角的 `门·事件二`（完成事件一才解锁）。这份文档写给负责事件的同学：不用改我的场景生成脚本，加自己的脚本和场景就能接进来。

---

## 一、三条接入路线（按需要选）

| 你想做的事 | 要动的地方 |
| --- | --- |
| 加一个能按 E 交互的东西（碎片、对话、门…） | 场景里复制一个现成的，或写个继承 `InteractableBase` 的脚本 |
| 加一个「进事件场景」的入口 | 门挂 `EventDoor`（可设前置事件），事件场景登记到 `EventCatalog` |
| 做事件玩法（棋盘 / 地图 / 解谜） | 自己新建场景 + 继承 `EventRoomBase` 的脚本 |

脚本放哪都行，建议 `Assets/Scripts/事件/`（新建文件夹）。

## 二、加一个事件（三步）

1. **做场景**：复制 `Assets/Scenes/Event_01.unity`，改成你的（`Ctrl+Shift+S` 另存）。
2. **挂脚本**：场景里放一个物体，挂一个继承 `EventRoomBase` 的脚本：

```csharp
using UnityEngine;

public class 回声室棋盘 : EventRoomBase
{
    protected override void Start()
    {
        base.Start();          // ← 必须调，不然 OnEnter 不会来
        // 这里初始化棋盘、读进度
        Debug.Log("当前事件：" + EventId + "，之前完成过吗：" + GameProgress.Ensure().IsEventCleared(EventId));
    }

    // 棋盘点亮完成时调用
    void 解谜成功()
    {
        Complete();            // 记录完成 + 回第 0 层（玩家会被放回离开的位置）
    }

    void 玩家退出()
    {
        Leave();               // 不记录完成，直接回第 0 层
    }
}
```

3. **登记**：
   - `File → Build Settings` 把你的场景拖进 Scenes In Build；
   - 打开 `Shelter_L0`，选中 `World/06_系统/事件登记表`，在 `Events` 里加一条：
     `id = event_01`、`displayName = 回声室`、`sceneName = 你的场景名`；
   - 选中 `World/03_可交互/门·事件一`，把 `EventDoor.eventId` 改成同一个 id（或者再加一扇门）。

`EventCatalog` 查不到、或者场景没进 Build Settings 时，门只会说「什么也没接上」，不会报错黑屏。

## 三、能用的 API

### 进度（跨场景，切场景不会丢）

```csharp
GameProgress.Ensure().AddFragment("frag_07");      // 加碎片，重复加返回 false
GameProgress.Ensure().HasFragment("frag_01");
GameProgress.Ensure().FragmentCount;               // 已收集数量
GameProgress.Ensure().MarkEventCleared("event_01");
GameProgress.Ensure().IsEventCleared("event_01");
GameProgress.FragmentTotal;                        // 总数（目前 6）

GameProgress.FragmentChanged += (id, n, total) => { /* 收到新碎片 */ };
GameProgress.EventCleared += id => { /* 事件第一次完成 */ };
```

### 场景进出

```csharp
ShelterFlow.BeginEvent("event_01", "你的场景名");   // 主动进事件（一般不用手动调）
ShelterFlow.ReturnToShelter();                      // 回第 0 层
ShelterFlow.CurrentEventId;                         // 当前事件 id（不在事件里为 null）
ShelterFlow.EventStarted / EventFinished;           // 进出事件的通知
```

### 对话 / 提示（避难所里的现成 UI）

```csharp
ShelterDialogue.Instance.Show("名字", new[] { "第一句", "第二句" });   // 打字机对话
ShelterDialogue.IsOpen;                                                 // 对话是否开着
ShelterDialogue.Instance.Close();

ShelterHUD.Instance.Toast("拾到了一片纸");          // 顶部一行小提示
ShelterHUD.Instance.SetPrompt("按 E 交互");         // 底部提示条
ShelterHUD.Instance.SetFragments(3, 6);             // 右上角计数
```

### 可交互物

继承 `InteractableBase`，实现 `Interact(ShelterPlayer player)` 就行：

```csharp
public class 一台机器 : InteractableBase
{
    public override void Interact(ShelterPlayer player)
    {
        ShelterDialogue.Instance.Show(displayName, new[] { "机器嗡了一声。" });
    }
}
```

要求：物体在 **Interactable 层（9）**，带 Trigger 碰撞体（尺寸决定玩家离多近能触发），`displayName` / `verb` 决定提示文字（「查看 展柜」）。现成的两个可以直接用：`DialogueProp`（对话）、`FragmentPickup`（碎片）。

## 四、场景里已经有的约定

| 东西 | 位置 | 说明 |
| --- | --- | --- |
| `GameProgress` | 场景根 | 跨场景进度，切场景自动保留 |
| `事件登记表（EventCatalog）` | `World/06_系统` | 事件 id → 场景名 |
| `场景状态（ShelterState）` | `World` | 事件完成后亮灯 / 暖墙 / 多一个人偶，`eventId` 决定听谁的事件 |
| `自测（ShelterSelfTest）` | `World/06_系统` | `-selftest` 才会跑 |
| 层 | `Ground = 8`（可站立）、`Interactable = 9`（可交互） | 玩家用 OverlapCircle 找 9 层 |
| 字体 | `Assets/Fonts/SimHei.ttf` | 本机测试用，发布前换可商用字体 |

## 五、注意

- **别在 `Shelter_L0` / `Event_01` / `Event_02` 里直接大改**：菜单 `工具 → 25小时 → 重建第0层` 会整个覆盖。要保留就另存场景，或者把东西做成 Prefab。
- 目前所有脚本都在全局命名空间，新类名别撞（`Shelter*` / `Event*` 我用得比较多，建议加个前缀）。
- 事件场景默认没有玩家和相机以外的逻辑；要做 2D 横版操作，可以复制 `04_角色/玩家_主控`，再给它加个相机。
- 自测 `-selftest` 会自动跑：下楼梯 → 看回声匣（只对话）→ 试锁着的事件二门 → 进事件一（找 `EventRoomBase` 调 `Complete()`）→ 事件二门解锁 → 进事件二 → 回 1F。你的事件只要继承 `EventRoomBase` 就能被一起验收。

## 六、验收

```powershell
"D:\Unity2D\Build\25Hours\25Hours.exe" -selftest -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

截图在 `D:\Unity2D\Build\25Hours\selftest\`，日志在 `%USERPROFILE%\AppData\LocalLow\Zhao\第25小时\Player.log`。
