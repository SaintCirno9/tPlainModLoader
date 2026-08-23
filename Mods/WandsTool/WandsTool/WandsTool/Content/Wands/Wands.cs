using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace WandsTool.Content
{
    public partial class Wands
    {
        public enum Shapes
        {
            line,
            circular,
            circularFilled,
            rectangle,
        }

        private static Vector2 position1;
        private static Vector2 position2;
        private static bool selecting = false;
        public static bool Selecting => selecting;
        private static List<Point> shapes = null;
        private static Shapes shapes_s = Shapes.line;

        public static void Reset()
        {
            selecting = false;
            shapes = null;
        }

        public static void Update()
        {
            Update_Select();
        }

        private static void Update_Select()
        {
            if (Main.mouseRight == true && selecting == true)
            {
                selecting = false;
                shapes = null;
                Main.mouseRightRelease = false;

                Terraria.Player player = Main.LocalPlayer;
                if (player == null) return;

                string cancelMsg = "取消操作";
                if (gameMain.Wand_LiquidMode != gameMain.LiquidMode.None)
                {
                    cancelMsg = "取消液体操作";
                }
                else
                {
                    cancelMsg = $"取消{(gameMain.Wand_isPlace ? "放置" : "破坏")}";
                }

                CombatText.NewText(player.getRect(), Color.Red, cancelMsg, true, false);
                return;
            }

            if (Main.LocalPlayer == null || Main.LocalPlayer.dead || Main.gameMenu)
            {
                Reset();
                return;
            }

            if (Main.mouseLeft == true && Main.mouseLeftRelease == true && Main.mouseRight == false && selecting == false)
            {
                if (Main.LocalPlayer.mouseInterface == true || Main.playerInventory || Main.editChest || Main.editSign || Main.ingameOptionsWindow || Main.drawingPlayerChat) return;

                selecting = true;
                position1 = Main.MouseWorld;
            }

            if (selecting == false) return;

            position2 = Main.MouseWorld;

            if (Main.mouseLeft == false)
            {
                Update_Shapes();
                selecting = false;

                if (shapes != null && shapes.Count > 0)
                {
                    // 1. 液体魔杖操作优先
                    if (gameMain.Wand_LiquidMode != gameMain.LiquidMode.None)
                    {
                        WandAction.HandleLiquid(shapes, gameMain.Wand_LiquidMode, gameMain.Wand_InfiniteLiquid);
                    }
                    else
                    {
                        // 2. 电线与制动器
                        bool wire =
                            gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red) ||
                            gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green) ||
                            gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue) ||
                            gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow) ||
                            gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator);

                        bool wireLine = shapes_s == Shapes.rectangle;

                        // 3. 物块与背景墙操作
                        if (gameMain.Wand_Tile || gameMain.Wand_Wall)
                        {
                            if (gameMain.Wand_isPlace)
                            {
                                WandAction.AddTile(shapes, gameMain.Wand_Tile, gameMain.Wand_Wall, gameMain.Wand_BlockType, gameMain.Wand_BlockReplace);
                            }
                            else
                            {
                                WandAction.DelTile(shapes, gameMain.Wand_Tile, gameMain.Wand_Wall, gameMain.Wand_CollectDrops);
                            }
                        }

                        if (wire)
                        {
                            if (wireLine)
                            {
                                Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode toolMode = gameMain.Wand_ToolMode;

                                if (gameMain.Wand_isPlace == false) toolMode |= Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Cutter;

                                WandAction.AddWireLine(position1, position2, toolMode);
                            }
                            else
                            {
                                if (gameMain.Wand_isPlace)
                                {
                                    WandAction.AddWire(shapes, gameMain.Wand_ToolMode);
                                }
                                else
                                {
                                    WandAction.DelWire(shapes, gameMain.Wand_ToolMode);
                                }
                            }
                        }
                    }
                }

                shapes = null;
            }
        }

        private static void Update_Shapes()
        {
            shapes_s = gameMain.Wand_Shapes;

            switch (shapes_s)
            {
                case Shapes.line: shapes = WandUtils.GetShapes_line(position1, position2); break;
                case Shapes.circular: shapes = WandUtils.GetShapes_Circular(position1, position2); break;
                case Shapes.circularFilled: shapes = WandUtils.GetShapes_CircularFilled(position1, position2); break;
                case Shapes.rectangle: shapes = WandUtils.GetShapes_Rectangle(position1, position2); break;
                default: break;
            }
        }

        public static void Draw()
        {
            if (selecting == false) return;

            if (Main.GameUpdateCount % 5 == 0 || shapes == null) Update_Shapes();

            if (shapes?.Count > 0 == false) return;

            Color borderColor;
            string modeName;

            if (gameMain.Wand_LiquidMode != gameMain.LiquidMode.None)
            {
                switch (gameMain.Wand_LiquidMode)
                {
                    case gameMain.LiquidMode.Water:
                        borderColor = new Color(30, 160, 255);
                        modeName = gameMain.Wand_InfiniteLiquid ? "无限水" : "放置水";
                        break;
                    case gameMain.LiquidMode.Lava:
                        borderColor = new Color(255, 90, 20);
                        modeName = gameMain.Wand_InfiniteLiquid ? "无限岩浆" : "放置岩浆";
                        break;
                    case gameMain.LiquidMode.Honey:
                        borderColor = new Color(255, 190, 0);
                        modeName = gameMain.Wand_InfiniteLiquid ? "无限蜂蜜" : "放置蜂蜜";
                        break;
                    case gameMain.LiquidMode.Shimmer:
                        borderColor = new Color(225, 120, 255);
                        modeName = gameMain.Wand_InfiniteLiquid ? "无限微光" : "放置微光";
                        break;
                    case gameMain.LiquidMode.Absorb:
                        borderColor = new Color(0, 230, 200);
                        modeName = "吸收液体";
                        break;
                    case gameMain.LiquidMode.Clear:
                        borderColor = new Color(220, 220, 220);
                        modeName = "清空液体";
                        break;
                    default:
                        borderColor = Color.White;
                        modeName = "液体操作";
                        break;
                }
            }
            else if (gameMain.Wand_isPlace)
            {
                if (gameMain.Wand_BlockReplace)
                {
                    borderColor = new Color(255, 165, 0);
                    modeName = "方块/墙替换";
                }
                else
                {
                    borderColor = new Color(40, 250, 80);
                    modeName = "放置";
                }
            }
            else
            {
                borderColor = new Color(250, 40, 80);
                modeName = (shapes_s == Shapes.circularFilled) ? "星爆破坏" : "区域破坏";
            }

            Color backgroundColor = borderColor * 0.35f;

            int w = (int)Math.Abs(Math.Floor(position1.X / 16) - Math.Floor(position2.X / 16)) + 1;
            int h = (int)Math.Abs(Math.Floor(position1.Y / 16) - Math.Floor(position2.Y / 16)) + 1;
            int count = shapes?.Count ?? 0;
            Terraria.Utils.DrawBorderString(Main.spriteBatch, $"[{modeName}] {w} x {h} ({count}格)", new Vector2(Main.mouseX, Main.mouseY + 50), borderColor, anchorx: 0.5f, anchory: 0.5f);

            switch (shapes_s)
            {
                case Shapes.line: WandUtils.Draw_line(shapes, position1, position2, borderColor, backgroundColor); break;
                case Shapes.circular: WandUtils.Draw_circular(shapes, position1, position2, borderColor, backgroundColor); break;
                case Shapes.circularFilled: WandUtils.Draw_circularFilled(shapes, position1, position2, borderColor, backgroundColor); break;
                case Shapes.rectangle: WandUtils.Draw_rectangle(shapes, position1, position2, borderColor, backgroundColor); break;
                default: break;
            }
        }
    }
}
