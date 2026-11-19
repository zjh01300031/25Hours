# Unity 入门手册（配合本工程）

写给第一次打开 Unity 的人。照着做一遍，能自己把第0层搭出来、跑起来、打包发出去。

工程路径：`D:\Unity2D\Projects\25Hours`　Unity 版本：`2022.3.62f3c1`（版本不一致会提示升级，选「继续」也能开，但尽量用同版本）

---

## 一、第一次打开

1. 打开 Unity Hub → `Projects` → 找到 `25Hours` → 点一下。
2. 如果 Hub 里没有，点 `Add` → 选 `D:\Unity2D\Projects\25Hours` 这个文件夹。
3. 第一次打开要等一会儿（导入资源）。下方进度条走完、`Console` 没有红色报错就行。
4. 场景不对的话，在 `Project` 窗口里双击 `Assets/Scenes/Shelter_L0.unity`。

> 编辑器里打开的场景和磁盘上的 `.unity` 文件是两回事：你改的东西要按 `Ctrl + S` 才会写回磁盘。

---

## 二、界面认一认

| 面板 | 干什么 |
| --- | --- |
| `Hierarchy`（左） | 当前场景里有哪些物体，父子关系 |
| `Scene`（中） | 3D/2D 视图，摆东西的地方 |
| `Game`（中，和 Scene 切换） | 运行时玩家看到的画面 |
| `Inspector`（右） | 选中物体的所有属性（位置、碰撞体、脚本字段） |
| `Project`（下） | 磁盘上的资源：场景、脚本、预制体、图片 |
| `Console`（下，和 Project 切换） | 报错和日志，**红了先看这里** |

`Window → General → Console` 可以把 Console 单独拖出来，方便一直看。

---

## 三、视图操作（Scene 窗口）

| 操作 | 快捷键 / 鼠标 |
| --- | --- |
| 平移视图 | 按住鼠标中键拖，或 `Shift + 中键` |
| 缩放 | 滚轮 |
| 框选物体 | 左键拖 |
| 选中物体 | 左键点；`F` 键把镜头对准选中物体 |
| 2D/3D 切换 | 视图右上角的 `2D` 按钮，做横版就固定在 2D |
| 移动 / 旋转 / 缩放工具 | `W` / `E` / `R` |
| 对齐网格 | 按住 `Ctrl` 拖动会吸附（默认 0.25） |

---

## 四、摆第一个东西：一块地面

目标：一块能让玩家站住的地。

1. `Hierarchy` 空白处右键 → `2D Object → Sprites → Square`，命名为 `地面`。
2. `Inspector` 里把 `Transform → Position` 设成 `(0, 0, 0)`，`Scale` 设成 `(20, 1, 1)`。
3. 点 `Add Component`，搜 `Box Collider 2D`，加上。
4. `Inspector` 顶部 `Layer` 下拉 → `Add Layer...` → 在 `User Layer 8` 填 `Ground` → 回到物体把 `Layer` 选成 `Ground`。

> **为什么一定要设 `Ground`**：玩家的 `ShelterPlayer.groundMask` 只认 `Ground` 这一层，别的层它当空气，会直接掉下去。

5. 想让地面看不见但能站人：把 `Sprite Renderer` 组件删掉就行，碰撞体和层留着。

---

## 五、摆玩家

1. 从 `Assets/Prefabs` 把 `玩家.prefab` 拖进 `Hierarchy`。
2. 选中它，`Transform → Position` 设成 `(0, 2, 0)`（在地面上方一点，别嵌进地面）。
3. `Inspector` 里 `Shelter Player` 组件的 `Spawn Point` 也填 `(0, 2, 0)`（这两个要一致，不然重开场景会跑回原位）。
4. 相机：选中 `Main Camera`，`Camera Rig 2D` 组件里 `Target` 拖玩家进去；`Min / Max` 是镜头能到的边界，按你的关卡范围改。

按 `Ctrl + S` 保存场景，然后点编辑器上方 ▶（或 `Ctrl + P`）进 Play 模式试试：`A/D` 左右走，`空格` 跳。

---

## 六、摆放可交互物

### 6.1 物品（捡起来）

1. 拖 `Assets/Prefabs/拾取物.prefab` 进场景，放在地面上方。
2. 选中它，`Inspector` 里改字段：
   - `Display Name`：提示条上显示的名字，如 `旧磁带`
   - `Item Id`：存档里的键，如 `item_tape`
   - `Story Id`：对应 `story.csv` 里的 id，如 `pickup.tape`；留空就只弹一句 toast
3. 确认它的 `Layer` 是 `Interactable`，`Circle Collider 2D` 勾了 `Is Trigger`。
4. 玩家靠近按 `E` 就能捡。捡过的东西会从存档里记住，重进不会再出现。

### 6.2 门（跳事件）

1. 拖 `Assets/Prefabs/事件门.prefab` 进场景。
2. 改字段：
   - `Display Name`：`门·事件一`
   - `Event Id`：`event_01`（要和 `事件登记表` 里的 id 对应）
   - `Requires Event Id`：想上锁就填前置事件 id，如 `event_02` 填 `event_01`；不上锁留空
   - `Enter / Locked / Cleared Story Id`：进门前 / 锁着时 / 已完成时播的文案 id
