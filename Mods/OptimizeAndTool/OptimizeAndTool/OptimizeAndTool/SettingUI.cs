using OptimizeAndTool.Content;
using OptimizeAndTool.Content.Cheat.Function1;
using OptimizeAndTool.Content.Cheat.HeldItemModify;
using OptimizeAndTool.Content.Cheat.PlayerModify;
using OptimizeAndTool.Content.Cheat.QoL;
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
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using tContentPatch;
using Terraria.UI;

namespace OptimizeAndTool
{
    internal class SettingUI_player : ModSetting
    {
        public class Data
        {
            public bool CleanRepeatChat = true;
            public bool CopyChat = true;
            public bool ServerList = true;
            public bool ItemToolTipAdditional = true;
            public bool ShowShimmerInfo = true;
            public bool ShowAmmoInfo = true;
            public bool ShowMoreDataInfo = false;
            public bool AccessoryBoxArmorSets = true;
            public bool AccessoryBoxHighlightSets = true;

            // 性能与输入优化
            public bool ReduceMouseLag = true;
            public bool ReduceMouseLagWin32 = true;

            // QoL 规则补丁配置项
            public bool ItemMaxStack = true;
            public bool PortableCraftingStation = true;
            public bool InfinitePotions = true;
            public int PotionThreshold = 30;
            public bool BuffStations = true;
            public bool MonsterBanners = true;
            public bool HideEndlessBuffs = false;

            // 首见保底掉落
            public bool GuaranteedDrop = true;
            public bool GuaranteedMultiOptionBurst = true;

            public bool NPCInstantHousingTeleport = true;
            public bool NPCNightAutoHome = false;
            public bool NPCAutoHouse = false;
            public bool NPCOptimalHappiness = true;
            public bool TravellingMerchantStay = true;
            public bool QuickNurse = true;
            public bool ReforgeOptimization = true;

            public bool NoAnglerCooldown = false;
            public bool QuestFishStack = true;
            public bool NoFishingPenalty = true;
            public bool CatchQuestFishAnywhere = true;

            // 钓鱼增强与 AutoFisher 自动化
            public bool GuaranteedCrate = false;
            public bool CrateChanceMultiplierEnabled = false;
            public int CrateChanceMult = 2;
            public bool AutoFish = true;
            public bool InstantBite = false;
            public bool FishingHoldItemProtect = true;
            public bool FishInShimmer = true;
            public bool FishInLavaAnywhere = false;
            public int MultiFishingLines = 1;
            public bool EnableMultiFishingLines = false;
            public bool AutoOpenCrates = false;
            public bool AutoOpenOysters = true;
            public bool AutoSellJunkCatches = false;
            public bool AutoSellAllCatches = false;
            public bool AutoKillFishingNPC = false;
            public bool InfiniteBait = false;
            public bool AutoDrinkFishingBuffs = true;
            public bool AutoChumBuckets = true;
            public bool AnglerArmorVanityBonus = true;
            public bool OnlyPositiveFishingInfluences = true;
            public bool IgnoreNegativeLuck = true;
            public bool FishingInfoHUD = true;

            // 连锁挖矿
            public bool VeinMining = true;
            public int VeinMiningMaxTiles = 128;
            public bool VeinMiningIncludeOres = true;
            public bool VeinMiningIncludeGems = true;
            public bool VeinMiningIncludeTrash = false;

            // 吸管工具
            public bool PipetteTool = true;
            public bool PipettePickWall = true;
            public bool PipettePlaySound = true;
            public bool PipetteShowNotification = true;

            // 扩展存储：巨大背包
            public bool BigBag = true;
            public bool BigBagCraft = true;
            public int BigBagCapacity = 100;
            public float? BigBagPosX = null;
            public float? BigBagPosY = null;
            public float? BigBagWidth = null;
            public float? BigBagHeight = null;

            // 扩展存储：饰品箱
            public bool AccessoryBox = true;
            public bool AccessoryBoxPassive = true;
            public int AccessoryBoxCapacity = 100;

            // 玩家属性修改 (PlayerModify)
            public bool PlayerDamage = false;
            public float PlayerDamageVal = 0f;
            public bool PlayerArmorPenetration = false;
            public int PlayerArmorPenetrationVal = 0;
            public bool PlayerMaxMinions = false;
            public int PlayerMaxMinionsVal = 0;
            public bool PlayerEndurance = false;
            public float PlayerEnduranceVal = 0f;
            public bool GrabRange = true;
            public int GrabRangeVal = 50;

            // 手持物品修改 (HeldItemModify)
            public bool ItemUseTime = false;
            public int ItemUseTimeVal = 0;
            public bool ItemUseAnimation = false;
            public int ItemUseAnimationVal = 0;
            public bool ItemShootSpeed = false;
            public float ItemShootSpeedVal = 0f;
            public bool ItemShoot = false;
            public int ItemShootVal = 0;
            public bool TileBoost = true;
            public int TileBoostVal = 20;

            // 杂项生态与便捷 QoL (Cheat.QoL)
            public bool PylonUnlimitedPlacement = true;
            public bool PylonFreeTeleport = true;
            public bool InstantRecall = true;
            public bool AltRightClickTeleport = true;
            public bool QuickRespawn = true;
            public int QuickRespawnFrames = 90;
            public bool AutoResummonMinions = true;
            public bool HerbFastGrow = true;
            public bool HerbBloomAnytime = true;
            public bool StaffOfRegenAutoReplant = true;
            public bool TreeFastGrow = true;
            public bool TreeShakeGuaranteeFruit = true;
            public bool GemTreeFullGemDrops = true;
            public bool RemoveGraveyardVisuals = true;
            public bool AntiGriefExplosions = true;

            // 基础作弊 (Cheat.Function1)
            public bool NoDead = false;
            public bool ManaMax = false;

            // 无限消耗规则 (QoL.NoConsumeItems)
            public bool NoConsumeSummonItem = false;
            public bool NoConsumeAmmo = false;
            public bool NoConsumeProjectile = false;
            public bool NoConsumeWire = false;

            // 旗帜与图鉴 (QoL.BannerAndBestiary)
            public bool BannerRequirement = false;
            public float BannerRequirementMult = 1f;
            public bool BestiaryQuickUnlock = false;

            // 史莱姆与熔岩 (QoL.SlimeAndLava)
            public bool SlimeExDrop = false;
            public bool LavalessLavaSlime = false;

            // 死亡与伤害 (QoL.DeathAndDamage)
            public bool BanTombstone = false;
            public bool DisableDamageVar = false;
            public bool RespawnWithFullHP = false;

            // 提炼机加速 (QoL.FasterExtractinator)
            public bool FasterExtractinator = false;

            // 生态生长 (QoL.EcoGrowth)
            public bool PumpkinFastGrow = false;
            public bool LifeFruitFastGrow = false;

            // 经济 (QoL.Economy)
            public bool CoinDropRateEnabled = false;
            public float CoinDropRateMult = 1f;

            // 死亡保存增益 (QoL.KeepBuffsOnDeath)
            public bool KeepBuffsOnDeath = false;

            // 专家 Debuff 时长 (QoL.ExpertDebuffTime)
            public bool ClassicDebuffTime = false;

            // 城镇 NPC 刷新 (QoL.TownNPCSpawnSpeed)
            public bool TownNPCSpawnSpeedEnabled = false;
            public float TownNPCSpawnSpeedMult = 2f;

            // 禁止邪恶蔓延 (QoL.NoBiomeSpread)
            public bool NoBiomeSpread = false;

