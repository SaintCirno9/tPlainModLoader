using OptimizeAndTool.Content;
using OptimizeAndTool.Content.QoL;
using OptimizeAndTool.Content.ServerList;
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

            // 巨大背包
            public bool BigBag = true;
            public bool BigBagCraft = true;
            public int BigBagCapacity = 100;
        }

        public override string Name => "设置";
        public override string Title => "优化和工具: 设置";
        public override string FilePath => "setting.json";
        public override Type DataType => typeof(Data);

        public override void Load(object v)
        {
            if (v is Data data)
            {
                CleanRepeatChat.Enable.val = data.CleanRepeatChat;
                CopyChat.Enable.val = data.CopyChat;
                ServerList.Enable.val = data.ServerList;
                ItemToolTipAdditional.Enable.val = data.ItemToolTipAdditional;

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

                Content.BigBag.BigBag.EnableBigBag.val = data.BigBag;
                Content.BigBag.BigBag.EnableBigBagCraft.val = data.BigBagCraft;
                Content.BigBag.BigBag.Capacity.val = data.BigBagCapacity;
            }

            CleanRepeatChat.Enable.OnValUpdate += _ => NeedSave = true;
            CopyChat.Enable.OnValUpdate += _ => NeedSave = true;
            ServerList.Enable.OnValUpdate += _ => NeedSave = true;
            ItemToolTipAdditional.Enable.OnValUpdate += _ => NeedSave = true;

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

            Content.BigBag.BigBag.EnableBigBag.OnValUpdate += _ => NeedSave = true;
            Content.BigBag.BigBag.EnableBigBagCraft.OnValUpdate += _ => NeedSave = true;
            Content.BigBag.BigBag.Capacity.OnValUpdate += _ => NeedSave = true;
        }

        public override UIElement GetUI()
        {
            return UIBuild.get3(Function.GetUI());
        }

        public override void SetDefault()
        {
            CleanRepeatChat.Enable.Reset();
            CopyChat.Enable.Reset();
            ServerList.Enable.Reset();
            ItemToolTipAdditional.Enable.Reset();

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

            Content.BigBag.BigBag.EnableBigBag.Reset();
            Content.BigBag.BigBag.EnableBigBagCraft.Reset();
            Content.BigBag.BigBag.Capacity.Reset();
        }

        public override object GetSaveData()
        {
            return new Data()
            {
                CleanRepeatChat = CleanRepeatChat.Enable.val,
                CopyChat = CopyChat.Enable.val,
                ServerList = ServerList.Enable.val,
                ItemToolTipAdditional = ItemToolTipAdditional.Enable.val,

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

                BigBag = Content.BigBag.BigBag.EnableBigBag.val,
                BigBagCraft = Content.BigBag.BigBag.EnableBigBagCraft.val,
                BigBagCapacity = Content.BigBag.BigBag.Capacity.val,
            };
        }
    }
}
