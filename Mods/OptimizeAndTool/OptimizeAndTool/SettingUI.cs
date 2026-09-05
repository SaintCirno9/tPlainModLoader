using OptimizeAndTool.Content;
using OptimizeAndTool.Content.Cheat.Function1;
using OptimizeAndTool.Content.Cheat.HeldItemModify;
using OptimizeAndTool.Content.Cheat.PlayerModify;
using OptimizeAndTool.Content.EnhancedTooltips;
using OptimizeAndTool.Content.Optimize.ReduceMouseLag;
using OptimizeAndTool.Content.QoL;
using OptimizeAndTool.Content.QoL.Fishing;
using OptimizeAndTool.Content.QoL.GuaranteedDrop;
using OptimizeAndTool.Content.QoL.InfiniteBuff;
using OptimizeAndTool.Content.QoL.Pipette;
using OptimizeAndTool.Content.QoL.Reforge;
using OptimizeAndTool.Content.QoL.VeinMining;
using OptimizeAndTool.Content.ServerList;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using TPML;
using Terraria.UI;

namespace OptimizeAndTool
{
    /// <summary>
    /// 模组设置核心类，统一驱动 setting.json 序列化与各模块状态双向同步。
    /// Author: SaintCirno9
    /// </summary>
    internal class SettingUI_player : ModSetting
    {
        public class Data
        {
            [ConfigBind(typeof(OptimizeAndTool.Content.CleanRepeatChat), nameof(OptimizeAndTool.Content.CleanRepeatChat.Enable))]
            public bool CleanRepeatChat = true;
            [ConfigBind(typeof(OptimizeAndTool.Content.CopyChat), nameof(OptimizeAndTool.Content.CopyChat.Enable))]
            public bool CopyChat = true;
            [ConfigBind(typeof(OptimizeAndTool.Content.ServerList.ServerList), nameof(OptimizeAndTool.Content.ServerList.ServerList.Enable))]
            public bool ServerList = true;
            [ConfigBind(typeof(OptimizeAndTool.Content.ItemToolTipAdditional), nameof(OptimizeAndTool.Content.ItemToolTipAdditional.Enable))]
            public bool ItemToolTipAdditional = true;
            [ConfigBind(typeof(EnhancedTooltipConfig), nameof(EnhancedTooltipConfig.ShowShimmerInfo))]
            public bool ShowShimmerInfo = true;
            [ConfigBind(typeof(EnhancedTooltipConfig), nameof(EnhancedTooltipConfig.ShowAmmoInfo))]
            public bool ShowAmmoInfo = true;
            [ConfigBind(typeof(EnhancedTooltipConfig), nameof(EnhancedTooltipConfig.ShowMoreDataInfo))]
            public bool ShowMoreDataInfo = false;
            [ConfigBind(typeof(AccessoryBagConfig), nameof(AccessoryBagConfig.EnableArmorSetBonuses))]
            public bool AccessoryBoxArmorSets = true;
            [ConfigBind(typeof(AccessoryBagConfig), nameof(AccessoryBagConfig.HighlightActiveSetBonusTooltips))]
            public bool AccessoryBoxHighlightSets = true;

            // 性能与输入优化
            [ConfigBind(typeof(MouseLagFixEngine), nameof(MouseLagFixEngine.Enabled))]
            public bool ReduceMouseLag = true;
            [ConfigBind(typeof(MouseLagFixEngine), nameof(MouseLagFixEngine.UseWin32Direct))]
            public bool ReduceMouseLagWin32 = true;

