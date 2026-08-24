# tPlainModLoader (TPML)

<div align="center">

**面向原版 Terraria 的轻量级模组加载与扩容框架**

[![Platform](https://img.shields.io/badge/Platform-Windows%20(x86)-blue.svg)]()
[![Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-purple.svg)]()
[![Terraria](https://img.shields.io/badge/Terraria-1.4.4%2B%20%7C%201.4.5%2B-green.svg)]()
[![License](https://img.shields.io/badge/License-MIT-orange.svg)](LICENSE)

</div>

---

## 📌 项目概述

`tPlainModLoader` (TPML) 是一个面向官方原版 Terraria (`Terraria.exe`) 的模组加载与补丁框架。

### 核心特性
- **直接运行于原版游戏**：基于原版客户端直接启动，保持对原生存档格式（`.plr` / `.wld`）的完整兼容；
- **公有化预处理 (Publicizer)**：启动阶段通过 Mono.Cecil 对游戏程序集进行成员公开化，支持开发者在编译期和运行期以强类型直连访问原版内部成员；
- **Prepatcher 预修补机制**：支持通过 `[PrepatcherField]` 向原生类动态注入实例字段并改写访问器 IL，同时支持 `IPrepatcher` 早期 Cecil 预补丁；
- **原生级统一按键框架 (KeybindLoader)**：模组快捷键自动注入原版控件设置界面与输入配置；
- **4GB 虚拟内存感知 (LargeAddressAware)**：构建流自动注入 LAA 标志，扩展 32 位寻址上限。

---

## 📖 完整使用与开发文档

关于启动器配置、模组安装、开发起步以及 API 接入的完整说明，请参阅独立的文档手册：

👉 **[使用与开发手册 (USAGE.md)](USAGE.md)**

该文档包含：
1. **玩家指南**：运行环境要求、`launchConfig.json` 路径配置、模组安装与 `enabled.json` 状态管理；
2. **开发者指南**：模组项目结构、`Mod` 生命周期、`[PrepatcherField]` 字段注入范例、`KeybindLoader` 快捷键接入与 Harmony 补丁编写规范；
3. **构建与部署**：MSBuild 自动化构建与自动部署命令。

---

## 📦 附带模组生态

TPML 仓库内包含 14 个官方扩展模组与实用工具（如 `ReduceMouseLag`、`PipetteTool`、`VeinMining`、`WandsTool`、`CreativeInventory`、`QuickSetting`、`QuickButton` 等）。

👉 各模组的详细功能介绍与操作指南请参阅：**[附带模组总览手册 (Mods/README.md)](Mods/README.md)**

---

## 📄 致谢与依赖

- **Harmony**：[Lib.Harmony](https://github.com/pardeike/Harmony/)
- **Mono.Cecil**：[jbevain/cecil](https://github.com/jbevain/cecil)
- **Newtonsoft.Json**：[JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json)
- **Prepatcher 架构参考**：[Zetrith/Prepatcher](https://github.com/Zetrith/Prepatcher)
- **Author**：`SaintCirno9`
