# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Awen is a Unity 6 narrative indie game (version 6000.3.10f1) targeting Steam PC, integrated with **MCPForUnity** — a Model Context Protocol implementation enabling AI assistants to interact with the Unity Editor programmatically. It uses Universal Render Pipeline (URP 17.3.0).

## Project Structure

资源按「加载方式」分处存放：**YooAsset 按地址加载的资源** → `Assets/GameRes/`；**代码** → `Assets/_Code/`；**启动场景** → `Assets/Scenes/`；**常驻/构建产物** → `Assets/Resources/` 与 `Assets/StreamingAssets/`。

```
Assets/
├── GameRes/                      # ★ YooAsset 收集根目录 —— 运行时按地址加载的资源都放这里
│   ├── Art/                      # 动画就近放在「所驱动实体」的目录里，按实体组织（不按资源类型平铺）
│   │   ├── Backgrounds/          # 场景背景图（静态）
│   │   ├── Characters/           # 角色：每个角色一个目录，内含立绘 + 动画
│   │   │   └── <角色名>/         #   ├ Sprites/      角色立绘
│   │   │                         #   └ Animations/   该角色 controller / clips
│   │   ├── Props/                # 场景物件：每个物件一个目录，含其 Sprites/Animations（动画别塞进 Characters）
│   │   │   └── <物件名>/
│   │   ├── UI/                   # Panels, Icons, Buttons, Fonts（UI 动效多用 DOTween 代码补间，无资源）
│   │   └── VFX/                  # 粒子特效（特效动画跟着特效放这里）
│   ├── Audio/
│   │   ├── BGM/                  # 背景音乐
│   │   ├── SFX/                  # 音效
│   │   └── Voice/                # 语音
│   ├── Data/                     # ScriptableObject 资产
│   │   ├── Characters/
│   │   ├── Chapters/
│   │   └── GameSettings/
│   ├── Narrative/                # 叙事内容文件
│   │   ├── Dialogues/            # 对话脚本 (Ink / Yarn / 自定义)
│   │   ├── Chapters/             # 章节数据
│   │   └── Localization/         # 本地化文本
│   ├── Prefabs/
│   │   ├── Characters/
│   │   ├── UI/
│   │   ├── Audio/
│   │   └── Systems/              # 全局系统 Prefab (GameManager, AudioManager…)
│   └── Scenes/
│       └── Chapters/             # 章节场景 —— 由 YooAsset 加载 (LoadSceneAsync)，可热更/按需
├── _Code/                        # 全部游戏代码（不被 YooAsset 收集；前导下划线排在最前）
│   ├── Core/                     # GameManager, SceneLoader, EventBus, MonoSingleton, Pool, Resource
│   ├── Narrative/                # DialogueManager, StoryProgress, ChoiceSystem
│   ├── Characters/               # 角色数据 / 控制逻辑
│   ├── UI/                       # UIManager, DialogueUI, MainMenuUI
│   ├── Audio/                    # AudioManager, BGMController, SFXController
│   ├── SaveSystem/               # SaveManager, SaveData（独立程序集 Awen.SaveSystem）
│   ├── Utilities/                # 扩展方法, 常量, 静态工具
│   ├── Gen/                      # Luban 生成的配置代码（勿手改）
│   └── Editor/                   # 编辑器扩展脚本
├── Scenes/                       # 内置场景（必须在 Build Settings，YooAsset 初始化前加载）
│   ├── Boot/                     # 启动场景 —— 初始化 YooAsset/各管理器
│   └── MainMenu/                 # 主菜单
├── Resources/                    # Unity 常驻资源（启动期、YooAsset 初始化前需要）：
│                                 #   框架配置 (AssetBundleCollectorSetting, DOTween, InputActions) 等。
│                                 #   ⚠ 别放大资源，会全量打进包并拖慢启动。
├── Plugins/                      # 第三方插件 (DOTween 等)
├── Settings/                     # URP / Input System 配置
└── StreamingAssets/
    ├── Tables/                   # Luban 配置数据 (*.json)，运行时读取
    ├── Localization/             # 运行时读取的本地化文件
    └── yoo/                      # ★ YooAsset 构建产物（Offline 内置包），由构建工具生成，勿手改
```

### 资源存放规则（接入 YooAsset 后）