            // QoL 规则补丁配置项
            [ConfigBind(typeof(ItemMaxStackPatch), nameof(ItemMaxStackPatch.Enable))]
            public bool ItemMaxStack = true;
            [ConfigBind(typeof(OptimizeAndTool.Content.QoL.PortableCraftingStation), nameof(OptimizeAndTool.Content.QoL.PortableCraftingStation.Enable))]
            public bool PortableCraftingStation = true;
            [ConfigBind(typeof(InfinitePotionAndBuff), nameof(InfinitePotionAndBuff.EnableInfinitePotions))]
            public bool InfinitePotions = true;
            [ConfigBind(typeof(InfinitePotionAndBuff), nameof(InfinitePotionAndBuff.PotionThreshold))]
            public int PotionThreshold = 30;
            [ConfigBind(typeof(InfinitePotionAndBuff), nameof(InfinitePotionAndBuff.EnableBuffStations))]
            public bool BuffStations = true;
            [ConfigBind(typeof(InfinitePotionAndBuff), nameof(InfinitePotionAndBuff.EnableMonsterBanners))]
            public bool MonsterBanners = true;
            [ConfigBind(typeof(InfinitePotionAndBuff), nameof(InfinitePotionAndBuff.HideEndlessBuffs))]
            public bool HideEndlessBuffs = false;

            // 首见保底掉落
            [ConfigBind(typeof(GuaranteedDropSystem), nameof(GuaranteedDropSystem.EnableGuaranteedDrop))]
            public bool GuaranteedDrop = true;
            [ConfigBind(typeof(GuaranteedDropSystem), nameof(GuaranteedDropSystem.EnableMultiOptionBurst))]
            public bool GuaranteedMultiOptionBurst = true;

            [ConfigBind(typeof(TownNPCOptimization), nameof(TownNPCOptimization.EnableInstantHousingTeleport))]
            public bool NPCInstantHousingTeleport = true;
            [ConfigBind(typeof(TownNPCOptimization), nameof(TownNPCOptimization.EnableNightAutoHome))]
            public bool NPCNightAutoHome = false;
            [ConfigBind(typeof(TownNPCOptimization), nameof(TownNPCOptimization.EnableAutoHouse))]
            public bool NPCAutoHouse = false;
            [ConfigBind(typeof(TownNPCOptimization), nameof(TownNPCOptimization.EnableOptimalHappiness))]
            public bool NPCOptimalHappiness = true;
            [ConfigBind(typeof(TownNPCOptimization), nameof(TownNPCOptimization.EnableTravellingMerchantStay))]
            public bool TravellingMerchantStay = true;
            [ConfigBind(typeof(TownNPCOptimization), nameof(TownNPCOptimization.EnableQuickNurse))]
            public bool QuickNurse = true;
            [ConfigBind(typeof(OptimizeAndTool.Content.QoL.Reforge.ReforgeOptimization), nameof(OptimizeAndTool.Content.QoL.Reforge.ReforgeOptimization.Enable))]
            public bool ReforgeOptimization = true;

            [ConfigBind(typeof(AnglerQuestOptimization), nameof(AnglerQuestOptimization.EnableNoAnglerCooldown))]
            public bool NoAnglerCooldown = false;
            [ConfigBind(typeof(AnglerQuestOptimization), nameof(AnglerQuestOptimization.EnableQuestFishStack))]
            public bool QuestFishStack = true;
            [ConfigBind(typeof(AnglerQuestOptimization), nameof(AnglerQuestOptimization.EnableNoFishingPenalty))]
            public bool NoFishingPenalty = true;
            [ConfigBind(typeof(AnglerQuestOptimization), nameof(AnglerQuestOptimization.EnableCatchQuestFishAnywhere))]
            public bool CatchQuestFishAnywhere = true;

