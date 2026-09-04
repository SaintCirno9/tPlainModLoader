using CommandHelp;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.UI;
using TPML.UI.ModSet;

namespace OptimizeAndTool.Utils
{
    /// <summary>
    /// 功能模块分类枚举（对齐设置界面的视觉与逻辑分组）
    /// 作者: SaintCirno9
    /// </summary>
    public enum ModuleCategory
    {
        /// <summary>常用基础与聊天（无独立分类标题）</summary>
        General = 0,
        /// <summary>性能与输入优化 (Images/Item_5010)</summary>
        Optimize,
        /// <summary>扩展存储系统 (Images/Item_3813)</summary>
        Storage,
        /// <summary>采矿与建造体验 (Images/Item_3509)</summary>
        MiningAndBuilding,
        /// <summary>便携制作与堆叠 (Images/Item_361)</summary>
        Crafting,
        /// <summary>无尽药水与增益 (Images/Item_289)</summary>
        Potion,
        /// <summary>城镇 NPC 与商贩 (Images/Item_267)</summary>
        TownNPC,
        /// <summary>渔夫任务与钓鱼 QoL (Images/Item_2422)</summary>
        Fishing,
        /// <summary>消耗、掉落与死亡规则 (Images/Item_6)</summary>
        DropAndDeath,
        /// <summary>经济、事件与环境规则 (Images/Item_73)</summary>
        EconomyAndWorld,
        /// <summary>床、晶塔与多人协作 (Images/Item_2129)</summary>
        BedAndPylon,
        /// <summary>杂项辅助 (玩家能力) (Images/Item_1326)</summary>
        CheatPlayer,
        /// <summary>杂项辅助 (世界与环境) (Images/Item_2997)</summary>
        CheatWorld,
        /// <summary>杂项 QoL 增强 (Images/Item_3611)</summary>
        CheatQoL,
        /// <summary>手持物品与属性微调 (Images/Item_3095)</summary>
        HeldItemAndPlayerModify,
        /// <summary>调试与信息显示 (Images/Item_2799)</summary>
        Debug
    }

    /// <summary>
    /// 分类元数据定义
    /// 作者: SaintCirno9
    /// </summary>
    public sealed class CategoryMetadata
    {
        public ModuleCategory Category { get; }
        public string Title { get; }
        public string IconTexturePath { get; }
        public int Order { get; }

        public CategoryMetadata(ModuleCategory category, string title, string iconTexturePath, int order)
        {
            Category = category;
            Title = title;
            IconTexturePath = iconTexturePath;
            Order = order;
        }
    }

    /// <summary>
    /// 统一功能模块契约接口
    /// 作者: SaintCirno9
    /// </summary>
    public interface IOptimizeModule
    {
        string Name { get; }
        ModuleCategory Category { get; }
        int UIOrder { get; }
        int CommandOrder { get; }
        List<UIElement> GetUI();
        List<CommandObject> GetCommands();
    }

    /// <summary>
    /// 结构化模块注册条目
    /// 作者: SaintCirno9
    /// </summary>
    public sealed class ModuleRegistration
    {
        public string Name { get; }
        public ModuleCategory Category { get; }
        public Func<List<UIElement>> UIProvider { get; }
        public Func<List<CommandObject>> CommandProvider { get; }
        public int UIOrder { get; }
        public int CommandOrder { get; }

        public ModuleRegistration(
            string name,
            ModuleCategory category,
            Func<List<UIElement>> uiProvider = null,
            Func<List<CommandObject>> commandProvider = null,
            int uiOrder = 0,
            int commandOrder = 0)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Category = category;
            UIProvider = uiProvider;
            CommandProvider = commandProvider;
            UIOrder = uiOrder;
            CommandOrder = commandOrder;
        }