            // 无条件队内传送 (QoL.NoConditionTeamTP)
            public bool NoConditionTeamTP = false;

            // 床与睡眠 (QoL.BedRules)
            public bool BedAnywhere = false;
            public bool NoSleepRestrictions = false;
            public bool BedTimeRateEnabled = false;
            public float BedTimeRateVal = 5f;
            public bool OnePlayerSleep = false;

            // 晶塔细分 (QoL.PylonRules)
            public bool PylonNoNPCNeeded = false;
            public bool PylonIgnoreBiome = false;

            // 失焦保持运行 (QoL.KeepRunningWhenUnfocused)
            public bool KeepRunningWhenUnfocused = false;

            // 队伍共享 (QoL.TeamShare)
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

            // 基础与性能
            CleanRepeatChat.Enable.val = data.CleanRepeatChat;
            CopyChat.Enable.val = data.CopyChat;
            ServerList.Enable.val = data.ServerList;
            ItemToolTipAdditional.Enable.val = data.ItemToolTipAdditional;
            EnhancedTooltipConfig.ShowShimmerInfo.val = data.ShowShimmerInfo;
            EnhancedTooltipConfig.ShowAmmoInfo.val = data.ShowAmmoInfo;
            EnhancedTooltipConfig.ShowMoreDataInfo.val = data.ShowMoreDataInfo;
            MouseLagFixEngine.Enabled.val = data.ReduceMouseLag;
            MouseLagFixEngine.UseWin32Direct.val = data.ReduceMouseLagWin32;

            // QoL
            ItemMaxStackPatch.Enable.val = data.ItemMaxStack;
            PortableCraftingStation.Enable.val = data.PortableCraftingStation;
            InfinitePotionAndBuff.EnableInfinitePotions.val = data.InfinitePotions;
            InfinitePotionAndBuff.PotionThreshold.val = data.PotionThreshold;
            InfinitePotionAndBuff.EnableBuffStations.val = data.BuffStations;
            InfinitePotionAndBuff.EnableMonsterBanners.val = data.MonsterBanners;
            InfinitePotionAndBuff.HideEndlessBuffs.val = data.HideEndlessBuffs;
            GuaranteedDropSystem.EnableGuaranteedDrop.val = data.GuaranteedDrop;
            GuaranteedDropSystem.EnableMultiOptionBurst.val = data.GuaranteedMultiOptionBurst;
            InfiniteBuffStorage.LoadFromConfig(data.InfiniteBuffBlacklist, data.InfiniteBuffFavorites);

            TownNPCOptimization.EnableInstantHousingTeleport.val = data.NPCInstantHousingTeleport;
            TownNPCOptimization.EnableNightAutoHome.val = data.NPCNightAutoHome;
            TownNPCOptimization.EnableAutoHouse.val = data.NPCAutoHouse;
            TownNPCOptimization.EnableOptimalHappiness.val = data.NPCOptimalHappiness;
            TownNPCOptimization.EnableTravellingMerchantStay.val = data.TravellingMerchantStay;
            TownNPCOptimization.EnableQuickNurse.val = data.QuickNurse;
            ReforgeOptimization.Enable.val = data.ReforgeOptimization;

            AnglerQuestOptimization.EnableNoAnglerCooldown.val = data.NoAnglerCooldown;
            AnglerQuestOptimization.EnableQuestFishStack.val = data.QuestFishStack;
            AnglerQuestOptimization.EnableNoFishingPenalty.val = data.NoFishingPenalty;
            AnglerQuestOptimization.EnableCatchQuestFishAnywhere.val = data.CatchQuestFishAnywhere;

            // 钓鱼与 AutoFisher 自动化
            FishingCrateModifier.EnableGuaranteedCrate.val = data.GuaranteedCrate;
            FishingCrateModifier.EnableCrateMultiplier.val = data.CrateChanceMultiplierEnabled;
            FishingCrateModifier.CrateChanceMultiplier.val = data.CrateChanceMult;
            AutoFishingSystem.EnableAutoFish.val = data.AutoFish;
            AutoFishingSystem.EnableInstantBite.val = data.InstantBite;
            AutoFishingSystem.EnableHoldItemProtection.val = data.FishingHoldItemProtect;
            AutoFishingSystem.EnableFishInShimmer.val = data.FishInShimmer;
            AutoFishingSystem.EnableFishInLavaAnywhere.val = data.FishInLavaAnywhere;
            AutoFishingSystem.EnableOnlyPositiveInfluences.val = data.OnlyPositiveFishingInfluences;
            AutoFishingSystem.EnableIgnoreNegativeLuck.val = data.IgnoreNegativeLuck;
            MultipleFishingLines.MultiLineCount.val = data.MultiFishingLines;
            MultipleFishingLines.EnableMultiLines.val = data.EnableMultiFishingLines;
            FishingCatchProcessor.EnableAutoOpenCrates.val = data.AutoOpenCrates;
            FishingCatchProcessor.EnableAutoOpenOysters.val = data.AutoOpenOysters;
            FishingCatchProcessor.EnableAutoSellJunk.val = data.AutoSellJunkCatches;
            FishingCatchProcessor.EnableAutoSellAllCatches.val = data.AutoSellAllCatches;
            FishingCatchProcessor.EnableAutoKillFishingNPC.val = data.AutoKillFishingNPC;
            AutoFishingSupplies.EnableInfiniteBait.val = data.InfiniteBait;
            AutoFishingSupplies.EnableAutoDrinkFishingBuffs.val = data.AutoDrinkFishingBuffs;
            AutoFishingSupplies.EnableAutoChumBuckets.val = data.AutoChumBuckets;
            AutoFishingSupplies.EnableAnglerArmorVanityBonus.val = data.AnglerArmorVanityBonus;
            FishingInfoHUD.EnableFishingInfoHUD.val = data.FishingInfoHUD;

            // 连锁挖矿
            VeinMiningLogic.Enable.val = data.VeinMining;
            VeinMiningLogic.MaxTiles.val = data.VeinMiningMaxTiles;
            VeinMiningLogic.IncludeOres.val = data.VeinMiningIncludeOres;
            VeinMiningLogic.IncludeGems.val = data.VeinMiningIncludeGems;
            VeinMiningLogic.IncludeTrash.val = data.VeinMiningIncludeTrash;

            // 吸管工具
            PipetteEngine.Enable.val = data.PipetteTool;
            PipetteEngine.PickWall.val = data.PipettePickWall;
            PipetteEngine.PlaySound.val = data.PipettePlaySound;
            PipetteEngine.ShowNotification.val = data.PipetteShowNotification;

            // 存储系统
            Content.BigBag.BigBag.EnableBigBag.val = data.BigBag;
            Content.BigBag.BigBag.EnableBigBagCraft.val = data.BigBagCraft;
            Content.BigBag.BigBag.Capacity.val = data.BigBagCapacity;
            BigBagPosX = data.BigBagPosX;
            BigBagPosY = data.BigBagPosY;
            BigBagWidth = data.BigBagWidth;
            BigBagHeight = data.BigBagHeight;

            AccessoryBagConfig.EnablePassive.val = data.AccessoryBoxPassive;
            AccessoryBagConfig.EnableArmorSetBonuses.val = data.AccessoryBoxArmorSets;
            AccessoryBagConfig.HighlightActiveSetBonusTooltips.val = data.AccessoryBoxHighlightSets;
            AccessoryBagConfig.TotalSlots.val = data.AccessoryBoxCapacity;

