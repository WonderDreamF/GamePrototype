# GamePrototype

> Unity 6 游戏项目模板 —— 面向 Steam PC 平台。

![Unity](https://img.shields.io/badge/Unity-6000.3.10f1-000000?logo=unity)
![Render Pipeline](https://img.shields.io/badge/URP-17.3.0-blue)
![Platform](https://img.shields.io/badge/platform-Windows%20(Steam)-lightgrey)
![Code Style](https://img.shields.io/badge/code%20style-Google%20C%23-4285F4)
![Template](https://img.shields.io/badge/template-ready-brightgreen)

基于 **Unity 6** 的游戏项目模板，在框架层做了完整的基础设施搭建——资源热加载、对象池、事件总线、配置表、本地化、音频、存档——并深度集成 **MCP for Unity**,支持通过 AI 助手(如 Claude Code)直接操作 Unity 编辑器。

---

## ✨ 技术栈

| 能力 | 方案 |
|---|---|
| 引擎 | Unity `6000.3.10f1` |
| 渲染管线 | Universal Render Pipeline (URP) `17.3.0`,PC / Mobile 两套 Renderer |
| 异步 | [UniTask](https://github.com/Cysharp/UniTask)（全面替代协程） |
| 资源加载 | [YooAsset](https://github.com/tuyoogame/YooAsset) —— 按「地址」加载,编辑器免打包模拟 / 打包离线内置 |
| 配置表 | [Luban](https://github.com/focus-creative-games/luban) —— Excel/XML → C# + JSON |
| 补间动画 | DOTween |
| 序列化 | Newtonsoft.Json（存档系统） |
| 输入 | Unity Input System `1.18.0` |
| 编辑器自动化 | [MCP for Unity](https://github.com/CoplayDev/unity-mcp) |
| 单元测试 | Unity Test Framework `1.6.0` (NUnit) |

---

## 🚀 快速开始

### 使用模板

点击 GitHub 页面上的 **"Use this template"** 按钮，即可基于此模板创建你自己的项目。

### 环境要求

- **Unity 6000.3.10f1**（建议用 Unity Hub 安装对应版本）
- Windows 10/11
- （可选）[Rider](https://www.jetbrains.com/rider/) 或 Visual Studio —— 已内置 `.editorconfig`,会自动套用代码规范

### 克隆

```bash
git clone https://github.com/WonderDreamF/GamePrototype.git
cd GamePrototype
```

### 首次打开

1. 用 Unity Hub 添加并以 `6000.3.10f1` 打开本工程。
2. **生成配置表**：菜单 `Tools/GamePrototype/生成 Luban 配表数据`（或运行 `DataTables/gen.bat`）。
   生成代码 → `Assets/_Code/Gen/`,数据 → `Assets/StreamingAssets/Tables/`。
3. **生成资源地址常量**（一般自动,可手动）：菜单 `Tools/GamePrototype/生成资源地址常量`。
4. 打开启动场景 `Assets/Scenes/Boot`,点击 ▶ 运行。

> 编辑器下 YooAsset 走 **EditorSimulateMode**（免打包,直接读工程内资源），无需提前构建资源包即可调试。

---

## 🧱 核心架构

启动流程由 `Boot` 场景中的 `GameBootstrap` 驱动:**加载配置表 → 初始化 YooAsset → Additive 加载主菜单**。`Boot` 是常驻场景,持有各全局管理器、相机与 EventSystem,后续场景叠加其上、不卸载它。

| 系统 | 入口 | 说明 |
|---|---|---|
| **单例基类** | `MonoSingleton<T>` | 常驻场景管理器,访问零查找开销;派生类重写 `OnAwake` |
| **资源管理** | `ResourceManager.Instance.LoadAsync<T>(ResAddress.Xxx)` | 按地址加载,加载一次即缓存常驻;强类型地址常量自动生成 |
| **对象池** | `PoolManager.Instance.Get / Release` | 频繁生成的对象走对象池,支持预热与延迟回收 |
| **事件总线** | `EventBus.Subscribe / Emit`（`GameEventType`） | 系统间解耦通信,避免遍历查找 |
| **配置表** | `ConfigManager.Tables` | Luban 生成的强类型表,运行时读 `StreamingAssets/Tables` |
| **本地化** | `LocalizationManager` + `LocalizedTMP` | 中/英/日,切换即时刷新 |
| **音频** | `AudioManager.Instance.Play(AudioId.Xxx)` | AudioMixer 控音量,配置表驱动,BGM 交叉淡入淡出 |
| **存档** | `SaveManager`（独立程序集 `GamePrototype.SaveSystem`） | 3 槽位 + 自动存档,Newtonsoft JSON,可单测 |

性能约定（详见 [`CLAUDE.md`](./CLAUDE.md)）：**热路径禁止查找**、**异步统一用 UniTask**、**频繁对象走池**、**资源按地址加载不拖 Inspector**。

---

## 📁 项目结构

```
Assets/
├── GameRes/          # ★ YooAsset 收集根 —— 运行时按地址加载的美术/音频/预制体/数据/叙事资源
├── _Code/            # 全部游戏代码（_ 前缀排最前；Gen/ 为 Luban 自动生成,勿手改）
│   ├── Core/         #   GameManager / Resource / Pool / Events / 本地化 / 配置
│   ├── Audio/  UI/  SaveSystem/  Editor/  Gen/
├── Scenes/           # 内置场景 Boot / MainMenu（在 Build Settings,YooAsset 初始化前加载）
├── Resources/        # YooAsset 初始化前需要的小体积常驻配置
├── Plugins/          # DOTween 等第三方插件
├── Settings/         # URP / Input System 配置
└── StreamingAssets/  # Tables（Luban 数据）/ Localization / yoo（YooAsset 构建产物）
DataTables/           # Luban 策划表（Datas 数据 / Defines 定义）+ gen 脚本
Tools/                # Luban 可执行与 gen_client.bat
```

> 完整的目录职责与资源存放规则见 [`CLAUDE.md`](./CLAUDE.md)。

---

## 🤖 MCP for Unity 集成

工程通过 git 包集成 [MCP for Unity](https://github.com/CoplayDev/unity-mcp),将 Unity 编辑器能力以 MCP 协议暴露。AI 助手可据此创建/修改 GameObject、编辑脚本、管理场景、运行测试、读取控制台等。

```json
"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main"
```

---

## 📐 代码规范

所有手写 C# 代码遵循 **[Google C# Style Guide](https://google.github.io/styleguide/csharp-style.html)**（2 空格缩进、K&R 大括号、`GamePrototype.*` 命名空间、显式访问修饰符等）。规范细则见 [`CLAUDE.md`](./CLAUDE.md) 的「C# 代码规范」一节;仓库根目录的 [`.editorconfig`](./.editorconfig) 已据此配置,IDE 会自动格式化与告警。

提交前可校验：

```bash
dotnet format GamePrototype.sln --verify-no-changes
```

---

## ✅ 测试与构建

**运行测试**（编辑器内：`Window > General > Test Runner`）：

```bash
Unity -projectPath . -runTests -testPlatform editmode -quit -batchmode
```

**打包（Windows 64 位）**：

```bash
Unity -projectPath . -buildTarget StandaloneWindows64 -executeMethod BuildPipeline.BuildPlayer -quit -batchmode
```

资源包通过 `YooAsset > AssetBundle Builder` 构建,离线产物输出到 `Assets/StreamingAssets/yoo/`。

---

## 🤝 贡献约定

- 改了配置表结构/数据后,**必须重跑 Luban 生成脚本**;`Gen/` 与 `ResAddress.cs` 为自动生成,**勿手改**。
- 提交范围：`Assets/`、`ProjectSettings/`、`Packages/manifest.json`、`Packages/packages-lock.json`、`.csproj`。
- 忽略提交：`Library/`、`Logs/`、`Temp/`、`UserSettings/`、`obj/`、`Build(s)/`。
- 新代码须符合上述代码规范。

---

<sub>更详尽的工程约定、架构说明与性能规范请阅读 [`CLAUDE.md`](./CLAUDE.md)。</sub>
