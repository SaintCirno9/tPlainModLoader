using OptimizeAndTool.Content;
using OptimizeAndTool.Content.Cheat.Function1;
using OptimizeAndTool.Content.Cheat.HeldItemModify;
using OptimizeAndTool.Content.Cheat.PlayerModify;
using OptimizeAndTool.Content.Cheat.QoL;
using OptimizeAndTool.Content.Optimize.ReduceMouseLag;
using OptimizeAndTool.Content.QoL;
using OptimizeAndTool.Content.QoL.Pipette;
using OptimizeAndTool.Content.QoL.VeinMining;
using OptimizeAndTool.Content.ServerList;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using OptimizeAndTool.Utils.quickBuild;
using System;
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

            public bool NPCAutoHouse = true;
            public bool NPCOptimalHappiness = true;
            public bool TravellingMerchantStay = true;
            public bool QuickNurse = true;

            public bool NoAnglerCooldown = true;
            public bool QuestFishStack = true;
            public bool NoFishingPenalty = true;

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
            public bool MarkStructuresOnMap = true;
            public bool MarkPlanteraBulb = true;
            public bool MarkSwordShrine = true;
            public bool MarkBeeHive = true;
            public bool MarkShimmer = true;
            public bool MarkPyramid = true;
            public bool MarkTempleAltar = true;

            // 基础作弊 (Cheat.Function1)
            public bool NoDead = false;
            public bool ManaMax = false;
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

            TownNPCOptimization.EnableAutoHouse.val = data.NPCAutoHouse;
            TownNPCOptimization.EnableOptimalHappiness.val = data.NPCOptimalHappiness;
            TownNPCOptimization.EnableTravellingMerchantStay.val = data.TravellingMerchantStay;
            TownNPCOptimization.EnableQuickNurse.val = data.QuickNurse;

            AnglerQuestOptimization.EnableNoAnglerCooldown.val = data.NoAnglerCooldown;
            AnglerQuestOptimization.EnableQuestFishStack.val = data.QuestFishStack;
            AnglerQuestOptimization.EnableNoFishingPenalty.val = data.NoFishingPenalty;

            VeinMiningLogic.Enable.val = data.VeinMining;
            VeinMiningLogic.MaxTiles.val = data.VeinMiningMaxTiles;
            VeinMiningLogic.IncludeOres.val = data.VeinMiningIncludeOres;
            VeinMiningLogic.IncludeGems.val = data.VeinMiningIncludeGems;
            VeinMiningLogic.IncludeTrash.val = data.VeinMiningIncludeTrash;

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
            QoLValSet.markStructuresOnMap.val = data.MarkStructuresOnMap;
            QoLValSet.markPlanteraBulb.val = data.MarkPlanteraBulb;
            QoLValSet.markSwordShrine.val = data.MarkSwordShrine;
            QoLValSet.markBeeHive.val = data.MarkBeeHive;
            QoLValSet.markShimmer.val = data.MarkShimmer;
            QoLValSet.markPyramid.val = data.MarkPyramid;
            QoLValSet.markTempleAltar.val = data.MarkTempleAltar;

            // 基础作弊 (Cheat.Function1)
            Content.Cheat.Function1.Function.noDead.val = data.NoDead;
            Content.Cheat.Function1.Function.manaMax.val = data.ManaMax;

            // 注册变更自动保存
            CleanRepeatChat.Enable.OnValUpdate += _ => NeedSave = true;
            CopyChat.Enable.OnValUpdate += _ => NeedSave = true;
            ServerList.Enable.OnValUpdate += _ => NeedSave = true;
            ItemToolTipAdditional.Enable.OnValUpdate += _ => NeedSave = true;
            MouseLagFixEngine.Enabled.OnValUpdate += _ => NeedSave = true;
            MouseLagFixEngine.UseWin32Direct.OnValUpdate += _ => NeedSave = true;

            ItemMaxStackPatch.Enable.OnValUpdate += _ => NeedSave = true;
            PortableCraftingStation.Enable.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.EnableInfinitePotions.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.PotionThreshold.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.EnableBuffStations.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.EnableMonsterBanners.OnValUpdate += _ => NeedSave = true;
            InfinitePotionAndBuff.HideEndlessBuffs.OnValUpdate += _ => NeedSave = true;

            TownNPCOptimization.EnableAutoHouse.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableOptimalHappiness.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableTravellingMerchantStay.OnValUpdate += _ => NeedSave = true;
            TownNPCOptimization.EnableQuickNurse.OnValUpdate += _ => NeedSave = true;

            AnglerQuestOptimization.EnableNoAnglerCooldown.OnValUpdate += _ => NeedSave = true;
            AnglerQuestOptimization.EnableQuestFishStack.OnValUpdate += _ => NeedSave = true;
            AnglerQuestOptimization.EnableNoFishingPenalty.OnValUpdate += _ => NeedSave = true;

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
            QoLValSet.markStructuresOnMap.OnValUpdate += _ => NeedSave = true;
            QoLValSet.markPlanteraBulb.OnValUpdate += _ => NeedSave = true;
            QoLValSet.markSwordShrine.OnValUpdate += _ => NeedSave = true;
            QoLValSet.markBeeHive.OnValUpdate += _ => NeedSave = true;
            QoLValSet.markShimmer.OnValUpdate += _ => NeedSave = true;
            QoLValSet.markPyramid.OnValUpdate += _ => NeedSave = true;
            QoLValSet.markTempleAltar.OnValUpdate += _ => NeedSave = true;

            // 基础作弊
            Content.Cheat.Function1.Function.noDead.OnValUpdate += _ => NeedSave = true;
            Content.Cheat.Function1.Function.manaMax.OnValUpdate += _ => NeedSave = true;
        }

        public override object GetSaveData()
        {
            return new Data
            {
                CleanRepeatChat = CleanRepeatChat.Enable.val,
                CopyChat = CopyChat.Enable.val,
                ServerList = ServerList.Enable.val,
                ItemToolTipAdditional = ItemToolTipAdditional.Enable.val,

                ReduceMouseLag = MouseLagFixEngine.Enabled.val,
                ReduceMouseLagWin32 = MouseLagFixEngine.UseWin32Direct.val,

                ItemMaxStack = ItemMaxStackPatch.Enable.val,
                PortableCraftingStation = PortableCraftingStation.Enable.val,
                InfinitePotions = InfinitePotionAndBuff.EnableInfinitePotions.val,
                PotionThreshold = InfinitePotionAndBuff.PotionThreshold.val,
                BuffStations = InfinitePotionAndBuff.EnableBuffStations.val,
                MonsterBanners = InfinitePotionAndBuff.EnableMonsterBanners.val,
                HideEndlessBuffs = InfinitePotionAndBuff.HideEndlessBuffs.val,

                NPCAutoHouse = TownNPCOptimization.EnableAutoHouse.val,
                NPCOptimalHappiness = TownNPCOptimization.EnableOptimalHappiness.val,
                TravellingMerchantStay = TownNPCOptimization.EnableTravellingMerchantStay.val,
                QuickNurse = TownNPCOptimization.EnableQuickNurse.val,

                NoAnglerCooldown = AnglerQuestOptimization.EnableNoAnglerCooldown.val,
                QuestFishStack = AnglerQuestOptimization.EnableQuestFishStack.val,
                NoFishingPenalty = AnglerQuestOptimization.EnableNoFishingPenalty.val,

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
                MarkStructuresOnMap = QoLValSet.markStructuresOnMap.val,
                MarkPlanteraBulb = QoLValSet.markPlanteraBulb.val,
                MarkSwordShrine = QoLValSet.markSwordShrine.val,
                MarkBeeHive = QoLValSet.markBeeHive.val,
                MarkShimmer = QoLValSet.markShimmer.val,
                MarkPyramid = QoLValSet.markPyramid.val,
                MarkTempleAltar = QoLValSet.markTempleAltar.val,

                // 基础作弊
                NoDead = Content.Cheat.Function1.Function.noDead.val,
                ManaMax = Content.Cheat.Function1.Function.manaMax.val
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
            MouseLagFixEngine.Enabled.Reset();
            MouseLagFixEngine.UseWin32Direct.Reset();

            ItemMaxStackPatch.Enable.Reset();
            PortableCraftingStation.Enable.Reset();
            InfinitePotionAndBuff.EnableInfinitePotions.Reset();
            InfinitePotionAndBuff.PotionThreshold.Reset();
            InfinitePotionAndBuff.EnableBuffStations.Reset();
            InfinitePotionAndBuff.EnableMonsterBanners.Reset();
            InfinitePotionAndBuff.HideEndlessBuffs.Reset();

            TownNPCOptimization.EnableAutoHouse.Reset();
            TownNPCOptimization.EnableOptimalHappiness.Reset();
            TownNPCOptimization.EnableTravellingMerchantStay.Reset();
            TownNPCOptimization.EnableQuickNurse.Reset();

            AnglerQuestOptimization.EnableNoAnglerCooldown.Reset();
            AnglerQuestOptimization.EnableQuestFishStack.Reset();
            AnglerQuestOptimization.EnableNoFishingPenalty.Reset();

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
            QoLValSet.markStructuresOnMap.Reset();
            QoLValSet.markPlanteraBulb.Reset();
            QoLValSet.markSwordShrine.Reset();
            QoLValSet.markBeeHive.Reset();
            QoLValSet.markShimmer.Reset();
            QoLValSet.markPyramid.Reset();
            QoLValSet.markTempleAltar.Reset();

            Content.Cheat.Function1.Function.noDead.Reset();
            Content.Cheat.Function1.Function.manaMax.Reset();
        }
    }
}