            // 玩家属性修改 (PlayerModify)
            Content.Cheat.PlayerModify.ValSet.damage.val = data.PlayerDamage;
            Content.Cheat.PlayerModify.ValSet.damage_val.val = data.PlayerDamageVal;
            Content.Cheat.PlayerModify.ValSet.armorPenetration.val = data.PlayerArmorPenetration;
            Content.Cheat.PlayerModify.ValSet.armorPenetration_val.val = data.PlayerArmorPenetrationVal;
            Content.Cheat.PlayerModify.ValSet.maxMinions.val = data.PlayerMaxMinions;
            Content.Cheat.PlayerModify.ValSet.maxMinions_val.val = data.PlayerMaxMinionsVal;
            Content.Cheat.PlayerModify.ValSet.endurance.val = data.PlayerEndurance;
            Content.Cheat.PlayerModify.ValSet.endurance_val.val = data.PlayerEnduranceVal;
            Content.Cheat.PlayerModify.ValSet.grabRange.val = data.GrabRange;
            Content.Cheat.PlayerModify.ValSet.grabRange_val.val = data.GrabRangeVal;

            // 手持物品修改 (HeldItemModify)
            Content.Cheat.HeldItemModify.ValSet.useTime.val = data.ItemUseTime;
            Content.Cheat.HeldItemModify.ValSet.useTime_val.val = data.ItemUseTimeVal;
            Content.Cheat.HeldItemModify.ValSet.useAnimation.val = data.ItemUseAnimation;
            Content.Cheat.HeldItemModify.ValSet.useAnimation_val.val = data.ItemUseAnimationVal;
            Content.Cheat.HeldItemModify.ValSet.shootSpeed.val = data.ItemShootSpeed;
            Content.Cheat.HeldItemModify.ValSet.shootSpeed_val.val = data.ItemShootSpeedVal;
            Content.Cheat.HeldItemModify.ValSet.shoot.val = data.ItemShoot;
            Content.Cheat.HeldItemModify.ValSet.shoot_val.val = data.ItemShootVal;
            Content.Cheat.HeldItemModify.ValSet.tileBoost.val = data.TileBoost;
            Content.Cheat.HeldItemModify.ValSet.tileBoost_val.val = data.TileBoostVal;

            // 杂项生态与便捷 QoL (Cheat.QoL)
            QoLValSet.pylonUnlimitedPlacement.val = data.PylonUnlimitedPlacement;
            QoLValSet.pylonFreeTeleport.val = data.PylonFreeTeleport;
            QoLValSet.instantRecall.val = data.InstantRecall;
            QoLValSet.altRightClickTeleport.val = data.AltRightClickTeleport;
            QoLValSet.quickRespawn.val = data.QuickRespawn;
            QoLValSet.quickRespawnFrames.val = data.QuickRespawnFrames;
            QoLValSet.autoResummonMinions.val = data.AutoResummonMinions;
            QoLValSet.herbFastGrow.val = data.HerbFastGrow;
            QoLValSet.herbBloomAnytime.val = data.HerbBloomAnytime;
            QoLValSet.staffOfRegenAutoReplant.val = data.StaffOfRegenAutoReplant;
            QoLValSet.treeFastGrow.val = data.TreeFastGrow;
            QoLValSet.treeShakeGuaranteeFruit.val = data.TreeShakeGuaranteeFruit;
            QoLValSet.gemTreeFullGemDrops.val = data.GemTreeFullGemDrops;
            QoLValSet.removeGraveyardVisuals.val = data.RemoveGraveyardVisuals;
            QoLValSet.antiGriefExplosions.val = data.AntiGriefExplosions;

            // 基础作弊 (Cheat.Function1)
            Content.Cheat.Function1.Function.noDead.val = data.NoDead;
            Content.Cheat.Function1.Function.manaMax.val = data.ManaMax;

            // 无限消耗规则 (QoL.NoConsumeItems)
            Content.QoL.NoConsumeItems.NoConsumeSummonItem.val = data.NoConsumeSummonItem;
            Content.QoL.NoConsumeItems.NoConsumeAmmo.val = data.NoConsumeAmmo;
            Content.QoL.NoConsumeItems.NoConsumeProjectile.val = data.NoConsumeProjectile;
            Content.QoL.NoConsumeItems.NoConsumeWire.val = data.NoConsumeWire;

            // 旗帜与图鉴 (QoL.BannerAndBestiary)
            Content.QoL.BannerAndBestiary.EnableBannerRequirement.val = data.BannerRequirement;
            Content.QoL.BannerAndBestiary.BannerRequirementMultiplier.val = data.BannerRequirementMult;
            Content.QoL.BannerAndBestiary.EnableBestiaryQuickUnlock.val = data.BestiaryQuickUnlock;

            // 史莱姆与熔岩 (QoL.SlimeAndLava)
            Content.QoL.SlimeAndLava.EnableSlimeExDrop.val = data.SlimeExDrop;
            Content.QoL.SlimeAndLava.EnableLavalessLavaSlime.val = data.LavalessLavaSlime;

            // 死亡与伤害 (QoL.DeathAndDamage)
            Content.QoL.DeathAndDamage.BanTombstone.val = data.BanTombstone;
            Content.QoL.DeathAndDamage.DisableDamageVar.val = data.DisableDamageVar;
            Content.QoL.DeathAndDamage.RespawnWithFullHP.val = data.RespawnWithFullHP;

            // 提炼机加速 (QoL.FasterExtractinator)
            Content.QoL.FasterExtractinator.Enable.val = data.FasterExtractinator;

            // 生态生长 (QoL.EcoGrowth)
            Content.QoL.EcoGrowth.EnablePumpkinFastGrow.val = data.PumpkinFastGrow;
            Content.QoL.EcoGrowth.EnableLifeFruitFastGrow.val = data.LifeFruitFastGrow;

            // 经济 (QoL.Economy)
            Content.QoL.Economy.EnableCoinDropRate.val = data.CoinDropRateEnabled;
            Content.QoL.Economy.CoinDropRate.val = data.CoinDropRateMult;

            // 死亡保存增益 (QoL.KeepBuffsOnDeath)
            Content.QoL.KeepBuffsOnDeath.Enable.val = data.KeepBuffsOnDeath;

            // 专家 Debuff 时长 (QoL.ExpertDebuffTime)
            Content.QoL.ExpertDebuffTime.Enable.val = data.ClassicDebuffTime;

            // 城镇 NPC 刷新 (QoL.TownNPCSpawnSpeed)
            Content.QoL.TownNPCSpawnSpeed.Enable.val = data.TownNPCSpawnSpeedEnabled;
            Content.QoL.TownNPCSpawnSpeed.Multiplier.val = data.TownNPCSpawnSpeedMult;

            // 禁止邪恶蔓延 (QoL.NoBiomeSpread)
            Content.QoL.NoBiomeSpread.Enable.val = data.NoBiomeSpread;

            // 无条件队内传送 (QoL.NoConditionTeamTP)
            Content.QoL.NoConditionTeamTP.Enable.val = data.NoConditionTeamTP;

            // 床与睡眠 (QoL.BedRules)
            Content.QoL.BedRules.EnableBedAnywhere.val = data.BedAnywhere;
            Content.QoL.BedRules.EnableNoSleepRestrictions.val = data.NoSleepRestrictions;
            Content.QoL.BedRules.EnableBedTimeRate.val = data.BedTimeRateEnabled;
            Content.QoL.BedRules.BedTimeRate.val = data.BedTimeRateVal;
            Content.QoL.BedRules.EnableOnePlayerSleep.val = data.OnePlayerSleep;

