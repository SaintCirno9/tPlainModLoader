using FishingMachine.Content.Tiles;
using FishingMachine.UI;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using TPML.Core.Logging;

namespace FishingMachine
{
    /// <summary>
    /// FishingMachine 模组主类 (TPML.Content.Mod)
    /// 自动钓鱼机全部内容由 ContentHost 自动扫描并注册 (ModItem, ModTile, ModTileEntity)
    /// 作者: SaintCirno9
    /// </summary>
    public class FishingMachineMod : Mod
    {
        public override string Name => "FishingMachine";
        public override string DisplayName => "自动钓鱼机";

        public override void Load()
        {
            LoadEmbeddedTextures();
        }

        private static void LoadEmbeddedTextures()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                FishingMachineTile.HighlightTexture = LoadTexture(asm, "FishingMachine.Resources.Autofisher_Highlight.png");

                FishingMachineUI.SlotPoleTexture = LoadTexture(asm, "FishingMachine.Resources.Slot_FishingPole.png");
                FishingMachineUI.SlotBaitTexture = LoadTexture(asm, "FishingMachine.Resources.Slot_Bait.png");
                FishingMachineUI.SlotAccTexture = LoadTexture(asm, "FishingMachine.Resources.Slot_Accessory.png");

                FishingMachineUI.FisherLootAll = LoadTexture(asm, "FishingMachine.Resources.FisherLootAll.png");
                FishingMachineUI.FisherLootAllHover = LoadTexture(asm, "FishingMachine.Resources.FisherLootAll_Hover.png");
                FishingMachineUI.ChestAutoDeposit = LoadTexture(asm, "FishingMachine.Resources.ChestAutoDeposit.png");
                FishingMachineUI.ChestAutoDepositHover = LoadTexture(asm, "FishingMachine.Resources.ChestAutoDeposit_Hover.png");
                FishingMachineUI.IconFreeFilter = LoadTexture(asm, "FishingMachine.Resources.IconFreeFilter.png");
                FishingMachineUI.IconFreeFilterHover = LoadTexture(asm, "FishingMachine.Resources.IconFreeFilterHover.png");
                FishingMachineUI.SelectPoolOff = LoadTexture(asm, "FishingMachine.Resources.SelectPoolOff.png");
                FishingMachineUI.SelectPoolOn = LoadTexture(asm, "FishingMachine.Resources.SelectPoolOn.png");
                FishingMachineUI.DisabledItem = LoadTexture(asm, "FishingMachine.Resources.DisabledItem.png");
            }
            catch (Exception ex)
            {
                LogManager.GetLogger("FishingMachine").Warn("加载嵌入贴图异常", ex);
            }
        }

        private static Texture2D LoadTexture(Assembly asm, string resourceName)
        {
            using (Stream s = asm.GetManifestResourceStream(resourceName))
            {
                if (s != null) return Texture2D.FromStream(Main.instance.GraphicsDevice, s);
            }
            return null;
        }
    }

    /// <summary>
    /// tPlainModLoader 原生 Mod 加载器入口
    /// </summary>
    public class FishingMachineTPMLEntry : tContentPatch.Mod
    {
        private static readonly ILogger Logger = LogManager.GetLogger("FishingMachine");

        public override void Load()
        {
            Logger.Info("===== FishingMachine 模组加载成功 =====");
        }
    }

    /// <summary>
    /// 挂钩 UI 图层绘制与世界生命周期
    /// </summary>
    public class FishingMachineMain : tContentPatch.PatchMain
    {
        public override void OnEnterWorldPrefix()
        {
            FishingMachineUI.Close();
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            int index = gameInterfaceLayers.FindIndex(layer => layer.Name.Equals("Vanilla: Cursor"));
            if (index != -1)
            {
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer("FishingMachine: GUI", () =>
                {
                    FishingMachineUI.Draw(Main.spriteBatch);
                    return true;
                }, InterfaceScaleType.UI));
            }
        }
    }

    /// <summary>
    /// 挂钩玩家鼠标世界交互（选定水域钓点）
    /// </summary>
    public class FishingMachinePlayerInteraction : tContentPatch.PatchPlayer
    {
        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This.whoAmI != Main.myPlayer || Main.gameMenu) return;

            int tileX = Player.tileTargetX;
            int tileY = Player.tileTargetY;

            // 选择水域模式下的交互处理
            if (FishingMachineUI.SelectPoolMode && FishingMachineUI.CurrentEntity != null)
            {
                if (This.mouseInterface || FishingMachineUI.IsMouseHoveringUI)
                {
                    return; // 鼠标位于 UI 交互范围内，阻断世界选水点击穿透
                }

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Tile target = Framing.GetTileSafely(tileX, tileY);
                    if (target.liquid > 0 && !WorldGen.SolidTile(tileX, tileY))
                    {
                        FishingMachineUI.CurrentEntity.locatePoint = new Point16(tileX, tileY);
                        FishingMachineUI.CurrentEntity.RefreshPond();
                        FishingMachineUI.SelectPoolMode = false;
                        SoundEngine.PlaySound(SoundID.Splash);
                        Main.NewText("[c/00FFDD:已成功选定新的钓点水域！]");
                        Main.mouseLeftRelease = false;
                        This.mouseInterface = true;
                    }
                    else
                    {
                        Main.NewText("[c/FF9900:目标位置没有液体，请点击有效的水体方块。]");
                    }
                }
                else if (Main.mouseRight && Main.mouseRightRelease)
                {
                    FishingMachineUI.SelectPoolMode = false;
                    SoundEngine.PlaySound(SoundID.MenuClose);
                    Main.NewText("[c/AAAAAA:已退出选择水域模式。]");
                    Main.mouseRightRelease = false;
                    This.mouseInterface = true;
                }
            }
        }
    }
}