- **`Assets/GameRes/`** 是 YooAsset 的收集根目录。所有运行时加载的美术、音频、预制体、ScriptableObject、叙事资源都放这里，统一通过 `ResourceManager.Instance.LoadAsync<T>(location)` 按地址加载。详见 Performance Conventions §1.5。
- **场景**：`Boot` / `MainMenu` 放 `Assets/Scenes/` 并加入 Build Settings（YooAsset 初始化前由 Unity 直接加载）；章节场景放 `GameRes/Scenes/Chapters/`，运行时用 `package.LoadSceneAsync(location)` 加载。Boot 不能走 YooAsset（初始化它的代码就在 Boot 里）。
- **`Assets/Resources/`** 只放「YooAsset 初始化前就要用」的小体积配置，不放游戏资源。
- **`Assets/StreamingAssets/Tables/`** 放 Luban 配置数据，运行时读取（loader 用 UnityWebRequest/File）。
- **`Assets/StreamingAssets/yoo/`** 是 YooAsset 离线包的构建输出，由 `YooAsset > AssetBundle Builder` 生成，不要手动改动或提交前确认 .gitignore 规则。
- 收集器 Package 名为 `DefaultPackage`，与 `ResourceManager.PackageName` 保持一致。

### Luban 配置表

策划表与定义在 `DataTables/`（`Datas/*.xlsx` 数据、`Defines/*.xml` 定义）。运行 `DataTables/gen.bat`（或 `gen.sh`）生成：

- **代码** → `Assets/_Code/Gen/`（C# 模板 `cs-simple-json`，命名空间 `cfg`，由 `outputCodeDir` 指定）。
- **数据** → `Assets/StreamingAssets/Tables/`（json，由 `outputDataDir` 指定）。

改了表结构/数据后必须重跑 gen 脚本；生成代码与数据**勿手改**（会被覆盖）。`Tables` 的 loader 按表名读取 `StreamingAssets/Tables/<表名>.json`。

## Building & Testing

Unity projects are built and tested through the Unity Editor or command line:

**Open project:**
```
Unity -projectPath C:\Project\Awen
```

**Run tests (command line):**
```
Unity -projectPath C:\Project\Awen -runTests -testPlatform editmode -quit -batchmode
Unity -projectPath C:\Project\Awen -runTests -testPlatform playmode -quit -batchmode
```

**Build (standalone):**
```
Unity -projectPath C:\Project\Awen -buildTarget StandaloneWindows64 -executeMethod BuildPipeline.BuildPlayer -quit -batchmode
```

In-editor: use **Window > General > Test Runner** for tests, **File > Build Settings** for builds.

## Architecture

### MCPForUnity Integration

The core purpose of this project. MCPForUnity is added as a git package:
```json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main"
```

This exposes Unity Editor capabilities over the MCP protocol — Claude Code (via the `mcp__UnityMCP__*` tools in this session) can create and modify GameObjects, run scripts, manage scenes, and read the console. Always check `mcpforunity://custom-tools` for project-specific capabilities, and `mcpforunity://editor_state` before mutating editor state.

**MCPForUnity workflow:**
1. After creating or modifying scripts, call `read_console` to verify compilation succeeded before using new types.
2. Poll `editor_state.isCompiling` to wait for domain reloads.
3. All asset paths are relative to `Assets/` and use forward slashes.

### Render Pipeline

Two URP renderer assets are configured:
- `Assets/Settings/PC_RPAsset.asset` — higher quality, desktop
- `Assets/Settings/Mobile_RPAsset.asset` — optimized, mobile

The active renderer is set in `ProjectSettings/URPProjectSettings.asset`.

### Input System

Unity's new Input System (1.18.0) is used. Input actions are defined in `Assets/InputSystem_Actions.inputactions`.

### Key Package Versions

| Package | Version |
|---|---|
| URP | 17.3.0 |
| Input System | 1.18.0 |
| Test Framework | 1.6.0 |
| AI Navigation | 2.0.10 |
| Timeline | 1.8.10 |
| Visual Scripting | 1.9.9 |

## Performance Conventions (性能规范)

以下为项目强制规范，编写运行时代码时必须遵守：

### 1. 禁止在热路径里查找

- **不要**在 `Update` / `FixedUpdate` / 协程循环 / 高频调用里使用 `GameObject.Find`、`FindObjectOfType`、`FindObjectsOfType`、`GetComponent`（在已知不变的对象上）、`Camera.main`、`SendMessage`。
- 同场景内已存在的引用：在 `Awake`/`Start` 里查一次并缓存到字段，不要每帧查。
- 跨系统拿管理器，用单例缓存字段：`PoolManager.Instance` / `ResourceManager.Instance`（继承 `MonoSingleton<T>`，访问零查找）。**不要**自己写 `FindObjectOfType<XxxManager>()`。
- 需要 `Camera.main` 时缓存到字段，不要每帧访问。