            // 晶塔细分 (QoL.PylonRules)
            Content.QoL.PylonRules.EnableNoNPCCheck.val = data.PylonNoNPCNeeded;
            Content.QoL.PylonRules.EnableIgnoreBiome.val = data.PylonIgnoreBiome;

            // 失焦保持运行 (QoL.KeepRunningWhenUnfocused)
            Content.QoL.KeepRunningWhenUnfocused.Enable.val = data.KeepRunningWhenUnfocused;

            // 队伍共享 (QoL.TeamShare)
            Content.QoL.TeamShare.EnableShareCraftingStation.val = data.ShareCraftingStation;

            // 注册变更自动保存
            CleanRepeatChat.Enable.OnValUpdate += _ => NeedSave = true;
            CopyChat.Enable.OnValUpdate += _ => NeedSave = true;
            ServerList.Enable.OnValUpdate += _ => NeedSave = true;
            ItemToolTipAdditional.Enable.OnValUpdate += _ => NeedSave = true;
            EnhancedTooltipConfig.ShowShimmerInfo.OnValUpdate += _ => NeedSave = true;
            EnhancedTooltipConfig.ShowAmmoInfo.OnValUpdate += _ => NeedSave = true;
            EnhancedTooltipConfig.ShowMoreDataInfo.OnValUpdate += _ => NeedSave = true;
            MouseLagFixEngine.Enabled.OnValUpdate += _ => NeedSave = true;
            MouseLagFixEngine.UseWin32Direct.OnValUpdate += _ => NeedSave = true;

            ItemMaxStackPatch.Enable.OnValUpdate += _ => NeedSave = true;
            PortableCraftingStation.Enable.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.EnableInfinitePotions.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.PotionThreshold.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.EnableBuffStations.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.EnableMonsterBanners.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.HideEndlessBuffs.OnValUpdate += _ => NeedSave = true;

            TownNPCOptimization.EnableInstantHousingTeleport.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableNightAutoHome.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableAutoHouse.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableOptimalHappiness.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableTravellingMerchantStay.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableQuickNurse.OnValUpdate += _ => NeedSave = true;
            ReforgeOptimization.Enable.OnValUpdate += _ => NeedSave = true;

            AnglerQuestOptimization.EnableNoAnglerCooldown.OnValUpdate += _ => NeedSave = true;
            AnglerQuestOptimization.EnableQuestFishStack.OnValUpdate += _ => NeedSave = true;
            AnglerQuestOptimization.EnableNoFishingPenalty.OnValUpdate += _ => NeedSave = true;
            AnglerQuestOptimization.EnableCatchQuestFishAnywhere.OnValUpdate += _ => NeedSave = true;

            FishingCrateModifier.EnableGuaranteedCrate.OnValUpdate += _ => NeedSave = true;
            FishingCrateModifier.EnableCrateMultiplier.OnValUpdate += _ => NeedSave = true;
            FishingCrateModifier.CrateChanceMultiplier.OnValUpdate += _ => NeedSave = true;
            AutoFishingSystem.EnableAutoFish.OnValUpdate += _ => NeedSave = true;
            AutoFishingSystem.EnableInstantBite.OnValUpdate += _ => NeedSave = true;
            AutoFishingSystem.EnableHoldItemProtection.OnValUpdate += _ => NeedSave = true;
            AutoFishingSystem.EnableFishInShimmer.OnValUpdate += _ => NeedSave = true;
            AutoFishingSystem.EnableFishInLavaAnywhere.OnValUpdate += _ => NeedSave = true;
            AutoFishingSystem.EnableOnlyPositiveInfluences.OnValUpdate += _ => NeedSave = true;
            AutoFishingSystem.EnableIgnoreNegativeLuck.OnValUpdate += _ => NeedSave = true;
            MultipleFishingLines.MultiLineCount.OnValUpdate += _ => NeedSave = true;
            MultipleFishingLines.EnableMultiLines.OnValUpdate += _ => NeedSave = true;
            FishingCatchProcessor.EnableAutoOpenCrates.OnValUpdate += _ => NeedSave = true;
            FishingCatchProcessor.EnableAutoOpenOysters.OnValUpdate += _ => NeedSave = true;
            FishingCatchProcessor.EnableAutoSellJunk.OnValUpdate += _ => NeedSave = true;
            FishingCatchProcessor.EnableAutoSellAllCatches.OnValUpdate += _ => NeedSave = true;
            FishingCatchProcessor.EnableAutoKillFishingNPC.OnValUpdate += _ => NeedSave = true;
            AutoFishingSupplies.EnableInfiniteBait.OnValUpdate += _ => NeedSave = true;
            AutoFishingSupplies.EnableAutoDrinkFishingBuffs.OnValUpdate += _ => NeedSave = true;
            AutoFishingSupplies.EnableAutoChumBuckets.OnValUpdate += _ => NeedSave = true;
            AutoFishingSupplies.EnableAnglerArmorVanityBonus.OnValUpdate += _ => NeedSave = true;
            FishingInfoHUD.EnableFishingInfoHUD.OnValUpdate += _ => NeedSave = true;

            VeinMiningLogic.Enable.OnValUpdate += _ => NeedSave = true;
            VeinMiningLogic.MaxTiles.OnValUpdate += _ => NeedSave = true;
            VeinMiningLogic.IncludeOres.OnValUpdate += _ => NeedSave = true;
            VeinMiningLogic.IncludeGems.OnValUpdate += _ => NeedSave = true;
            VeinMiningLogic.IncludeTrash.OnValUpdate += _ => NeedSave = true;

            PipetteEngine.Enable.OnValUpdate += _ => NeedSave = true;
            PipetteEngine.PickWall.OnValUpdate += _ => NeedSave = true;
            PipetteEngine.PlaySound.OnValUpdate += _ => NeedSave = true;
            PipetteEngine.ShowNotification.OnValUpdate += _ => NeedSave = true;

            Content.BigBag.BigBag.EnableBigBag.OnValUpdate += _ => NeedSave = true;
            Content.BigBag.BigBag.EnableBigBagCraft.OnValUpdate += _ => NeedSave = true;
            Content.BigBag.BigBag.Capacity.OnValUpdate += _ => NeedSave = true;

            AccessoryBagConfig.EnablePassive.OnValUpdate += _ => NeedSave = true;
            AccessoryBagConfig.EnableArmorSetBonuses.OnValUpdate += _ => NeedSave = true;
            AccessoryBagConfig.HighlightActiveSetBonusTooltips.OnValUpdate += _ => NeedSave = true;
            AccessoryBagConfig.TotalSlots.OnValUpdate += _ => NeedSave = true;

            // PlayerModify 自动保存监听
            Content.Cheat.PlayerModify.ValSet.damage.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.damage_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.armorPenetration.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.armorPenetration_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.maxMinions.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.maxMinions_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.endurance.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.endurance_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.grabRange.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.PlayerModify.ValSet.grabRange_val.OnValUpdate += _ => NeedSave = true;

            // HeldItemModify 自动保存监听
            Content.Cheat.HeldItemModify.ValSet.useTime.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.useTime_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.useAnimation.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.useAnimation_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.shootSpeed.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.shootSpeed_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.shoot.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.shoot_val.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.tileBoost.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.HeldItemModify.ValSet.tileBoost_val.OnValUpdate += _ => NeedSave = true;

