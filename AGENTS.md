# 游戏项目开发准则

适用于本工作区及其中的 Unity 项目（`25Hours/`）。默认让脚本负责行为，让预制件和场景负责组装，让可配置数据负责调参。

## 1. 保持低耦合，谨慎修改核心链路

- 组件只负责自己的行为与状态。跨组件依赖优先通过 Inspector 显式绑定或通过清晰的参数传入；不要靠对象名称、层级路径、全局查找或隐式单例定位依赖。
- 需要请求明确动作时，调用目标组件的公开方法；需要通知多个独立模块时，再考虑事件。避免循环依赖，也不要为简单的调用额外引入接口层或全局事件总线。
- 修改核心玩法、启动顺序、共享数据、场景切换、序列化字段或公共预制件前，先确认调用方、场景引用和预制件覆盖，说明影响范围与验证方式。优先做小步、兼容的改动；需要迁移时，给出迁移与回退步骤，不夹带无关重构。

## 2. 编辑器配置优先，重复内容做成预制件

- 场景列表与入口通过 Unity 编辑器的构建配置维护。不要把场景名称、路径或构建索引写死在游戏逻辑里；禁止用启动脚本或构建钩子自动生成场景来绕过编辑器配置，或强制改写构建场景列表。通过编辑器配置后的正常构建不受限制。
- 可复用的棋子、格子、界面面板、收集物、棋盘入口等优先做成 Prefab。玩法确实需要动态生成对象时，由脚本实例化 Inspector 中配置的 Prefab，不在代码里逐个拼装本可在编辑器配置的对象。
- 依赖、素材和常调参数应能在 Inspector 中查看和修改；需要跨实例共享的配置可使用 ScriptableObject / TextAsset（例如棋盘的 `layoutJson`）。缺少必需引用时，报错应指出具体对象和字段，不要静默补建对象。
- 交付涉及编辑器配置的功能时，说明要打开哪个场景或预制件、如何挂载和绑定、怎样在 Play 模式验证。

## 3. 按职责拆分，管理器只做协调

- 按功能组织相关脚本、Prefab 和配置。组件、规则、界面展示各自独立：例如 `PuzzleBoard` 只做格子与覆盖规则（纯 C#，不碰 UI），`PuzzleController` 负责把点击/按键翻译成规则调用，`BoardPreview` 只在编辑期画预览。
- 当多个组件或场景确实需要共享生命周期或流程时，再引入职责明确的管理器（`GameProgress`、`ShelterFlow`、`LevelFlow`、`SceneFade`）。管理器负责协调各自的流程，但不替组件处理细节，不做万能 `GameManager`，也不默认使用全局单例。
- 用能说明用途的文件名、类名和方法名。出现独立职责或真实复用时再拆分；不要为了增加文件数量，把小功能拆成难以追踪的多层结构。

## 4. 本项目的既有约定

- 目录：`25Hours/Assets/Scripts`（按 `Board/`、`Story/`、`Menu/` 分组）、`Assets/Editor`（编辑器工具）、`Assets/Prefabs`（可拖拽物件）、`Assets/Scenes`、`Assets/Resources`（棋盘 JSON、字体）。
- 场景：`MainMenu`、`Shelter_L0`、`Shelter_2F`、`Event_01`、`Event_02`、`Board_01..05`。加新场景后要手动加入 Build Settings。
- 玩家移动/跳跃：`ShelterPlayer`（横版）与 `TopDownPlayer`（俯视）；通用的「锁输入」用 `InputGate.Blocked`，不要各场景各写一套。
- 交互：任何可交互物继承 `InteractableBase`（`displayName` / `verb` / `Interact`），层设 `Interactable`；文本用 `Story/` 下的 `SimpleTextTrigger`（Inspector 填）或 `StoryInteractable`（读 `story.csv`）。
- 场景切换统一走 `SceneFade.FadeTo`，不要直接 `SceneManager.LoadScene`。
- `Assets/Editor` 下的 `ExploreBuilder` / `UpperRoomBuilder` / `MenuBuilder` / `BoardBuilder` 是白盒期的快速生成器：它们会覆盖对应场景。正式关卡请手搭或改生成器后重建，不要既手改又重建。

## 改动验收

- **依赖可查：** 能从 Inspector 或代码入口找到引用来源，没有新增循环依赖；场景里没有 `Missing (Mono Script)`（拷场景进仓库前跑一遍 `Tools/check_missing2.py`）。
- **内容可配：** 可复用的新增对象通过预制件复用；换素材、调参数或配置场景不需要改脚本。
- **行为可验证：** 在 Unity 中检查改动涉及的场景和预制件，确认引用无丢失、主要流程能运行；涉及启动或场景列表时，加做一次构建与启动检查。无法运行的检查要在交付时注明。