        public static ModuleRegistration FromModule(IOptimizeModule module)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            return new ModuleRegistration(
                module.Name,
                module.Category,
                module.GetUI,
                module.GetCommands,
                module.UIOrder,
                module.CommandOrder
            );
        }
    }

    /// <summary>
    /// 统一功能模块契约与注册分发中心
    /// 集中管理所有模块的 UI 与命令行自注册，消除硬编码 AddRange 脆弱装配与漏挂风险。
    /// 作者: SaintCirno9
    /// </summary>
    public static class ModuleRegistry
    {
        private static readonly List<CategoryMetadata> _categories = new List<CategoryMetadata>();
        private static readonly List<ModuleRegistration> _modules = new List<ModuleRegistration>();
        private static readonly object _lock = new object();

        static ModuleRegistry()
        {
            RegisterDefaultCategories();
        }

        private static void RegisterDefaultCategories()
        {
            _categories.Clear();
            _categories.Add(new CategoryMetadata(ModuleCategory.General, null, null, 0));
            _categories.Add(new CategoryMetadata(ModuleCategory.Optimize, "性能与输入优化", "Images/Item_5010", 10));
            _categories.Add(new CategoryMetadata(ModuleCategory.Storage, "扩展存储系统", "Images/Item_3813", 20));
            _categories.Add(new CategoryMetadata(ModuleCategory.MiningAndBuilding, "采矿与建造体验", "Images/Item_3509", 30));
            _categories.Add(new CategoryMetadata(ModuleCategory.Crafting, "便携制作与堆叠", "Images/Item_361", 40));
            _categories.Add(new CategoryMetadata(ModuleCategory.Potion, "无尽药水与增益", "Images/Item_289", 50));
            _categories.Add(new CategoryMetadata(ModuleCategory.TownNPC, "城镇 NPC 与商贩", "Images/Item_267", 60));
            _categories.Add(new CategoryMetadata(ModuleCategory.Fishing, "渔夫任务与钓鱼 QoL", "Images/Item_2422", 70));
            _categories.Add(new CategoryMetadata(ModuleCategory.DropAndDeath, "消耗、掉落与死亡规则", "Images/Item_6", 80));
            _categories.Add(new CategoryMetadata(ModuleCategory.EconomyAndWorld, "经济、事件与环境规则", "Images/Item_73", 90));
            _categories.Add(new CategoryMetadata(ModuleCategory.BedAndPylon, "床、晶塔与多人协作", "Images/Item_2129", 100));
            _categories.Add(new CategoryMetadata(ModuleCategory.CheatPlayer, "杂项辅助 (玩家能力)", "Images/Item_1326", 110));
            _categories.Add(new CategoryMetadata(ModuleCategory.CheatWorld, "杂项辅助 (世界与环境)", "Images/Item_2997", 120));
            _categories.Add(new CategoryMetadata(ModuleCategory.CheatQoL, "杂项 QoL 增强", "Images/Item_3611", 130));
            _categories.Add(new CategoryMetadata(ModuleCategory.HeldItemAndPlayerModify, "手持物品与属性微调", "Images/Item_3095", 140));
            _categories.Add(new CategoryMetadata(ModuleCategory.Debug, "调试与信息显示", "Images/Item_2799", 150));
        }

        /// <summary>
        /// 注册单个模块
        /// </summary>
        public static void Register(ModuleRegistration registration)
        {
            if (registration == null) return;
            lock (_lock)
            {
                _modules.Add(registration);
            }
        }

        /// <summary>
        /// 注册实现契约接口的模块
        /// </summary>
        public static void Register(IOptimizeModule module)
        {
            if (module == null) return;
            Register(ModuleRegistration.FromModule(module));
        }

        /// <summary>
        /// 批量注册模块列表
        /// </summary>
        public static void RegisterModules(IEnumerable<ModuleRegistration> registrations)
        {
            if (registrations == null) return;
            lock (_lock)
            {
                foreach (var reg in registrations)
                {
                    if (reg != null)
                    {
                        _modules.Add(reg);
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有已注册的模块（只读快照）
        /// </summary>
        public static IReadOnlyList<ModuleRegistration> GetRegisteredModules()
        {
            lock (_lock)
            {
                return _modules.ToList();
            }
        }

        /// <summary>
        /// 统一构建完整的设置界面 UI 元素列表（分类标题与模块 UI 元素）
        /// </summary>
        public static List<UIElement> BuildUI()
        {
            List<UIElement> uis = new List<UIElement>();
            List<CategoryMetadata> categoriesCopy;
            List<ModuleRegistration> modulesCopy;

            lock (_lock)
            {
                categoriesCopy = _categories.OrderBy(c => c.Order).ToList();
                modulesCopy = _modules.ToList();
            }

            foreach (var category in categoriesCopy)
            {
                var catModules = modulesCopy
                    .Where(m => m.Category == category.Category && m.UIProvider != null)
                    .OrderBy(m => m.UIOrder)
                    .ToList();

                if (catModules.Count == 0) continue;

                List<UIElement> catUIs = new List<UIElement>();
                foreach (var module in catModules)
                {
                    var items = module.UIProvider?.Invoke();
                    if (items != null && items.Count > 0)
                    {
                        catUIs.AddRange(items);
                    }
                }

                if (catUIs.Count > 0)
                {
                    // 仅当分类有显式标题和图标时生成 UIItemTitle
                    if (!string.IsNullOrEmpty(category.Title) && !string.IsNullOrEmpty(category.IconTexturePath))
                    {
                        var texture = Main.Assets.Request<Texture2D>(category.IconTexturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                        uis.Add(new UIItemTitle(texture, category.Title));
                    }
                    uis.AddRange(catUIs);
                }
            }

            return uis;
        }

        /// <summary>
        /// 统一构建完整的命令行对象列表
        /// </summary>
        public static List<CommandObject> BuildCommands()
        {
            List<CommandObject> cos = new List<CommandObject>();
            List<ModuleRegistration> modulesCopy;

            lock (_lock)
            {
                modulesCopy = _modules
                    .Where(m => m.CommandProvider != null)
                    .OrderBy(m => m.CommandOrder)
                    .ToList();
            }

            foreach (var module in modulesCopy)
            {
                var cmds = module.CommandProvider?.Invoke();
                if (cmds != null && cmds.Count > 0)
                {
                    cos.AddRange(cmds);
                }
            }

            return cos;
        }
    }
}