            // QoL 自动保存监听
            QoLValSet.pylonUnlimitedPlacement.OnValUpdate += _ => NeedSave = true;
            QoLValSet.pylonFreeTeleport.OnValUpdate += _ => NeedSave = true;
            QoLValSet.instantRecall.OnValUpdate += _ => NeedSave = true;
            QoLValSet.altRightClickTeleport.OnValUpdate += _ => NeedSave = true;
            QoLValSet.quickRespawn.OnValUpdate += _ => NeedSave = true;
            QoLValSet.quickRespawnFrames.OnValUpdate += _ => NeedSave = true;
            QoLValSet.autoResummonMinions.OnValUpdate += _ => NeedSave = true;
            QoLValSet.herbFastGrow.OnValUpdate += _ => NeedSave = true;
            QoLValSet.herbBloomAnytime.OnValUpdate += _ => NeedSave = true;
            QoLValSet.staffOfRegenAutoReplant.OnValUpdate += _ => NeedSave = true;
            QoLValSet.treeFastGrow.OnValUpdate += _ => NeedSave = true;
            QoLValSet.treeShakeGuaranteeFruit.OnValUpdate += _ => NeedSave = true;
            QoLValSet.gemTreeFullGemDrops.OnValUpdate += _ => NeedSave = true;
            QoLValSet.removeGraveyardVisuals.OnValUpdate += _ => NeedSave = true;
            QoLValSet.antiGriefExplosions.OnValUpdate += _ => NeedSave = true;

            // 基础作弊
            Content.Cheat.Function1.Function.noDead.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.Function1.Function.manaMax.OnValUpdate += _ => NeedSave = true;

            // 无限消耗规则
            Content.QoL.NoConsumeItems.NoConsumeSummonItem.OnValUpdate += _ => NeedSave = true;
            Content.QoL.NoConsumeItems.NoConsumeAmmo.OnValUpdate += _ => NeedSave = true;
            Content.QoL.NoConsumeItems.NoConsumeProjectile.OnValUpdate += _ => NeedSave = true;
            Content.QoL.NoConsumeItems.NoConsumeWire.OnValUpdate += _ => NeedSave = true;

            // 首见保底掉落
            GuaranteedDropSystem.EnableGuaranteedDrop.OnValUpdate += _ => NeedSave = true;
            GuaranteedDropSystem.EnableMultiOptionBurst.OnValUpdate += _ => NeedSave = true;

            // 旗帜与图鉴
            Content.QoL.BannerAndBestiary.EnableBannerRequirement.OnValUpdate += _ => NeedSave = true;
            Content.QoL.BannerAndBestiary.BannerRequirementMultiplier.OnValUpdate += _ => NeedSave = true;
            Content.QoL.BannerAndBestiary.EnableBestiaryQuickUnlock.OnValUpdate += _ => NeedSave = true;

            // 史莱姆与熔岩
            Content.QoL.SlimeAndLava.EnableSlimeExDrop.OnValUpdate += _ => NeedSave = true;
            Content.QoL.SlimeAndLava.EnableLavalessLavaSlime.OnValUpdate += _ => NeedSave = true;

            // 死亡与伤害
            Content.QoL.DeathAndDamage.BanTombstone.OnValUpdate += _ => NeedSave = true;
            Content.QoL.DeathAndDamage.DisableDamageVar.OnValUpdate += _ => NeedSave = true;
            Content.QoL.DeathAndDamage.RespawnWithFullHP.OnValUpdate += _ => NeedSave = true;

            // 提炼机加速
            Content.QoL.FasterExtractinator.Enable.OnValUpdate += _ => NeedSave = true;

            // 生态生长
            Content.QoL.EcoGrowth.EnablePumpkinFastGrow.OnValUpdate += _ => NeedSave = true;
            Content.QoL.EcoGrowth.EnableLifeFruitFastGrow.OnValUpdate += _ => NeedSave = true;

            // 经济
            Content.QoL.Economy.EnableCoinDropRate.OnValUpdate += _ => NeedSave = true;
            Content.QoL.Economy.CoinDropRate.OnValUpdate += _ => NeedSave = true;

            // 死亡保存增益
            Content.QoL.KeepBuffsOnDeath.Enable.OnValUpdate += _ => NeedSave = true;

            // 专家 Debuff 时长
            Content.QoL.ExpertDebuffTime.Enable.OnValUpdate += _ => NeedSave = true;

            // 城镇 NPC 刷新
            Content.QoL.TownNPCSpawnSpeed.Enable.OnValUpdate += _ => NeedSave = true;
            Content.QoL.TownNPCSpawnSpeed.Multiplier.OnValUpdate += _ => NeedSave = true;

            // 禁止邪恶蔓延
            Content.QoL.NoBiomeSpread.Enable.OnValUpdate += _ => NeedSave = true;

            // 无条件队内传送
            Content.QoL.NoConditionTeamTP.Enable.OnValUpdate += _ => NeedSave = true;

            // 床与睡眠
            Content.QoL.BedRules.EnableBedAnywhere.OnValUpdate += _ => NeedSave = true;
            Content.QoL.BedRules.EnableNoSleepRestrictions.OnValUpdate += _ => NeedSave = true;
            Content.QoL.BedRules.EnableBedTimeRate.OnValUpdate += _ => NeedSave = true;
            Content.QoL.BedRules.BedTimeRate.OnValUpdate += _ => NeedSave = true;
            Content.QoL.BedRules.EnableOnePlayerSleep.OnValUpdate += _ => NeedSave = true;

            // 晶塔细分
            Content.QoL.PylonRules.EnableNoNPCCheck.OnValUpdate += _ => NeedSave = true;
            Content.QoL.PylonRules.EnableIgnoreBiome.OnValUpdate += _ => NeedSave = true;

            // 失焦保持运行
            Content.QoL.KeepRunningWhenUnfocused.Enable.OnValUpdate += _ => NeedSave = true;

            // 队伍共享
            Content.QoL.TeamShare.EnableShareCraftingStation.OnValUpdate += _ => NeedSave = true;
        }

