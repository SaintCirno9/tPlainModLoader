using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OptimizeAndTool.Content.Creative;
using OptimizeAndTool.Content.Storage.AccessoryBox;
using tContentPatch;
using tContentPatch.ModLoad;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;

namespace OptimizeAndTool.ModLinkage
{
    /// <summary>
    /// QuickButton 悬浮工具栏接入：注册巨大背包、随身饰品箱、创造物品栏等快捷按钮
    /// 作者: SaintCirno9
    /// </summary>
    public class ModQuickButton : Mod
    {
        public static bool EnableBigBagBtn = true;
        public static bool EnableAccessoryBoxBtn = true;
        public static bool EnableCreativeInventoryBtn = true;
        public static bool EnableTownNPCHomeBtn = true;
        public override void Loaded()
        {
            if (Main.dedServ) return;

            List<ModObject> mos = ContentPatch.GetModObjects();
            if (mos == null) return;

            ModObject mo = mos.FirstOrDefault(i => i.assembly?.GetName().Name == "QuickButton");
            if (mo == null) return;

            Type type = mo.assembly.GetType("QuickButton.QuickButton.QuickButton");
            if (type == null) return;

            MethodInfo mi = type.GetMethod("Add", BindingFlags.Static | BindingFlags.Public);
            if (mi == null) return;

            // 1. 巨大背包按钮
            if (EnableBigBagBtn)
            {
                UIImage ui_bag = new UIImage(Main.Assets.Request<Texture2D>("Images/Item_4131", ReLogic.Content.AssetRequestMode.ImmediateLoad));
                ui_bag.Width.Pixels = 32;
                ui_bag.Height.Pixels = 32;
                ui_bag.ScaleToFit = true;
                ui_bag.OnUpdate += _ =>
                {
                    if (ui_bag.IsMouseHovering) Main.instance.MouseText("巨大背包");
                };
                ui_bag.OnLeftClick += (e, s) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    ModifyInterfaceLayers.SwitchBigBag();
                };
                mi.Invoke(null, new object[] { "OptimizeAndTool.BigBag.Switch", ui_bag });
            }

            // 2. 随身饰品袋按钮
            if (EnableAccessoryBoxBtn)
            {
                UIImage ui_box = new UIImage(Main.Assets.Request<Texture2D>("Images/Item_1862", ReLogic.Content.AssetRequestMode.ImmediateLoad));
                ui_box.Width.Pixels = 32;
                ui_box.Height.Pixels = 32;
                ui_box.ScaleToFit = true;
                ui_box.OnUpdate += _ =>
                {
                    if (ui_box.IsMouseHovering) Main.instance.MouseText("随身饰品袋");
                };
                ui_box.OnLeftClick += (e, s) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    AccessoryBagWindow.Toggle();
                };
                mi.Invoke(null, new object[] { "OptimizeAndTool.AccessoryBox.Switch", ui_box });
            }

            // 3. 创造物品栏按钮
            if (EnableCreativeInventoryBtn)
            {
                UIImage ui_creative = new UIImage(Main.Assets.Request<Texture2D>("Images/Item_306", ReLogic.Content.AssetRequestMode.ImmediateLoad));
                ui_creative.Width.Pixels = 32;
                ui_creative.Height.Pixels = 32;
                ui_creative.ScaleToFit = true;
                ui_creative.OnUpdate += _ =>
                {
                    if (ui_creative.IsMouseHovering) Main.instance.MouseText("创造物品栏");
                };
                ui_creative.OnLeftClick += (e, s) =>
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    CreativeInventory.SwitchOpenOrClose();
                };
                mi.Invoke(null, new object[] { "OptimizeAndTool.CreativeInventory.Switch", ui_creative });
            }

            // 4. 城镇 NPC 全员回家按钮
            if (EnableTownNPCHomeBtn)
            {
                UIImage ui_npcHome = new UIImage(Main.Assets.Request<Texture2D>("Images/Item_2350", ReLogic.Content.AssetRequestMode.ImmediateLoad));
                ui_npcHome.Width.Pixels = 32;
                ui_npcHome.Height.Pixels = 32;
                ui_npcHome.ScaleToFit = true;
                ui_npcHome.OnUpdate += _ =>
                {
                    if (ui_npcHome.IsMouseHovering) Main.instance.MouseText("城镇 NPC 全员回家（召回回房）");
                };
                ui_npcHome.OnLeftClick += (e, s) =>
                {
                    OptimizeAndTool.Content.QoL.TownNPCOptimization.TeleportAllTownNPCsHome();
                };
                mi.Invoke(null, new object[] { "OptimizeAndTool.TownNPC.Home", ui_npcHome });
            }
        }
    }
}