            // 钓鱼增强与 AutoFisher 自动化
            [ConfigBind(typeof(FishingCrateModifier), nameof(FishingCrateModifier.EnableGuaranteedCrate))]
            public bool GuaranteedCrate = false;
            [ConfigBind(typeof(FishingCrateModifier), nameof(FishingCrateModifier.EnableCrateMultiplier))]
            public bool CrateChanceMultiplierEnabled = false;
            [ConfigBind(typeof(FishingCrateModifier), nameof(FishingCrateModifier.CrateChanceMultiplier))]
            public int CrateChanceMult = 2;
            [ConfigBind(typeof(AutoFishingSystem), nameof(AutoFishingSystem.EnableAutoFish))]
            public bool AutoFish = true;
            [ConfigBind(typeof(AutoFishingSystem), nameof(AutoFishingSystem.EnableInstantBite))]
            public bool InstantBite = false;
            [ConfigBind(typeof(AutoFishingSystem), nameof(AutoFishingSystem.EnableHoldItemProtection))]
            public bool FishingHoldItemProtect = true;
            [ConfigBind(typeof(AutoFishingSystem), nameof(AutoFishingSystem.EnableFishInShimmer))]
            public bool FishInShimmer = true;
            [ConfigBind(typeof(AutoFishingSystem), nameof(AutoFishingSystem.EnableFishInLavaAnywhere))]
            public bool FishInLavaAnywhere = false;
            [ConfigBind(typeof(MultipleFishingLines), nameof(MultipleFishingLines.MultiLineCount))]
            public int MultiFishingLines = 1;
            [ConfigBind(typeof(MultipleFishingLines), nameof(MultipleFishingLines.EnableMultiLines))]
            public bool EnableMultiFishingLines = false;
            [ConfigBind(typeof(FishingCatchProcessor), nameof(FishingCatchProcessor.EnableAutoOpenCrates))]
            public bool AutoOpenCrates = false;
            [ConfigBind(typeof(FishingCatchProcessor), nameof(FishingCatchProcessor.EnableAutoOpenOysters))]
            public bool AutoOpenOysters = true;
            [ConfigBind(typeof(FishingCatchProcessor), nameof(FishingCatchProcessor.EnableAutoSellJunk))]
            public bool AutoSellJunkCatches = false;
            [ConfigBind(typeof(FishingCatchProcessor), nameof(FishingCatchProcessor.EnableAutoSellAllCatches))]
            public bool AutoSellAllCatches = false;
            [ConfigBind(typeof(FishingCatchProcessor), nameof(FishingCatchProcessor.EnableAutoKillFishingNPC))]
            public bool AutoKillFishingNPC = false;
            [ConfigBind(typeof(AutoFishingSupplies), nameof(AutoFishingSupplies.EnableInfiniteBait))]
            public bool InfiniteBait = false;
            [ConfigBind(typeof(AutoFishingSupplies), nameof(AutoFishingSupplies.EnableAutoDrinkFishingBuffs))]
            public bool AutoDrinkFishingBuffs = true;
            [ConfigBind(typeof(AutoFishingSupplies), nameof(AutoFishingSupplies.EnableAutoChumBuckets))]
            public bool AutoChumBuckets = true;
            [ConfigBind(typeof(AutoFishingSupplies), nameof(AutoFishingSupplies.EnableAnglerArmorVanityBonus))]
            public bool AnglerArmorVanityBonus = true;
            [ConfigBind(typeof(AutoFishingSystem), nameof(AutoFishingSystem.EnableOnlyPositiveInfluences))]
            public bool OnlyPositiveFishingInfluences = true;
            [ConfigBind(typeof(AutoFishingSystem), nameof(AutoFishingSystem.EnableIgnoreNegativeLuck))]
            public bool IgnoreNegativeLuck = true;
            [ConfigBind(typeof(OptimizeAndTool.Content.QoL.Fishing.FishingInfoHUD), nameof(OptimizeAndTool.Content.QoL.Fishing.FishingInfoHUD.EnableFishingInfoHUD))]
            public bool FishingInfoHUD = true;

            // 连锁挖矿
            [ConfigBind(typeof(VeinMiningLogic), nameof(VeinMiningLogic.Enable))]
            public bool VeinMining = true;
            [ConfigBind(typeof(VeinMiningLogic), nameof(VeinMiningLogic.MaxTiles))]
            public int VeinMiningMaxTiles = 128;
            [ConfigBind(typeof(VeinMiningLogic), nameof(VeinMiningLogic.IncludeOres))]
            public bool VeinMiningIncludeOres = true;
            [ConfigBind(typeof(VeinMiningLogic), nameof(VeinMiningLogic.IncludeGems))]
            public bool VeinMiningIncludeGems = true;
            [ConfigBind(typeof(VeinMiningLogic), nameof(VeinMiningLogic.IncludeTrash))]
            public bool VeinMiningIncludeTrash = false;