        public override object GetSaveData()
        {
            return new Data
            {
                CleanRepeatChat = CleanRepeatChat.Enable.val,
                CopyChat = CopyChat.Enable.val,
                ServerList = ServerList.Enable.val,
                ItemToolTipAdditional = ItemToolTipAdditional.Enable.val,
                ShowShimmerInfo = EnhancedTooltipConfig.ShowShimmerInfo.val,
                ShowAmmoInfo = EnhancedTooltipConfig.ShowAmmoInfo.val,
                ShowMoreDataInfo = EnhancedTooltipConfig.ShowMoreDataInfo.val,

                ReduceMouseLag = MouseLagFixEngine.Enabled.val,
                ReduceMouseLagWin32 = MouseLagFixEngine.UseWin32Direct.val,

                ItemMaxStack = ItemMaxStackPatch.Enable.val,
                PortableCraftingStation = PortableCraftingStation.Enable.val,
                InfinitePotions = InfinitePotionAndBuff.EnableInfinitePotions.val,
                PotionThreshold = InfinitePotionAndBuff.PotionThreshold.val,
                BuffStations = InfinitePotionAndBuff.EnableBuffStations.val,
                MonsterBanners = InfinitePotionAndBuff.EnableMonsterBanners.val,
                HideEndlessBuffs = InfinitePotionAndBuff.HideEndlessBuffs.val,

                GuaranteedDrop = GuaranteedDropSystem.EnableGuaranteedDrop.val,
                GuaranteedMultiOptionBurst = GuaranteedDropSystem.EnableMultiOptionBurst.val,

                NPCInstantHousingTeleport = TownNPCOptimization.EnableInstantHousingTeleport.val,
                NPCNightAutoHome = TownNPCOptimization.EnableNightAutoHome.val,
                NPCAutoHouse = TownNPCOptimization.EnableAutoHouse.val,
                NPCOptimalHappiness = TownNPCOptimization.EnableOptimalHappiness.val,
                TravellingMerchantStay = TownNPCOptimization.EnableTravellingMerchantStay.val,
                QuickNurse = TownNPCOptimization.EnableQuickNurse.val,
                ReforgeOptimization = ReforgeOptimization.Enable.val,

                NoAnglerCooldown = AnglerQuestOptimization.EnableNoAnglerCooldown.val,
                QuestFishStack = AnglerQuestOptimization.EnableQuestFishStack.val,
                NoFishingPenalty = AnglerQuestOptimization.EnableNoFishingPenalty.val,
                CatchQuestFishAnywhere = AnglerQuestOptimization.EnableCatchQuestFishAnywhere.val,

                GuaranteedCrate = FishingCrateModifier.EnableGuaranteedCrate.val,
                CrateChanceMultiplierEnabled = FishingCrateModifier.EnableCrateMultiplier.val,
                CrateChanceMult = FishingCrateModifier.CrateChanceMultiplier.val,
                AutoFish = AutoFishingSystem.EnableAutoFish.val,
                InstantBite = AutoFishingSystem.EnableInstantBite.val,
                FishingHoldItemProtect = AutoFishingSystem.EnableHoldItemProtection.val,
                FishInShimmer = AutoFishingSystem.EnableFishInShimmer.val,
                FishInLavaAnywhere = AutoFishingSystem.EnableFishInLavaAnywhere.val,
                OnlyPositiveFishingInfluences = AutoFishingSystem.EnableOnlyPositiveInfluences.val,
                IgnoreNegativeLuck = AutoFishingSystem.EnableIgnoreNegativeLuck.val,
                MultiFishingLines = MultipleFishingLines.MultiLineCount.val,
                EnableMultiFishingLines = MultipleFishingLines.EnableMultiLines.val,
                AutoOpenCrates = FishingCatchProcessor.EnableAutoOpenCrates.val,
                AutoOpenOysters = FishingCatchProcessor.EnableAutoOpenOysters.val,
                AutoSellJunkCatches = FishingCatchProcessor.EnableAutoSellJunk.val,
                AutoSellAllCatches = FishingCatchProcessor.EnableAutoSellAllCatches.val,
                AutoKillFishingNPC = FishingCatchProcessor.EnableAutoKillFishingNPC.val,
                InfiniteBait = AutoFishingSupplies.EnableInfiniteBait.val,
                AutoDrinkFishingBuffs = AutoFishingSupplies.EnableAutoDrinkFishingBuffs.val,
                AutoChumBuckets = AutoFishingSupplies.EnableAutoChumBuckets.val,
                AnglerArmorVanityBonus = AutoFishingSupplies.EnableAnglerArmorVanityBonus.val,
                FishingInfoHUD = FishingInfoHUD.EnableFishingInfoHUD.val,

                VeinMining = VeinMiningLogic.Enable.val,
                VeinMiningMaxTiles = VeinMiningLogic.MaxTiles.val,
                VeinMiningIncludeOres = VeinMiningLogic.IncludeOres.val,
                VeinMiningIncludeGems = VeinMiningLogic.IncludeGems.val,
                VeinMiningIncludeTrash = VeinMiningLogic.IncludeTrash.val,

                PipetteTool = PipetteEngine.Enable.val,
                PipettePickWall = PipetteEngine.PickWall.val,
                PipettePlaySound = PipetteEngine.PlaySound.val,
                PipetteShowNotification = PipetteEngine.ShowNotification.val,

                BigBag = Content.BigBag.BigBag.EnableBigBag.val,
                BigBagCraft = Content.BigBag.BigBag.EnableBigBagCraft.val,
                BigBagCapacity = Content.BigBag.BigBag.Capacity.val,
                BigBagPosX = BigBagPosX,
                BigBagPosY = BigBagPosY,
                BigBagWidth = BigBagWidth,
                BigBagHeight = BigBagHeight,

                AccessoryBox = true,
                AccessoryBoxPassive = AccessoryBagConfig.EnablePassive.val,
                AccessoryBoxArmorSets = AccessoryBagConfig.EnableArmorSetBonuses.val,
                AccessoryBoxHighlightSets = AccessoryBagConfig.HighlightActiveSetBonusTooltips.val,
                AccessoryBoxCapacity = AccessoryBagConfig.TotalSlots.val,

                // 玩家属性修改
                PlayerDamage = Content.Cheat.PlayerModify.ValSet.damage.val,
                PlayerDamageVal = Content.Cheat.PlayerModify.ValSet.damage_val.val,
                PlayerArmorPenetration = Content.Cheat.PlayerModify.ValSet.armorPenetration.val,
                PlayerArmorPenetrationVal = Content.Cheat.PlayerModify.ValSet.armorPenetration_val.val,
                PlayerMaxMinions = Content.Cheat.PlayerModify.ValSet.maxMinions.val,
                PlayerMaxMinionsVal = Content.Cheat.PlayerModify.ValSet.maxMinions_val.val,
                PlayerEndurance = Content.Cheat.PlayerModify.ValSet.endurance.val,
                PlayerEnduranceVal = Content.Cheat.PlayerModify.ValSet.endurance_val.val,
                GrabRange = Content.Cheat.PlayerModify.ValSet.grabRange.val,
                GrabRangeVal = Content.Cheat.PlayerModify.ValSet.grabRange_val.val,

                // 手持物品修改
                ItemUseTime = Content.Cheat.HeldItemModify.ValSet.useTime.val,
                ItemUseTimeVal = Content.Cheat.HeldItemModify.ValSet.useTime_val.val,
                ItemUseAnimation = Content.Cheat.HeldItemModify.ValSet.useAnimation.val,
                ItemUseAnimationVal = Content.Cheat.HeldItemModify.ValSet.useAnimation_val.val,
                ItemShootSpeed = Content.Cheat.HeldItemModify.ValSet.shootSpeed.val,
                ItemShootSpeedVal = Content.Cheat.HeldItemModify.ValSet.shootSpeed_val.val,
                ItemShoot = Content.Cheat.HeldItemModify.ValSet.shoot.val,
                ItemShootVal = Content.Cheat.HeldItemModify.ValSet.shoot_val.val,
                TileBoost = Content.Cheat.HeldItemModify.ValSet.tileBoost.val,
                TileBoostVal = Content.Cheat.HeldItemModify.ValSet.tileBoost_val.val,

                // 杂项生态与便捷 QoL
                PylonUnlimitedPlacement = QoLValSet.pylonUnlimitedPlacement.val,
                PylonFreeTeleport = QoLValSet.pylonFreeTeleport.val,
                InstantRecall = QoLValSet.instantRecall.val,
                AltRightClickTeleport = QoLValSet.altRightClickTeleport.val,
                QuickRespawn = QoLValSet.quickRespawn.val,
                QuickRespawnFrames = QoLValSet.quickRespawnFrames.val,
                AutoResummonMinions = QoLValSet.autoResummonMinions.val,
                HerbFastGrow = QoLValSet.herbFastGrow.val,
                HerbBloomAnytime = QoLValSet.herbBloomAnytime.val,
                StaffOfRegenAutoReplant = QoLValSet.staffOfRegenAutoReplant.val,
                TreeFastGrow = QoLValSet.treeFastGrow.val,
                TreeShakeGuaranteeFruit = QoLValSet.treeShakeGuaranteeFruit.val,
                GemTreeFullGemDrops = QoLValSet.gemTreeFullGemDrops.val,
                RemoveGraveyardVisuals = QoLValSet.removeGraveyardVisuals.val,
                AntiGriefExplosions = QoLValSet.antiGriefExplosions.val,

                // 基础作弊
                NoDead = Content.Cheat.Function1.Function.noDead.val,
                ManaMax = Content.Cheat.Function1.Function.manaMax.val,

                // 无限消耗规则
                NoConsumeSummonItem = Content.QoL.NoConsumeItems.NoConsumeSummonItem.val,
                NoConsumeAmmo = Content.QoL.NoConsumeItems.NoConsumeAmmo.val,
                NoConsumeProjectile = Content.QoL.NoConsumeItems.NoConsumeProjectile.val,
                NoConsumeWire = Content.QoL.NoConsumeItems.NoConsumeWire.val,

                // 旗帜与图鉴
                BannerRequirement = Content.QoL.BannerAndBestiary.EnableBannerRequirement.val,
                BannerRequirementMult = Content.QoL.BannerAndBestiary.BannerRequirementMultiplier.val,
                BestiaryQuickUnlock = Content.QoL.BannerAndBestiary.EnableBestiaryQuickUnlock.val,

                // 史莱姆与熔岩
                SlimeExDrop = Content.QoL.SlimeAndLava.EnableSlimeExDrop.val,
                LavalessLavaSlime = Content.QoL.SlimeAndLava.EnableLavalessLavaSlime.val,

                // 死亡与伤害
                BanTombstone = Content.QoL.DeathAndDamage.BanTombstone.val,
                DisableDamageVar = Content.QoL.DeathAndDamage.DisableDamageVar.val,
                RespawnWithFullHP = Content.QoL.DeathAndDamage.RespawnWithFullHP.val,

                // 提炼机加速
                FasterExtractinator = Content.QoL.FasterExtractinator.Enable.val,

                // 生态生长
                PumpkinFastGrow = Content.QoL.EcoGrowth.EnablePumpkinFastGrow.val,
                LifeFruitFastGrow = Content.QoL.EcoGrowth.EnableLifeFruitFastGrow.val,

                // 经济
                CoinDropRateEnabled = Content.QoL.Economy.EnableCoinDropRate.val,
                CoinDropRateMult = Content.QoL.Economy.CoinDropRate.val,

                // 死亡保存增益
                KeepBuffsOnDeath = Content.QoL.KeepBuffsOnDeath.Enable.val,

                // 专家 Debuff 时长
                ClassicDebuffTime = Content.QoL.ExpertDebuffTime.Enable.val,

                // 城镇 NPC 刷新
                TownNPCSpawnSpeedEnabled = Content.QoL.TownNPCSpawnSpeed.Enable.val,
                TownNPCSpawnSpeedMult = Content.QoL.TownNPCSpawnSpeed.Multiplier.val,

                // 禁止邪恶蔓延
                NoBiomeSpread = Content.QoL.NoBiomeSpread.Enable.val,

                // 无条件队内传送
                NoConditionTeamTP = Content.QoL.NoConditionTeamTP.Enable.val,

                // 床与睡眠
                BedAnywhere = Content.QoL.BedRules.EnableBedAnywhere.val,
                NoSleepRestrictions = Content.QoL.BedRules.EnableNoSleepRestrictions.val,
                BedTimeRateEnabled = Content.QoL.BedRules.EnableBedTimeRate.val,
                BedTimeRateVal = Content.QoL.BedRules.BedTimeRate.val,
                OnePlayerSleep = Content.QoL.BedRules.EnableOnePlayerSleep.val,

                // 晶塔细分
                PylonNoNPCNeeded = Content.QoL.PylonRules.EnableNoNPCCheck.val,
                PylonIgnoreBiome = Content.QoL.PylonRules.EnableIgnoreBiome.val,

                // 失焦保持运行
                KeepRunningWhenUnfocused = Content.QoL.KeepRunningWhenUnfocused.Enable.val,

                // 队伍共享
                ShareCraftingStation = Content.QoL.TeamShare.EnableShareCraftingStation.val,

                // 无限 Buff 偏好设置
                InfiniteBuffBlacklist = InfiniteBuffStorage.ExportBlacklist(),
                InfiniteBuffFavorites = InfiniteBuffStorage.ExportFavorites()
            };
        }