3. 玩家靠近按 `E`：进事件场景 → 事件场景里调 `Complete()` → 自动回到原位。

> 事件场景必须加进 `File → Build Settings → Scenes In Build`，否则跳不过去。

---

## 七、写文案（不用碰代码）

1. 打开 `Assets/Resources/Story/story.csv`（Excel / VSCode 都行）。
2. 格式：

```csv
id,speaker,text
door01.enter,门·事件一,门缝里有风，很冷。
door01.enter,门·事件一,你把手放上去，它在抖。
pickup.tape,旧磁带,标签被人撕掉了。
```

- 同一个 `id` 写多行 = 这个对话有多句，会一句一句往下播
- `text` 留空的行会被跳过；整个 id 都没文本时，只弹一句 toast
- 改完在编辑器里点 `工具 / 25小时 / 检查文案`，缺哪个 id 会在 Console 里报出来

---

## 八、层（Layer）与常见坑

| Layer | 谁用 | 说明 |
| --- | --- | --- |
| `Ground`(8) | 地面、墙、楼梯 | 玩家判站地只看这一层 |
| `Interactable`(9) | 门、物品 | 玩家的 `E` 只在最近的一个里生效 |

常见坑：

| 现象 | 原因 |
| --- | --- |
| 玩家一直往下掉 | 地面没加 `Box Collider 2D`，或层不是 `Ground` |
| 玩家穿墙 | 墙没碰撞体，或碰撞体是 `Is Trigger` |
| 按 `E` 没反应 | 物体层不是 `Interactable`，或离得太远（`ShelterInteractor.radius` 默认 1.9） |
| 提示条乱闪 | 可交互物太多、挤在一起，把距离拉开一点 |
| 中文显示成方块 | `Assets/Fonts/SimHei.ttf` 没生成，跑一次菜单 `导出预制体` 会自动准备字体 |
| 画面全黑 | 相机 `Position` 的 z 不是 -10，或 `Size` 太小 |

---

## 九、运行与调试

- `Ctrl + P` 或点 ▶ 进 Play，再按一次退出。**Play 模式下改的东西退出后会还原。**
- Play 时选中玩家，`Inspector` 里能看到 `Rigidbody 2D` 的实时速度。
- 想看变量：脚本里写 `Debug.Log("值=" + 某变量);`，输出在 Console。
- 想看帧率：打包版带命令行参数 `-fps` 启动，会每秒打一条日志。

---

## 十、菜单（本工程专有）

`工具 / 25小时` 下：

| 菜单 | 作用 |
| --- | --- |
| 重建场景骨架（只有功能） | **会覆盖手搭内容**，只保留相机/玩家/系统/UI |
| 导出预制体（玩家 / 事件门 / 拾取物） | 重新生成 `Assets/Prefabs` |
| 打开第0层并运行 | 打开场景并进 Play |
| 重建并截图 | 生成一张骨架截图 |
| 一键：重建 + 截图 + 打包 | 上面几步 + 出版本 |
| 构建 Windows 试玩版 | 只打包，输出 `D:/Unity2D/Build/25Hours/25Hours.exe` |
| 检查文案 | 校验 `story.csv` 里缺哪些 id |

---

## 十一、保存、版本与提交

- 改场景后 `Ctrl + S`。改脚本也一样。
- 提交前先看 `File → Build Settings` 里三个场景都在。
- 不要手改 `Assets/Fonts`、`Assets/Whitebox`、`Assets/Art` 这类生成物；要改就改生成它们的 Editor 脚本。
- 提交用 `git`：`git add -A && git commit -m "说明" && git push`（仓库是 `https://github.com/zjh01300031/25Hours`）。
- 两个人同时改同一个场景容易冲突，约定好谁改哪个场景。

---

## 十二、打包发出去

1. `File → Build Settings` → 平台选 `Windows, Mac, Linux` → `Target Platform` = `Windows`。
2. 确认 `Scenes In Build` 里勾了 `Shelter_L0`、`Event_01`、`Event_02`。
3. 点 `Build` → 选输出文件夹（本工程用 `D:/Unity2D/Build/25Hours`）。
4. 产物：`25Hours.exe` + `25Hours_Data/` + `UnityPlayer.dll` 等，**整个文件夹一起发**，只发 exe 别人打不开。

---

## 十三、一分钟速查

```
移动/旋转/缩放   W / E / R
对准选中物体     F
2D 视图          视图右上角 2D
保存             Ctrl + S
运行/停止        Ctrl + P
复制物体         Ctrl + D
新建物体         右键 Hierarchy
加组件           选中物体 → Inspector → Add Component
对齐网格         按住 Ctrl 拖动
```

有拿不准的地方，先看 `Console`，再去 [Unity 手册](https://docs.unity3d.com/2022.3/Documentation/Manual/index.html) 搜组件名。
