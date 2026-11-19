# 事件与棋盘玩法接入

第 0 层通过两扇门进入两个事件场景。事件场景由玩法负责人实现，本仓库只提供空模板与接入约定。

## 1. 现有结构

| 位置 | 内容 |
| --- | --- |
| `Shelter_L0` | 第 0 层；`06_系统/事件登记表` 存有事件 id → 场景名 |
| `Event_01` | 事件一场景，空房间 + `EventRoomTemplate` |
| `Event_02` | 事件二场景，空房间 + `EventRoomTemplate` |
| `Assets/Scripts/EventDoor.cs` | 门：进入事件、按前置事件解锁、切换外观 |
| `Assets/Scripts/EventRoomBase.cs` | 事件场景基类，负责完成与返回 |
| `Assets/Scripts/EventCatalog.cs` | 事件登记表（id、显示名、场景名） |
| `Assets/Scripts/ShelterFlow.cs` | 进入 / 返回事件场景，记录返回位置 |

事件二的门上 `requiresEventId = event_01`，事件一完成前保持锁定外观；完成后自动换门贴图与光色。

## 2. 新增一个事件

1. 复制 `Assets/Scenes/Event_01.unity`，改名（例如 `Event_03.unity`），保留一个挂 `EventRoomBase` 子类的物体
2. 在场景中实现玩法，结束时调用：

```csharp
public class Event03Board : EventRoomBase
{
    protected override void Start() { base.Start(); }

    void 解谜成功() => Complete();   // 记录完成并返回第 0 层
    void 玩家退出() => Leave();       // 返回，但不记录完成
}
```

3. 场景加入 `Build Settings`（`File / Build Settings / Scenes In Build`）
4. 在第 0 层 `06_系统/事件登记表` 中新增一条：id、显示名、场景名
5. 在 `03_可交互` 下复制一扇门，设置 `eventId`；需要前置条件时填 `requiresEventId`

## 3. API

```csharp
// 进度（场景切换不丢失，DontDestroyOnLoad）
GameProgress.Ensure().IsEventCleared("event_01");
GameProgress.Ensure().MarkEventCleared("event_01");
GameProgress.Ensure().AddFragment("frag_07");   // 碎片上限 FragmentTotal = 6

// 事件场景进出
ShelterFlow.BeginEvent("event_01", "Event_01");
ShelterFlow.ReturnToShelter();
ShelterFlow.CurrentEventId;

// 对话与提示（不依赖事件入口）
ShelterDialogue.Instance.Show("标题", new[] { "第一行", "第二行" });
ShelterDialogue.IsOpen;
ShelterHUD.Instance.Toast("提示");
```

`EventRoomBase` 提供的成员：

| 成员 | 说明 |
| --- | --- |
| `EventId` | 当前事件 id，由门在进入时写入 |
| `Start()` | 虚方法，子类重写时先调用 `base.Start()` |
| `OnEnter()` | 进入场景时的钩子 |
| `Complete()` | 记录完成，返回第 0 层 |
| `Leave()` | 返回第 0 层，不记录完成 |

## 4. 约定

- 物理层：`Ground = 8`（可站立）、`Interactable = 9`（可交互）
- 事件 id 用小写加下划线：`event_01`、`event_02`
- 事件场景不要自带第 0 层的 UI；需要提示时用 `EventRoomBase` 自己的 Canvas
- 返回位置由 `ShelterFlow.RememberShelterPosition` 记录，事件场景不需要关心
- 不要在事件场景里放 `DontDestroyOnLoad` 的物体，`GameProgress` 已经承担跨场景状态

## 5. 注意事项

- 重建（`工具 / 25小时 / 一键：重建 + 截图 + 打包`）会覆盖 `Shelter_L0`、`Event_01`、`Event_02`。自建场景请用新名字，不要复用这三个
- 场景未加入 Build Settings 时，门会提示「（这扇门后面还没接事件）」
- 帧率与手感相关设置集中在 `Assets/Scripts/AppBoot.cs`，不要在事件场景里重复设置
