using CommandHelp;
using OptimizeAndTool.Content.EnhancedTooltips;
using OptimizeAndTool.Content.Optimize.ReduceMouseLag;
using OptimizeAndTool.Content.QoL;
using OptimizeAndTool.Content.QoL.Fishing;
using OptimizeAndTool.Content.QoL.GuaranteedDrop;
using OptimizeAndTool.Content.QoL.Pipette;
using OptimizeAndTool.Content.QoL.Reforge;
using OptimizeAndTool.Content.QoL.VeinMining;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Utils;
using System.Collections.Generic;
using Terraria.UI;

namespace OptimizeAndTool.Content
{
    /// <summary>
    /// 功能模块装配门面
    /// 通过 ModuleRegistry 统一契约与自注册系统进行各模块 UI 与命令行的集中装配与构建分发，彻底消除硬编码遗漏风险。
    /// 作者: SaintCirno9
    /// </summary>
    internal partial class Function : TPML.Content.ModPlayer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        private static void EnsureRegistryInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;

                // 结构化注册所有 40+ 个功能模块契约项（UI 委托、Command 委托、分类归属与排序）
                ModuleRegistry.RegisterModules(GetModuleDefinitions());
                _initialized = true;
            }
        }

        private static IEnumerable<ModuleRegistration> GetModuleDefinitions()
        {
            // 0. 常用基础与聊天 (General - 无分类标题)
            yield return new ModuleRegistration("CleanRepeatChat", ModuleCategory.General, CleanRepeatChat.GetUI, CleanRepeatChat.GetCO, 0, 10);
            yield return new ModuleRegistration("CopyChat", ModuleCategory.General, CopyChat.GetUI, CopyChat.GetCO, 10, 20);
            yield return new ModuleRegistration("ServerList", ModuleCategory.General, ServerList.ServerList.GetUI, ServerList.ServerList.GetCO, 20, 30);
            yield return new ModuleRegistration("ItemToolTipAdditional", ModuleCategory.General, ItemToolTipAdditional.GetUI, ItemToolTipAdditional.GetCO, 30, 40);
            yield return new ModuleRegistration("EnhancedTooltipConfig", ModuleCategory.General, EnhancedTooltipConfig.GetUI, EnhancedTooltipConfig.GetCO, 40, 50);

            // 1. 性能与输入优化 (Optimize - Images/Item_5010)
            yield return new ModuleRegistration("MouseLagFixEngine", ModuleCategory.Optimize, MouseLagFixEngine.GetUI, MouseLagFixEngine.GetCO, 0, 70);
            yield return new ModuleRegistration("GameViewMatrixZoomLimitHooks", ModuleCategory.Optimize, GameViewMatrixZoomLimitHooks.GetUI, GameViewMatrixZoomLimitHooks.GetCO, 10, 80);

            // 2. 扩展存储系统 (Storage - Images/Item_3813)
            yield return new ModuleRegistration("BigBag", ModuleCategory.Storage, Content.BigBag.BigBag.GetUI, Content.BigBag.BigBag.GetCO, 0, 90);
            yield return new ModuleRegistration("AccessoryBagConfig", ModuleCategory.Storage, AccessoryBagConfig.GetUI, AccessoryBagConfig.GetCO, 10, 100);

            // 3. 采矿与建造体验 (MiningAndBuilding - Images/Item_3509)
            yield return new ModuleRegistration("VeinMiningLogic", ModuleCategory.MiningAndBuilding, VeinMiningLogic.GetUI, VeinMiningLogic.GetCO, 0, 110);
            yield return new ModuleRegistration("PipetteEngine", ModuleCategory.MiningAndBuilding, PipetteEngine.GetUI, PipetteEngine.GetCO, 10, 120);

            // 4. 便携制作与堆叠 (Crafting - Images/Item_361)
            yield return new ModuleRegistration("ItemMaxStackPatch", ModuleCategory.Crafting, ItemMaxStackPatch.GetUI, ItemMaxStackPatch.GetCO, 0, 130);
            yield return new ModuleRegistration("UncapMaxLifePatch", ModuleCategory.Crafting, UncapMaxLifePatch.GetUI, UncapMaxLifePatch.GetCO, 10, 140);
            yield return new ModuleRegistration("PortableCraftingStation", ModuleCategory.Crafting, PortableCraftingStation.GetUI, PortableCraftingStation.GetCO, 20, 150);
            yield return new ModuleRegistration("UniversalCraftingEnvironment", ModuleCategory.Crafting, UniversalCraftingEnvironment.GetUI, UniversalCraftingEnvironment.GetCO, 30, 160);
            yield return new ModuleRegistration("PortableContainer", ModuleCategory.Crafting, PortableContainer.GetUI, PortableContainer.GetCO, 40, 170);

            // 5. 无尽药水与增益 (Potion - Images/Item_289)
            yield return new ModuleRegistration("InfinitePotionAndBuff", ModuleCategory.Potion, InfinitePotionAndBuff.GetUI, InfinitePotionAndBuff.GetCO, 0, 180);

            // 6. 城镇 NPC 与商贩 (TownNPC - Images/Item_267)
            yield return new ModuleRegistration("TownNPCOptimization", ModuleCategory.TownNPC, TownNPCOptimization.GetUI, TownNPCOptimization.GetCO, 0, 190);
            yield return new ModuleRegistration("ReforgeOptimization", ModuleCategory.TownNPC, ReforgeOptimization.GetUI, ReforgeOptimization.GetCO, 10, 200);

            // 7. 渔夫任务与钓鱼 QoL (Fishing - Images/Item_2422)
            yield return new ModuleRegistration("AnglerQuestOptimization", ModuleCategory.Fishing, AnglerQuestOptimization.GetUI, AnglerQuestOptimization.GetCO, 0, 210);
            yield return new ModuleRegistration("FishingCrateModifier", ModuleCategory.Fishing, FishingCrateModifier.GetUI, FishingCrateModifier.GetCO, 10, 220);
            yield return new ModuleRegistration("AutoFishingSystem", ModuleCategory.Fishing, AutoFishingSystem.GetUI, AutoFishingSystem.GetCO, 20, 230);
            yield return new ModuleRegistration("MultipleFishingLines", ModuleCategory.Fishing, MultipleFishingLines.GetUI, MultipleFishingLines.GetCO, 30, 240);
            yield return new ModuleRegistration("FishingCatchProcessor", ModuleCategory.Fishing, FishingCatchProcessor.GetUI, FishingCatchProcessor.GetCO, 40, 250);
            yield return new ModuleRegistration("AutoFishingSupplies", ModuleCategory.Fishing, AutoFishingSupplies.GetUI, AutoFishingSupplies.GetCO, 50, 260);
            yield return new ModuleRegistration("FishingInfoHUD", ModuleCategory.Fishing, FishingInfoHUD.GetUI, FishingInfoHUD.GetCO, 60, 270);

            // 8. 消耗、掉落与死亡规则 (DropAndDeath - Images/Item_6)
            yield return new ModuleRegistration("NoConsumeItems", ModuleCategory.DropAndDeath, NoConsumeItems.GetUI, NoConsumeItems.GetCO, 0, 280);
            yield return new ModuleRegistration("GuaranteedDropSystem", ModuleCategory.DropAndDeath, GuaranteedDropSystem.GetUI, GuaranteedDropSystem.GetCO, 10, 290);
            yield return new ModuleRegistration("BannerAndBestiary", ModuleCategory.DropAndDeath, BannerAndBestiary.GetUI, BannerAndBestiary.GetCO, 20, 300);
            yield return new ModuleRegistration("SlimeAndLava", ModuleCategory.DropAndDeath, SlimeAndLava.GetUI, SlimeAndLava.GetCO, 30, 310);
            yield return new ModuleRegistration("DeathAndDamage", ModuleCategory.DropAndDeath, DeathAndDamage.GetUI, DeathAndDamage.GetCO, 40, 320);
            yield return new ModuleRegistration("FasterExtractinator", ModuleCategory.DropAndDeath, FasterExtractinator.GetUI, FasterExtractinator.GetCO, 50, 330);
            // EcoGrowth: UI 选项已内嵌于 QoLValSet 统一呈现，此处仅提供命令对象以防 UI 重复
            yield return new ModuleRegistration("EcoGrowth", ModuleCategory.DropAndDeath, null, EcoGrowth.GetCO, 60, 340);

            // 9. 经济、事件与环境规则 (EconomyAndWorld - Images/Item_73)
            yield return new ModuleRegistration("Economy", ModuleCategory.EconomyAndWorld, Economy.GetUI, Economy.GetCO, 0, 350);
            yield return new ModuleRegistration("KeepBuffsOnDeath", ModuleCategory.EconomyAndWorld, KeepBuffsOnDeath.GetUI, KeepBuffsOnDeath.GetCO, 10, 360);
            yield return new ModuleRegistration("ExpertDebuffTime", ModuleCategory.EconomyAndWorld, ExpertDebuffTime.GetUI, ExpertDebuffTime.GetCO, 20, 370);
            yield return new ModuleRegistration("TownNPCSpawnSpeed", ModuleCategory.EconomyAndWorld, TownNPCSpawnSpeed.GetUI, TownNPCSpawnSpeed.GetCO, 30, 380);
            yield return new ModuleRegistration("NoBiomeSpread", ModuleCategory.EconomyAndWorld, NoBiomeSpread.GetUI, NoBiomeSpread.GetCO, 40, 390);
            yield return new ModuleRegistration("NoConditionTeamTP", ModuleCategory.EconomyAndWorld, NoConditionTeamTP.GetUI, NoConditionTeamTP.GetCO, 50, 400);

            // 10. 床、晶塔与多人协作 (BedAndPylon - Images/Item_2129)
            yield return new ModuleRegistration("BedRules", ModuleCategory.BedAndPylon, BedRules.GetUI, BedRules.GetCO, 0, 410);
            yield return new ModuleRegistration("PylonRules", ModuleCategory.BedAndPylon, PylonRules.GetUI, PylonRules.GetCO, 10, 420);
            yield return new ModuleRegistration("KeepRunningWhenUnfocused", ModuleCategory.BedAndPylon, KeepRunningWhenUnfocused.GetUI, KeepRunningWhenUnfocused.GetCO, 20, 430);
            yield return new ModuleRegistration("TeamShare", ModuleCategory.BedAndPylon, TeamShare.GetUI, TeamShare.GetCO, 30, 440);

            // 11. 杂项辅助 (玩家能力) (CheatPlayer - Images/Item_1326)
            yield return new ModuleRegistration("CheatFunction1", ModuleCategory.CheatPlayer, Cheat.Function1.PlayerCheatFunctions.GetUI, Cheat.Function1.PlayerCheatFunctions.GetCO, 0, 460);

            // 12. 杂项辅助 (世界与环境) (CheatWorld - Images/Item_2997)
            yield return new ModuleRegistration("CheatFunction2", ModuleCategory.CheatWorld, Cheat.Function2.WorldCheatFunctions.GetUI, Cheat.Function2.WorldCheatFunctions.GetCO, 0, 470);

            // 13. 杂项 QoL 增强 (CheatQoL - Images/Item_3611)
            yield return new ModuleRegistration("QoLValSet", ModuleCategory.CheatQoL, QoLValSet.GetUI, QoLValSet.GetCO, 0, 450);

            // 14. 手持物品与属性微调 (HeldItemAndPlayerModify - Images/Item_3095)
            yield return new ModuleRegistration("HeldItemModify", ModuleCategory.HeldItemAndPlayerModify, Cheat.HeldItemModify.ValSet.GetUI, Cheat.HeldItemModify.ValSet.GetCO, 0, 480);
            yield return new ModuleRegistration("PlayerModify", ModuleCategory.HeldItemAndPlayerModify, Cheat.PlayerModify.ValSet.GetUI, Cheat.PlayerModify.ValSet.GetCO, 10, 490);

            // 15. 调试与信息显示 (Debug - Images/Item_2799)
            // CommandOrder 设为 60（位于 EnhancedTooltipConfig: 50 之后，MouseLagFixEngine: 70 之前），保持命令树顺序严格一致
            yield return new ModuleRegistration("DisplayProjectileInfo", ModuleCategory.Debug, DisplayProjectileInfo.GetUI, DisplayProjectileInfo.GetCO, 0, 60);
        }

        public static List<CommandObject> GetCO()
        {
            EnsureRegistryInitialized();
            return ModuleRegistry.BuildCommands();
        }

        public static List<UIElement> GetUI()
        {
            EnsureRegistryInitialized();
            return ModuleRegistry.BuildUI();
        }
    }
}
