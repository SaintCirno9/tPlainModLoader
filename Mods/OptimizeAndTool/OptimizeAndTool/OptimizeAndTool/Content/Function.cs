using CommandHelp;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Content.EnhancedTooltips;
using OptimizeAndTool.Content.Optimize.ReduceMouseLag;
using OptimizeAndTool.Content.QoL;
using OptimizeAndTool.Content.QoL.Fishing;
using OptimizeAndTool.Content.QoL.Pipette;
using OptimizeAndTool.Content.QoL.Reforge;
using OptimizeAndTool.Content.QoL.VeinMining;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using System.Collections.Generic;
using tContentPatch;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content
{
    internal partial class Function : PatchPlayer
    {
        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>();

            // 1. 基础系统与信息
            cos.AddRange(CleanRepeatChat.GetCO());
            cos.AddRange(CopyChat.GetCO());
            cos.AddRange(ServerList.ServerList.GetCO());
            cos.AddRange(ItemToolTipAdditional.GetCO());
            cos.AddRange(EnhancedTooltipConfig.GetCO());
            cos.AddRange(DisplayProjectileInfo.GetCO());

            // 2. 性能与输入优化
            cos.AddRange(MouseLagFixEngine.GetCO());
            cos.AddRange(PatchGameViewMatrixZoomLimit.GetCO());

            // 3. 扩展存储系统
            cos.AddRange(Content.BigBag.BigBag.GetCO());
            cos.AddRange(AccessoryBagConfig.GetCO());

            // 4. QoL 规则与自动化
            cos.AddRange(VeinMiningLogic.GetCO());
            cos.AddRange(PipetteEngine.GetCO());
            cos.AddRange(ItemMaxStackPatch.GetCO());
            cos.AddRange(UncapMaxLifePatch.GetCO());
            cos.AddRange(PortableCraftingStation.GetCO());
            cos.AddRange(PortableContainer.GetCO());
            cos.AddRange(InfinitePotionAndBuff.GetCO());
            cos.AddRange(TownNPCOptimization.GetCO());
            cos.AddRange(ReforgeOptimization.GetCO());
            cos.AddRange(AnglerQuestOptimization.GetCO());
            cos.AddRange(FishingCrateModifier.GetCO());
            cos.AddRange(AutoFishingSystem.GetCO());
            cos.AddRange(MultipleFishingLines.GetCO());
            cos.AddRange(FishingCatchProcessor.GetCO());
            cos.AddRange(AutoFishingSupplies.GetCO());
            cos.AddRange(FishingInfoHUD.GetCO());

            // 5. 杂项辅助与调试 (原 SundryTool)
            cos.AddRange(Cheat.Function1.Function.GetCO());
            cos.AddRange(Cheat.Function2.Function.GetCO());
            cos.AddRange(Cheat.HeldItemModify.ValSet.GetCO());
            cos.AddRange(Cheat.PlayerModify.ValSet.GetCO());
            cos.AddRange(Cheat.QoL.QoLValSet.GetCO());

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>();

            // 0. 常用基础与聊天
            uis.AddRange(CleanRepeatChat.GetUI());
            uis.AddRange(CopyChat.GetUI());
            uis.AddRange(ServerList.ServerList.GetUI());
            uis.AddRange(ItemToolTipAdditional.GetUI());
            uis.AddRange(EnhancedTooltipConfig.GetUI());

            // 1. 性能与输入优化
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_5010", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "性能与输入优化"));
            uis.AddRange(MouseLagFixEngine.GetUI());
            uis.AddRange(PatchGameViewMatrixZoomLimit.GetUI());

            // 2. 扩展存储系统 (大背包 + 随身饰品袋)
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_3813", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "扩展存储系统"));
            uis.AddRange(Content.BigBag.BigBag.GetUI());
            uis.AddRange(AccessoryBagConfig.GetUI());

            // 3. 采矿与建筑 QoL
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_3509", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "采矿与建造体验"));
            uis.AddRange(VeinMiningLogic.GetUI());
            uis.AddRange(PipetteEngine.GetUI());

            // 4. 背包与便携制作
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_361", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "便携制作与堆叠"));
            uis.AddRange(ItemMaxStackPatch.GetUI());
            uis.AddRange(UncapMaxLifePatch.GetUI());
            uis.AddRange(PortableCraftingStation.GetUI());
            uis.AddRange(PortableContainer.GetUI());

            // 5. 药水与随身增益
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_289", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "无尽药水与增益"));
            uis.AddRange(InfinitePotionAndBuff.GetUI());

            // 6. 城镇 NPC 与渔夫优化
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_267", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "城镇 NPC 与商贩"));
            uis.AddRange(TownNPCOptimization.GetUI());
            uis.AddRange(ReforgeOptimization.GetUI());
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_2422", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "渔夫任务与钓鱼 QoL"));
            uis.AddRange(AnglerQuestOptimization.GetUI());
            uis.AddRange(FishingCrateModifier.GetUI());
            uis.AddRange(AutoFishingSystem.GetUI());
            uis.AddRange(MultipleFishingLines.GetUI());
            uis.AddRange(FishingCatchProcessor.GetUI());
            uis.AddRange(AutoFishingSupplies.GetUI());
            uis.AddRange(FishingInfoHUD.GetUI());

            // 7. 杂项辅助与调试 (原 SundryTool 功能合集)
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_1326", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "杂项辅助 (玩家能力)"));
            uis.AddRange(Cheat.Function1.Function.GetUI());

            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_2997", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "杂项辅助 (世界与环境)"));
            uis.AddRange(Cheat.Function2.Function.GetUI());

            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_3611", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "杂项 QoL 增强"));
            uis.AddRange(Cheat.QoL.QoLValSet.GetUI());

            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_3095", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "手持物品与属性微调"));
            uis.AddRange(Cheat.HeldItemModify.ValSet.GetUI());
            uis.AddRange(Cheat.PlayerModify.ValSet.GetUI());

            // 8. 调试与视图工具
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_2799", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "调试与信息显示"));
            uis.AddRange(DisplayProjectileInfo.GetUI());

            return uis;
        }
    }
}
