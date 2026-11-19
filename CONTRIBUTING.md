# 给队友：怎么一起改这个工程

这份文档写给被加成 collaborator 的同学。目标是：**clone 下来能跑、改了能推、别把别人手改的场景冲掉**。

---

## 0. 你需要什么

| 需要 | 说明 |
| --- | --- |
| Unity **2022.3.62f3c1** | 版本必须一致，差小版本也尽量一致；Hub 里装这个版本 |
| Git | Windows 装 Git for Windows 就行 |
| 权限 | 仓库 owner 会把你加成 collaborator（Settings → Collaborators） |

## 1. 拿到工程

```bash
git clone https://github.com/zjh01300031/25Hours.git
```

或者网页上 **Code → Download ZIP**（这样不能 push，只能自己玩）。

然后 Unity Hub → **Add** → 选 `25Hours` 这个文件夹。第一次打开会自动导入，大概一两分钟。

> `Library/`、`Temp/`、`Logs/`、`Build/` 都在 `.gitignore` 里，不会进仓库，也不会互相覆盖。

## 2. 打开哪几个场景

| 场景 | 用途 |
| --- | --- |
| `Assets/Scenes/Shelter_L0.unity` | **主场景**，第 0 层避难所（1F + 楼梯井 + B1） |
| `Assets/Scenes/Event_01.unity` | 事件一（现在只有模板说明） |
| `Assets/Scenes/Event_02.unity` | 事件二（现在只有模板说明） |

旧场景 `Board` / `LevelL1_1` / `MapBoard` / `SampleScene` 是更早的原型，**不要改也不要删**，先放着。

## 3. 三条不能碰的线

1. **`Assets/Art/` 是生成物**，别手改里面的 png，改了下次重建会被覆盖。要换美术请改 `Assets/Editor/WhiteBoxArt.cs`。
2. **菜单「工具 → 25小时 → 重建第0层（白盒）」会整个重写** `Shelter_L0` / `Event_01` / `Event_02` 三个场景。想保留手改的结果，请**先另存**成 `Shelter_L0_dev.unity` 之类的名字，或者只改不重建。
3. **中文字体不进仓库**：`Assets/Fonts/SimHei.ttf` 是 Windows 自带的黑体，版权上不适合再分发。生成器 `WhiteBoxArt.EnsureFont()` 会自动从 `C:\Windows\Fonts\simhei.ttf` 复制一份到工程里。别的系统没有这个文件的话，换一个 ttf 并改 `EnsureFont()` 里的路径。

## 4. 改东西的约定（避免互相踩）

- **一个场景一个人改**：Unity 的场景/预制体是 YAML 文本，两个人同时改同一个 `.unity` 基本一定冲突。要并行就分场景、分物体改，或者先商量谁改哪块。
- 提交前先 `git pull --rebase`，推之前再拉一次。
- 提交信息写清楚的动词开头：`改楼梯坡度`、`加一盏 B1 壁灯`、`修事件二门的贴图`。
- 大文件（视频、大图、压缩包）不要进仓库，仓库里已经有一个 31MB 的可运行包，再加就劝退了。

### 建议开启 Unity 的 Smart Merge（YAML 合并工具）

```ini
# C:\Users\<你>\.gitconfig
[merge]
	tool = unityyamlmerge
[mergetool "unityyamlmerge"]
	trustExitCode = false
	cmd = '"D:/Unity2D/_install/2022.3.62f3c1/Editor/Data/Tools/UnityYAMLMerge.exe"' merge -p "$BASE" "$REMOTE" "$LOCAL" "$MERGED"
```

```bash
git config --global merge.tool unityyamlmerge
```

外加把 `*.unity merge=unityyamlmerge`、`*.prefab merge=unityyamlmerge` 写进 `.gitattributes`（仓库里已有这份文件，需要的话直接加两行）。

## 5. 代码在哪

| 目录 | 内容 |
| --- | --- |
| `Assets/Scripts/` | 运行时脚本：玩家、交互、事件门、进度、对话、HUD、自测 |
| `Assets/Editor/` | 编辑器脚本：白盒生成器（`Level0Builder`）、占位图生成（`WhiteBoxArt`）、UI 工具（`WhiteBoxUI`）、工具箱窗口（`WhiteBoxWindow`） |

接事件/棋盘请看 [`README_事件接入.md`](README_事件接入.md)；白盒怎么微调看 [`README_第0层白盒.md`](README_第0层白盒.md)。

## 6. 自测一下再推

```powershell
# 重新生成场景 + 预览 + Windows 包
Unity.exe -batchmode -quit -projectPath <工程路径> -executeMethod Level0Builder.BuildEverything -logFile build.log

# 或者直接跑打包版自测
25Hours.exe -selftest -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

日志里出现 `error CS` 就说明有编译错误；自测会在 `%USERPROFILE%\AppData\LocalLow\Zhao\第25小时\Player.log` 留一份记录。

## 7. 推上去

```bash
git add -A
git commit -m "改了什么"
git push
```

没有 push 权限就是还没被加成 collaborator，找 owner 加一下。
