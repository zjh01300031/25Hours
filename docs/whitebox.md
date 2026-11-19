# 第 0 层白盒：结构与调整说明

本文记录 `Shelter_L0` 的构建方式、坐标与可交互物，以及不用改代码就能做的调整。

## 1. 场景概况

单张横版场景，分两层空间，中间由楼梯井连接。

| 区域 | X 范围 | 地面 / 顶板 | 说明 |
| --- | --- | --- | --- |
| 1F | 2 → 17 | 地面 y = 0，顶棚 y = 7.5 | 主要活动区，东墙为门 · 事件一 |
| 楼梯井 | -3 → 2 | — | 20 级台阶，从 y = 0 降到 y = -8 |
| B1 | -17 → -3 | 地面 y = -8，顶板 y = -0.8 | 西南角为门 · 事件二 |

- 出生点：`(2.6, 0.76)`，紧邻楼梯口
- 相机：正交，`orthographicSize = 4.6`；跟随并对坐标做夹取，范围 `(-8, -6.4)` → `(8, 2.4)`
- 玩家：`moveSpeed = 5`、`jumpForce = 12.5`、`gravityScale = 3.5`、`stepHeight = 0.55`

## 2. 楼梯

台阶只是视觉表现，站立面是一条不可见的斜坡碰撞体（`01_建筑/楼梯/楼梯_斜坡碰撞`），从 `(2, 0)` 连到 `(-3, -8)`，倾角约 58°。

- 斜坡上关闭重力，速度按坡面方向给，上下楼一致
- 判定地面用脚底 BoxCast，因此在斜坡上也能起跳
- 台阶本身不参与碰撞，角度调整只需修改该物体的位置、旋转与长度

## 3. 可交互物

坐标为物体中心点，尺寸为触发区大小。

| 名称 | 类型 | 位置 (x, y) | 尺寸 | ID / 说明 |
| --- | --- | --- | --- | --- |
| 展柜 | DialogueProp | 4.4, 1.5 | 1.3 × 2.4 | 事件一完成后多一个人偶 |
| 纸箱 | FragmentPickup | 5.6, 0.5 | 1.0 × 1.0 | frag_01 |
| 柜子 | FragmentPickup | 7.6, 0.9 | 1.1 × 1.8 | frag_02 |
| 回声匣 | DialogueProp | 10.2, 1.2 | 0.55 × 0.55 | 仅对话，不进入事件 |
| 门 · 事件一 | EventDoor | 16.2, 1.35 | 1.4 × 2.7 | event_01，无前置条件 |
| 人偶（1F） | DialogueProp | 12.8, 0.62 | 0.7 × 1.2 | — |
| 书架 | FragmentPickup | 14.6, 0.6 | 1.4 × 1.2 | frag_03 |
| 玻璃罐 | FragmentPickup | 14.6, 1.4 | 0.4 × 0.4 | frag_04 |
| 门 · 事件二 | EventDoor | -4.0, -6.6 | 1.2 × 2.8 | event_02，前置 event_01 |
| 抽屉柜 | FragmentPickup | -7.6, -7.1 | 1.3 × 1.8 | frag_06 |
| 人偶（B1） | DialogueProp | -9.8, -7.4 | 0.7 × 1.2 | — |
| 旧收音机 | FragmentPickup | -12.2, -7.55 | 0.9 × 0.7 | frag_05 |

家具与可交互物只有 Trigger 碰撞，不阻挡移动；只有 `01_建筑` 是实心。装饰物（B1 木箱、线缆盘、门框、墙裙等）也不阻挡移动。

## 4. 生成脚本

| 文件 | 作用 |
| --- | --- |
| `Assets/Editor/Level0Builder.cs` | 生成三个场景、Build Settings、预览图、Windows 包 |
| `Assets/Editor/WhiteBoxArt.cs` | 生成全部占位贴图，并按 PPU 16 / Point / 无压缩导入 |
| `Assets/Editor/WhiteBoxUI.cs` | 代码构建 Canvas、面板、文字、按钮 |
| `Assets/Editor/WhiteBoxWindow.cs` | 白盒工具箱窗口 |

菜单：

- `工具 / 25小时 / 重建第0层（白盒）` — 贴图 + 三个场景 + Build Settings
- `工具 / 25小时 / 白盒工具箱` — 打开场景、只出预览图、网格对齐、按尺寸加平台
- `工具 / 25小时 / 一键：重建 + 截图 + 打包` — 上述全部，输出 `D:/Unity2D/Build/25Hours`

生成的内容会覆盖同名场景，手改前请先另存。

## 5. 手动调整

- 移动、缩放家具：直接在场景里拖；只有 `01_建筑` 里的物体影响通行
- 加一块可站立的地面：工具箱 →「在选中物体位置加一块平台」，自动放到 `Ground` 层
- 对齐：工具箱 → 网格大小 +「把选中物体对齐到网格」
- 换贴图：`Assets/Art` 下同名文件直接替换；新增贴图后需要把 Import 设置改成 Sprite、PPU 16、Point、No Compression

## 6. 自测

```powershell
.\25Hours.exe -selftest -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

依次执行：出生 → 下楼 → B1 行走 → 回声匣 → 事件二（锁态）→ 事件一 → 完成 → 事件二（解锁）→ 完成 → 展柜。截图输出到 `D:/Unity2D/Build/25Hours/selftest`，坐标写入 Player.log。加 `-fps` 可同时记录帧率。

## 7. 性能与手感

- `AppBoot` 在启动时设 `vSyncCount = 1`、`fixedDeltaTime = 1/60`
- 玩家刚体使用插值 + 连续碰撞检测
- HUD 提示只在文字变化时更新，避免每帧重建 UGUI 文字网格
- 相机平滑时间 0.08s

## 8. 字体

中文 UI 使用系统字体 SimHei。仓库不包含字体文件，`WhiteBoxArt.EnsureFont` 会在重建时从 `C:\Windows\Fonts\simhei.ttf` 复制一份到 `Assets/Fonts`。更换字体时同步修改该方法的源路径与 `WhiteBoxUI.Font` 的加载路径。
