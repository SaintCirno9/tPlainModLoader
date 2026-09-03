using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using TPML;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using WandsTool.Content;
using WandsTool.Content.Structure;
using WandsTool.KeyBind;

namespace WandsTool
{
    /// <summary>
    /// 魔杖工具（WandsTool）主系统生命周期管理与事件调度
    /// 负责 UI 图层挂载、快捷键检测、手持物品工作模式自适应以及施工协程驱动
    /// </summary>
    public class WandsToolSystem : ModSystem
    {
        private static WandsPanel uiInstance = null;
        private static WandsPanel UI
        {
            get
            {
                if (uiInstance == null && !Main.dedServ)
                {
                    Resources.Load();
                    uiInstance = new WandsPanel();
                }
                return uiInstance;
            }
        }

        public override void Load()
        {
            base.Load();
            WandsKeybind.Initialize();
        }

        public override void Initialize()
        {
            WandsKeybind.Initialize();
        }

        public override void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers)
        {
            WandsKeybind.Initialize();

            int index = gameInterfaceLayers.FindIndex(i => i.Name == "Vanilla: Laser Ruler");
            if (index != -1)
            {
                ++index;
                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.WandsTool: Laser Ruler Postfix Game",
                    () =>
                    {
                        if (GameMain.Wand_isEnable)
                        {
                            Wands.Draw();
                        }
                        return true;
                    },
                    InterfaceScaleType.Game));

                gameInterfaceLayers.Insert(index, new LegacyGameInterfaceLayer(
                    "StaticTile.WandsTool: Laser Ruler Postfix UI",
                    () =>
                    {
                        if (GameMain.Wand_isEnable)
                        {
                            Wands.DrawCursorModeTooltip(Main.spriteBatch);
                        }

                        if (GameMain.UI_WandsPanel1_isOpen && GameMain.Wand_isEnable)
                        {
                            UI?.Draw(Main.spriteBatch, Main.gameTimeCache);
                        }

                        // 蓝图库卡片悬浮材料清单（自判定 hover 存活，未悬停时不绘制任何内容）
                        WandsTool.Content.Structure.StructureMaterialSummary.DrawOverlay(Main.spriteBatch);

                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        public override void UpdateUIStatesPostfix(GameTime gameTime)
        {
            if (Main.gameMenu)
            {
                GameMain.Wand_isEnable = false;
                UI?.Close();
                StructurePlacement.Abort();
                return;
            }

            if (GameMain.UI_WandsPanel1_isOpen && GameMain.Wand_isEnable)
            {
                UI?.Update(gameTime);
                UI?.update(gameTime);
            }
        }

        private static int lastSelectedItem = -1;

        public override void DoUpdateInWorldPostfix()
        {
            Player player = Main.LocalPlayer;
            if (player == null || Main.gameMenu) return;

            // 监听统一 ModKeybind 开关魔杖模式（自动全局静默聊天/打字等）
            if (WandsKeybind.ToggleWand?.JustPressed == true)
            {
                SoundEngine.PlaySound(12);
                GameMain.ToggleWand();
            }

            if (GameMain.Wand_isEnable)
            {
                // 监听快捷栏手持物品切换，实时自适应魔棒工作模式（放置作业进行中不切换，防止协程中途改模式）
                if (player.selectedItem != lastSelectedItem && !StructurePlacement.IsPlacing)
                {
                    lastSelectedItem = player.selectedItem;
                    GameMain.AutoAdaptModeToHeldItem(player);
                }

                // 监听施工一键撤销快捷键（U）（放置作业进行中封锁，防止撤销写格与协程写格交错）
                if (WandsKeybind.UndoAction?.JustPressed == true)
                {
                    if (StructurePlacement.IsPlacing)
                    {
                        Main.NewText("[魔杖] 蓝图放置进行中，暂不能撤销", 255, 200, 100);
                    }
                    else
                    {
                        int undone = WandHistory.Undo(player);
                        if (undone == -1)
                        {
                            Main.NewText("[魔杖] 没有可撤销的操作", 255, 170, 170);
                        }
                        else if (undone == -2)
                        {
                            Main.NewText("[魔杖] 上一次操作尚未处理完成，请稍候再撤销", 255, 200, 100);
                        }
                        else
                        {
                            Terraria.CombatText.NewText(player.getRect(), Microsoft.Xna.Framework.Color.LightBlue, $"已撤销 {undone} 格", true, false);
                            SoundEngine.PlaySound(SoundID.MenuOpen);
                        }
                    }
                }

                bool wasSelecting = Wands.Selecting;
                Wands.Update();
                WandAction.Update();

                // 驱动蓝图分帧放置协程
                StructurePlacement.Update();

                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    // 放置作业进行中：不弹出轮盘也不吞 release 标志，放行原版右键交互与 UI 点击
                    if (!StructurePlacement.IsPlacing)
                    {
                        if (!wasSelecting && !Wands.Selecting)
                        {
                            UI?.Toggle();
                        }
                        else
                        {
                            Main.mouseRightRelease = false;
                        }
                    }
                }
            }
            else
            {
                lastSelectedItem = -1;
                UI?.Close();

                Wands.Reset();
                WandAction.Clear();
                WandHistory.Clear();
                if (StructurePlacement.IsPlacing)
                {
                    Terraria.CombatText.NewText(player.getRect(), Microsoft.Xna.Framework.Color.Orange, "放置已中止", true, false);
                }
                StructurePlacement.Abort();
                GameMain.CutSourceRect = null;
                GameMain.Wand_StructureMode = GameMain.StructureMode.None;
                WandsPanel.AutoReopenManagerAfterPlacement = false;
            }
        }
    }

    public class WandsToolContentMod : TPML.Content.Mod { }
}