            // 吸管工具
            [ConfigBind(typeof(PipetteEngine), nameof(PipetteEngine.Enable))]
            public bool PipetteTool = true;
            [ConfigBind(typeof(PipetteEngine), nameof(PipetteEngine.PickWall))]
            public bool PipettePickWall = true;
            [ConfigBind(typeof(PipetteEngine), nameof(PipetteEngine.PlaySound))]
            public bool PipettePlaySound = true;
            [ConfigBind(typeof(PipetteEngine), nameof(PipetteEngine.ShowNotification))]
            public bool PipetteShowNotification = true;

            // 扩展存储：巨大背包
            [ConfigBind(typeof(Content.BigBag.BigBag), nameof(Content.BigBag.BigBag.EnableBigBag))]
            public bool BigBag = true;
            [ConfigBind(typeof(Content.BigBag.BigBag), nameof(Content.BigBag.BigBag.EnableBigBagCraft))]
            public bool BigBagCraft = true;
            [ConfigBind(typeof(Content.BigBag.BigBag), nameof(Content.BigBag.BigBag.Capacity))]
            public int BigBagCapacity = 100;
            public float? BigBagPosX = null;
            public float? BigBagPosY = null;
            public float? BigBagWidth = null;
            public float? BigBagHeight = null;

            // 扩展存储：饰品箱
            public bool AccessoryBox = true;
            [ConfigBind(typeof(AccessoryBagConfig), nameof(AccessoryBagConfig.EnablePassive))]
            public bool AccessoryBoxPassive = true;
            [ConfigBind(typeof(AccessoryBagConfig), nameof(AccessoryBagConfig.TotalSlots))]
            public int AccessoryBoxCapacity = 100;

