# 第0层白盒说明

第 0 层只是一个能跑通「上下楼 + 进事件 + 解锁」的框架。房间怎么设计、放什么，不在这里决定。

## 场景与分组

`Assets/Scenes/Shelter_L0.unity`

| 分组 | 内容 |
| --- | --- |
| `01_结构` | 1F 地板 / 右墙，楼梯（13 级台阶 + 隐形斜坡），B1 地板 / 左墙 |
| `02_角色` | 玩家_主控（Rigidbody2D + BoxCollider2D + `ShelterPlayer` + `ShelterInteractor`） |
| `03_门` | 门·事件一、门·事件二（各带 `图` 与 `光` 两个子物体） |
| `04_系统` | GameProgress、AppBoot、事件登记表（EventCatalog）、自测（ShelterSelfTest） |
| `UI` | 标题、操作提示、提示条、toast、对话框 |

## 坐标

| 物件 | 位置 | 尺寸 |
| --- | --- | --- |
| 1F 地板 | `(5, -0.4)` | `22 × 0.8`（顶面 y = 0） |
| 1F 右墙 | `(16.3, 4.1)` | `0.6 × 9` |
| B1 地板 | `(-16.5, -6.4)` | `11 × 0.8`（顶面 y = -6） |
| B1 左墙 | `(-22.3, -2.1)` | `0.6 × 8.6` |
| 台阶 | 从 `(-6, 0)` 到 `(-11, -6)`，13 级 | 每级 `≈0.38 × 0.46`，无碰撞 |
| 楼梯_斜坡碰撞 | 两点的中点 | 长 `√(5² + 6²)`、厚 `0.3`，旋转 `atan2(-6, -5)` |
| 玩家出生点 | `(0, 1)` | 碰撞盒 `0.8 × 1.45` |
| 门·事件一 | `(15, 1.3)` | `1.2 × 2.6`，`eventId = event_01` |
| 门·事件二 | `(-21, -4.7)` | `1.2 × 2.6`，`eventId = event_02`，`requiresEventId = event_01` |

1F 左边不砌墙：走到 x = -6 就是楼梯口，斜坡从那里起步。B1 走到 x = -22 被墙挡住。

## 层与碰撞

| Layer | 用途 |
| --- | --- |
| `Ground`(8) | 地板、墙、斜坡碰撞 |
| `Interactable`(9) | 门的触发器（`CircleCollider2D`，半径 1.0） |

只有地板、墙、斜坡是实体；台阶只是视觉，玩家靠斜坡上下。玩家脚底用 `BoxCast` 判地面，斜坡上按坡面方向给速度，所以在 58° 的隐形斜坡上也能正常起跳。

## 生成脚本

| 文件 | 作用 |
| --- | --- |
| `Assets/Editor/WhiteBoxArt.cs` | 生成 `Assets/Whitebox/white.png`（8×8 纯白）+ 从系统复制中文字体 |
| `Assets/Editor/WhiteBoxUI.cs` | Canvas、面板、文字、按钮 |
| `Assets/Editor/Level0Builder.cs` | 生成三个场景、Build Settings、预览截图、Windows 包 |

菜单 `工具 / 25小时`：

- 重建第0层（框架）
- 打开第0层并运行
- 重建并截图
- 一键：重建 + 截图 + 打包
- 截图预览（世界）
- 构建 Windows 试玩版

## 手动调整

- 房间大小：改 `Level0Builder.BuildStructure` 里的 `Box(...)`，或者直接在 Scene 里拖
- 楼梯坡度：改 `StairTopX` / `StairBottomX` 常量后重建；斜坡碰撞体会跟着重算
- 门的位置：Scene 里拖 `门·事件一` / `门·事件二`，或在生成器 `BuildDoor(...)` 里改坐标
- 出生点：`玩家_主控` 上的 `ShelterPlayer.spawnPoint`
- 相机范围：`Main Camera` 上的 `CameraRig2D.min` / `max`

手改之后不要再点「重建」，重建会用生成器的内容覆盖场景。

## 自测

```powershell
25Hours.exe -selftest -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

流程：出生 → 平地跳 → 斜坡跳 → 下楼梯到 B1 → 试事件二的门（锁着）→ 进事件一 → 完成回避难所 → 事件二门解锁 → 进事件二 → 完成。11 张图输出到 `D:/Unity2D/Build/25Hours/selftest`。

## 性能

- 物理固定步长 `1/60`（`AppBoot`）
- 提示文字只在变化时更新，避免 UGUI 每帧重建
- 相机 `smoothTime = 0.08`

## 字体

`Assets/Fonts/SimHei.ttf` 不进仓库，构建时从 `C:\Windows\Fonts\simhei.ttf` 复制。换字体改 `WhiteBoxArt.FontAssetPath`。
