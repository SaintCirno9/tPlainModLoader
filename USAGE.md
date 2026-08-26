# tPlainModLoader (TPML) 使用与开发手册

> **项目来源**：Fork 自 [github-user-64/tPlainModLoader](https://github.com/github-user-64/tPlainModLoader)  
> **文档版本**：v2.1　**维护者**：`SaintCirno9`  
> **适用环境**：.NET Framework 4.7.2 / Terraria 1.4.4+ & 1.4.5+

---

## 目录

- [一、 玩家使用指南](#一-玩家使用指南)
  - [1.1 环境要求](#11-环境要求)
  - [1.2 安装与初次配置](#12-安装与初次配置)
  - [1.3 模组安装与管理](#13-模组安装与管理)
- [二、 开发者指南](#二-开发者指南)
  - [2.1 模组工程结构](#21-模组工程结构)
  - [2.2 模组入口类 (Mod)](#22-模组入口类-mod)
  - [2.3 配置文件格式](#23-配置文件格式)
  - [2.4 统一日志系统接入 (TPML.Core.Logging)](#24-统一日志系统接入-tpmlcorelogging)
  - [2.5 统一按键系统接入 (KeybindLoader)](#25-统一按键系统接入-keybindloader)
  - [2.6 Prepatcher 自由字段与早期补丁](#26-prepatcher-自由字段与早期补丁)
  - [2.7 Harmony 运行时补丁规范](#27-harmony-运行时补丁规范)
  - [2.8 统一文本输入框 (UITextBox)](#28-统一文本输入框-uitextbox)
  - [2.9 模组配置与自动持久化 (ModSetting)](#29-模组配置与自动持久化-modsetting)
  - [2.10 全局拼音搜索与多模匹配 (TPML.Core.Pinyin)](#210-全局拼音搜索与多模匹配-tpmlcorepinyin)
- [三、 构建与部署](#三-构建与部署)

---

## 一、 玩家使用指南

### 1.1 环境要求
- **操作系统**：Windows 7 / 8 / 10 / 11
- **运行依赖**：.NET Framework 4.7.2+ 以及 XNA Framework 4.0 Redistributable
- **游戏本体**：正版 Terraria 原版可执行文件（`Terraria.exe`）

### 1.2 安装与初次配置
1. 将编译产物解压或放置到任意目录（或直接放置在 Terraria 游戏根目录下）；
2. 运行 `tPlainModLoader.exe`；
3. **配置游戏路径**：如果启动器未能自动识别到游戏可执行文件，可在生成的 `launchConfig.json` 中指定 `Terraria.exe` 的绝对路径：
   ```json
   {
     "LauncherFilePath": "C:\\Games\\Steam\\steamapps\\common\\Terraria\\Terraria.exe"
   }
   ```

### 1.3 模组安装与管理
- **模组安装**：将模组文件夹放入 `tPlainModLoader/Mods/` 目录下；
- **启用与禁用**：
  - 启动游戏后，可通过主菜单的模组管理界面进行切换；
  - 启用状态记录在用户文档目录下的 `Documents/My Games/Terraria/tPlainModLoader/enabled.json` 中；
- **日志查看**：启动与模组运行时日志自动输出至控制台并写入 `tPlainModLoader/tpml.log`（历史日志在启动时自动归档为 `tpml_old.log`）。

---

## 二、 开发者指南

### 2.1 模组工程结构
推荐采用标准的 SDK 风格 `.csproj`（`net472` 目标框架）：

```
YourMod/
├── Properties/
├── Content/
├── YourMod.cs             # 模组入口类
├── info.json              # 模组显示信息
├── loadConfig.json        # 模组加载行为配置
├── ico.png                # 模组图标（可选）
└── YourMod.csproj
```

**工程文件推荐配置 (`.csproj`)**：
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Library</OutputType>
    <RootNamespace>YourMod</RootNamespace>
    <AssemblyName>YourMod</AssemblyName>
    <TargetFramework>net472</TargetFramework>
    <DebugType Condition="'$(Configuration)' == 'Release'">pdbonly</DebugType>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\tPlainModLoader\TPML.Core\TPML.Core.csproj" />
    <ProjectReference Include="..\..\tPlainModLoader\TPML.Content\TPML.Content.csproj" />
    <ProjectReference Include="..\..\tPlainModLoader\tContentPatch\tContentPatch.csproj" />
  </ItemGroup>

  <Target Name="DeployToGameDir" AfterTargets="Build" Condition="'$(Configuration)' == 'Release' and '$(TerrariaDir)' != ''">
    <PropertyGroup>
      <ModDeployDir>$(TerrariaDir)\tPlainModLoader\Mods\$(ProjectName)</ModDeployDir>
    </PropertyGroup>
    <MakeDir Directories="$(ModDeployDir)" />
    <ItemGroup>
      <DeployDll Include="$(TargetPath);$(TargetDir)$(TargetName).pdb" />
      <DeployConfig Include="$(ProjectDir)info.json;$(ProjectDir)loadConfig.json;$(ProjectDir)ico.png" Condition="Exists('%(FullPath)')" />
    </ItemGroup>
    <Copy SourceFiles="@(DeployDll);@(DeployConfig)" DestinationFolder="$(ModDeployDir)" SkipUnchangedFiles="true" />
  </Target>

</Project>
```

---

### 2.2 模组入口类 (Mod)
继承 `TPML.Content.Mod` 并根据需要重写生命周期钩子：

```csharp
using TPML.Content;
using tContentPatch.Patch;

namespace YourMod
{
    public class YourMod : Mod
    {
        public static YourMod Instance { get; private set; }

        public override void Load()
        {
            Instance = this;
            // 模组被实例化时调用，Logger 已自动绑定并可用
            Logger.Info("模组初始化成功");
        }

        public override void Loaded()
        {
            // 配置及前置模组加载完成后调用
            MyKeybinds.Register(this);
        }

        public override void AddPatch(IAddPatch addPatch)
        {
            // 注册 Harmony 补丁
            addPatch.AddPatch(new MyGameplayPatch());
        }

        public override void Unload()
        {
            // 卸载与清理
            Logger.Info("模组已卸载");
            Instance = null;
        }
    }
}
```

---

### 2.3 配置文件格式

#### `loadConfig.json`
定义模组的核心加载信息：
```json
{
  "key": "YourModKey",
  "dllPath": "YourMod.dll",
  "isLoad": true,
  "frontModKeys": []
}
```

#### `info.json`
定义用于在模组列表显示的元数据：
```json
{
  "name": "模组名称",
  "version": "1.0.0",
  "author": "SaintCirno9",
  "description": "模组详细介绍与功能说明..."
}
```

---

### 2.4 统一日志系统接入 (TPML.Core.Logging)

TPML 提供了高性能、线程安全且非阻塞的统一日志架构 `TPML.Core.Logging`：

#### 1. 模组内直接使用 `Logger`
继承自 `Mod` 的模组类内置了强类型 `ILogger Logger` 属性：
```csharp
Logger.Debug("调试级别信息");
Logger.Info("普通提示信息");
Logger.Warn("警告信息");
Logger.Error("业务执行失败", exception);
Logger.Fatal("致命崩溃异常", exception);
```

#### 2. 在非 Mod 类或静态工具中获取 Logger
```csharp
using TPML.Core.Logging;

public static class MyHelper
{
    private static readonly ILogger Logger = LogManager.GetLogger("MyHelper");

    public static void DoWork()
    {
        Logger.Info("执行辅助任务...");
    }
}
```

#### 3. 性能诊断计时器 (ScopedTimer)
```csharp
using TPML.Core.Diagnostics;

public void HeavyCalculation()
{
    // 超过 10ms 时自动以 Warn 级别输出耗时，未超时以 Debug 级别记录
    using (ScopedTimer.Profile(Logger, "复杂计算任务", warnThresholdMs: 10f))
    {
        // 耗时计算逻辑
    }
}
```

---

### 2.5 统一按键系统接入 (KeybindLoader)

TPML 核心提供了原生级快捷键框架，自动集成到游戏原版【控件】设置界面中，并在打字聊天时自动全局静默。

```csharp
using Microsoft.Xna.Framework.Input;
using tContentPatch.Input;

public static class MyKeybinds
{
    public static ModKeybind ToggleKeybind { get; private set; }

    public static void Register(object mod)
    {
        ToggleKeybind = KeybindLoader.RegisterKeybind(
            mod: mod,
            name: "ToggleFeature",
            defaultBinding: "V",
            displayName: "开关功能"
        );
    }
}
```

**按键状态判定**：
- `ToggleKeybind.JustPressed`：当前帧按下；
- `ToggleKeybind.Current`：当前处于按住状态；
- `ToggleKeybind.JustReleased`：当前帧释放。

---

### 2.6 Prepatcher 自由字段与早期补丁

TPML 内置了基于 `Mono.Cecil` 的 Prepatcher 预修补机制。

#### 1. 自由字段注入（Free Fields）
通过 `[PrepatcherField]` 声明扩展方法，Prepatcher 引擎会在启动时向原版目标类注入字段，并将扩展方法体改写为单条原生 IL 访问指令：

```csharp
using tContentPatch.Prepatcher;
using Terraria;

// 1. 数据结构
public class MyPlayerState
{
    public int CustomValue = 0;
}

// 2. 声明扩展访问器
public static class PlayerExtensions
{
    [PrepatcherField]
    public static ref MyPlayerState GetMyState(this Player player) => ref Prepatcher.UnsafeRef<MyPlayerState>();
}

// 3. 业务调用
player.GetMyState().CustomValue++;
```

#### 2. 早期 Cecil 预补丁 (IPrepatcher / FreePatch)
若需要在 CLR 载入前对 `Terraria.exe` 程序集进行底层修改（如常量改写、静态数组重分配等），可实现 `IPrepatcher` 接口：

```csharp
using Mono.Cecil;
using tContentPatch.Prepatcher;

public class MyEarlyPatcher : IPrepatcher
{
    public void EarlyPatch(AssemblyDefinition terrariaAssembly)
    {
        // 直接操作 Mono.Cecil 进行底层改写
    }
}
```

---

### 2.7 Harmony 运行时补丁规范

- **强类型零反射**：由于 TPML 在启动期对 `Terraria.exe` 全量执行了公有化处理，原版所有私有/内部成员均可直接以强类型语法访问，无需使用反射；
- **引用安全性**：补丁目标方法名推荐使用 `nameof(...)` 进行引用。

```csharp
using HarmonyLib;
using Terraria;
using tContentPatch.Patch;

public class MyGameplayPatch : IPatch
{
    public void Patch(Harmony harmony)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Player), nameof(Player.Update)),
            postfix: new HarmonyMethod(typeof(MyGameplayPatch), nameof(PostUpdate))
        );
    }

    public static void PostUpdate(Player __instance)
    {
        if (__instance.whoAmI == Main.myPlayer)
        {
            // 客户端主玩家逻辑
        }
    }
}
```

---

### 2.8 统一文本输入框 (UITextBox)

TPML 在 `tContentPatch.Content.UI.UITextBox` 中提供了通用的单行文本输入控件：
- **IME 输入法**：完整支持中文 Windows IME 拼音合成串内联反馈与闪烁光标；
- **操作安全隔离**：在获得焦点期间由底层自动接管 `Main.blockInput = true` 与 `PlayerInput.WritingText = true`，彻底消除在输入数字时意外触发原版快捷栏切换（1~9, 0）或移动跑跳的问题；
- **视口与回车响应**：超长文本平滑水平滚动，支持回车（`OnSubmit`）/失焦即时提交与 Esc 取消。

---

### 2.9 模组配置与自动持久化 (ModSetting)

模组可通过继承 `ModSetting` 实现结构化配置与文件持久化：
- **自动落盘机制**：配置项变更时仅需设置 `NeedSave = true`；
- **全生命周期安全网**：框架提供了 `ModSetting.SaveAllDirty()`，在**快速设置菜单关闭 (`UIQuickSetting.Close`)**、**玩家离开世界**、**退回主菜单**以及**进程退出 (`ProcessExit`)** 时全自动执行持久化写盘，确保任何游戏内调参不丢失。

---

### 2.10 全局拼音搜索与多模匹配 (TPML.Core.Pinyin)

TPML 核心库提供了原生的拼音分词、全拼生成与首字母缩写多模模糊搜索基础设施：
- **零外部 DLL 依赖**：443KB 拼音字典以内嵌资源形式直接编译至 `TPML.Core.dll`，无需额外分发或部署外部动态链接库；
- **高性能字典树与内存缓存**：基于 Trie 前缀树进行汉字与多音词分词，结合 `ConcurrentDictionary` 元数据缓存，微秒级极速比对（~6µs/次）；
- **全覆盖多模匹配**：
  - 中文原文（如 `"钻石"`）；
  - 全拼连写与局部拼音（如 `"zuanshigao"` / `"shigao"` 匹配 `"钻石镐"`）；
  - 拼音首字母缩写（如 `"zsg"` 匹配 `"钻石镐"`，`"sjzh"` 匹配 `"世纪之花"`）；
  - 自动忽略空格与大小写。

```csharp
using TPML.Core.Pinyin;

// 1. 直接多模匹配 (支持首拼/全拼/中文/英文)
bool isMatch = PinyinHelper.Matches("钻石镐", "zsg"); // true
bool isMatch2 = PinyinHelper.Matches("世纪之花", "shiji"); // true

// 2. 字符串扩展方法
bool isMatch3 = "天界星盘".MatchesPinyin("tjxp"); // true

// 3. 原生系统全量接入
// - 原版网格制作系统 (NewCraftingUI) & 向导配方查询窗口输入框
// - 原版旅程模式制作与复制搜索框 (UICreativeInfiniteItemsDisplay)
// - 原版怪物图鉴搜索 (Bestiary.Filters.BySearch)
// - RecipeBrowser 物品/配方/怪物图鉴搜索
// - OptimizeAndTool 创造模式背包搜索 (UICreativeInventory)
// - WandsTool 蓝图管理器搜索 (UIBlueprintManager)
```

---

## 三、 构建与部署

仓库配置了全自动热部署流：

```bash
# 全量构建解决方案并部署全部组件 (19 工程静态图并发加速)
dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph

# 单个模组独立构建与部署
dotnet build tPlainModLoader/Mods/<ModName>/<ModName>.csproj -c Release
```
