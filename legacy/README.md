# 旧原型归档（2024-11）

这里存放第 0 层白盒之前的探索版本，已从 `Assets/` 移出，不再参与编译与构建，仅作留档。

| 文件 | 说明 |
| --- | --- |
| `LevelBuilder.cs` | 旧菜单 `Tools/25Hours/Build L1-1 Scene`，生成 `LevelL1_1.unity` 并打包 L1-1 |
| `PrototypeBuilder.cs` | 旧棋盘原型生成器（`TwentyFiveHours.cs`） |
| `LevelL1_1.cs` | L1-1 棋盘原型逻辑 |
| `TwentyFiveHours.cs` | 3×3 棋盘点亮原型 |
| `Board.unity` / `MapBoard.unity` / `LevelL1_1.unity` / `SampleScene.unity` | 对应场景 |

这些文件保留了早期的棋盘规则探索（点亮的形状、技能组合），规则整理见项目外的关卡设计文档。若需要恢复，连 `.meta` 一起移回 `Assets/` 对应目录，GUID 保持不变即可重新关联。

注意：`LevelBuilder.cs` / `PrototypeBuilder.cs` 依赖的旧场景已不参与构建，直接移回可能编译失败，建议先确认依赖。