        public override UIElement GetUI()
        {
            return UIBuild.get3(Content.Function.GetUI());
        }

        public override void SetDefault()
        {
            CleanRepeatChat.Enable.Reset();
            CopyChat.Enable.Reset();
            ServerList.Enable.Reset();
            ItemToolTipAdditional.Enable.Reset();
            EnhancedTooltipConfig.ShowShimmerInfo.Reset();
            EnhancedTooltipConfig.ShowAmmoInfo.Reset();
            EnhancedTooltipConfig.ShowMoreDataInfo.Reset();
            MouseLagFixEngine.Enabled.Reset();
            MouseLagFixEngine.UseWin32Direct.Reset();

            ItemMaxStackPatch.Enable.Reset();
            PortableCraftingStation.Enable.Reset();
            InfinitePotionAndBuff.EnableInfinitePotions.Reset();
            InfinitePotionAndBuff.PotionThreshold.Reset();
            InfinitePotionAndBuff.EnableBuffStations.Reset();
            InfinitePotionAndBuff.EnableMonsterBanners.Reset();
            InfinitePotionAndBuff.HideEndlessBuffs.Reset();
            GuaranteedDropSystem.EnableGuaranteedDrop.Reset();
            GuaranteedDropSystem.EnableMultiOptionBurst.Reset();

            TownNPCOptimization.EnableInstantHousingTeleport.Reset();
            TownNPCOptimization.EnableNightAutoHome.Reset();
            TownNPCOptimization.EnableAutoHouse.Reset();
            TownNPCOptimization.EnableOptimalHappiness.Reset();
            TownNPCOptimization.EnableTravellingMerchantStay.Reset();
            TownNPCOptimization.EnableQuickNurse.Reset();
            ReforgeOptimization.Enable.Reset();

            AnglerQuestOptimization.EnableNoAnglerCooldown.Reset();
            AnglerQuestOptimization.EnableQuestFishStack.Reset();
            AnglerQuestOptimization.EnableNoFishingPenalty.Reset();
            AnglerQuestOptimization.EnableCatchQuestFishAnywhere.Reset();

            FishingCrateModifier.EnableGuaranteedCrate.Reset();
            FishingCrateModifier.EnableCrateMultiplier.Reset();
            FishingCrateModifier.CrateChanceMultiplier.Reset();
            AutoFishingSystem.EnableAutoFish.Reset();
            AutoFishingSystem.EnableInstantBite.Reset();
            AutoFishingSystem.EnableHoldItemProtection.Reset();
            AutoFishingSystem.EnableFishInShimmer.Reset();
            AutoFishingSystem.EnableFishInLavaAnywhere.Reset();
            AutoFishingSystem.EnableOnlyPositiveInfluences.Reset();
            AutoFishingSystem.EnableIgnoreNegativeLuck.Reset();
            MultipleFishingLines.MultiLineCount.Reset();
            MultipleFishingLines.EnableMultiLines.Reset();
            FishingCatchProcessor.EnableAutoOpenCrates.Reset();
            FishingCatchProcessor.EnableAutoOpenOysters.Reset();
            FishingCatchProcessor.EnableAutoSellJunk.Reset();
            FishingCatchProcessor.EnableAutoSellAllCatches.Reset();
            FishingCatchProcessor.EnableAutoKillFishingNPC.Reset();
            AutoFishingSupplies.EnableInfiniteBait.Reset();
            AutoFishingSupplies.EnableAutoDrinkFishingBuffs.Reset();
            AutoFishingSupplies.EnableAutoChumBuckets.Reset();
            AutoFishingSupplies.EnableAnglerArmorVanityBonus.Reset();
            FishingInfoHUD.EnableFishingInfoHUD.Reset();

            VeinMiningLogic.Enable.Reset();
            VeinMiningLogic.MaxTiles.Reset();
            VeinMiningLogic.IncludeOres.Reset();
            VeinMiningLogic.IncludeGems.Reset();
            VeinMiningLogic.IncludeTrash.Reset();

            PipetteEngine.Enable.Reset();
            PipetteEngine.PickWall.Reset();
            PipetteEngine.PlaySound.Reset();
            PipetteEngine.ShowNotification.Reset();

            Content.BigBag.BigBag.EnableBigBag.Reset();
            Content.BigBag.BigBag.EnableBigBagCraft.Reset();
            Content.BigBag.BigBag.Capacity.Reset();

            AccessoryBagConfig.EnablePassive.Reset();
            AccessoryBagConfig.EnableArmorSetBonuses.Reset();
            AccessoryBagConfig.HighlightActiveSetBonusTooltips.Reset();
            AccessoryBagConfig.TotalSlots.Reset();

            Content.Cheat.PlayerModify.ValSet.damage.Reset();
            Content.Cheat.PlayerModify.ValSet.damage_val.Reset();
            Content.Cheat.PlayerModify.ValSet.armorPenetration.Reset();
            Content.Cheat.PlayerModify.ValSet.armorPenetration_val.Reset();
            Content.Cheat.PlayerModify.ValSet.maxMinions.Reset();
            Content.Cheat.PlayerModify.ValSet.maxMinions_val.Reset();
            Content.Cheat.PlayerModify.ValSet.endurance.Reset();
            Content.Cheat.PlayerModify.ValSet.endurance_val.Reset();
            Content.Cheat.PlayerModify.ValSet.grabRange.Reset();
            Content.Cheat.PlayerModify.ValSet.grabRange_val.Reset();

            Content.Cheat.HeldItemModify.ValSet.useTime.Reset();
            Content.Cheat.HeldItemModify.ValSet.useTime_val.Reset();
            Content.Cheat.HeldItemModify.ValSet.useAnimation.Reset();
            Content.Cheat.HeldItemModify.ValSet.useAnimation_val.Reset();
            Content.Cheat.HeldItemModify.ValSet.shootSpeed.Reset();
            Content.Cheat.HeldItemModify.ValSet.shootSpeed_val.Reset();
            Content.Cheat.HeldItemModify.ValSet.shoot.Reset();
            Content.Cheat.HeldItemModify.ValSet.shoot_val.Reset();
            Content.Cheat.HeldItemModify.ValSet.tileBoost.Reset();
            Content.Cheat.HeldItemModify.ValSet.tileBoost_val.Reset();

            QoLValSet.pylonUnlimitedPlacement.Reset();
            QoLValSet.pylonFreeTeleport.Reset();
            QoLValSet.instantRecall.Reset();
            QoLValSet.altRightClickTeleport.Reset();
            QoLValSet.quickRespawn.Reset();
            QoLValSet.quickRespawnFrames.Reset();
            QoLValSet.autoResummonMinions.Reset();
            QoLValSet.herbFastGrow.Reset();
            QoLValSet.herbBloomAnytime.Reset();
            QoLValSet.staffOfRegenAutoReplant.Reset();
            QoLValSet.treeFastGrow.Reset();
            QoLValSet.treeShakeGuaranteeFruit.Reset();
            QoLValSet.gemTreeFullGemDrops.Reset();
            QoLValSet.removeGraveyardVisuals.Reset();
            QoLValSet.antiGriefExplosions.Reset();

            Content.Cheat.Function1.Function.noDead.Reset();
            Content.Cheat.Function1.Function.manaMax.Reset();

            // 无限消耗规则
            Content.QoL.NoConsumeItems.NoConsumeSummonItem.Reset();
            Content.QoL.NoConsumeItems.NoConsumeAmmo.Reset();
            Content.QoL.NoConsumeItems.NoConsumeProjectile.Reset();
            Content.QoL.NoConsumeItems.NoConsumeWire.Reset();

            // 旗帜与图鉴
            Content.QoL.BannerAndBestiary.EnableBannerRequirement.Reset();
            Content.QoL.BannerAndBestiary.BannerRequirementMultiplier.Reset();
            Content.QoL.BannerAndBestiary.EnableBestiaryQuickUnlock.Reset();

            // 史莱姆与熔岩
            Content.QoL.SlimeAndLava.EnableSlimeExDrop.Reset();
            Content.QoL.SlimeAndLava.EnableLavalessLavaSlime.Reset();

            // 死亡与伤害
            Content.QoL.DeathAndDamage.BanTombstone.Reset();
            Content.QoL.DeathAndDamage.DisableDamageVar.Reset();
            Content.QoL.DeathAndDamage.RespawnWithFullHP.Reset();

            // 提炼机加速
            Content.QoL.FasterExtractinator.Enable.Reset();

            // 生态生长
            Content.QoL.EcoGrowth.EnablePumpkinFastGrow.Reset();
            Content.QoL.EcoGrowth.EnableLifeFruitFastGrow.Reset();

            // 经济
            Content.QoL.Economy.EnableCoinDropRate.Reset();
            Content.QoL.Economy.CoinDropRate.Reset();

            // 死亡保存增益
            Content.QoL.KeepBuffsOnDeath.Enable.Reset();

            // 专家 Debuff 时长
            Content.QoL.ExpertDebuffTime.Enable.Reset();

            // 城镇 NPC 刷新
            Content.QoL.TownNPCSpawnSpeed.Enable.Reset();
            Content.QoL.TownNPCSpawnSpeed.Multiplier.Reset();

            // 禁止邪恶蔓延
            Content.QoL.NoBiomeSpread.Enable.Reset();

            // 无条件队内传送
            Content.QoL.NoConditionTeamTP.Enable.Reset();

            // 床与睡眠
            Content.QoL.BedRules.EnableBedAnywhere.Reset();
            Content.QoL.BedRules.EnableNoSleepRestrictions.Reset();
            Content.QoL.BedRules.EnableBedTimeRate.Reset();
            Content.QoL.BedRules.BedTimeRate.Reset();
            Content.QoL.BedRules.EnableOnePlayerSleep.Reset();

            // 晶塔细分
            Content.QoL.PylonRules.EnableNoNPCCheck.Reset();
            Content.QoL.PylonRules.EnableIgnoreBiome.Reset();

            // 失焦保持运行
            Content.QoL.KeepRunningWhenUnfocused.Enable.Reset();

            // 队伍共享
            Content.QoL.TeamShare.EnableShareCraftingStation.Reset();
        }
    }
}
