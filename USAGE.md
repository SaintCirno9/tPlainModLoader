# tPlainModLoader (TPML) 使用与开发手册

> **项目来源**：Fork 自 [github-user-64/tPlainModLoader](https://github.com/github-user-64/tPlainModLoader)  
> **文档版本**：v2.0　**维护者**：`SaintCirno9`  
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
  - [2.4 统一按键系统接入 (KeybindLoader)](#24-统一按键系统接入-keybindloader)
  - [2.5 Prepatcher 自由字段与早期补丁](#25-prepatcher-自由字段与早期补丁)
  - [2.6 Harmony 运行时补丁规范](#26-harmony-运行时补丁规范)
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
  - 启用状态记录在用户文档目录下的 `Documents/My Games/Terraria/tPlainModLoader/enabled.json` 中。

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
继承 `tContentPatch.Mod` 并根据需要重写生命周期钩子：

```csharp
using tContentPatch;
using tContentPatch.Patch;

namespace YourMod
{
    public class YourMod : Mod
    {
        public static YourMod Instance { get; private set; }

        public override void Load()
        {
            Instance = this;
            // 模组被实例化时调用
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

### 2.4 统一按键系统接入 (KeybindLoader)

TPML 核心提供了原生级快捷键框架，自动集成到游戏原版【控件】设置界面中，并在打字聊天时自动全局静默。

```csharp
using Microsoft.Xna.Framework.Input;
using tContentPatch.Input;
using Terraria.ModLoader;

public static class MyKeybinds
{
    public static ModKeybind ToggleKeybind { get; private set; }

    public static void Register(tContentPatch.Mod mod)
    {
        ToggleKeybind = KeybindLoader.RegisterKeybind(
            mod: mod,
            name: "ToggleFeature",
            displayName: "开关功能",
            defaultKey: Keys.V
        );
    }
}
```

**按键状态判定**：
- `ToggleKeybind.JustPressed`：当前帧按下；
- `ToggleKeybind.Current`：当前处于按住状态；
- `ToggleKeybind.JustReleased`：当前帧释放。

---

### 2.5 Prepatcher 自由字段与早期补丁

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

### 2.6 Harmony 运行时补丁规范

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

## 三、 构建与部署

仓库配置了全自动热部署流：

```bash
# 全量构建解决方案并部署全部组件
dotnet build tPlainModLoader/tPlainModLoader/tPlainModLoader.sln -c Release -m /graph

# 单个模组独立构建与部署
dotnet build tPlainModLoader/Mods/<ModName>/<ModName>.csproj -c Release
```
