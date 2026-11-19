# 第0层骨架说明

`Assets/Scenes/Shelter_L0.unity` 里只有功能，没有任何关卡内容。地面、墙、楼梯、门、物品都由项目里的人自己搭。

## 场景里有什么

| 分组 / 物体 | 内容 |
| --- | --- |
| `Main Camera` | 正交相机 + `CameraRig2D`（跟随 `target`，带边界 `min/max`） |
| `GameProgress` | 进度、背包、存档（`DontDestroyOnLoad`） |
| `AppBoot` | 启动设置：`vSyncCount`、`targetFrameRate`、`fixedDeltaTime = 1/60` |
| `World/01_玩家` | `玩家_主控`（`Rigidbody2D` + `BoxCollider2D` + `ShelterPlayer` + `ShelterInteractor`） |
| `World/02_系统` | `事件登记表`（`EventCatalog`，id → 场景名） |
| `UI` | 标题、操作提示、提示条、toast、对话框（`ShelterHUD` + `ShelterDialogue`） |

相机默认 `orthographicSize 5`、跟随范围 `min(-22,-8.5)`、`max(16,4)`，按关卡大小改。

## 预制体

`Assets/Prefabs/`：

| 预制体 | 组成 | 拖进场景后要改的字段 |
| --- | --- | --- |
| `玩家` | `Rigidbody2D`、`BoxCollider2D`、`ShelterPlayer`、`ShelterInteractor`、`GroundCheck` 子物体 | `spawnPoint`、`groundMask`（默认 `Ground`） |
| `事件门` | `EventDoor` + `图`/`光` 两个子物体 + `CircleCollider2D`（`Is Trigger`, 半径 1.0） | `displayName`、`eventId`、`requiresEventId`、三个 storyId |
| `拾取物` | `PickupItem` + `图` 子物体 + `CircleCollider2D`（`Is Trigger`, 半径 0.9） | `displayName`、`itemId`、`storyId`、`amount` |

想有画面就给子物体 `图` 换成正式 Sprite；想自己搭就用空物体 + `SpriteRenderer` + 碰撞体。

## 层与碰撞

| Layer | 用途 |
| --- | --- |
| `Ground`(8) | 地面、墙、斜坡 —— `ShelterPlayer.groundMask` 只认这一层 |
| `Interactable`(9) | 门、物品 —— `ShelterInteractor.interactableMask` 只认这一层 |

玩家的交互是「自己周围 `radius` 内的最近一个 `Interactable`」，所以可交互物只需要触发器，不需要阻挡。

## 生成器菜单（`工具 / 25小时`）

| 菜单项 | 作用 |
| --- | --- |
| 重建场景骨架（只有功能） | 重置 `Shelter_L0` / `Event_01` / `Event_02`，手搭的内容会被覆盖 |
| 导出预制体（玩家 / 事件门 / 拾取物） | 重新生成 `Assets/Prefabs` |
| 打开第0层并运行 | 打开场景并进 Play |
| 重建并截图 | 生成一张骨架截图到 `D:/Unity2D/Build/25Hours/preview` |
| 一键：重建 + 截图 + 打包 | 上面三步 + 打 Windows 包 |
| 构建 Windows 试玩版 | 只打包 |
| 检查文案 | 遍历场景里的 storyId，报告 `story.csv` 里缺哪些 |

## 手搭关卡的顺序

1. 先决定这一层几层、怎么上下，再铺 `Ground` 层的地面和墙
2. 玩家出生点放在地面上方一点（预制体的 `spawnPoint` 与 `transform.position` 保持一致）
3. 楼梯可以用斜坡碰撞体（`BoxCollider2D` 旋转）代替逐级台阶，`ShelterPlayer` 会自动沿坡走
4. 门和物品用预制体，改字段；文案 id 先在 `story.csv` 里写好，再点「检查文案」
5. 相机 `CameraRig2D.min/max` 按关卡范围调，避免镜头拍到空白

## 文案与存档

- 台词全在 `Assets/Resources/Story/story.csv`（`id,speaker,text`，同 id 多行 = 多句）；读取逻辑在 `Assets/Scripts/Story/StoryDatabase.cs`（类名 `Story`）
- 门用 `enterStoryId / lockedStoryId / clearedStoryId`，物品用 `storyId`；文本为空时跳过，整段没有文本就回落到 toast
- 存档 `%USERPROFILE%\AppData\LocalLow\Zhao\第25小时\progress.json`（碎片、已完成事件、背包）
- 菜单 `工具 / 25小时 / 检查文案` 报告缺失的 id

## 字体

`Assets/Fonts/SimHei.ttf` 不进仓库：生成器会从 `C:\Windows\Fonts\simhei.ttf` 复制。没有这个文件时，界面中文会显示成方块。
