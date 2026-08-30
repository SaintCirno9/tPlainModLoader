using System;
using OptimizeAndTool.Content.Storage.ItemContainer;
using tContentPatch;
using tContentPatch.Patch;
using TPML.Content;
using OptimizeAndTool.Content.Storage.AccessoryBox;

namespace OptimizeAndTool.Content.Patch
{
    /// <summary>
    /// OptimizeAndTool 模组生命周期与强类型 MonoMod 门控总入口
    /// 作者: SaintCirno9
    /// </summary>
    internal class OptimizeAndToolHookInit : tContentPatch.Mod
    {
        public static OptimizeAndToolContentMod ContentModInstance { get; private set; }

        public override void Load()
        {
            try
            {
                // 内容模组由统一 ContentHost 自动注册并触发 Load，入口只保留旧引擎钩子职责
                ContentModInstance = ContentHost.Find<OptimizeAndToolContentMod>();
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndTool] Content 注册异常: {ex}");
            }
        }

        public override void AddPatch(IAddPatch addPatch) // 添加强类型 MonoMod 门控
        {
            // 批次 1: 渲染与客户端优化
            Optimize.ReduceMouseLag.ReduceMouseLagHooks.RegisterAll();
            GameViewMatrixZoomLimitHooks.RegisterAll();
            Cheat.HeldItemModify.SmartSelectRangeHooks.RegisterAll();
            QoL.KeepRunningWhenUnfocused.RegisterAll();
            QoL.PortableCraftingStation.RegisterAll();

            // 批次 2: 背包与存储系统
            BigBag.BigBagPickupHooks.RegisterAll();
            BigBag.BigBagShiftTransferHooks.RegisterAll();
            BigBag.HotbarScrollHooks.RegisterAll();
            Storage.AccessoryBox.AccessoryBagInteractionHooks.RegisterAll();
            Storage.ItemContainer.ItemContainerInteractionHooks.RegisterAll();
            QoL.PortableContainerHooks.RegisterAll();

            // 批次 3: 钓鱼增强子系统
            QoL.Fishing.AutoFishingSuppliesHooks.RegisterAll();
            QoL.Fishing.AutoFishingSystemHooks.RegisterAll();
            QoL.Fishing.FishingCrateModifierHooks.RegisterAll();
            QoL.Fishing.FishingInfoHUDHooks.RegisterAll();
            QoL.Fishing.MultipleFishingLinesHooks.RegisterAll();
            QoL.AnglerQuestOptimizationHooks.RegisterAll();

            // 批次 4: 物块破坏与防作祟
            QoL.VeinMining.PlayerPickTileHooks.RegisterAll();
            Cheat.QoL.AntiGriefHooks.RegisterAll();

            // 批次 5.1: 世界规则与生态优化
            Cheat.QoL.EcologyHooks.RegisterAll();
            Cheat.QoL.PylonHooks.RegisterAll();
            QoL.PylonRuleHooks.RegisterAll();
            QoL.EcoGrowthHooks.RegisterAll();
            QoL.SlimeAndLavaHooks.RegisterAll();
            QoL.BedRulesHooks.RegisterAll();
            QoL.TownNPCOptimizationHooks.RegisterAll();
            QoL.TownNPCSpawnSpeedHooks.RegisterAll();
            QoL.FasterExtractinatorHooks.RegisterAll();

            // 批次 5.2: 玩家战斗、Buff 与团队
            QoL.DeathAndDamageHooks.RegisterAll();
            QoL.ExpertDebuffTimeHooks.RegisterAll();
            QoL.KeepBuffsOnDeathHooks.RegisterAll();
            QoL.NoConditionTeamTPHooks.RegisterAll();
            QoL.NoConsumeItemHooks.RegisterAll();
            QoL.TeamShareHooks.RegisterAll();
            QoL.UncapMaxLifeHooks.RegisterAll();
            QoL.ItemMaxStackHooks.RegisterAll();
            QoL.BannerAndBestiaryHooks.RegisterAll();

            // 批次 5.3: 无限 Buff、保底掉落与重铸
            QoL.InfiniteBuff.BuffInteractionHooks.RegisterAll();
            QoL.InfinitePotionAndBuffHooks.RegisterAll();
            QoL.GuaranteedDrop.GuaranteedDropHooks.RegisterAll();
            QoL.Reforge.ReforgeHooks.RegisterAll();
        }

