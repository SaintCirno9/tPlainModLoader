using FishingMachine.Content.IO;
using FishingMachine.Content.Tiles;
using FishingMachine.UI;
using HarmonyLib;
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
using TPML.Content.Engine;
using TPML.Core.Logging;

namespace FishingMachine
{
    /// <summary>
    /// FishingMachine 模组内容注册管理器
    /// 作者: SaintCirno9
    /// </summary>
    public class FishingMachineMod : Mod
    {
        public override string Name => "FishingMachine";
        public override string DisplayName => "自动钓鱼机";

        public override void Load()
        {
            // 注册自动钓鱼机 ModItem
            AddContent(new Content.Items.FishingMachine());
        }
    }

    /// <summary>
    /// tPlainModLoader 原生 Mod 加载器入口
    /// </summary>
    public class FishingMachineTPMLEntry : tContentPatch.Mod
    {
        private static readonly ILogger Logger = LogManager.GetLogger("FishingMachine");
        public static FishingMachineMod ModInstance { get; private set; }

        public override void Load()
        {
            try
            {
                Logger.Info("===== 开始载入 FishingMachine 模组 =====");

                // 1. 初始化 Content 钩子系统
                ContentHookDispatcher.Initialize();

                // 2. 注册模组与物品
                ModInstance = new FishingMachineMod();
                ModContent.RegisterMod(ModInstance);
                ModInstance.Load();

                // 3. 应用 Harmony 补丁
                var harmony = new Harmony("SaintCirno9.FishingMachine");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                Logger.Info("===== FishingMachine 模组加载成功 =====");
            }
            catch (Exception ex)
            {
                Logger.Error("FishingMachine 载入失败", ex);
            }
        }

        public override void Loaded()
        {
            try
            {
                // 构建并注入配方
                RecipeLoader.SetupRecipes();
                Logger.Info($"★ FishingMachine 配方加载完成，全局配方数: {Recipe.numRecipes}");
            }
            catch (Exception ex)
            {
                Logger.Error("FishingMachine 配方注入失败", ex);
            }
        }
    }

    /// <summary>
    /// 挂钩主循环更新、世界物块绘制与交互面板
    /// </summary>
    public class FishingMachinePatchMain : tContentPatch.PatchMain
    {
        private static bool _texturesLoaded = false;

        public override void UpdatePrefix(Microsoft.Xna.Framework.GameTime gameTime)
        {
            // 更新世界中所有放置的钓鱼机实体
            FishingMachineTileManager.UpdateAll();

            if (!_texturesLoaded && Main.instance?.GraphicsDevice != null)
            {
                _texturesLoaded = true;
                LoadEmbeddedTextures();
            }
        }

        public override void DoDrawPostfix(Microsoft.Xna.Framework.GameTime gameTime)
        {
            // 在原版物块绘制完成后再覆盖显示自动钓鱼机本体，避免被底层方块贴图盖住
            if (!Main.gameMenu && Main.spriteBatch != null)
            {
                FishingMachineTileManager.DrawAll(Main.spriteBatch);
            }
        }

        public override void OnEnterWorldPrefix()
        {
            FishingMachineTileManager.ClearAll();
            FishingMachineUI.Close();
        }

        public override void OnEnterWorld()
        {
            FishingMachineSaveManager.LoadMachines();
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

        private static void LoadEmbeddedTextures()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                FishingMachineTileManager.TileTexture = LoadTexture(asm, "FishingMachine.Resources.AutofisherTile.png");
                FishingMachineTileManager.HighlightTexture = LoadTexture(asm, "FishingMachine.Resources.Autofisher_Highlight.png");

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
    /// 挂钩玩家鼠标世界交互（右键打开机器、选定水域钓点）
    /// </summary>
    public class FishingMachinePlayerInteraction : tContentPatch.PatchPlayer
    {
        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This.whoAmI != Main.myPlayer || Main.gameMenu) return;

            int tileX = Player.tileTargetX;
            int tileY = Player.tileTargetY;

            // 1. 选择水域模式下的交互处理
            if (FishingMachineUI.SelectPoolMode && FishingMachineUI.CurrentEntity != null)
            {
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
                return;
            }

            // 2. 右键点击物块打开/关闭钓鱼机界面
            if (Main.mouseRight && Main.mouseRightRelease && !Main.playerInventory)
            {
                if (FishingMachineTileManager.CheckRightClick(tileX, tileY))
                {
                    Main.mouseRightRelease = false;
                    This.mouseInterface = true;
                }
            }
        }
    }

    /// <summary>
    /// 挂钩世界保存与读取，还原所有机器及其内部物品
    /// </summary>
    public class FishingMachineWorldHook : tContentPatch.PatchWorldFile
    {
        public override void SaveWorldPrefix(bool useCloudSaving, bool resetTime, bool useTemps, bool canBeSkipped)
        {
            FishingMachineSaveManager.SaveMachines();
        }

        public override void LoadWorldPostfix()
        {
            FishingMachineSaveManager.LoadMachines();
        }
    }
}