            // 玩家属性修改 (PlayerModify)
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.damage))]
            public bool PlayerDamage = false;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.damage_val))]
            public float PlayerDamageVal = 0f;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.armorPenetration))]
            public bool PlayerArmorPenetration = false;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.armorPenetration_val))]
            public int PlayerArmorPenetrationVal = 0;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.maxMinions))]
            public bool PlayerMaxMinions = false;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.maxMinions_val))]
            public int PlayerMaxMinionsVal = 0;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.endurance))]
            public bool PlayerEndurance = false;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.endurance_val))]
            public float PlayerEnduranceVal = 0f;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.grabRange))]
            public bool GrabRange = true;
            [ConfigBind(typeof(Content.Cheat.PlayerModify.ValSet), nameof(Content.Cheat.PlayerModify.ValSet.grabRange_val))]
            public int GrabRangeVal = 50;

            // 手持物品修改 (HeldItemModify)
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.useTime))]
            public bool ItemUseTime = false;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.useTime_val))]
            public int ItemUseTimeVal = 0;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.useAnimation))]
            public bool ItemUseAnimation = false;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.useAnimation_val))]
            public int ItemUseAnimationVal = 0;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.shootSpeed))]
            public bool ItemShootSpeed = false;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.shootSpeed_val))]
            public float ItemShootSpeedVal = 0f;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.shoot))]
            public bool ItemShoot = false;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.shoot_val))]
            public int ItemShootVal = 0;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.tileBoost))]
            public bool TileBoost = true;
            [ConfigBind(typeof(Content.Cheat.HeldItemModify.ValSet), nameof(Content.Cheat.HeldItemModify.ValSet.tileBoost_val))]
            public int TileBoostVal = 20;

            // 杂项生态与便捷 QoL (Content.QoL)
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.pylonUnlimitedPlacement))]
            public bool PylonUnlimitedPlacement = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.pylonFreeTeleport))]
            public bool PylonFreeTeleport = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.instantRecall))]
            public bool InstantRecall = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.altRightClickTeleport))]
            public bool AltRightClickTeleport = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.quickRespawn))]
            public bool QuickRespawn = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.quickRespawnFrames))]
            public int QuickRespawnFrames = 90;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.autoResummonMinions))]
            public bool AutoResummonMinions = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.minionPhasing))]
            public bool MinionPhasing = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.minionRangeBoost))]
            public bool MinionRangeBoost = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.naturalGrowthBoost))]
            public bool NaturalGrowthBoost = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.naturalGrowthMultiplier))]
            public int NaturalGrowthMultiplier = 5;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.mushroomWeightBoost))]
            public bool MushroomWeightBoost = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.mushroomWeightMultiplier))]
            public int MushroomWeightMultiplier = 10;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.evilMushroomWeightBoost))]
            public bool EvilMushroomWeightBoost = false;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.evilMushroomWeightMultiplier))]
            public int EvilMushroomWeightMultiplier = 5;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.wildHerbSpawnBoost))]
            public bool WildHerbSpawnBoost = false;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.wildHerbSpawnMultiplier))]
            public int WildHerbSpawnMultiplier = 3;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.herbFastGrow))]
            public bool HerbFastGrow = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.herbBloomAnytime))]
            public bool HerbBloomAnytime = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.staffOfRegenAutoReplant))]
            public bool StaffOfRegenAutoReplant = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.treeFastGrow))]
            public bool TreeFastGrow = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.treeShakeGuaranteeFruit))]
            public bool TreeShakeGuaranteeFruit = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.gemTreeFullGemDrops))]
            public bool GemTreeFullGemDrops = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.removeGraveyardVisuals))]
            public bool RemoveGraveyardVisuals = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.antiGriefExplosions))]
            public bool AntiGriefExplosions = true;
            [ConfigBind(typeof(QoLValSet), nameof(QoLValSet.unsafeWallDrops))]
            public bool UnsafeWallDrops = true;

            // 基础作弊 (Cheat.Function1)
            [ConfigBind(typeof(Content.Cheat.Function1.PlayerCheatFunctions), nameof(Content.Cheat.Function1.PlayerCheatFunctions.noDead))]
            public bool NoDead = false;
            [ConfigBind(typeof(Content.Cheat.Function1.PlayerCheatFunctions), nameof(Content.Cheat.Function1.PlayerCheatFunctions.manaMax))]
            public bool ManaMax = false;

            // 无限消耗规则 (QoL.NoConsumeItems)
            [ConfigBind(typeof(Content.QoL.NoConsumeItems), nameof(Content.QoL.NoConsumeItems.NoConsumeSummonItem))]
            public bool NoConsumeSummonItem = false;
            [ConfigBind(typeof(Content.QoL.NoConsumeItems), nameof(Content.QoL.NoConsumeItems.NoConsumeAmmo))]
            public bool NoConsumeAmmo = false;
            [ConfigBind(typeof(Content.QoL.NoConsumeItems), nameof(Content.QoL.NoConsumeItems.NoConsumeProjectile))]
            public bool NoConsumeProjectile = false;
            [ConfigBind(typeof(Content.QoL.NoConsumeItems), nameof(Content.QoL.NoConsumeItems.NoConsumeWire))]
            public bool NoConsumeWire = false;

            // 旗帜与图鉴 (QoL.BannerAndBestiary)
            [ConfigBind(typeof(Content.QoL.BannerAndBestiary), nameof(Content.QoL.BannerAndBestiary.EnableBannerRequirement))]
            public bool BannerRequirement = false;
            [ConfigBind(typeof(Content.QoL.BannerAndBestiary), nameof(Content.QoL.BannerAndBestiary.BannerRequirementMultiplier))]
            public float BannerRequirementMult = 1f;
            [ConfigBind(typeof(Content.QoL.BannerAndBestiary), nameof(Content.QoL.BannerAndBestiary.EnableBestiaryQuickUnlock))]
            public bool BestiaryQuickUnlock = false;

            // 史莱姆与熔岩 (QoL.SlimeAndLava)
            [ConfigBind(typeof(Content.QoL.SlimeAndLava), nameof(Content.QoL.SlimeAndLava.EnableSlimeExDrop))]
            public bool SlimeExDrop = false;
            [ConfigBind(typeof(Content.QoL.SlimeAndLava), nameof(Content.QoL.SlimeAndLava.EnableLavalessLavaSlime))]
            public bool LavalessLavaSlime = false;

            // 死亡与伤害 (QoL.DeathAndDamage)
            [ConfigBind(typeof(Content.QoL.DeathAndDamage), nameof(Content.QoL.DeathAndDamage.BanTombstone))]
            public bool BanTombstone = false;
            [ConfigBind(typeof(Content.QoL.DeathAndDamage), nameof(Content.QoL.DeathAndDamage.DisableDamageVar))]
            public bool DisableDamageVar = false;
            [ConfigBind(typeof(Content.QoL.DeathAndDamage), nameof(Content.QoL.DeathAndDamage.RespawnWithFullHP))]
            public bool RespawnWithFullHP = false;

            // 提炼机加速 (QoL.FasterExtractinator)
            [ConfigBind(typeof(Content.QoL.FasterExtractinator), nameof(Content.QoL.FasterExtractinator.Enable))]
            public bool FasterExtractinator = false;

            // 生态生长 (QoL.EcoGrowth)
            [ConfigBind(typeof(Content.QoL.EcoGrowth), nameof(Content.QoL.EcoGrowth.EnablePumpkinFastGrow))]
            public bool PumpkinFastGrow = false;
            [ConfigBind(typeof(Content.QoL.EcoGrowth), nameof(Content.QoL.EcoGrowth.EnableLifeFruitFastGrow))]
            public bool LifeFruitFastGrow = false;

            // 经济 (QoL.Economy)
            [ConfigBind(typeof(Content.QoL.Economy), nameof(Content.QoL.Economy.EnableCoinDropRate))]
            public bool CoinDropRateEnabled = false;
            [ConfigBind(typeof(Content.QoL.Economy), nameof(Content.QoL.Economy.CoinDropRate))]
            public float CoinDropRateMult = 1f;

            // 死亡保存增益 (QoL.KeepBuffsOnDeath)
            [ConfigBind(typeof(Content.QoL.KeepBuffsOnDeath), nameof(Content.QoL.KeepBuffsOnDeath.Enable))]
            public bool KeepBuffsOnDeath = false;

            // 专家 Debuff 时长 (QoL.ExpertDebuffTime)
            [ConfigBind(typeof(Content.QoL.ExpertDebuffTime), nameof(Content.QoL.ExpertDebuffTime.Enable))]
            public bool ClassicDebuffTime = false;

            // 城镇 NPC 刷新 (QoL.TownNPCSpawnSpeed)
            [ConfigBind(typeof(Content.QoL.TownNPCSpawnSpeed), nameof(Content.QoL.TownNPCSpawnSpeed.Enable))]
            public bool TownNPCSpawnSpeedEnabled = false;
            [ConfigBind(typeof(Content.QoL.TownNPCSpawnSpeed), nameof(Content.QoL.TownNPCSpawnSpeed.Multiplier))]
            public float TownNPCSpawnSpeedMult = 2f;

            // 禁止邪恶蔓延 (QoL.NoBiomeSpread)
            [ConfigBind(typeof(Content.QoL.NoBiomeSpread), nameof(Content.QoL.NoBiomeSpread.Enable))]
            public bool NoBiomeSpread = false;

            // 无条件队内传送 (QoL.NoConditionTeamTP)
            [ConfigBind(typeof(Content.QoL.NoConditionTeamTP), nameof(Content.QoL.NoConditionTeamTP.Enable))]
            public bool NoConditionTeamTP = false;

            // 床与睡眠 (QoL.BedRules)
            [ConfigBind(typeof(Content.QoL.BedRules), nameof(Content.QoL.BedRules.EnableBedAnywhere))]
            public bool BedAnywhere = false;
            [ConfigBind(typeof(Content.QoL.BedRules), nameof(Content.QoL.BedRules.EnableNoSleepRestrictions))]
            public bool NoSleepRestrictions = false;
            [ConfigBind(typeof(Content.QoL.BedRules), nameof(Content.QoL.BedRules.EnableBedTimeRate))]
            public bool BedTimeRateEnabled = false;
            [ConfigBind(typeof(Content.QoL.BedRules), nameof(Content.QoL.BedRules.BedTimeRate))]
            public float BedTimeRateVal = 5f;
            [ConfigBind(typeof(Content.QoL.BedRules), nameof(Content.QoL.BedRules.EnableOnePlayerSleep))]
            public bool OnePlayerSleep = false;

            // 晶塔细分 (QoL.PylonRules)
            [ConfigBind(typeof(Content.QoL.PylonRules), nameof(Content.QoL.PylonRules.EnableNoNPCCheck))]
            public bool PylonNoNPCNeeded = false;
            [ConfigBind(typeof(Content.QoL.PylonRules), nameof(Content.QoL.PylonRules.EnableIgnoreBiome))]
            public bool PylonIgnoreBiome = false;

            // 失焦保持运行 (QoL.KeepRunningWhenUnfocused)
            [ConfigBind(typeof(Content.QoL.KeepRunningWhenUnfocused), nameof(Content.QoL.KeepRunningWhenUnfocused.Enable))]
            public bool KeepRunningWhenUnfocused = false;

            // 队伍共享 (QoL.TeamShare)
            [ConfigBind(typeof(Content.QoL.TeamShare), nameof(Content.QoL.TeamShare.EnableShareCraftingStation))]
            public bool ShareCraftingStation = false;

            // 无限 Buff 偏好设置 (全局配置)
            public List<int> InfiniteBuffBlacklist = new List<int>();
            public List<int> InfiniteBuffFavorites = new List<int>();
        }

        public override string Name => "设置";
        public override string Title => "优化和工具: 设置";
        public override string FilePath => "setting.json";
        public override Type DataType => typeof(Data);

        private static SettingUI_player instance = null;

        /// <summary>
        /// 配置绑定器实例：启动时自省扫描 [ConfigBind] 特性，建立高速直连映射缓存
        /// </summary>
        private static readonly ConfigBinder<Data> Binder = new ConfigBinder<Data>().AutoBindFromAttributes();

        public static float? BigBagPosX { get; set; } = null;
        public static float? BigBagPosY { get; set; } = null;
        public static float? BigBagWidth { get; set; } = null;
        public static float? BigBagHeight { get; set; } = null;

        public static void SaveSetting()
        {
            if (instance != null)
            {
                instance.NeedSave = true;
                instance.Save();
            }
        }

        public override void Load(object v)
        {
            instance = this;
            Data data = v as Data ?? new Data();

            // 批量分发配置到所有底层模块的 GetSetReset 实例
            Binder.BindAll(data);

            // 批量注册配置变更自动落盘事件（内置防重复注册机制）
            Binder.RegisterAutoSave(() => NeedSave = true);

            // 专属业务逻辑处理
            InfiniteBuffStorage.LoadFromConfig(data.InfiniteBuffBlacklist, data.InfiniteBuffFavorites);
            BigBagPosX = data.BigBagPosX;
            BigBagPosY = data.BigBagPosY;
            BigBagWidth = data.BigBagWidth;
            BigBagHeight = data.BigBagHeight;
        }

        public override object GetSaveData()
        {
            var data = new Data
            {
                BigBagPosX = BigBagPosX,
                BigBagPosY = BigBagPosY,
                BigBagWidth = BigBagWidth,
                BigBagHeight = BigBagHeight,
                AccessoryBox = true,
                InfiniteBuffBlacklist = InfiniteBuffStorage.ExportBlacklist(),
                InfiniteBuffFavorites = InfiniteBuffStorage.ExportFavorites()
            };

            // 批量从各个 GetSetReset 实例收集最新值写回 data
            Binder.ExportToData(data);
            return data;
        }

        public override UIElement GetUI()
        {
            return UIBuild.get3(Content.Function.GetUI());
        }

        public override void SetDefault()
        {
            Binder.ResetDefaults();
        }
    }
}