### 1.5 资源通过 YooAsset 按地址加载，不拖 Inspector

- 预制体、Sprite、AudioClip、ScriptableObject 等资源，一律通过 `ResourceManager` 按「地址」加载，**不再往 Inspector 拖引用**：`await ResourceManager.Instance.LoadAsync<T>(ResAddress.Xxx)`。
- 地址**不要手打字符串**，用生成的强类型常量 `ResAddress.UI_Icons_heart`（值为 `"UI/Icons/heart"`）。常量由 `ResAddressGenerator` 扫描 `GameRes` 自动生成到 `_Code/Core/Resource/ResAddress.cs`：资源增删改时自动重生成，也可手动跑菜单 `Tools/Awen/生成资源地址常量`。该文件是自动生成的，**勿手改**。
- `ResourceManager` 加载一次即缓存句柄（常驻、不重复加载、不被自动卸载），所以「读取到一遍就保存在脚本里」是默认行为，调用方无需持有引用、无需手动释放。
- 频繁生成的对象：`await ResourceManager.Instance.InstantiateAsync(location, pos, rot)` —— 资源由 `ResourceManager` 缓存，实例由 `PoolManager` 复用。

### 2. 不要在运行时频繁 `new GameObject` / `Instantiate` / `Destroy`

- 频繁生成销毁的对象（子弹、特效、敌人、伤害飘字、UI 列表项…）一律走对象池 `PoolManager.Instance.Get/Release`，**不要**直接 `Instantiate`/`Destroy`。
- 一次性、生命周期内只创建一次的容器/根节点（如池的类别容器）允许 `new GameObject`，但要在初始化阶段完成，不能在循环里。
- 预热：进战斗/进场景前用 `Prewarm` 提前实例化，避免运行时实例化卡顿。

### 3. 异步优先用 UniTask，不用协程

- 异步逻辑（延迟、等待、加载、网络）一律用 **UniTask 的 async/await**，**不要**写 `StartCoroutine` / `IEnumerator` / `yield return`。项目已集成 UniTask（`using Cysharp.Threading.Tasks;`）。
- 常用替换：`yield return new WaitForSeconds(t)` → `await UniTask.Delay(TimeSpan.FromSeconds(t))`；`yield return null` → `await UniTask.Yield()`；等待条件 → `await UniTask.WaitUntil(...)`。
- 即发即忘用 `.Forget()`，并传入 `this.GetCancellationTokenOnDestroy()`，确保对象销毁时自动取消，避免操作已销毁对象（参考 `PoolManager.ReleaseAfterAsync`）。
- 方法返回 `UniTask` / `UniTask<T>`；纯即发即忘的方法用 `UniTaskVoid`。

### 4. 其它

- 避免每帧分配产生 GC：复用 `List`/数组、避免 LINQ 在热路径、避免字符串拼接（用缓存或 `StringBuilder`）。
- 事件解耦优先用 `EventBus`，不要靠遍历查找对象来通信。
- 延迟回收用 `PoolManager.Instance.Release(go, delay)`，不要 `Destroy(go, delay)`。

## C# 代码规范（Google C# Style Guide）

本项目所有手写 C# 代码**强制**遵循 [Google C# Style Guide](https://google.github.io/styleguide/csharp-style.html)。仓库根目录的 `.editorconfig` 已据此配置，IDE（Rider/VS）会自动格式化与告警；提交前可跑 `dotnet format` 校验。**编写或修改任何脚本都必须符合本节规范。**

### 适用范围

- **适用**：`Assets/_Code/` 下除 `Gen/` 外的全部手写代码，以及 `Assets/Tests/`。
- **豁免（勿手改、不受规范约束）**：`Assets/_Code/Gen/`（Luban 生成）、`Assets/_Code/Core/Resource/ResAddress.cs`（ResAddressGenerator 生成）。这些是自动生成文件，保持生成器输出原样。

### 格式（Formatting）

- **缩进 2 个空格**，禁止 Tab。
- **行宽上限 100 列**；续行缩进 4 空格（函数实参换行可与首个实参对齐，或换行后缩进 4 空格）。
- **K&R 大括号（同行）**：左大括号不换行，跟在声明/语句同一行末尾；`else`/`catch`/`finally` 不换行，紧跟前一个 `}`。
  ```csharp
  if (ready) {
    Run();
  } else {
    Wait();
  }
  ```
