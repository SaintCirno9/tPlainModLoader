using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace WandsTool.Content
{
    public class wandsPanel_btn1 : UIState
    {
        private UIImage back = null;
        private Asset<Texture2D> back_img1 = null;
        private Asset<Texture2D> back_img2 = null;
        private UIImage ico = null;
        private string mouseText = null;
        public bool isBack = false;

        public wandsPanel_btn1(Texture2D img1, string mouseText)
        {
            back_img1 = Main.Assets.Request<Texture2D>("Images/UI/Wires_0", AssetRequestMode.ImmediateLoad);
            back_img2 = Main.Assets.Request<Texture2D>("Images/UI/Wires_1", AssetRequestMode.ImmediateLoad);
            this.mouseText = mouseText;

            back = new UIImage(back_img1);
            ico = new UIImage(img1);

            Width.Set(40, 0);
            Height.Set(Width.Pixels, 0);

            back.Width.Set(Width.Pixels, 0);
            back.Height.Set(Height.Pixels, 0);

            ico.Width.Set(16, 0);
            ico.Height.Set(16, 0);
            ico.HAlign = 0.5f;
            ico.VAlign = 0.5f;
            ico.ScaleToFit = true;

            OnLeftClick += (e, s) => SoundEngine.PlaySound(SoundID.MenuTick);

            Append(back);
            Append(ico);
        }

        public wandsPanel_btn1(string img1, string mouseText) :
            this(Main.Assets.Request<Texture2D>(img1, AssetRequestMode.ImmediateLoad)?.Value, mouseText)
        {
        }

        public override void Update(GameTime gameTime)
        {
            if (IsMouseHovering)
            {
                Terraria.Player player = Main.LocalPlayer;
                if (player != null) player.mouseInterface = true;

                if (mouseText != null) Main.instance.MouseText(mouseText);

                back.SetImage(isBack ? back_img2 : back_img1);
            }
            else
            {
                back.SetImage(isBack ? back_img2 : back_img1);
            }

            base.Update(gameTime);
        }

        public void SetIco(Texture2D img1)
        {
            ico.SetImage(img1);
        }

        public void SetIco(string img1)
        {
            SetIco(Main.Assets.Request<Texture2D>(img1, AssetRequestMode.ImmediateLoad)?.Value);
        }

        public void SetTooltip(string text)
        {
            mouseText = text;
        }
    }

    public class wandsPanel : UserInterface
    {
        protected UIState container = null;
        protected UIState btns = null;
        protected UIState btns_2 = null;
        protected UIState btns_3 = null;
        protected UIState btns_4 = null;
        protected UIState btns_5 = null;
        protected UIState btns_6 = null;

        // 主环 6 个按钮
        protected wandsPanel_btn1 btn1 = null; // 破坏/放置
        protected wandsPanel_btn1 btn2 = null; // 操作目标分类
        protected wandsPanel_btn1 btn3 = null; // 几何形状
        protected wandsPanel_btn1 btn4 = null; // 方块方向/坡度
        protected wandsPanel_btn1 btn5 = null; // 液体魔杖
        protected wandsPanel_btn1 btn6 = null; // 蓝图与结构魔杖

        // 子菜单 2：操作目标（方块、墙壁、替换、收集、电线）
        protected wandsPanel_btn1 btn2_tile = null;
        protected wandsPanel_btn1 btn2_wall = null;
        protected wandsPanel_btn1 btn2_replace = null;
        protected wandsPanel_btn1 btn2_collect = null;
        protected wandsPanel_btn1 btn2_wire_red = null;
        protected wandsPanel_btn1 btn2_wire_green = null;
        protected wandsPanel_btn1 btn2_wire_blue = null;
        protected wandsPanel_btn1 btn2_wire_yellow = null;
        protected wandsPanel_btn1 btn2_wire_actuator = null;

        // 子菜单 3：几何形状（直线、空心圆、矩形框选）
        protected wandsPanel_btn1 btn3_line = null;
        protected wandsPanel_btn1 btn3_circular = null;
        protected wandsPanel_btn1 btn3_rectangle = null;
        // 子菜单 4：方向/坡度
        protected wandsPanel_btn1 btn4_Solid = null;
        protected wandsPanel_btn1 btn4_HalfBlock = null;
        protected wandsPanel_btn1 btn4_SlopeUpLeft = null;
        protected wandsPanel_btn1 btn4_SlopeUpRight = null;
        protected wandsPanel_btn1 btn4_SlopeDownLeft = null;
        protected wandsPanel_btn1 btn4_SlopeDownRight = null;

        // 子菜单 5：液体魔杖模式
        protected wandsPanel_btn1 btn5_off = null;
        protected wandsPanel_btn1 btn5_absorb = null;
        protected wandsPanel_btn1 btn5_clear = null;
        protected wandsPanel_btn1 btn5_water = null;
        protected wandsPanel_btn1 btn5_lava = null;
        protected wandsPanel_btn1 btn5_honey = null;
        protected wandsPanel_btn1 btn5_shimmer = null;
        protected wandsPanel_btn1 btn5_infinite = null;

        // 子菜单 6：蓝图与结构系统
        protected wandsPanel_btn1 btn6_off = null;
        protected wandsPanel_btn1 btn6_copy = null;
        protected wandsPanel_btn1 btn6_cut = null;
        protected wandsPanel_btn1 btn6_delete = null;
        protected wandsPanel_btn1 btn6_paste = null;
        protected wandsPanel_btn1 btn6_flip_h = null;
        protected wandsPanel_btn1 btn6_flip_v = null;
        protected wandsPanel_btn1 btn6_overwrite = null;
        protected wandsPanel_btn1 btn6_consume = null;
        protected wandsPanel_btn1 btn6_manager = null;

        public static Structure.UI.UIBlueprintManager BlueprintManager = new Structure.UI.UIBlueprintManager();

        public bool isReset = true;

        public wandsPanel()
        {
            container = new UIState();
            btns = new UIState();
            btns_2 = new UIState();
            btns_3 = new UIState();
            btns_4 = new UIState();
            btns_5 = new UIState();
            btns_6 = new UIState();

            // 主按钮初始化
            btn1 = new wandsPanel_btn1("Images/Item_1", "放置 / 破坏切换");
            btn2 = new wandsPanel_btn1("Images/Item_2", "操作目标: 方块/墙壁/替换/收集/电线");
            btn3 = new wandsPanel_btn1(Resources.Images_ShapesRectangle, "几何形状: 直线 / 空心圆 / 矩形框选");
            btn4 = new wandsPanel_btn1(Resources.Images_SlopeSolid, "方块坡度与朝向");
            btn5 = new wandsPanel_btn1("Images/Item_3031", "液体魔杖: 吸收/清空/铺设液体");
            btn6 = new wandsPanel_btn1("Images/Item_3611", "建筑蓝图与结构系统: 复制/剪切/删除/粘贴/保存");

            // 子按钮初始化
            btn2_tile = new wandsPanel_btn1("Images/Item_2", "方块操作开关");
            btn2_wall = new wandsPanel_btn1("Images/Item_30", "背景墙操作开关");
            btn2_replace = new wandsPanel_btn1("Images/Item_4082", "方块/墙壁替换模式开关");
            btn2_collect = new wandsPanel_btn1("Images/Item_5010", "破坏产生掉落物开关 (开: 产生并吸入背包 / 关: 彻底销毁无掉落)");
            btn2_wire_red = new wandsPanel_btn1("Images/UI/Wires_2", "红线");
            btn2_wire_green = new wandsPanel_btn1("Images/UI/Wires_3", "绿线");
            btn2_wire_blue = new wandsPanel_btn1("Images/UI/Wires_4", "蓝线");
            btn2_wire_yellow = new wandsPanel_btn1("Images/UI/Wires_5", "黄线");
            btn2_wire_actuator = new wandsPanel_btn1("Images/UI/Wires_10", "制动器");

            btn3_line = new wandsPanel_btn1(Resources.Images_ShapesLine, "直线");
            btn3_circular = new wandsPanel_btn1(Resources.Images_ShapesCircular, "空心圆/圆周");
            btn3_rectangle = new wandsPanel_btn1(Resources.Images_ShapesRectangle, "矩形大范围框选");
            btn4_Solid = new wandsPanel_btn1(Resources.Images_SlopeSolid, "实体方块");
            btn4_HalfBlock = new wandsPanel_btn1(Resources.Images_SlopeHalfBlock, "半砖");
            btn4_SlopeUpLeft = new wandsPanel_btn1(Resources.Images_SlopeUpLeft, "左上斜坡");
            btn4_SlopeUpRight = new wandsPanel_btn1(Resources.Images_SlopeUpRight, "右上斜坡");
            btn4_SlopeDownLeft = new wandsPanel_btn1(Resources.Images_SlopeDownLeft, "左下斜坡");
            btn4_SlopeDownRight = new wandsPanel_btn1(Resources.Images_SlopeDownRight, "右下斜坡");

            btn5_off = new wandsPanel_btn1("Images/Item_1", "关闭液体模式 (恢复方块操作)");
            btn5_absorb = new wandsPanel_btn1("Images/Item_4820", "吸收液体 (抽取并装入空桶)");
            btn5_clear = new wandsPanel_btn1("Images/Item_5304", "一键清空/蒸发液体");
            btn5_water = new wandsPanel_btn1("Images/Item_206", "铺设水");
            btn5_lava = new wandsPanel_btn1("Images/Item_207", "铺设岩浆");
            btn5_honey = new wandsPanel_btn1("Images/Item_1128", "铺设蜂蜜");
            btn5_shimmer = new wandsPanel_btn1("Images/Item_5303", "铺设微光");
            btn5_infinite = new wandsPanel_btn1("Images/Item_3031", "无限液体放置模式开关 (免消耗背包桶)");

            // 蓝图子按钮
            btn6_off = new wandsPanel_btn1("Images/Item_1", "退出蓝图模式 (恢复常规魔杖)");
            btn6_copy = new wandsPanel_btn1("Images/Item_5098", "结构框选复制 (划选区域自动存入剪贴板)");
            btn6_cut = new wandsPanel_btn1("Images/UI/Wires_1", "结构框选剪切 (划选区域抓取至剪贴板，一键搬家)");
            btn6_delete = new wandsPanel_btn1("Images/Item_166", "结构区域删除 (划选同时破坏物块与背景墙)");
            btn6_paste = new wandsPanel_btn1("Images/Item_3611", "蓝图放置模式 (投射虚影与一键摆放)");
            btn6_flip_h = new wandsPanel_btn1("Images/Item_3612", "水平镜像翻转 [快捷键: H]");
            btn6_flip_v = new wandsPanel_btn1("Images/Item_3625", "垂直翻转 [快捷键: V]");
            btn6_overwrite = new wandsPanel_btn1("Images/Item_4082", "覆盖已有物块开关 (开: 覆盖原有物块 / 关: 仅在空白处放置)");
            btn6_consume = new wandsPanel_btn1("Images/Item_5010", "材料消耗模式开关 (开: 放置需消耗对应材料 / 关: 免消耗自由摆放)");
            btn6_manager = new wandsPanel_btn1("Images/Item_3611", "📖 蓝图管理器 (在游戏内浏览、载入、保存与管理本地蓝图)");

            // 主按钮点击事件
            btn1.OnLeftClick += (e, s) => onClick(0);
            btn2.OnLeftClick += (e, s) => onClick(1);
            btn3.OnLeftClick += (e, s) => onClick(2);
            btn4.OnLeftClick += (e, s) => onClick(3);
            btn5.OnLeftClick += (e, s) => onClick(4);
            btn6.OnLeftClick += (e, s) => onClick(5);

            // 子按钮 2 事件
            Action<Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode> btn2_wire_action = (v) =>
            {
                if (gameMain.Wand_ToolMode.HasFlag(v)) gameMain.Wand_ToolMode &= ~v;
                else gameMain.Wand_ToolMode |= v;
            };
            btn2_tile.OnLeftClick += (e, s) => gameMain.Wand_Tile = !gameMain.Wand_Tile;
            btn2_wall.OnLeftClick += (e, s) => gameMain.Wand_Wall = !gameMain.Wand_Wall;
            btn2_replace.OnLeftClick += (e, s) => gameMain.Wand_BlockReplace = !gameMain.Wand_BlockReplace;
            btn2_collect.OnLeftClick += (e, s) =>
            {
                gameMain.Wand_CollectDrops = !gameMain.Wand_CollectDrops;
                Main.NewText($"[魔杖] 产生掉落物: {(gameMain.Wand_CollectDrops ? "开" : "关")}", 255, 255, 150);
            };
            btn2_wire_red.OnLeftClick += (e, s) => btn2_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red);
            btn2_wire_green.OnLeftClick += (e, s) => btn2_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green);
            btn2_wire_blue.OnLeftClick += (e, s) => btn2_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue);
            btn2_wire_yellow.OnLeftClick += (e, s) => btn2_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow);
            btn2_wire_actuator.OnLeftClick += (e, s) => btn2_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator);

            // 子按钮 3 事件
            btn3_line.OnLeftClick += (e, s) => gameMain.Wand_Shapes = Wands.Shapes.line;
            btn3_circular.OnLeftClick += (e, s) => gameMain.Wand_Shapes = Wands.Shapes.circular;
            btn3_rectangle.OnLeftClick += (e, s) => gameMain.Wand_Shapes = Wands.Shapes.rectangle;
            // 子按钮 4 事件
            btn4_Solid.OnLeftClick += (e, s) => gameMain.Wand_BlockType = WandAction.BlockType.Solid;
            btn4_HalfBlock.OnLeftClick += (e, s) => gameMain.Wand_BlockType = WandAction.BlockType.HalfBlock;
            btn4_SlopeUpLeft.OnLeftClick += (e, s) => gameMain.Wand_BlockType = WandAction.BlockType.SlopeUpLeft;
            btn4_SlopeUpRight.OnLeftClick += (e, s) => gameMain.Wand_BlockType = WandAction.BlockType.SlopeUpRight;
            btn4_SlopeDownLeft.OnLeftClick += (e, s) => gameMain.Wand_BlockType = WandAction.BlockType.SlopeDownLeft;
            btn4_SlopeDownRight.OnLeftClick += (e, s) => gameMain.Wand_BlockType = WandAction.BlockType.SlopeDownRight;

            // 子按钮 5 事件
            btn5_off.OnLeftClick += (e, s) => gameMain.Wand_LiquidMode = gameMain.LiquidMode.None;
            btn5_absorb.OnLeftClick += (e, s) => gameMain.Wand_LiquidMode = gameMain.LiquidMode.Absorb;
            btn5_clear.OnLeftClick += (e, s) => gameMain.Wand_LiquidMode = gameMain.LiquidMode.Clear;
            btn5_water.OnLeftClick += (e, s) => gameMain.Wand_LiquidMode = gameMain.LiquidMode.Water;
            btn5_lava.OnLeftClick += (e, s) => gameMain.Wand_LiquidMode = gameMain.LiquidMode.Lava;
            btn5_honey.OnLeftClick += (e, s) => gameMain.Wand_LiquidMode = gameMain.LiquidMode.Honey;
            btn5_shimmer.OnLeftClick += (e, s) => gameMain.Wand_LiquidMode = gameMain.LiquidMode.Shimmer;
            btn5_infinite.OnLeftClick += (e, s) => gameMain.Wand_InfiniteLiquid = !gameMain.Wand_InfiniteLiquid;

            // 子按钮 6 事件
            btn6_off.OnLeftClick += (e, s) =>
            {
                gameMain.Wand_StructureMode = gameMain.StructureMode.None;
                Main.NewText("[魔杖] 已退出蓝图模式 (恢复常规魔杖)", 100, 220, 255);
            };
            btn6_copy.OnLeftClick += (e, s) =>
            {
                gameMain.Wand_StructureMode = gameMain.StructureMode.Copy;
                gameMain.LastActiveStructureMode = gameMain.StructureMode.Copy;
                Main.NewText("[魔杖] 进入结构复制模式", 255, 240, 100);
            };
            btn6_cut.OnLeftClick += (e, s) =>
            {
                gameMain.Wand_StructureMode = gameMain.StructureMode.Cut;
                gameMain.LastActiveStructureMode = gameMain.StructureMode.Cut;
                Main.NewText("[魔杖] 进入结构剪切模式", 255, 180, 80);
            };
            btn6_delete.OnLeftClick += (e, s) =>
            {
                gameMain.Wand_StructureMode = gameMain.StructureMode.Delete;
                gameMain.LastActiveStructureMode = gameMain.StructureMode.Delete;
                Main.NewText("[魔杖] 进入结构删除模式", 255, 100, 100);
            };
            btn6_paste.OnLeftClick += (e, s) =>
            {
                if (Structure.StructureStorage.Clipboard == null)
                {
                    Main.NewText("[魔杖] 剪贴板为空，请先复制或载入蓝图", 255, 100, 100);
                    return;
                }
                gameMain.Wand_StructureMode = gameMain.StructureMode.Paste;
                Main.NewText($"[魔杖] 进入放置模式: {Structure.StructureStorage.Clipboard.Name}", 100, 255, 150);
            };
            btn6_flip_h.OnLeftClick += (e, s) =>
            {
                if (Structure.StructureStorage.Clipboard != null)
                {
                    Structure.StructureStorage.Clipboard = Structure.StructureStorage.Clipboard.FlipHorizontal();
                    Main.NewText("[魔杖] 蓝图已水平镜像翻转", 100, 255, 255);
                }
            };
            btn6_flip_v.OnLeftClick += (e, s) =>
            {
                if (Structure.StructureStorage.Clipboard != null)
                {
                    Structure.StructureStorage.Clipboard = Structure.StructureStorage.Clipboard.FlipVertical();
                    Main.NewText("[魔杖] 蓝图已垂直翻转", 100, 255, 255);
                }
            };
            btn6_overwrite.OnLeftClick += (e, s) =>
            {
                gameMain.Wand_StructureOverwrite = !gameMain.Wand_StructureOverwrite;
                Main.NewText($"[魔杖] 蓝图覆盖: {(gameMain.Wand_StructureOverwrite ? "开" : "关")}", 255, 255, 150);
            };
            btn6_consume.OnLeftClick += (e, s) =>
            {
                gameMain.Wand_StructureConsumeMaterials = !gameMain.Wand_StructureConsumeMaterials;
                Main.NewText($"[魔杖] 材料消耗: {(gameMain.Wand_StructureConsumeMaterials ? "开" : "关")}", 255, 255, 150);
            };
            btn6_manager.OnLeftClick += (e, s) =>
            {
                if (BlueprintManager.IsOpen)
                {
                    BlueprintManager.Close();
                }
                else
                {
                    BlueprintManager.Open(container);
                }
            };

            // 装配 UI
            SetState(container);
            container.Append(btns);
            container.Append(btn1);
            container.Append(btn2);
            container.Append(btn3);
            container.Append(btn4);
            container.Append(btn5);
            container.Append(btn6);

            btns_2.Append(btn2_tile);
            btns_2.Append(btn2_wall);
            btns_2.Append(btn2_replace);
            btns_2.Append(btn2_collect);
            btns_2.Append(btn2_wire_red);
            btns_2.Append(btn2_wire_green);
            btns_2.Append(btn2_wire_blue);
            btns_2.Append(btn2_wire_yellow);
            btns_2.Append(btn2_wire_actuator);

            btns_3.Append(btn3_line);
            btns_3.Append(btn3_circular);
            btns_3.Append(btn3_rectangle);
            btns_4.Append(btn4_Solid);
            btns_4.Append(btn4_HalfBlock);
            btns_4.Append(btn4_SlopeUpLeft);
            btns_4.Append(btn4_SlopeUpRight);
            btns_4.Append(btn4_SlopeDownLeft);
            btns_4.Append(btn4_SlopeDownRight);

            btns_5.Append(btn5_off);
            btns_5.Append(btn5_absorb);
            btns_5.Append(btn5_clear);
            btns_5.Append(btn5_water);
            btns_5.Append(btn5_lava);
            btns_5.Append(btn5_honey);
            btns_5.Append(btn5_shimmer);
            btns_5.Append(btn5_infinite);

            btns_6.Append(btn6_off);
            btns_6.Append(btn6_copy);
            btns_6.Append(btn6_cut);
            btns_6.Append(btn6_delete);
            btns_6.Append(btn6_paste);
            btns_6.Append(btn6_flip_h);
            btns_6.Append(btn6_flip_v);
            btns_6.Append(btn6_overwrite);
            btns_6.Append(btn6_consume);
            btns_6.Append(btn6_manager);
        }

        public void Open()
        {
            gameMain.UI_WandsPanel1_isOpen = true;
            isReset = false;
            btns.RemoveAllChildren();
            Reset();
            update(null);
            Recalculate();
        }

        public void Close()
        {
            gameMain.UI_WandsPanel1_isOpen = false;
            btns.RemoveAllChildren();
            BlueprintManager?.Close();
        }

        public void Toggle()
        {
            if (gameMain.UI_WandsPanel1_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void update(GameTime time)
        {
            if (isReset)
            {
                isReset = false;
                Reset();
                Recalculate();
            }

            // 主按钮图标与状态更新
            btn1.SetIco(gameMain.Wand_isPlace ? "Images/Item_129" : "Images/Item_1");
            btn1.SetTooltip($"当前模式: {(gameMain.Wand_isPlace ? "放置" : "破坏")} (点击切换)");

            switch (gameMain.Wand_Shapes)
            {
                case Wands.Shapes.line: btn3.SetIco(Resources.Images_ShapesLine); break;
                case Wands.Shapes.circular: btn3.SetIco(Resources.Images_ShapesCircular); break;
                case Wands.Shapes.rectangle: btn3.SetIco(Resources.Images_ShapesRectangle); break;
                default: break;
            }

            switch (gameMain.Wand_BlockType)
            {
                case WandAction.BlockType.Solid: btn4.SetIco(Resources.Images_SlopeSolid); break;
                case WandAction.BlockType.HalfBlock: btn4.SetIco(Resources.Images_SlopeHalfBlock); break;
                case WandAction.BlockType.SlopeUpLeft: btn4.SetIco(Resources.Images_SlopeUpLeft); break;
                case WandAction.BlockType.SlopeUpRight: btn4.SetIco(Resources.Images_SlopeUpRight); break;
                case WandAction.BlockType.SlopeDownLeft: btn4.SetIco(Resources.Images_SlopeDownLeft); break;
                case WandAction.BlockType.SlopeDownRight: btn4.SetIco(Resources.Images_SlopeDownRight); break;
                default: break;
            }

            switch (gameMain.Wand_LiquidMode)
            {
                case gameMain.LiquidMode.None: btn5.SetIco("Images/Item_3031"); break;
                case gameMain.LiquidMode.Absorb: btn5.SetIco("Images/Item_4820"); break;
                case gameMain.LiquidMode.Clear: btn5.SetIco("Images/Item_5304"); break;
                case gameMain.LiquidMode.Water: btn5.SetIco("Images/Item_206"); break;
                case gameMain.LiquidMode.Lava: btn5.SetIco("Images/Item_207"); break;
                case gameMain.LiquidMode.Honey: btn5.SetIco("Images/Item_1128"); break;
                case gameMain.LiquidMode.Shimmer: btn5.SetIco("Images/Item_5303"); break;
                default: break;
            }
            btn5.SetTooltip($"液体魔杖: {gameMain.Wand_LiquidMode} [无限:{(gameMain.Wand_InfiniteLiquid ? "开" : "关")}]");

            // 子按钮激活高亮背景
            btn2_tile.isBack = gameMain.Wand_Tile;
            btn2_wall.isBack = gameMain.Wand_Wall;
            btn2_replace.isBack = gameMain.Wand_BlockReplace;
            btn2_collect.isBack = gameMain.Wand_CollectDrops;
            btn2_wire_red.isBack = gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red);
            btn2_wire_green.isBack = gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green);
            btn2_wire_blue.isBack = gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue);
            btn2_wire_yellow.isBack = gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow);
            btn2_wire_actuator.isBack = gameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator);

            btn3_line.isBack = gameMain.Wand_Shapes == Wands.Shapes.line;
            btn3_circular.isBack = gameMain.Wand_Shapes == Wands.Shapes.circular;
            btn3_rectangle.isBack = gameMain.Wand_Shapes == Wands.Shapes.rectangle;
            btn4_Solid.isBack = gameMain.Wand_BlockType == WandAction.BlockType.Solid;
            btn4_HalfBlock.isBack = gameMain.Wand_BlockType == WandAction.BlockType.HalfBlock;
            btn4_SlopeUpLeft.isBack = gameMain.Wand_BlockType == WandAction.BlockType.SlopeUpLeft;
            btn4_SlopeUpRight.isBack = gameMain.Wand_BlockType == WandAction.BlockType.SlopeUpRight;
            btn4_SlopeDownLeft.isBack = gameMain.Wand_BlockType == WandAction.BlockType.SlopeDownLeft;
            btn4_SlopeDownRight.isBack = gameMain.Wand_BlockType == WandAction.BlockType.SlopeDownRight;

            btn5_off.isBack = gameMain.Wand_LiquidMode == gameMain.LiquidMode.None;
            btn5_absorb.isBack = gameMain.Wand_LiquidMode == gameMain.LiquidMode.Absorb;
            btn5_clear.isBack = gameMain.Wand_LiquidMode == gameMain.LiquidMode.Clear;
            btn5_water.isBack = gameMain.Wand_LiquidMode == gameMain.LiquidMode.Water;
            btn5_lava.isBack = gameMain.Wand_LiquidMode == gameMain.LiquidMode.Lava;
            btn5_honey.isBack = gameMain.Wand_LiquidMode == gameMain.LiquidMode.Honey;
            btn5_shimmer.isBack = gameMain.Wand_LiquidMode == gameMain.LiquidMode.Shimmer;
            btn5_infinite.isBack = gameMain.Wand_InfiniteLiquid;

            btn6.SetTooltip($"建筑蓝图模式: {gameMain.Wand_StructureMode} [覆盖:{(gameMain.Wand_StructureOverwrite ? "开" : "关")}] [材料消耗:{(gameMain.Wand_StructureConsumeMaterials ? "开" : "关")}]");
            btn6_off.isBack = gameMain.Wand_StructureMode == gameMain.StructureMode.None;
            btn6_copy.isBack = gameMain.Wand_StructureMode == gameMain.StructureMode.Copy;
            btn6_cut.isBack = gameMain.Wand_StructureMode == gameMain.StructureMode.Cut;
            btn6_delete.isBack = gameMain.Wand_StructureMode == gameMain.StructureMode.Delete;
            btn6_paste.isBack = gameMain.Wand_StructureMode == gameMain.StructureMode.Paste;
            btn6_overwrite.isBack = gameMain.Wand_StructureOverwrite;
            btn6_consume.isBack = gameMain.Wand_StructureConsumeMaterials;
            btn6_manager.isBack = BlueprintManager.IsOpen;
        }

        public void Reset()
        {
            Action<int, int, Terraria.UI.UIElement, float> layoutCircle = (total, index, ui, radius) =>
            {
                float angle = -MathHelper.PiOver2 + (MathHelper.TwoPi / total) * index;
                Vector2 p = angle.ToRotationVector2() * radius + Main.MouseScreen;
                ui.Left.Set(p.X - ui.Width.Pixels / 2, 0);
                ui.Top.Set(p.Y - ui.Height.Pixels / 2, 0);
            };

            // 主轮盘 6 个按钮均匀环绕鼠标 (半径 48px)
            layoutCircle.Invoke(6, 0, btn1, 48f); // 正上方: 放置/破坏
            layoutCircle.Invoke(6, 1, btn2, 48f); // 右上方: 操作目标
            layoutCircle.Invoke(6, 2, btn3, 48f); // 右下方: 形状
            layoutCircle.Invoke(6, 3, btn4, 48f); // 正下方: 坡度
            layoutCircle.Invoke(6, 4, btn5, 48f); // 左下方: 液体
            layoutCircle.Invoke(6, 5, btn6, 48f); // 左上方: 蓝图与结构

            // 子菜单环绕 (半径 96px)
            layoutCircle.Invoke(9, 0, btn2_tile, 96f);
            layoutCircle.Invoke(9, 1, btn2_wall, 96f);
            layoutCircle.Invoke(9, 2, btn2_replace, 96f);
            layoutCircle.Invoke(9, 3, btn2_collect, 96f);
            layoutCircle.Invoke(9, 4, btn2_wire_red, 96f);
            layoutCircle.Invoke(9, 5, btn2_wire_green, 96f);
            layoutCircle.Invoke(9, 6, btn2_wire_blue, 96f);
            layoutCircle.Invoke(9, 7, btn2_wire_yellow, 96f);
            layoutCircle.Invoke(9, 8, btn2_wire_actuator, 96f);

            layoutCircle.Invoke(3, 0, btn3_line, 96f);
            layoutCircle.Invoke(3, 1, btn3_circular, 96f);
            layoutCircle.Invoke(3, 2, btn3_rectangle, 96f);
            layoutCircle.Invoke(6, 0, btn4_Solid, 96f);
            layoutCircle.Invoke(6, 1, btn4_HalfBlock, 96f);
            layoutCircle.Invoke(6, 2, btn4_SlopeUpLeft, 96f);
            layoutCircle.Invoke(6, 3, btn4_SlopeUpRight, 96f);
            layoutCircle.Invoke(6, 4, btn4_SlopeDownLeft, 96f);
            layoutCircle.Invoke(6, 5, btn4_SlopeDownRight, 96f);

            layoutCircle.Invoke(8, 0, btn5_off, 96f);
            layoutCircle.Invoke(8, 1, btn5_absorb, 96f);
            layoutCircle.Invoke(8, 2, btn5_clear, 96f);
            layoutCircle.Invoke(8, 3, btn5_water, 96f);
            layoutCircle.Invoke(8, 4, btn5_lava, 96f);
            layoutCircle.Invoke(8, 5, btn5_honey, 96f);
            layoutCircle.Invoke(8, 6, btn5_shimmer, 96f);
            layoutCircle.Invoke(8, 7, btn5_infinite, 96f);

            layoutCircle.Invoke(10, 0, btn6_off, 96f);
            layoutCircle.Invoke(10, 1, btn6_copy, 96f);
            layoutCircle.Invoke(10, 2, btn6_cut, 96f);
            layoutCircle.Invoke(10, 3, btn6_delete, 96f);
            layoutCircle.Invoke(10, 4, btn6_paste, 96f);
            layoutCircle.Invoke(10, 5, btn6_flip_h, 96f);
            layoutCircle.Invoke(10, 6, btn6_flip_v, 96f);
            layoutCircle.Invoke(10, 7, btn6_overwrite, 96f);
            layoutCircle.Invoke(10, 8, btn6_consume, 96f);
            layoutCircle.Invoke(10, 9, btn6_manager, 96f);
        }

        private void onClick(int index)
        {
            btns.RemoveAllChildren();

            switch (index)
            {
                case 0: gameMain.Wand_isPlace = !gameMain.Wand_isPlace; break;
                case 1: btns.Append(btns_2); break;
                case 2: btns.Append(btns_3); break;
                case 3: btns.Append(btns_4); break;
                case 4: btns.Append(btns_5); break;
                case 5:
                    btns.Append(btns_6);
                    if (gameMain.Wand_StructureMode == gameMain.StructureMode.None)
                    {
                        gameMain.Wand_StructureMode = gameMain.StructureMode.Copy;
                        gameMain.LastActiveStructureMode = gameMain.StructureMode.Copy;
                        Main.NewText("[魔杖] 蓝图模式已开启 (默认: 结构复制)", 255, 240, 100);
                    }
                    break;
                default: break;
            }
        }
    }
}