        public override void Unload()
        {
            // 批次 5.3 注销
            QoL.Reforge.ReforgeHooks.UnregisterAll();
            QoL.GuaranteedDrop.GuaranteedDropHooks.UnregisterAll();
            QoL.InfinitePotionAndBuffHooks.UnregisterAll();
            QoL.InfiniteBuff.BuffInteractionHooks.UnregisterAll();

            // 批次 5.2 注销
            QoL.BannerAndBestiaryHooks.UnregisterAll();
            QoL.ItemMaxStackHooks.UnregisterAll();
            QoL.UncapMaxLifeHooks.UnregisterAll();
            QoL.TeamShareHooks.UnregisterAll();
            QoL.NoConsumeItemHooks.UnregisterAll();
            QoL.NoConditionTeamTPHooks.UnregisterAll();
            QoL.KeepBuffsOnDeathHooks.UnregisterAll();
            QoL.ExpertDebuffTimeHooks.UnregisterAll();
            QoL.DeathAndDamageHooks.UnregisterAll();

            // 批次 5.1 注销
            QoL.FasterExtractinatorHooks.UnregisterAll();
            QoL.TownNPCSpawnSpeedHooks.UnregisterAll();
            QoL.TownNPCOptimizationHooks.UnregisterAll();
            QoL.BedRulesHooks.UnregisterAll();
            QoL.SlimeAndLavaHooks.UnregisterAll();
            QoL.EcoGrowthHooks.UnregisterAll();
            QoL.PylonRuleHooks.UnregisterAll();
            Cheat.QoL.PylonHooks.UnregisterAll();
            Cheat.QoL.EcologyHooks.UnregisterAll();

            // 批次 4 注销
            Cheat.QoL.AntiGriefHooks.UnregisterAll();
            QoL.VeinMining.PlayerPickTileHooks.UnregisterAll();

            // 批次 3 注销
            QoL.AnglerQuestOptimizationHooks.UnregisterAll();
            QoL.Fishing.MultipleFishingLinesHooks.UnregisterAll();
            QoL.Fishing.FishingInfoHUDHooks.UnregisterAll();
            QoL.Fishing.FishingCrateModifierHooks.UnregisterAll();
            QoL.Fishing.AutoFishingSystemHooks.UnregisterAll();
            QoL.Fishing.AutoFishingSuppliesHooks.UnregisterAll();

            // 批次 2 注销
            QoL.PortableContainerHooks.UnregisterAll();
            Storage.ItemContainer.ItemContainerInteractionHooks.UnregisterAll();
            Storage.AccessoryBox.AccessoryBagInteractionHooks.UnregisterAll();
            BigBag.HotbarScrollHooks.UnregisterAll();
            BigBag.BigBagShiftTransferHooks.UnregisterAll();
            BigBag.BigBagPickupHooks.UnregisterAll();

            // 批次 1 注销
            QoL.PortableCraftingStation.UnregisterAll();
            QoL.KeepRunningWhenUnfocused.UnregisterAll();
            Cheat.HeldItemModify.SmartSelectRangeHooks.UnregisterAll();
            GameViewMatrixZoomLimitHooks.UnregisterAll();
            Optimize.ReduceMouseLag.ReduceMouseLagHooks.UnregisterAll();
        }
    }

    /// <summary>
    /// 兼容旧入口类型
    /// </summary>
    internal class PatchInit : OptimizeAndToolHookInit
    {
    }

    public class OptimizeAndToolContentMod : TPML.Content.Mod
    {
        public override string Name => "OptimizeAndTool";
        public override string DisplayName => "优化与实用工具 (OptimizeAndTool)";

        public override void Load()
        {
            try
            {
                AddContent(new PotionBagItem());
                AddContent(new BannerChestItem());
                AddContent(new AccessoryBagItem());
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndToolContentMod] 注册内容异常: {ex}");
            }
        }
    }
}
