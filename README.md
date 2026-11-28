# 第 25 小时

Unity 2D 横版 + 俯视混合玩法的白盒工程：避难所一层/二层、事件探索区、棋盘解谜（控制器 / 技能 / 覆盖率 / 通关判定）。

## 打开

1. Unity **2022.3.62f3c1**，Unity Hub → Add → 选本仓库的 `25Hours` 这一层（不是仓库根目录）。
2. 打开场景 `25Hours/Assets/Scenes/MainMenu.unity`，按 ▶。
3. 也可以直接打开 `Shelter_L0` / `Event_01` / `Board_01` 单独试。

> `25Hours/Assets/Fonts/SimHei.ttf` 不进仓库（Windows 自带字体）。首次打开若中文显示成方块，跑一次菜单 `工具 / 25小时 / 重建场景骨架` 或手动把 `C:\Windows\Fonts\simhei.ttf` 复制到 `Assets/Fonts/`。

## 按键

| 场景 | 按键 |
| --- | --- |
| 避难所（横版） | A / D 移动、空格 交互、Q 背包 |
| 探索区（俯视） | A / D 移动（默认不含 W/S）、空格 交互 |
| 棋盘 | WASD 移动（换方向走 1 格 = 1 次行动，同向连走免费）、1-6 选技能、←→↑↓ 翻转形状、空格 释放、Z 撤销、R 重置、Esc 退出、Tab 直接通关（调试） |
| 任意 | Esc 关闭面板 |

## 场景

| 场景 | 说明 |
| --- | --- |
| `MainMenu` | 开始 / 继续 / 设置（音乐、音效、关于）/ 退出 |
| `Shelter_L0` | 避难所一层：收集物、收集检查点、NPC、通往二层 |
| `Shelter_2F` | 避难所二层（空房间，等人摆） |
| `Event_01` | 事件一：俯视探索区 + 五个棋盘入口（投人偶进入） |
| `Event_02` | 事件二：俯视探索区（棋盘等设计稿，先放了占位事件点） |
| `Board_01..05` | 五块棋盘，数据来自 `Assets/Resources/Board/board_0X.json` |

## 结构

```
25Hours/
  Assets/
    Board/         棋盘规则（PuzzleBoard 纯 C#）与控制器
    Story/         文本、收集物、收集检查点、NPC 多选项、背包
    Menu/          主菜单与音量
    Scripts/       玩家、交互、流程、场景转场、提示
    Editor/        白盒生成器与测试菜单（工具 / 25小时）
    Prefabs/       玩家、事件门、拾取物、收集物、对话 NPC、棋盘入口
    Resources/     棋盘 JSON、字体
    Scenes/
  Packages/
  ProjectSettings/
```

## 自测

命令行（需关掉编辑器）：

```
Unity.exe -batchmode -quit -projectPath 25Hours -executeMethod BoardLogicTest.RunTests -logFile run.log
```

输出 `[棋盘自测] 通过 N 项，失败 0 项`。

## 已知限制

- 美术全部是占位（内置九宫格图 + 立绘 `player.png` / `npc.png`）。
- 事件二的棋盘等设计稿；`Event_02` 里是占位事件点（走近按空格即可完成）。
- 棋盘 5 的镜像格规则已按设计稿实现（`mirrors` 180° 点对称）。
- 存档默认只在本次运行内有效（`GameProgress.persistProgress` 勾上才写 `progress.json`）。
