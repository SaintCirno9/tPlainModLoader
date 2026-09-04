using System;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Content.Storage.ItemContainer;
using OptimizeAndTool.Utils;
using TPML;
using TPML.Content;
using TPML.Patch;

namespace OptimizeAndTool.Content.Patch
{
    /// <summary>
    /// OptimizeAndTool 模组生命周期与强类型 MonoMod 门控总入口
    /// 作者: SaintCirno9
    /// </summary>
    internal class OptimizeAndToolHookInit : TPML.Mod
    {
        public static OptimizeAndToolContentMod ContentModInstance { get; private set; }

        private static bool _hooksDeclared = false;
        private static readonly object _declareLock = new object();

        /// <summary>
        /// 集中声明全部 Hook 门控类型，由 HookLifecycleRegistry 统一按序激活与 LIFO 逆序注销
        /// </summary>
        private static void EnsureHooksDeclared()
        {
            if (_hooksDeclared) return;
            lock (_declareLock)
            {
                if (_hooksDeclared) return;

                HookLifecycleRegistry.RegisterTypes(
                    // 批次 1: 渲染与客户端优化
                    typeof(Optimize.ReduceMouseLag.ReduceMouseLagHooks),
                    typeof(GameViewMatrixZoomLimitHooks),
                    typeof(Cheat.HeldItemModify.SmartSelectRangeHooks),
                    typeof(QoL.KeepRunningWhenUnfocused),
                    typeof(QoL.PortableCraftingStation),
                    typeof(QoL.UniversalCraftingEnvironmentHooks),

                    // 批次 2: 背包与存储系统
                    typeof(BigBag.BigBagPickupHooks),
                    typeof(BigBag.BigBagShiftTransferHooks),
                    typeof(BigBag.HotbarScrollHooks),
                    typeof(Storage.Core.CarriedBagInteractionHooks),
                    typeof(QoL.PortableContainerHooks),

                    // 批次 3: 钓鱼增强子系统
                    typeof(QoL.Fishing.AutoFishingSuppliesHooks),
                    typeof(QoL.Fishing.AutoFishingSystemHooks),
                    typeof(QoL.Fishing.FishingCrateModifierHooks),
                    typeof(QoL.Fishing.FishingInfoHUDHooks),
                    typeof(QoL.Fishing.MultipleFishingLinesHooks),
                    typeof(QoL.AnglerQuestOptimizationHooks),

                    // 批次 4: 物块破坏与防作祟
                    typeof(QoL.VeinMining.PlayerPickTileHooks),
                    typeof(QoL.AntiGriefHooks),
                    typeof(QoL.UnsafeWallDropHooks),

                    // 批次 5.1: 世界规则与生态优化
                    typeof(QoL.EcologyHooks),
                    typeof(QoL.PylonHooks),
                    typeof(QoL.PylonRuleHooks),
                    typeof(QoL.EcoGrowthHooks),
                    typeof(QoL.SlimeAndLavaHooks),
                    typeof(QoL.BedRulesHooks),
                    typeof(QoL.TownNPCOptimizationHooks),
                    typeof(QoL.TownNPCSpawnSpeedHooks),
                    typeof(QoL.FasterExtractinatorHooks),

                    // 批次 5.2: 玩家战斗、Buff 与团队
                    typeof(QoL.DeathAndDamageHooks),
                    typeof(QoL.ExpertDebuffTimeHooks),
                    typeof(QoL.KeepBuffsOnDeathHooks),
                    typeof(QoL.NoConditionTeamTPHooks),
                    typeof(QoL.NoConsumeItemHooks),
                    typeof(QoL.TeamShareHooks),
                    typeof(QoL.UncapMaxLifeHooks),
                    typeof(QoL.ItemMaxStackHooks),
                    typeof(QoL.BannerAndBestiaryHooks),

                    // 批次 5.3: 无限 Buff、保底掉落与重铸
                    typeof(QoL.InfiniteBuff.BuffInteractionHooks),
                    typeof(QoL.InfinitePotionAndBuffHooks),
                    typeof(QoL.GuaranteedDrop.GuaranteedDropHooks),
                    typeof(QoL.Reforge.ReforgeHooks)
                );

                _hooksDeclared = true;
            }
        }

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
            EnsureHooksDeclared();
            HookLifecycleRegistry.RegisterAll();
        }

        public override void Unload()
        {
            HookLifecycleRegistry.UnregisterAll();
        }
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
                AddContent(new TrashBagItem());
            }
            catch (Exception ex)
            {
                ModLoader.Log($"[OptimizeAndToolContentMod] 注册内容异常: {ex}");
            }
        }
    }
}
