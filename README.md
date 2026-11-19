# 第25小时 · 第0层功能框架

《第25小时》第 0 层的功能骨架。

**这个仓库里只有功能，没有关卡内容**：地面、墙、楼梯、门、物品都不预置，由项目里的人自己在 Unity 里搭。代码负责的是「走、跳、交互、说台词、存进度、进事件、解锁」。

- Unity 版本：`2022.3.62f3c1`
- 目标平台：Windows（Standalone）
- 场景：`Shelter_L0`（骨架）、`Event_01`、`Event_02`（空房间模板）

## 仓库里有什么

| 位置 | 内容 |
| --- | --- |
| `Assets/Scripts/` | 移动、交互、对话、进度存档、事件流转 |
| `Assets/Scripts/Story/` | 文案表读取、可拾取物、纯对话物件 |
| `Assets/Editor/` | 生成骨架场景 / 导出预制体的编辑器脚本 |
| `Assets/Prefabs/` | `玩家`、`事件门`、`拾取物` —— 拖进自己的场景就能用 |
| `Assets/Resources/Story/story.csv` | 所有台词 |
| `Assets/Scenes/` | `Shelter_L0` 骨架 + 两个空事件房间 |
| `docs/` | 骨架说明、事件接入说明 |

## 快速开始

```powershell
git clone git@github.com:zjh01300031/25Hours.git
```

1. Unity Hub 添加仓库目录，用 Unity `2022.3.62f3c1` 打开
2. 打开 `Assets/Scenes/Shelter_L0.unity`
3. 用 `Assets/Prefabs/玩家` 或空物体自己搭地面（`Layer` 设 `Ground`，加 `BoxCollider2D`）
4. Play

中文字体不进仓库：首次生成时 `Assets/Fonts/SimHei.ttf` 会从 `C:\Windows\Fonts\simhei.ttf` 自动复制；没有字体时 UI 中文会显示成方块。


> 第一次用 Unity？看 [`docs/Unity 入门手册.pdf`](docs/Unity%20入门手册.pdf)（同目录还有 `.docx` 和 Markdown 版 `unity-guide.md`），从「摆一块地面」开始一步步教。
## 操作

| 按键 | 作用 |
| --- | --- |
| `A` / `D` | 左右移动（斜坡自动贴着走） |
| `空格` | 跳跃（空中不可二段跳，落地有土狼时间） |
| `E` | 与附近的门 / 物品交互 |
| `Esc` | 关闭提示 |

手感数值都在 `玩家` 的 `ShelterPlayer` 组件上：`moveSpeed 5`、`jumpForce 12.5`、`gravityScale 3.5`、`stepHeight 0.55`、`coyoteTime 0.08`、`jumpBufferTime 0.1`。

## 自己搭关卡

场景里现在只有这些：

```
Main Camera（带 CameraRig2D 跟随）
GameProgress / AppBoot
World
  01_玩家   玩家_主控
  02_系统   事件登记表（EventCatalog，id → 场景名）
UI（标题 / 操作提示 / 提示条 / toast / 对话框）
```

要加东西，两条路：

- **拖预制体**：`玩家`、`事件门`、`拾取物`。门和拾取物拖进来后改 `eventId` / `itemId` / `storyId` 就行
- **自己搭**：空物体 + `SpriteRenderer`（`Assets/Whitebox/white.png` + 颜色）+ 碰撞体；地面和墙的 `Layer` 必须是 `Ground`(8)，可交互物的 `Layer` 必须是 `Interactable`(9)

> 菜单 `工具 / 25小时 / 重建场景骨架（只有功能）` 会把 `Shelter_L0` 重置成上面这个骨架，手搭的内容会被覆盖。搭之前先确认不需要再重建。

## 交互怎么用

三种物件，脚本挂在 `Interactable` 层、带 `Is Trigger` 的 `CircleCollider2D`：

| 脚本 | 用途 | 关键字段 |
| --- | --- | --- |
| `EventDoor` | 进事件的门 | `eventId`、`requiresEventId`（前置事件）、`enterStoryId / lockedStoryId / clearedStoryId` |
| `PickupItem` | 可拾取物 | `itemId`（存档键）、`storyId`、`amount` |
| `StoryInteractable` | 只看不拿的物件 | `storyId` |