- 一行最多一条语句、一次赋值。
- 空格：`if`/`for`/`while` 关键字后、逗号后加空格；`(` 后与 `)` 前不加空格；一元运算符与操作数之间不加空格；其它二元运算符两侧各一个空格。

### 命名（Naming）

| 元素 | 规则 | 例 |
|---|---|---|
| 类、方法、枚举、命名空间、**public** 字段/属性 | `PascalCase` | `ResourceManager`、`LoadAsync` |
| 局部变量、参数 | `camelCase` | `assetPath`、`fade` |
| **private / protected / internal** 字段和属性 | `_camelCase` | `_instance`、`_assetCache` |
| 接口 | 以 `I` 开头 | `IPoolable` |

- **命名不受 `const`/`static`/`readonly` 等修饰符影响**——由可见性决定：非 public 的 `const`/`static readonly` 字段也用 `_camelCase`（如 `const float _defaultFade`、`static readonly ... _jsonSettings`）；public 常量才用 PascalCase（如 `public const string PackageName`）。
- 缩写按「词」处理：`MyRpc` 而非 `MyRPC`。
- 文件名 `PascalCase.cs`，与主类名一致；一个文件一个核心类。

### Unity 序列化字段的特殊处理

- `[SerializeField]` 的私有字段同样遵循 `_camelCase`（如 `_startButton`、`_channel`）。
- **重命名已被场景/预制体引用的序列化字段时，必须加 `[FormerlySerializedAs("旧名")]`**（`using UnityEngine.Serialization;`），否则 Inspector 绑定会丢失。
- JSON 序列化的 DTO（如 `SaveData`，经 Newtonsoft）字段为 public，按 public 规则用 `PascalCase`——注意改名会改变 JSON 键，需同步更新读档兼容逻辑与测试。

### 命名空间（Namespaces）

- 手写代码按目录归入 `Awen.*` 命名空间，**最多 2 级**：`Awen.Core`（含 Core 下所有子目录 Pool/Events/Resource）、`Awen.Audio`、`Awen.UI`、`Awen.Editor`；存档系统沿用程序集名 `Awen.SaveSystem`。
- `using` 放在 `namespace` **外**。
- 生成文件（`ResAddress`、Luban `cfg.*`）保持其原命名空间（`ResAddress` 在全局命名空间，任何命名空间内都可直接访问，无需 `using`）。
- 经反射按**短类型名**发现的类型（如 YooAsset 的 `IAddressRule` 实现 `AddressByGameResPath`）可安全加命名空间——YooAsset 用 `type.Name` 匹配，与命名空间无关。

### 组织（Organization）

- **所有成员（方法、字段、属性、构造、嵌套类型）必须显式写出访问修饰符**（`private`/`public`/`protected`/`internal`），不依赖 C# 的默认可见性。唯一例外：显式接口实现（如 `void IPoolable.OnSpawn()`）语法上不允许加修饰符。
- 修饰符顺序中访问修饰符在最前，如 `private static readonly`、`private const`、`private async UniTaskVoid`。
- `using`：`System.*` 在最前，其余按字母序。
- 类成员顺序：① 嵌套类型/枚举/委托/事件 → ② const/static/readonly 字段 → ③ 字段与属性 → ④ 构造/析构 → ⑤ 方法；每组内按 public → internal → protected → private。
- 修饰符顺序：`public protected internal private new abstract virtual override sealed static readonly extern unsafe volatile async`。

### 语言用法（Language Usage）

- **`var`**：类型显而易见时用（`var go = new GameObject();`）；内置基础类型（`int`/`bool`/`float` 等）用显式类型（`bool success = true;`）。
- **属性 vs 字段**：单行只读属性优先表达式体 `=>`；其余用 `{ get; set; }`。表达式体**不要用在方法定义上**。
- 能 `const` 就 `const`，否则 `readonly`；避免魔法数字，用具名常量。
- 委托调用用 `SomeDelegate?.Invoke(...)`（空条件调用）。
- 入参用最受限类型（`IReadOnlyList`/`IEnumerable`）；`out` 参数放在最后，少用 `ref`。
- 优先具名类而非 `Tuple<>`；不用 `using` 给长类型起别名。
- 与本项目性能规范一致：热路径避免 LINQ 链与每帧分配（详见「Performance Conventions」）。

## Git Notes

The following are git-ignored and should not be committed: `Library/`, `Logs/`, `Temp/`, `UserSettings/`, `obj/`, `Build/`, `Builds/`.

Track and commit: `Assets/`, `ProjectSettings/`, `Packages/manifest.json`, `Packages/packages-lock.json`, and `.csproj` files.
