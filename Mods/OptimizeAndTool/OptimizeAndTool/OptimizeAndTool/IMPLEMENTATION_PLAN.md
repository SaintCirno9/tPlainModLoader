# 实施计划：巨大额外背包（BigBag）

> 作者: SaintCirno9　日期: 2026-08-23
> 目标工程: OptimizeAndTool（优化和工具）

## 一、需求

类似 ImproveGame（更好的体验）的巨大额外背包：一个随身携带的大容量仓库，随时打开存取物品，其中的材料可参与制作。

## 二、设计核心：第 5 个随身 Chest

大背包内部就是一个 `Item[Capacity]`，包装为一个 `Chest` 实例（`bankChest = true`）。这样：

- **材料参与制作零成本**：复用 `PortableContainer` 已打通的机制——Chest 加进 `Recipe._recipeChests` 即同时获得可用判定与消耗扣除，`bankChest = true` 纯本地扣除，多人安全；
- **格子交互整套复用原版**：`ItemSlot.Handle(Item[], context, slot)`（已验证存在）+ `ItemSlot.Context.BankItem = 4`——左键拿起/放下/交换/堆叠、右键半取/单个放、**Shift 在背包↔容器间快速转移**全部是原版存钱罐同款行为，无需自研交互；
- **绘制复用原版**：`ItemSlot.Draw(sb, inv, context, slot, pos, lightColor)` + `ItemSlot.MouseHover`（tooltip、堆叠数、前缀）。

## 三、文件结构（OptimizeAndTool 内新增）

```
Content/BigBag/
  BigBag.cs                    // 存储 Item[]、Chest 包装、存取 API、持久化(ModSetting)
  BigBagWindow.cs              // UIWindow + UIScrollViewer + UIWrapPanel 格子窗口
  BigBagItem.cs                // 格子 UI 元素：封装 ItemSlot.Handle/Draw（context=BankItem）
  ModifyInterfaceLayers.cs     // UIState/接口层插入（样板抄 AccessoryBox，PatchMain 钩子）
ModLinkage/ModQuickButton.cs   // QuickButton 按钮（反射软联动，抄 AccessoryBox）+ 快捷键（默认 B）
```

修改既有文件：

- `Content/QoL/PortableContainer.cs`：增加静态注册点 `RegisterContainer(Chest)`，扫描随身容器时一并纳入（开关 `portableContainerCraft` 继续控制）；
- `Content/Function.cs`：注册 `GetCO()/GetUI()`（"背包与便携制作"分组）。

## 四、持久化

- `ModSetting` 子类，`FilePath = "bigBag.json"`，数据 `List<SlotData> { type, prefix, stack }`（比饰品箱多存 stack）；
- 时机：模组 `Load` 时读取构建 `Item[]`；窗口关闭/退出对局时保存（参照饰品箱 `SavePlayerPostfix` 时机 + 窗口操作后即时保存，避免丢档）；
- 容量调整时：扩容补空格，缩容截断尾部并尝试溢出物回背包/掉落。

## 五、配置

- `EnableBigBagCraft`（大背包材料参与制作，默认开启，实际生效同时受 `portableContainerCraft` 约束）；
- `BigBagCapacity`（容量滑条，默认 100，范围 40~500）。

## 六、打开入口

1. QuickButton 悬浮工具栏按钮（反射软联动 `QuickButton.Add`，悬停提示"巨大背包"）；
2. 快捷键默认 **B**（对局中物品栏任意状态可开，监听样板参照 CreativeInventory 的 ListenInput）。

## 七、风险与对策

- **ItemSlot.Handle 内部状态**：BankItem context 可能访问 `player.chest` 相关逻辑——大背包纯自绘窗口、不设置 `player.chest`，验证时重点测试 shift 转移与堆叠；
- **UI 性能**：500 格上限时 UIWrapPanel 一次性创建 500 个轻量元素，滚动裁剪下遍历开销可接受；默认 100 无压力；
- **存档安全**：窗口内每次物品操作后即时保存 JSON（低频写盘），另挂 `SavePlayerPostfix` 兜底。

## 八、验证

1. `dotnet build -c Release` 0 警告 0 错误，自动部署；
2. 游戏内：
   - B 键 / QuickButton 按钮打开关闭窗口；
   - 存入/取出/右键半取/Shift 快速转移/堆叠合并，tooltip 正常；
   - 重启游戏后物品与堆叠数完好；
   - 大背包内材料参与制作并可被消耗扣除；
   - 容量 40↔500 调整不丢物品；
   - 关闭 `portableContainerCraft` 后大背包材料退出制作判定。
3. 更新 `docs/tPlainModLoader/WALKTHROUGH.md`。