`E` 交互、靠近自动出提示条；有文案就弹对话框，没文案只给 toast。

## 文案放在哪

所有台词都在 `Assets/Resources/Story/story.csv`，一行一句，同一个 id 写多行就是多句：

```csv
id,speaker,text
door01.enter,门·事件一,（示例文案）东边这扇门通向事件一。
door02.locked,门·事件二,（示例文案）门锁着，先在事件一里把那摊事做完。
```

- `text` 留空的句子会被跳过；整段 id 都没文本时，交互自动回落到功能反馈（toast）
- 改完用 `工具 / 25小时 / 检查文案` 查有没有漏 id
- 存档在 `%USERPROFILE%\AppData\LocalLow\Zhao\第25小时\progress.json`（碎片、已完成事件、背包）

## 事件怎么接进来

事件场景里挂一个继承 `EventRoomBase` 的脚本，玩法完成时调 `Complete()`；中途退出调 `Leave()`。

```csharp
public class MyEventRoom : EventRoomBase
{
    void Update()
    {
        if (/* 棋盘全部点亮 */) Complete();   // 记录完成 + 回到避难所
    }
}
```

加一个新事件：

1. 复制 `Assets/Scenes/Event_01.unity` 改名，挂上自己的 `EventRoomBase` 子类
2. 场景加入 Build Settings
3. 在 `Shelter_L0` 的 `事件登记表` 里加一条 `id → 场景名`，门的 `eventId` 填同一个 id；有前置条件就填 `requiresEventId`

进度、背包在 `GameProgress`（`DontDestroyOnLoad`，跨场景保留，自动写盘）；场景切换用 `ShelterFlow`。详细 API 见 [docs/event-integration.md](docs/event-integration.md)。

## 生成器菜单

| 菜单项 | 作用 |
| --- | --- |
| 重建场景骨架（只有功能） | 重置 `Shelter_L0` / `Event_01` / `Event_02` |
| 导出预制体（玩家 / 事件门 / 拾取物） | 重新生成 `Assets/Prefabs` |
| 打开第0层并运行 | 打开场景并进 Play |
| 重建并截图 | 生成一张骨架截图到 `D:/Unity2D/Build/25Hours/preview` |
| 一键：重建 + 截图 + 打包 | 上面三步 + 打 Windows 包 |
| 构建 Windows 试玩版 | 只打包 |
| 检查文案 | 报告 `story.csv` 里缺哪些 id |

## 仓库结构

```
Assets/
  Editor/          Level0Builder（骨架 + 预制体）、WhiteBoxArt（白方块 + 字体）、WhiteBoxUI
  Scripts/         ShelterPlayer、ShelterInteractor、ShelterDialogue、ShelterHUD、CameraRig2D
  Scripts/Story/   StoryDatabase（读 CSV）、StoryInteractable、PickupItem
  Scripts/         GameProgress、ShelterFlow、EventCatalog、EventDoor、EventRoomBase…
  Prefabs/         玩家、事件门、拾取物
  Resources/Story/ story.csv
  Scenes/          Shelter_L0、Event_01、Event_02
  Whitebox/        white.png —— 8×8 纯白贴图，界面与物件共用
  Fonts/           字体（.gitignore 里，生成时自动补齐）
docs/              骨架说明、事件接入说明
dist/              打包产物
legacy/            2024-11 旧原型，仅存档
```

## 现状与限制

- **没有关卡内容**：地面、墙、楼梯、门、物品都由项目里的人搭，仓库只给预制体和脚本
- **没有美术**：可用素材只有一块白方块，正式美术由美术出图后替换
- **事件场景是空房间**：棋盘 / 玩法由事件负责人接入
- **台词是占位文案**：在 `story.csv` 里，正式文本直接替换

## 贡献

见 [CONTRIBUTING.md](CONTRIBUTING.md)。重建会覆盖三个场景，改场景前先看「谁维护哪个场景」。

## 参考

- [The Level Design Book · Blockout](https://book.leveldesignbook.com/process/blockout)
- [The Level Design Book · How to make a level](https://book.leveldesignbook.com/process/overview)

## 许可

代码部分可自由使用；字体为 Windows 系统自带，不随仓库分发。
