using CommandHelp;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Content.QoL;
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
            cos.AddRange(CleanRepeatChat.GetCO());
            cos.AddRange(CopyChat.GetCO());
            cos.AddRange(ServerList.ServerList.GetCO());
            cos.AddRange(ItemToolTipAdditional.GetCO());

            // QoL 核心补丁指令
            cos.AddRange(ItemMaxStackPatch.GetCO());
            cos.AddRange(PortableCraftingStation.GetCO());
            cos.AddRange(PortableContainer.GetCO());
            cos.AddRange(Content.BigBag.BigBag.GetCO());
            cos.AddRange(InfinitePotionAndBuff.GetCO());
            cos.AddRange(TownNPCOptimization.GetCO());
            cos.AddRange(AnglerQuestOptimization.GetCO());

            cos.AddRange(DisplayProjectileInfo.GetCO());
            cos.AddRange(PatchGameViewMatrixZoomLimit.GetCO());

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>();
            uis.AddRange(CleanRepeatChat.GetUI());
            uis.AddRange(CopyChat.GetUI());
            uis.AddRange(ServerList.ServerList.GetUI());
            uis.AddRange(ItemToolTipAdditional.GetUI());

            // 1. 背包与制作优化
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_361", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "背包与便携制作"));
            uis.AddRange(ItemMaxStackPatch.GetUI());
            uis.AddRange(PortableCraftingStation.GetUI());
            uis.AddRange(PortableContainer.GetUI());
            uis.AddRange(Content.BigBag.BigBag.GetUI());

            // 2. 药水与随身增益
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_289", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "无尽药水与增益"));
            uis.AddRange(InfinitePotionAndBuff.GetUI());

            // 3. 城镇 NPC 与商贩优化
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_267", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "城镇 NPC 与商贩"));
            uis.AddRange(TownNPCOptimization.GetUI());

            // 4. 渔夫任务与钓鱼
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_2422", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "渔夫任务与钓鱼"));
            uis.AddRange(AnglerQuestOptimization.GetUI());

            // 5. 调试与视图工具
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_2799", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "调试工具"));
            uis.AddRange(DisplayProjectileInfo.GetUI());
            uis.Add(new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_4766", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "缩放限制"));
            uis.AddRange(PatchGameViewMatrixZoomLimit.GetUI());

            return uis;
        }
    }
}
