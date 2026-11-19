# 参与开发

## 环境

- Unity `2022.3.62f3c1`（必须一致，跨版本会导致场景与材质序列化差异）
- 内置渲染管线，项目未使用 URP
- Windows + Visual Studio / Rider 均可

克隆后在 Unity Hub 中添加该目录并打开。

## 场景说明

| 场景 | 用途 | 维护者 |
| --- | --- | --- |
| `Assets/Scenes/Shelter_L0.unity` | 第 0 层避难所，白盒 | 白盒生成脚本 |
| `Assets/Scenes/Event_01.unity` | 事件一，空模板 | 事件 / 棋盘负责人 |
| `Assets/Scenes/Event_02.unity` | 事件二，空模板 | 事件 / 棋盘负责人 |

`Shelter_L0`、`Event_01`、`Event_02` 由编辑器脚本生成，重新生成会覆盖手改内容。需要长期保留的改动请另存为新场景，或修改生成脚本。

## 不要手改的部分

- `Assets/Art/`：只有一块由 `WhiteBoxArt.cs` 生成的白方块 `wb_block.png`，手改会在下次重建时丢失
- `Assets/Fonts/`：SimHei 为系统字体，不随仓库分发；重建时会自动从系统复制
- `ProjectSettings/TagManager.asset` 中的层：`8 = Ground`、`9 = Interactable`，新增层请从 10 起

## 白盒修改流程

1. 修改 `Assets/Editor/Level0Builder.cs`（物件位置 / 尺寸 / 颜色）
2. Unity 菜单 `工具 / 25小时 / 重建第0层（白盒）`
3. 运行自测确认流程正常（见下）
4. 提交时一并带上场景文件与 `docs/` 下的截图

如需在编辑器里临时调整，使用 `工具 / 25小时 / 白盒工具箱`（网格对齐、加平台、打开场景）。

## 自测

```powershell
& "D:\Unity2D\_install\2022.3.62f3c1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "D:\Unity2D\Projects\25Hours" `
  -executeMethod Level0Builder.BuildEverything -logFile build.log

& "D:\Unity2D\Build\25Hours\25Hours.exe" -selftest `
  -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

期望结果：`build.log` 含 `构建结果：Succeeded` 且无 `error CS`；自测截图 15 张，Player.log 中 B1 行走落点为地板高度、两个事件均可完成、门锁状态正确。

## 提交

- 提交信息用中文，格式 `模块: 做了什么`，例如 `Shelter: 修正楼梯落地判定`
- 一次提交只做一件事
- 不要提交 `Library/`、`Temp/`、`Logs/`、`*.csproj`、`*.sln`、字体文件（已在 `.gitignore`）

## 合并冲突

场景与预制体是 YAML，冲突时使用 Unity 的 Smart Merge：

```powershell
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver '"D:/Unity2D/_install/2022.3.62f3c1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %A %B %L %P'
```

在 `.gitattributes` 中已声明 `*.unity`、`*.prefab` 等为文本，按上述驱动合并。冲突后请在 Unity 中重新打开场景确认。

## 目录约定

- 运行时脚本：`Assets/Scripts/`，一个类一个文件，文件名与类名一致
- 编辑器脚本：`Assets/Editor/`，只能引用 UnityEditor
- 文档：`docs/`，截图放 `docs/preview`（静态预览）与 `docs/selftest`（自测流程）
