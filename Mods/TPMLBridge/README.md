# TPMLBridge — tPlainModLoader GABP 自动化桥接模组

> **作者**：`SaintCirno9`  
> **协议**：Game Agent Bridge Protocol v1.1 (`gabp/1`)  
> **端口**：默认 `127.0.0.1:49153`（支持 `GABP_SERVER_PORT` 环境变量覆盖）

---

## 📌 模组定位与特性

`TPMLBridge` 是面向 `tPlainModLoader` 原生环境的自动化控制与测试桥接模组：
- **对接 GABS MCP 服务器**：让 AI 代理（OMP / Codex 等）能够直接以 MCP 工具形式调用游戏底层 API；
- **秒级直入存档**：跳过所有主菜单过渡动画与选人选图步骤，调用 `WorldGen.playWorld()` 在 ~1 秒内直入世界；
- **无感数据读写**：毫秒级直读/直控玩家背包 58 格物品、坐标传送、血量恢复、聊天指令与界面开关；
- **零性能损耗**：无客户端连接时保持轻量 Socket 待机，主线程调度队列仅在有任务时介入。

---

## 📖 完整接口文档与自动化测试指南

详细的协议定义、10 项核心 API 规范及 PowerShell / Python 自动化测试脚本，请参阅全局文档手册：

👉 **[GABP 游戏自动化测试与调试指南 (docs/tPlainModLoader/GABP_AUTOMATION_GUIDE.md)](../../../docs/tPlainModLoader/GABP_AUTOMATION_GUIDE.md)**
