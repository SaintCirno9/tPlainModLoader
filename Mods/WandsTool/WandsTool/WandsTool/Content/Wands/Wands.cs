using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace WandsTool.Content
{
    public partial class Wands
    {
        public enum Shapes
        {
            line,
            circular,
            filledCircular,
            rectangle,
            hollowRectangle,
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
            // 蓝图放置模式快捷键检测（已接入统一 ModKeybind 系统，支持原版设置改键并自动静默输入）
            if (gameMain.Wand_StructureMode == gameMain.StructureMode.Paste && Structure.StructureStorage.Clipboard != null && !Structure.StructurePlacement.IsPlacing)
            {
                if (WandsTool.KeyBind.WandsKeybind.FlipHorizontal?.JustPressed == true)
                {
                    Structure.StructureStorage.Clipboard = Structure.StructureStorage.Clipboard.FlipHorizontal();
                    CombatText.NewText(Main.LocalPlayer.getRect(), Color.Cyan, "水平翻转", true, false);
                }
                if (WandsTool.KeyBind.WandsKeybind.FlipVertical?.JustPressed == true)
                {
                    Structure.StructureStorage.Clipboard = Structure.StructureStorage.Clipboard.FlipVertical();
                    CombatText.NewText(Main.LocalPlayer.getRect(), Color.Cyan, "垂直翻转", true, false);
                }
            }

            Update_Select();
        }

        private static void Update_Select()
        {
            if (Main.LocalPlayer == null || Main.LocalPlayer.dead || Main.gameMenu)
            {
                Reset();
                return;
            }

            // 1. 蓝图粘贴模式处理
            if (gameMain.Wand_StructureMode == gameMain.StructureMode.Paste)
            {
                // 放置作业进行中：忽略粘贴与取消输入，但不吞鼠标 release 标志，
                // 保证背包/合成/装备栏等原版 Draw 阶段 UI 的点击不受影响
                if (Structure.StructurePlacement.IsPlacing)
                {
                    return;
                }

                if (Main.mouseRight && Main.mouseRightRelease)
                {
                    bool wasCut = gameMain.CutSourceRect.HasValue;
                    gameMain.CutSourceRect = null;

                    gameMain.StructureMode fallbackMode = gameMain.LastActiveStructureMode;
                    if (fallbackMode == gameMain.StructureMode.None || fallbackMode == gameMain.StructureMode.Paste)
                    {
                        fallbackMode = gameMain.StructureMode.Copy;
                    }
                    gameMain.Wand_StructureMode = fallbackMode;
                    Main.mouseRightRelease = false;

                    string modeText = (fallbackMode == gameMain.StructureMode.Cut) ? "剪切模式" :
                                      (fallbackMode == gameMain.StructureMode.Copy) ? "复制模式" : "蓝图模式";
                    CombatText.NewText(Main.LocalPlayer.getRect(), Color.Orange, wasCut ? $"已取消剪切 (回到{modeText})" : $"已退出放置 (回到{modeText})", true, false);

                    if (wandsPanel.AutoReopenManagerAfterPlacement)
                    {
                        wandsPanel.AutoReopenManagerAfterPlacement = false;
                        wandsPanel.OpenBlueprintManager();
                    }
                    return;
                }

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    // 背包开启也允许放置（仅拦截真正处于 UI 输入状态的控件悬停）
                    if (Main.LocalPlayer.mouseInterface || Main.editChest || Main.editSign || Main.ingameOptionsWindow || Main.drawingPlayerChat) return;

                    if (Structure.StructureStorage.Clipboard != null)
                    {
                        // 发起分帧放置：材料校验与扣除同步完成，落格主体跨帧执行，完成后回调重开蓝图管理器
                        bool accepted = Structure.StructurePlacement.BeginPlace(
                            Structure.StructureStorage.Clipboard,
                            Main.MouseWorld.ToTileCoordinates(),
                            Main.LocalPlayer,
                            gameMain.Wand_StructureOverwrite,
                            () =>
                            {
                                if (wandsPanel.AutoReopenManagerAfterPlacement)
                                {
                                    wandsPanel.AutoReopenManagerAfterPlacement = false;
                                    wandsPanel.OpenBlueprintManager();
                                }
                            });
                    }
                    Main.mouseLeftRelease = false;
                    return;
                }
                return;
            }

            // 2. 常规或复制模式右键取消选区
            if (Main.mouseRight == true && selecting == true)
            {
                selecting = false;
                shapes = null;
                Main.mouseRightRelease = false;

                Terraria.Player player = Main.LocalPlayer;
                if (player == null) return;

                string cancelMsg = (gameMain.Wand_StructureMode == gameMain.StructureMode.Copy) ? "取消复制" :
                    (gameMain.Wand_StructureMode == gameMain.StructureMode.Cut) ? "取消剪切" :
                    (gameMain.Wand_StructureMode == gameMain.StructureMode.Delete) ? "取消删除" :
                    (gameMain.Wand_LiquidMode != gameMain.LiquidMode.None) ? "取消液体操作" :
                    $"取消{(gameMain.Wand_isPlace ? "放置" : "破坏")}";

                CombatText.NewText(player.getRect(), Color.Red, cancelMsg, true, false);
                return;
            }

            if (Main.mouseLeft == true && Main.mouseLeftRelease == true && Main.mouseRight == false && selecting == false)
            {
                // 背包开启也能在空白世界区域启动框选（鼠标悬停背包 UI 控件时 mouseInterface 为 true 仍会拦截）
                if (Main.LocalPlayer.mouseInterface == true || Main.editChest || Main.editSign || Main.ingameOptionsWindow || Main.drawingPlayerChat) return;

                selecting = true;
                position1 = Main.MouseWorld;
            }

            if (selecting == false) return;

            position2 = Main.MouseWorld;

            if (Main.mouseLeft == false)
            {
                // 3. 结构删除模式（同时清除物块与背景墙）
                if (gameMain.Wand_StructureMode == gameMain.StructureMode.Delete)
                {
                    Point p1 = position1.ToTileCoordinates();
                    Point p2 = position2.ToTileCoordinates();
                    int minX = Math.Min(p1.X, p2.X);
                    int minY = Math.Min(p1.Y, p2.Y);
                    int w = Math.Abs(p1.X - p2.X) + 1;
                    int h = Math.Abs(p1.Y - p2.Y) + 1;

                    bool noItem = !gameMain.Wand_CollectDrops;
                    for (int x = minX; x < minX + w; x++)
                    {
                        for (int y = minY; y < minY + h; y++)
                        {
                            if (x < 0 || x >= Main.tile.GetLength(0) || y < 0 || y >= Main.tile.GetLength(1)) continue;
                            Tile tile = Main.tile[x, y];
                            if (tile != null)
                            {
                                if (tile.active()) WorldGen.KillTile(x, y, false, false, noItem);
                                if (tile.wall > 0) WorldGen.KillWall(x, y, false);
                            }
                        }
                    }

                    if (Main.netMode == 1)
                    {
                        NetMessage.SendTileSquare(Main.LocalPlayer.whoAmI, minX, minY, w, h);
                    }

                    if (gameMain.Wand_CollectDrops && Main.LocalPlayer != null)
                    {
                        Rectangle worldRect = new Rectangle(minX * 16 - 32, minY * 16 - 32, w * 16 + 64, h * 16 + 64);
                        for (int i = 0; i < 400; i++)
                        {
                            var it = Main.item[i];
                            if (it != null && it.active && it.stack > 0)
                            {
                                if (worldRect.Contains((int)it.Center.X, (int)it.Center.Y))
                                {
                                    it.position = Main.LocalPlayer.Center - new Vector2(it.width / 2f, it.height / 2f);
                                    it.velocity = Vector2.Zero;
                                    it.beingGrabbed = true;
                                }
                            }
                        }
                    }

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Main.LocalPlayer.position);
                    CombatText.NewText(Main.LocalPlayer.getRect(), Color.Crimson, $"已清除 {w}×{h}", true, false);
                    selecting = false;
                    return;
                }

                // 4. 结构复制 / 剪切模式完成划选
                if (gameMain.Wand_StructureMode == gameMain.StructureMode.Copy || gameMain.Wand_StructureMode == gameMain.StructureMode.Cut)
                {
                    bool isCut = gameMain.Wand_StructureMode == gameMain.StructureMode.Cut;
                    gameMain.LastActiveStructureMode = gameMain.Wand_StructureMode;
                    Point p1 = position1.ToTileCoordinates();
                    Point p2 = position2.ToTileCoordinates();
                    int minX = Math.Min(p1.X, p2.X);
                    int minY = Math.Min(p1.Y, p2.Y);
                    int w = Math.Abs(p1.X - p2.X) + 1;
                    int h = Math.Abs(p1.Y - p2.Y) + 1;
                    Rectangle rect = new Rectangle(minX, minY, w, h);

                    Structure.StructureData data = Structure.StructureCapture.Capture(rect, $"{(isCut ? "剪切结构" : "建筑结构")}_{w}x{h}");
                    if (data != null)
                    {
                        Structure.StructureStorage.Clipboard = data;

                        if (isCut)
                        {
                            // 延迟原子性剪切：先不清除原建筑，记录原区域！
                            gameMain.CutSourceRect = rect;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Main.LocalPlayer.position);
                            gameMain.Wand_StructureMode = gameMain.StructureMode.Paste; // 自动切入放置模式
                            Main.NewText($"[魔杖] 已剪切 {w}×{h} 结构", 255, 200, 100);
                            CombatText.NewText(Main.LocalPlayer.getRect(), Color.Gold, "剪切就绪", true, false);
                        }
                        else
                        {
                            gameMain.CutSourceRect = null;
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item4, Main.LocalPlayer.position);
                            gameMain.Wand_StructureMode = gameMain.StructureMode.Paste; // 自动切入放置模式
                            CombatText.NewText(Main.LocalPlayer.getRect(), Color.LimeGreen, $"已复制 {w}×{h}", true, false);
                        }
                    }
                    selecting = false;
                    return;
                }

                Update_Shapes();
                selecting = false;

                if (shapes != null && shapes.Count > 0)
                {
                    // 1. 液体魔杖操作优先
                    if (gameMain.Wand_LiquidMode != gameMain.LiquidMode.None)
                    {
                        WandHistory.BeginRecord(Main.LocalPlayer, shapes);
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
                            WandHistory.BeginRecord(Main.LocalPlayer, shapes);
                            if (gameMain.Wand_isPlace)
                            {
                                WandAction.AddTile(shapes, gameMain.Wand_Tile, gameMain.Wand_Wall, gameMain.Wand_BlockType, gameMain.Wand_BlockReplace);
                            }
                            else
                            {
                                WandAction.DelTile(shapes, gameMain.Wand_Tile, gameMain.Wand_Wall, gameMain.Wand_CollectDrops);
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Main.LocalPlayer.position);
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
                case Shapes.filledCircular: shapes = WandUtils.GetShapes_FilledCircular(position1, position2); break;
                case Shapes.rectangle: shapes = WandUtils.GetShapes_Rectangle(position1, position2); break;
                case Shapes.hollowRectangle: shapes = WandUtils.GetShapes_HollowRectangle(position1, position2); break;
                default: break;
            }
        }

        public static void Draw()
        {
            // 蓝图放置分帧作业进度提示（含完成后驻留反馈）
            Structure.StructurePlacement.DrawProgress();

            if (gameMain.Wand_StructureMode == gameMain.StructureMode.Paste && !Structure.StructurePlacement.IsPlacing)
            {
                // 放置作业进行中不再绘制跟随鼠标的虚影（实际放置已在别处落地，虚影会误导落点）
                Structure.StructurePreview.Draw(Main.spriteBatch);
            }

            if (selecting == false) return;

            if (Main.GameUpdateCount % 5 == 0 || shapes == null) Update_Shapes();

            if (shapes?.Count > 0 == false) return;

            Color borderColor;
            string modeName;

            if (gameMain.Wand_StructureMode == gameMain.StructureMode.Copy)
            {
                borderColor = new Color(255, 215, 0);
                modeName = "结构复制";
            }
            else if (gameMain.Wand_StructureMode == gameMain.StructureMode.Cut)
            {
                borderColor = new Color(255, 120, 50);
                modeName = "结构剪切";
            }
            else if (gameMain.Wand_StructureMode == gameMain.StructureMode.Delete)
            {
                borderColor = new Color(255, 50, 60);
                modeName = "结构删除";
            }
            else if (gameMain.Wand_LiquidMode != gameMain.LiquidMode.None)
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
                modeName = "区域破坏";
            }
            Color backgroundColor = borderColor * 0.35f;

            int w = (int)Math.Abs(Math.Floor(position1.X / 16) - Math.Floor(position2.X / 16)) + 1;
            int h = (int)Math.Abs(Math.Floor(position1.Y / 16) - Math.Floor(position2.Y / 16)) + 1;
            int count = shapes?.Count ?? 0;
            Terraria.Utils.DrawBorderString(Main.spriteBatch, $"[{modeName}] {w} x {h} ({count}格)", new Vector2(Main.mouseX, Main.mouseY + 50), borderColor, anchorx: 0.5f, anchory: 0.5f);

            // 拖拽框选阶段渲染半透明材质施工虚影（放置/破坏/液体），置于边框线下方
            WandPreview.Draw(Main.spriteBatch, shapes);

            switch (shapes_s)
            {
                case Shapes.line: WandUtils.Draw_line(shapes, position1, position2, borderColor, backgroundColor); break;
                case Shapes.circular: WandUtils.Draw_circular(shapes, position1, position2, borderColor, backgroundColor); break;
                case Shapes.filledCircular: WandUtils.Draw_filledCircular(shapes, position1, position2, borderColor, backgroundColor); break;
                case Shapes.rectangle: WandUtils.Draw_rectangle(shapes, position1, position2, borderColor, backgroundColor); break;
                case Shapes.hollowRectangle: WandUtils.Draw_hollowRectangle(shapes, position1, position2, borderColor, backgroundColor); break;
                default: break;
            }
        }

        /// <summary>
        /// 在鼠标光标右下方绘制当前魔杖工作模式与物品图标（100% 对齐原版 ItemSlot.DrawItemIcon 渲染规范）
        /// </summary>
        public static void DrawCursorModeTooltip(Microsoft.Xna.Framework.Graphics.SpriteBatch sb)
        {
            if (!gameMain.Wand_isEnable || Main.gameMenu || Main.mapFullscreen) return;

            Player player = Main.LocalPlayer;
            if (player == null) return;

            Item heldItem = player.HeldItem;
            int iconItemId = (heldItem != null && !heldItem.IsAir) ? heldItem.type : 0;

            string text;
            Color textColor;

            // 1. 蓝图模式
            if (gameMain.Wand_StructureMode != gameMain.StructureMode.None)
            {
                iconItemId = 3611; // 蓝图/建筑设计图标

                switch (gameMain.Wand_StructureMode)
                {
                    case gameMain.StructureMode.Copy:
                        text = "蓝图复制";
                        textColor = Color.Gold;
                        break;
                    case gameMain.StructureMode.Cut:
                        text = "蓝图剪切";
                        textColor = new Color(255, 140, 50);
                        break;
                    case gameMain.StructureMode.Delete:
                        text = "结构删除";
                        textColor = new Color(255, 60, 70);
                        break;
                    case gameMain.StructureMode.Paste:
                        string structName = Structure.StructureStorage.Clipboard?.Name;
                        if (!string.IsNullOrEmpty(structName) && structName.Length > 10) structName = structName.Substring(0, 10) + "...";
                        text = string.IsNullOrEmpty(structName) ? "蓝图放置" : $"蓝图: {structName}";
                        textColor = Color.Cyan;
                        break;
                    default:
                        text = "蓝图模式";
                        textColor = Color.Cyan;
                        break;
                }
            }
            // 2. 液体魔杖模式
            else if (gameMain.Wand_LiquidMode != gameMain.LiquidMode.None)
            {
                switch (gameMain.Wand_LiquidMode)
                {
                    case gameMain.LiquidMode.Water:
                        if (iconItemId <= 0) iconItemId = gameMain.Wand_InfiniteLiquid ? ItemID.BottomlessBucket : ItemID.WaterBucket;
                        text = gameMain.Wand_InfiniteLiquid ? "无限水" : "铺设水";
                        textColor = new Color(50, 180, 255);
                        break;
                    case gameMain.LiquidMode.Lava:
                        if (iconItemId <= 0) iconItemId = gameMain.Wand_InfiniteLiquid ? ItemID.BottomlessLavaBucket : ItemID.LavaBucket;
                        text = gameMain.Wand_InfiniteLiquid ? "无限岩浆" : "铺设岩浆";
                        textColor = new Color(255, 100, 30);
                        break;
                    case gameMain.LiquidMode.Honey:
                        if (iconItemId <= 0) iconItemId = gameMain.Wand_InfiniteLiquid ? ItemID.BottomlessHoneyBucket : ItemID.HoneyBucket;
                        text = gameMain.Wand_InfiniteLiquid ? "无限蜂蜜" : "铺设蜂蜜";
                        textColor = new Color(255, 195, 0);
                        break;
                    case gameMain.LiquidMode.Shimmer:
                        if (iconItemId <= 0) iconItemId = ItemID.BottomlessShimmerBucket;
                        text = gameMain.Wand_InfiniteLiquid ? "无限微光" : "铺设微光";
                        textColor = new Color(230, 130, 255);
                        break;
                    case gameMain.LiquidMode.Absorb:
                        if (iconItemId <= 0) iconItemId = ItemID.SuperAbsorbantSponge;
                        text = "吸收液体";
                        textColor = new Color(0, 235, 205);
                        break;
                    case gameMain.LiquidMode.Clear:
                        if (iconItemId <= 0) iconItemId = ItemID.EmptyBucket;
                        text = "清空液体";
                        textColor = new Color(220, 220, 220);
                        break;
                    default:
                        text = "液体模式";
                        textColor = Color.White;
                        break;
                }
            }
            // 3. 电线与制动器
            else if (gameMain.Wand_ToolMode != 0)
            {
                bool isCut = !gameMain.Wand_isPlace;
                string toolName = "";
                if (gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red)) toolName += "红";
                if (gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green)) toolName += "绿";
                if (gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue)) toolName += "蓝";
                if (gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow)) toolName += "黄";
                if (gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator)) toolName += "促动器";

                text = isCut ? $"拆除[{toolName}]" : $"铺设[{toolName}]";
                textColor = isCut ? new Color(255, 100, 100) : new Color(100, 220, 255);
            }
            // 4. 常规方块与墙壁魔杖
            else if (gameMain.Wand_isPlace)
            {
                Item placeItem = null;
                if (gameMain.Wand_Tile) placeItem = WandAction.FirstItem_Tile(player);
                if (placeItem == null && gameMain.Wand_Wall) placeItem = WandAction.FirstItem_Wall(player);

                if (placeItem != null)
                {
                    iconItemId = placeItem.type;
                }

                string target = (gameMain.Wand_Tile && gameMain.Wand_Wall) ? "物块+墙" :
                                gameMain.Wand_Wall ? "背景墙" : "物块";

                if (gameMain.Wand_BlockReplace)
                {
                    text = (placeItem != null) ? $"替换 ({target})" : "替换 [缺材料]";
                    textColor = (placeItem != null) ? new Color(255, 175, 20) : new Color(255, 100, 100);
                }
                else
                {
                    text = (placeItem != null) ? $"放置 ({target})" : "放置 [缺材料]";
                    textColor = (placeItem != null) ? new Color(60, 255, 100) : new Color(255, 100, 100);
                }
            }
            else
            {
                string target = (gameMain.Wand_Tile && gameMain.Wand_Wall) ? "物块+墙" :
                                gameMain.Wand_Wall ? "背景墙" : "物块";

                text = (target == "物块") ? "破坏" : $"破坏 ({target})";
                textColor = new Color(255, 70, 90);
            }

            // 附加非常规几何形状标注（矩形为默认不标注，其余显示形状提示）
            if (gameMain.Wand_StructureMode == gameMain.StructureMode.None)
            {
                switch (gameMain.Wand_Shapes)
                {
                    case Shapes.line: text += " [线]"; break;
                    case Shapes.circular: text += " [空心圆]"; break;
                    case Shapes.filledCircular: text += " [实心圆]"; break;
                    case Shapes.rectangle: text += " [实心矩形]"; break;
                    case Shapes.hollowRectangle: text += " [空心框]"; break;
                    default: break;
                }
            }

            // 按照原版光标物品图标规范进行绘制
            float cursorScale = Main.cursorScale;
            float iconPush = 10f;
            Vector2 textPos;

            if (iconItemId > 0 && iconItemId < Terraria.GameContent.TextureAssets.Item.Length)
            {
                try
                {
                    Main.instance.LoadItem(iconItemId);
                    Item drawItem = (ContentSamples.ItemsByType != null && ContentSamples.ItemsByType.TryGetValue(iconItemId, out var sample)) ? sample : null;
                    if (drawItem == null)
                    {
                        drawItem = new Item();
                        drawItem.SetDefaults(iconItemId);
                    }

                    Color currentColor = (heldItem != null && heldItem.type == iconItemId) ? heldItem.GetAlpha(Color.White) : Color.White;
                    Terraria.UI.ItemSlot.GetItemLight(ref currentColor, iconItemId);

                    Vector2 itemSize = Item.GetDrawHitbox(iconItemId, null).Size();
                    Vector2 vector = itemSize * cursorScale * 0.5f;
                    Vector2 center = new Vector2(Main.mouseX + iconPush, Main.mouseY + iconPush) + vector;

                    Terraria.UI.ItemSlot.DrawItemIcon(drawItem, 21, sb, center, cursorScale, 32f, currentColor);

                    textPos = new Vector2(center.X + vector.X + 6f, center.Y);
                    Terraria.Utils.DrawBorderString(sb, text, textPos, textColor, 0.82f, anchorx: 0f, anchory: 0.5f);
                    return;
                }
                catch { }
            }

            textPos = new Vector2(Main.mouseX + 20, Main.mouseY + 20);
            Terraria.Utils.DrawBorderString(sb, text, textPos, textColor, 0.82f);
        }
    }
}
