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
    public class WandsPanelButton : UIState
    {
        private UIImage _backImage = null;
        private Asset<Texture2D> _backTextureInactive = null;
        private Asset<Texture2D> _backTextureActive = null;
        private UIImage _iconImage = null;
        private string _tooltipText = null;
        public bool IsActive { get; set; } = false;

        public WandsPanelButton(Texture2D img1, string mouseText)
        {
            _backTextureInactive = Main.Assets.Request<Texture2D>("Images/UI/Wires_0", AssetRequestMode.ImmediateLoad);
            _backTextureActive = Main.Assets.Request<Texture2D>("Images/UI/Wires_1", AssetRequestMode.ImmediateLoad);
            _tooltipText = mouseText;

            _backImage = new UIImage(_backTextureInactive);
            _iconImage = new UIImage(img1);

            Width.Set(40, 0);
            Height.Set(Width.Pixels, 0);

            _backImage.Width.Set(Width.Pixels, 0);
            _backImage.Height.Set(Height.Pixels, 0);

            _iconImage.Width.Set(16, 0);
            _iconImage.Height.Set(16, 0);
            _iconImage.HAlign = 0.5f;
            _iconImage.VAlign = 0.5f;
            _iconImage.ScaleToFit = true;

            OnLeftClick += (e, s) => SoundEngine.PlaySound(SoundID.MenuTick);

            Append(_backImage);
            Append(_iconImage);
        }

        public WandsPanelButton(string img1, string mouseText) :
            this(Main.Assets.Request<Texture2D>(img1, AssetRequestMode.ImmediateLoad)?.Value, mouseText)
        {
        }

        public override void Update(GameTime gameTime)
        {
            if (IsMouseHovering)
            {
                Terraria.Player player = Main.LocalPlayer;
                if (player != null) player.mouseInterface = true;

                if (_tooltipText != null) Main.instance.MouseText(_tooltipText);
            }

            _backImage.SetImage(IsActive ? _backTextureActive : _backTextureInactive);

            base.Update(gameTime);
        }

        public void SetIco(Texture2D img1)
        {
            _iconImage.SetImage(img1);
        }

        public void SetIco(string img1)
        {
            SetIco(Main.Assets.Request<Texture2D>(img1, AssetRequestMode.ImmediateLoad)?.Value);
        }

        public void SetTooltip(string text)
        {
            _tooltipText = text;
        }
    }

    public class WandsPanel : UserInterface
    {
        protected UIState container = null;
        protected UIState btns = null;
        protected UIState btns_2 = null;
        protected UIState btns_3 = null;
        protected UIState btns_4 = null;
        protected UIState btns_5 = null;
        protected UIState btns_6 = null;
        protected UIState btns_7 = null;
        protected UIState btns_8 = null; // 子菜单 8：电线与机关
        private int currentSubmenu = -1; // 当前展开的子菜单索引 (-1 表示未展开)

        // 主环 8 个按钮 (8 轴对称轮盘)
        protected WandsPanelButton btn1 = null; // 放置 / 破坏切换 (正上方)
        protected WandsPanelButton btn2 = null; // 建筑行为与过滤 (右上方)
        protected WandsPanelButton btn3 = null; // 几何形状 (正右方)
        protected WandsPanelButton btn4 = null; // 方块坡度与朝向 (右下方)
        protected WandsPanelButton btn8 = null; // 电线与机关 (正下方)
        protected WandsPanelButton btn5 = null; // 液体魔杖 (左下方)
        protected WandsPanelButton btn6 = null; // 建筑蓝图与结构系统 (正左方)
        protected WandsPanelButton btn7 = null; // 地形净化与环境改造 (左上方)

        // 子菜单 2：建筑行为（方块、墙壁、同材质过滤、填充空处、替换已有、掉落收集）
        protected WandsPanelButton btn2_tile = null;
        protected WandsPanelButton btn2_wall = null;
        protected WandsPanelButton btn2_match_filter = null;
        protected WandsPanelButton btn2_fill_empty = null;
        protected WandsPanelButton btn2_replace_existing = null;
        protected WandsPanelButton btn2_collect = null;

        // 子菜单 8：电线与机关（红、绿、蓝、黄电线、制动器）
        protected WandsPanelButton btn8_wire_red = null;
        protected WandsPanelButton btn8_wire_green = null;
        protected WandsPanelButton btn8_wire_blue = null;
        protected WandsPanelButton btn8_wire_yellow = null;
        protected WandsPanelButton btn8_wire_actuator = null;

        // 子菜单 3：几何形状（直线、空心圆、实心圆、实心矩形、空心矩形）
        protected WandsPanelButton btn3_line = null;
        protected WandsPanelButton btn3_circular = null;
        protected WandsPanelButton btn3_filledCircular = null;
        protected WandsPanelButton btn3_rectangle = null;
        protected WandsPanelButton btn3_hollowRectangle = null;
        // 子菜单 4：方向/坡度
        protected WandsPanelButton btn4_Solid = null;
        protected WandsPanelButton btn4_HalfBlock = null;
        protected WandsPanelButton btn4_SlopeUpLeft = null;
        protected WandsPanelButton btn4_SlopeUpRight = null;
        protected WandsPanelButton btn4_SlopeDownLeft = null;
        protected WandsPanelButton btn4_SlopeDownRight = null;

        // 子菜单 5：液体魔杖模式
        protected WandsPanelButton btn5_off = null;
        protected WandsPanelButton btn5_absorb = null;
        protected WandsPanelButton btn5_clear = null;
        protected WandsPanelButton btn5_water = null;
        protected WandsPanelButton btn5_lava = null;
        protected WandsPanelButton btn5_honey = null;
        protected WandsPanelButton btn5_shimmer = null;
        protected WandsPanelButton btn5_infinite = null;

        // 子菜单 6：蓝图与结构系统
        protected WandsPanelButton btn6_off = null;
        protected WandsPanelButton btn6_copy = null;
        protected WandsPanelButton btn6_cut = null;
        protected WandsPanelButton btn6_delete = null;
        protected WandsPanelButton btn6_paste = null;
        protected WandsPanelButton btn6_flip_h = null;
        protected WandsPanelButton btn6_flip_v = null;
        protected WandsPanelButton btn6_overwrite = null;
        protected WandsPanelButton btn6_wall = null;
        protected WandsPanelButton btn6_consume = null;
        protected WandsPanelButton btn6_manager = null;

        // 子菜单 7：地形净化与环境改造
        protected WandsPanelButton btn7_off = null;
        protected WandsPanelButton btn7_purity = null;
        protected WandsPanelButton btn7_hallow = null;
        protected WandsPanelButton btn7_corruption = null;
        protected WandsPanelButton btn7_crimson = null;
        protected WandsPanelButton btn7_mushroom = null;
        protected WandsPanelButton btn7_desert = null;
        protected WandsPanelButton btn7_snow = null;
        protected WandsPanelButton btn7_wall = null;

        public static Structure.UI.UIBlueprintManager BlueprintManager = new Structure.UI.UIBlueprintManager();
        public static WandsPanel Instance { get; private set; }
        public static bool AutoReopenManagerAfterPlacement = false;
        public static bool IsOpen { get; set; } = false;

        public static void OpenBlueprintManager()
        {
            if (Instance == null) return;
            if (!IsOpen)
            {
                Instance.Open();
            }
            if (!BlueprintManager.IsOpen)
            {
                BlueprintManager.Open(Instance.container);
            }
            else
            {
                BlueprintManager.RefreshList();
            }
        }

        public bool IsReset { get; set; } = true;

        public WandsPanel()
        {
            Instance = this;
            container = new UIState();
            btns = new UIState();
            btns_2 = new UIState();
            btns_3 = new UIState();
            btns_4 = new UIState();
            btns_5 = new UIState();
            btns_6 = new UIState();
            btns_7 = new UIState();
            btns_8 = new UIState();

            // 主按钮初始化 (8 轴对称轮盘)
            btn1 = new WandsPanelButton("Images/Item_1", "放置 / 破坏切换");
            btn2 = new WandsPanelButton("Images/Item_2", "建筑行为与过滤: 方块/墙壁/同材质过滤/填充/替换/掉落物");
            btn3 = new WandsPanelButton(Resources.Images_ShapesRectangle, "几何形状: 直线 / 空心圆 / 矩形框选");
            btn4 = new WandsPanelButton(Resources.Images_SlopeSolid, "方块坡度与朝向");
            btn8 = new WandsPanelButton("Images/UI/Wires_0", "电线与机关: 红/绿/蓝/黄电线与制动器");
            btn5 = new WandsPanelButton("Images/Item_3031", "液体魔杖: 吸收/清空/铺设液体");
            btn6 = new WandsPanelButton("Images/Item_3611", "建筑蓝图与结构系统: 复制/剪切/删除/粘贴/保存");
            btn7 = new WandsPanelButton("Images/Item_779", "地形净化与环境改造: 纯净/神圣/腐化/猩红/蘑菇/沙漠/雪原");

            // 子按钮 2 初始化（建筑行为与过滤，6 个按钮）
            btn2_tile = new WandsPanelButton("Images/Item_2", "方块操作开关");
            btn2_wall = new WandsPanelButton("Images/Item_30", "背景墙操作开关");
            btn2_match_filter = new WandsPanelButton("Images/Item_1071", "同材质过滤(保护家具): 开/关 (以鼠标起点材质为准，放置仅替换同类，破坏仅清除同类)");
            btn2_fill_empty = new WandsPanelButton("Images/Item_2", "填充空处开关 (开: 允许在空白处放置 / 关: 不填塞空白)");
            btn2_replace_existing = new WandsPanelButton("Images/Item_4082", "替换已有物块/墙壁开关 (开: 允许替换已有物块 / 关: 跳过不破坏已有物块)");
            btn2_collect = new WandsPanelButton("Images/Item_5010", "破坏产生掉落物开关 (开: 产生并吸入背包 / 关: 彻底销毁无掉落)");

            // 子按钮 8 初始化（电线与机关，5 个按钮）
            btn8_wire_red = new WandsPanelButton("Images/UI/Wires_2", "红线");
            btn8_wire_green = new WandsPanelButton("Images/UI/Wires_3", "绿线");
            btn8_wire_blue = new WandsPanelButton("Images/UI/Wires_4", "蓝线");
            btn8_wire_yellow = new WandsPanelButton("Images/UI/Wires_5", "黄线");
            btn8_wire_actuator = new WandsPanelButton("Images/UI/Wires_10", "制动器");

            btn3_line = new WandsPanelButton(Resources.Images_ShapesLine, "直线");
            btn3_circular = new WandsPanelButton(Resources.Images_ShapesCircular, "空心圆/圆周");
            btn3_filledCircular = new WandsPanelButton(Resources.Images_ShapesFilledCircular, "实心圆/椭圆填充");
            btn3_rectangle = new WandsPanelButton(Resources.Images_ShapesRectangle, "实心矩形大范围框选");
            btn3_hollowRectangle = new WandsPanelButton(Resources.Images_ShapesHollowRectangle, "空心矩形/房屋边框框架");
            btn4_Solid = new WandsPanelButton(Resources.Images_SlopeSolid, "实体方块");
            btn4_HalfBlock = new WandsPanelButton(Resources.Images_SlopeHalfBlock, "半砖");
            btn4_SlopeUpLeft = new WandsPanelButton(Resources.Images_SlopeUpLeft, "左上斜坡");
            btn4_SlopeUpRight = new WandsPanelButton(Resources.Images_SlopeUpRight, "右上斜坡");
            btn4_SlopeDownLeft = new WandsPanelButton(Resources.Images_SlopeDownLeft, "左下斜坡");
            btn4_SlopeDownRight = new WandsPanelButton(Resources.Images_SlopeDownRight, "右下斜坡");

            btn5_off = new WandsPanelButton("Images/Item_1", "关闭液体模式 (恢复方块操作)");
            btn5_absorb = new WandsPanelButton("Images/Item_4820", "吸收液体 (抽取并装入空桶)");
            btn5_clear = new WandsPanelButton("Images/Item_5304", "一键清空/蒸发液体");
            btn5_water = new WandsPanelButton("Images/Item_206", "铺设水");
            btn5_lava = new WandsPanelButton("Images/Item_207", "铺设岩浆");
            btn5_honey = new WandsPanelButton("Images/Item_1128", "铺设蜂蜜");
            btn5_shimmer = new WandsPanelButton("Images/Item_5303", "铺设微光");
            btn5_infinite = new WandsPanelButton("Images/Item_3031", "无限液体放置模式开关 (免消耗背包桶)");

            // 蓝图子按钮
            btn6_off = new WandsPanelButton("Images/Item_1", "退出蓝图模式 (恢复常规魔杖)");
            btn6_copy = new WandsPanelButton("Images/Item_5098", "结构框选复制 (划选区域自动存入剪贴板)");
            btn6_cut = new WandsPanelButton("Images/UI/Wires_1", "结构框选剪切 (划选区域抓取至剪贴板，一键搬家)");
            btn6_delete = new WandsPanelButton("Images/Item_166", "结构区域删除 (划选同时破坏物块与背景墙)");
            btn6_paste = new WandsPanelButton("Images/Item_3611", "蓝图放置模式 (投射虚影与一键摆放)");
            btn6_flip_h = new WandsPanelButton("Images/Item_3612", "水平镜像翻转 [快捷键: H]");
            btn6_flip_v = new WandsPanelButton("Images/Item_3625", "垂直翻转 [快捷键: V]");
            btn6_overwrite = new WandsPanelButton("Images/Item_4082", "覆盖已有物块开关 (开: 覆盖原有物块 / 关: 仅在空白处放置)");
            btn6_wall = new WandsPanelButton("Images/Item_30", "蓝图考虑墙壁开关 (开: 包含背景墙 / 关: 忽略背景墙)");
            btn6_consume = new WandsPanelButton("Images/Item_5010", "材料消耗模式开关 (开: 放置需消耗对应材料 / 关: 免消耗自由摆放)");
            btn6_manager = new WandsPanelButton("Images/Item_3611", "📖 蓝图管理器 (在游戏内浏览、载入、保存与管理本地蓝图)");

            // 地形净化与环境改造子按钮
            btn7_off = new WandsPanelButton("Images/Item_1", "退出环境改造 (恢复常规魔杖)");
            btn7_purity = new WandsPanelButton("Images/Item_780", "纯净净化 (绿溶液: 净化腐化/猩红/神圣回森林)");
            btn7_hallow = new WandsPanelButton("Images/Item_781", "神圣环境 (蓝溶液)");
            btn7_corruption = new WandsPanelButton("Images/Item_782", "腐化环境 (紫溶液)");
            btn7_crimson = new WandsPanelButton("Images/Item_784", "猩红环境 (红溶液)");
            btn7_mushroom = new WandsPanelButton("Images/Item_783", "发光蘑菇群落 (深蓝溶液)");
            btn7_desert = new WandsPanelButton("Images/Item_5392", "沙漠化 (黄溶液)");
            btn7_snow = new WandsPanelButton("Images/Item_5393", "冰雪化 (白溶液)");
            btn7_wall = new WandsPanelButton("Images/Item_30", "包含背景墙开关 (开: 同步改造背景墙 / 关: 仅改造物块)");

            // 主按钮点击事件 (8 轴对称排布)
            btn1.OnLeftClick += (e, s) => OnClick(0); // 正上方: 放置/破坏切换
            btn2.OnLeftClick += (e, s) => OnClick(1); // 右上方: 建筑行为与过滤
            btn3.OnLeftClick += (e, s) => OnClick(2); // 正右方: 几何形状
            btn4.OnLeftClick += (e, s) => OnClick(3); // 右下方: 坡度与朝向
            btn8.OnLeftClick += (e, s) => OnClick(4); // 正下方: 电线与机关
            btn5.OnLeftClick += (e, s) => OnClick(5); // 左下方: 液体魔杖
            btn6.OnLeftClick += (e, s) => OnClick(6); // 正左方: 建筑蓝图系统
            btn7.OnLeftClick += (e, s) => OnClick(7); // 左上方: 地形净化与改造

            // 子按钮 2 事件（建筑行为与过滤）
            btn2_tile.OnLeftClick += (e, s) => GameMain.Wand_Tile = !GameMain.Wand_Tile;
            btn2_wall.OnLeftClick += (e, s) => GameMain.Wand_Wall = !GameMain.Wand_Wall;
            btn2_match_filter.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_MatchFilter = !GameMain.Wand_MatchFilter;
                Main.NewText($"[魔杖] 同材质过滤(保护家具): {(GameMain.Wand_MatchFilter ? "开启 (以鼠标起点材质为准，放置仅替换同类，破坏仅清除同类)" : "关闭 (选区全量生效)")}", 120, 200, 255);
            };
            btn2_fill_empty.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_FillEmpty = !GameMain.Wand_FillEmpty;
                Main.NewText($"[魔杖] 填充空处: {(GameMain.Wand_FillEmpty ? "开" : "关")}", 255, 255, 150);
            };
            btn2_replace_existing.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_ReplaceExisting = !GameMain.Wand_ReplaceExisting;
                Main.NewText($"[魔杖] 替换已有物块/墙壁: {(GameMain.Wand_ReplaceExisting ? "开" : "关")}", 255, 255, 150);
            };
            btn2_collect.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_CollectDrops = !GameMain.Wand_CollectDrops;
                Main.NewText($"[魔杖] 产生掉落物: {(GameMain.Wand_CollectDrops ? "开" : "关")}", 255, 255, 150);
            };

            // 子按钮 8 事件（电线与机关）
            Action<Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode> btn8_wire_action = (v) =>
            {
                if (GameMain.Wand_ToolMode.HasFlag(v)) GameMain.Wand_ToolMode &= ~v;
                else GameMain.Wand_ToolMode |= v;
            };
            btn8_wire_red.OnLeftClick += (e, s) => btn8_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red);
            btn8_wire_green.OnLeftClick += (e, s) => btn8_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green);
            btn8_wire_blue.OnLeftClick += (e, s) => btn8_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue);
            btn8_wire_yellow.OnLeftClick += (e, s) => btn8_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow);
            btn8_wire_actuator.OnLeftClick += (e, s) => btn8_wire_action(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator);

            // 子按钮 3 事件
            btn3_line.OnLeftClick += (e, s) => GameMain.Wand_Shapes = Wands.Shapes.line;
            btn3_circular.OnLeftClick += (e, s) => GameMain.Wand_Shapes = Wands.Shapes.circular;
            btn3_filledCircular.OnLeftClick += (e, s) => GameMain.Wand_Shapes = Wands.Shapes.filledCircular;
            btn3_rectangle.OnLeftClick += (e, s) => GameMain.Wand_Shapes = Wands.Shapes.rectangle;
            btn3_hollowRectangle.OnLeftClick += (e, s) => GameMain.Wand_Shapes = Wands.Shapes.hollowRectangle;
            // 子按钮 4 事件
            btn4_Solid.OnLeftClick += (e, s) => GameMain.Wand_BlockType = WandAction.BlockType.Solid;
            btn4_HalfBlock.OnLeftClick += (e, s) => GameMain.Wand_BlockType = WandAction.BlockType.HalfBlock;
            btn4_SlopeUpLeft.OnLeftClick += (e, s) => GameMain.Wand_BlockType = WandAction.BlockType.SlopeUpLeft;
            btn4_SlopeUpRight.OnLeftClick += (e, s) => GameMain.Wand_BlockType = WandAction.BlockType.SlopeUpRight;
            btn4_SlopeDownLeft.OnLeftClick += (e, s) => GameMain.Wand_BlockType = WandAction.BlockType.SlopeDownLeft;
            btn4_SlopeDownRight.OnLeftClick += (e, s) => GameMain.Wand_BlockType = WandAction.BlockType.SlopeDownRight;

            // 子按钮 5 事件
            btn5_off.OnLeftClick += (e, s) => GameMain.Wand_LiquidMode = GameMain.LiquidMode.None;
            btn5_absorb.OnLeftClick += (e, s) => GameMain.Wand_LiquidMode = GameMain.LiquidMode.Absorb;
            btn5_clear.OnLeftClick += (e, s) => GameMain.Wand_LiquidMode = GameMain.LiquidMode.Clear;
            btn5_water.OnLeftClick += (e, s) => GameMain.Wand_LiquidMode = GameMain.LiquidMode.Water;
            btn5_lava.OnLeftClick += (e, s) => GameMain.Wand_LiquidMode = GameMain.LiquidMode.Lava;
            btn5_honey.OnLeftClick += (e, s) => GameMain.Wand_LiquidMode = GameMain.LiquidMode.Honey;
            btn5_shimmer.OnLeftClick += (e, s) => GameMain.Wand_LiquidMode = GameMain.LiquidMode.Shimmer;
            btn5_infinite.OnLeftClick += (e, s) => GameMain.Wand_InfiniteLiquid = !GameMain.Wand_InfiniteLiquid;

            // 子按钮 6 事件
            btn6_off.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_StructureMode = GameMain.StructureMode.None;
                Main.NewText("[魔杖] 已退出蓝图模式 (恢复常规魔杖)", 100, 220, 255);
            };
            btn6_copy.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_StructureMode = GameMain.StructureMode.Copy;
                GameMain.LastActiveStructureMode = GameMain.StructureMode.Copy;
                Main.NewText("[魔杖] 进入结构复制模式", 255, 240, 100);
            };
            btn6_cut.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_StructureMode = GameMain.StructureMode.Cut;
                GameMain.LastActiveStructureMode = GameMain.StructureMode.Cut;
                Main.NewText("[魔杖] 进入结构剪切模式", 255, 180, 80);
            };
            btn6_delete.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_StructureMode = GameMain.StructureMode.Delete;
                GameMain.LastActiveStructureMode = GameMain.StructureMode.Delete;
                Main.NewText("[魔杖] 进入结构删除模式", 255, 100, 100);
            };
            btn6_paste.OnLeftClick += (e, s) =>
            {
                if (Structure.StructureStorage.Clipboard == null)
                {
                    Main.NewText("[魔杖] 剪贴板为空，请先复制或载入蓝图", 255, 100, 100);
                    return;
                }
                GameMain.Wand_StructureMode = GameMain.StructureMode.Paste;
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
                GameMain.Wand_StructureOverwrite = !GameMain.Wand_StructureOverwrite;
                Main.NewText($"[魔杖] 蓝图覆盖: {(GameMain.Wand_StructureOverwrite ? "开" : "关")}", 255, 255, 150);
            };
            btn6_wall.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_StructureIncludeWall = !GameMain.Wand_StructureIncludeWall;
                Main.NewText($"[魔杖] 蓝图考虑背景墙: {(GameMain.Wand_StructureIncludeWall ? "开" : "关")}", 255, 255, 150);
            };
            btn6_consume.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_StructureConsumeMaterials = !GameMain.Wand_StructureConsumeMaterials;
                Main.NewText($"[魔杖] 材料消耗: {(GameMain.Wand_StructureConsumeMaterials ? "开" : "关")}", 255, 255, 150);
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

            // 子按钮 7 事件
            btn7_off.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.None;
                Main.NewText("[魔杖] 已退出环境改造模式", 100, 220, 255);
            };
            btn7_purity.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.Purity;
                Main.NewText("[魔杖] 纯净净化模式 (绿溶液)", 100, 255, 120);
            };
            btn7_hallow.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.Hallow;
                Main.NewText("[魔杖] 神圣环境模式 (蓝溶液)", 100, 240, 255);
            };
            btn7_corruption.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.Corruption;
                Main.NewText("[魔杖] 腐化环境模式 (紫溶液)", 180, 100, 255);
            };
            btn7_crimson.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.Crimson;
                Main.NewText("[魔杖] 猩红环境模式 (红溶液)", 255, 80, 100);
            };
            btn7_mushroom.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.Mushroom;
                Main.NewText("[魔杖] 发光蘑菇环境模式 (深蓝溶液)", 80, 120, 255);
            };
            btn7_desert.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.Desert;
                Main.NewText("[魔杖] 沙漠环境模式 (黄溶液)", 255, 220, 100);
            };
            btn7_snow.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeMode = GameMain.BiomeMode.Snow;
                Main.NewText("[魔杖] 冰雪环境模式 (白溶液)", 180, 240, 255);
            };
            btn7_wall.OnLeftClick += (e, s) =>
            {
                GameMain.Wand_BiomeIncludeWall = !GameMain.Wand_BiomeIncludeWall;
                Main.NewText($"[魔杖] 环境改造包含背景墙: {(GameMain.Wand_BiomeIncludeWall ? "开" : "关")}", 255, 255, 150);
            };

            // 装配 UI (主轮盘 8 个大类)
            SetState(container);
            container.Append(btns);
            container.Append(btn1);
            container.Append(btn2);
            container.Append(btn3);
            container.Append(btn4);
            container.Append(btn8);
            container.Append(btn5);
            container.Append(btn6);
            container.Append(btn7);

            // 子菜单 2：建筑行为与过滤 (6 键)
            btns_2.Append(btn2_tile);
            btns_2.Append(btn2_wall);
            btns_2.Append(btn2_match_filter);
            btns_2.Append(btn2_fill_empty);
            btns_2.Append(btn2_replace_existing);
            btns_2.Append(btn2_collect);

            // 子菜单 8：电线与机关 (5 键)
            btns_8.Append(btn8_wire_red);
            btns_8.Append(btn8_wire_green);
            btns_8.Append(btn8_wire_blue);
            btns_8.Append(btn8_wire_yellow);
            btns_8.Append(btn8_wire_actuator);

            btns_3.Append(btn3_line);
            btns_3.Append(btn3_circular);
            btns_3.Append(btn3_filledCircular);
            btns_3.Append(btn3_rectangle);
            btns_3.Append(btn3_hollowRectangle);
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
            btns_6.Append(btn6_wall);
            btns_6.Append(btn6_consume);
            btns_6.Append(btn6_manager);

            btns_7.Append(btn7_off);
            btns_7.Append(btn7_purity);
            btns_7.Append(btn7_hallow);
            btns_7.Append(btn7_corruption);
            btns_7.Append(btn7_crimson);
            btns_7.Append(btn7_mushroom);
            btns_7.Append(btn7_desert);
            btns_7.Append(btn7_snow);
            btns_7.Append(btn7_wall);
        }

        public void Open()
        {
            IsOpen = true;
            IsReset = false;
            currentSubmenu = -1;
            btns.RemoveAllChildren();
            Reset();
            UpdateVisuals(null);
            Recalculate();
        }

        public void Close()
        {
            IsOpen = false;
            currentSubmenu = -1;
            btns.RemoveAllChildren();
            BlueprintManager?.Close();
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void UpdateVisuals(GameTime time)
        {
            if (IsReset)
            {
                IsReset = false;
                Reset();
                Recalculate();
            }

            // 主按钮图标与状态更新
            btn1.SetIco(GameMain.Wand_isPlace ? "Images/Item_129" : "Images/Item_1");
            btn1.SetTooltip($"当前模式: {(GameMain.Wand_isPlace ? "放置" : "破坏")} (点击切换)");

            switch (GameMain.Wand_Shapes)
            {
                case Wands.Shapes.line: btn3.SetIco(Resources.Images_ShapesLine); break;
                case Wands.Shapes.circular: btn3.SetIco(Resources.Images_ShapesCircular); break;
                case Wands.Shapes.filledCircular: btn3.SetIco(Resources.Images_ShapesFilledCircular); break;
                case Wands.Shapes.rectangle: btn3.SetIco(Resources.Images_ShapesRectangle); break;
                case Wands.Shapes.hollowRectangle: btn3.SetIco(Resources.Images_ShapesHollowRectangle); break;
                default: break;
            }

            switch (GameMain.Wand_BlockType)
            {
                case WandAction.BlockType.Solid: btn4.SetIco(Resources.Images_SlopeSolid); break;
                case WandAction.BlockType.HalfBlock: btn4.SetIco(Resources.Images_SlopeHalfBlock); break;
                case WandAction.BlockType.SlopeUpLeft: btn4.SetIco(Resources.Images_SlopeUpLeft); break;
                case WandAction.BlockType.SlopeUpRight: btn4.SetIco(Resources.Images_SlopeUpRight); break;
                case WandAction.BlockType.SlopeDownLeft: btn4.SetIco(Resources.Images_SlopeDownLeft); break;
                case WandAction.BlockType.SlopeDownRight: btn4.SetIco(Resources.Images_SlopeDownRight); break;
                default: break;
            }

            switch (GameMain.Wand_LiquidMode)
            {
                case GameMain.LiquidMode.None: btn5.SetIco("Images/Item_3031"); break;
                case GameMain.LiquidMode.Absorb: btn5.SetIco("Images/Item_4820"); break;
                case GameMain.LiquidMode.Clear: btn5.SetIco("Images/Item_5304"); break;
                case GameMain.LiquidMode.Water: btn5.SetIco("Images/Item_206"); break;
                case GameMain.LiquidMode.Lava: btn5.SetIco("Images/Item_207"); break;
                case GameMain.LiquidMode.Honey: btn5.SetIco("Images/Item_1128"); break;
                case GameMain.LiquidMode.Shimmer: btn5.SetIco("Images/Item_5303"); break;
                default: break;
            }
            btn5.SetTooltip($"液体魔杖: {GameMain.Wand_LiquidMode} [无限:{(GameMain.Wand_InfiniteLiquid ? "开" : "关")}]");

            switch (GameMain.Wand_BiomeMode)
            {
                case GameMain.BiomeMode.None: btn7.SetIco("Images/Item_779"); break;
                case GameMain.BiomeMode.Purity: btn7.SetIco("Images/Item_780"); break;
                case GameMain.BiomeMode.Hallow: btn7.SetIco("Images/Item_781"); break;
                case GameMain.BiomeMode.Corruption: btn7.SetIco("Images/Item_782"); break;
                case GameMain.BiomeMode.Mushroom: btn7.SetIco("Images/Item_783"); break;
                case GameMain.BiomeMode.Crimson: btn7.SetIco("Images/Item_784"); break;
                case GameMain.BiomeMode.Desert: btn7.SetIco("Images/Item_5392"); break;
                case GameMain.BiomeMode.Snow: btn7.SetIco("Images/Item_5393"); break;
                default: break;
            }
            btn7.SetTooltip($"地形净化与环境改造: {GameMain.Wand_BiomeMode} [包含背景墙:{(GameMain.Wand_BiomeIncludeWall ? "开" : "关")}]");

            // 子按钮激活高亮背景
            btn2_tile.IsActive = GameMain.Wand_Tile;
            btn2_wall.IsActive = GameMain.Wand_Wall;
            btn2_match_filter.IsActive = GameMain.Wand_MatchFilter;
            btn2_match_filter.SetTooltip($"同材质过滤(保护家具): {(GameMain.Wand_MatchFilter ? "开" : "关")} (以鼠标起点材质为准，放置仅替换同类，破坏仅清除同类)");
            btn2_fill_empty.IsActive = GameMain.Wand_FillEmpty;
            btn2_replace_existing.IsActive = GameMain.Wand_ReplaceExisting;
            btn2_collect.IsActive = GameMain.Wand_CollectDrops;

            // 电线子按钮高亮与主按钮状态
            btn8_wire_red.IsActive = GameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red);
            btn8_wire_green.IsActive = GameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green);
            btn8_wire_blue.IsActive = GameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue);
            btn8_wire_yellow.IsActive = GameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow);
            btn8_wire_actuator.IsActive = GameMain.Wand_ToolMode.HasFlag(Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator);
            btn8.IsActive = GameMain.Wand_ToolMode != 0;
            btn8.SetTooltip($"电线与机关: [{(GameMain.Wand_ToolMode == 0 ? "全部关闭" : GameMain.Wand_ToolMode.ToString())}]");

            btn3_line.IsActive = GameMain.Wand_Shapes == Wands.Shapes.line;
            btn3_circular.IsActive = GameMain.Wand_Shapes == Wands.Shapes.circular;
            btn3_filledCircular.IsActive = GameMain.Wand_Shapes == Wands.Shapes.filledCircular;
            btn3_rectangle.IsActive = GameMain.Wand_Shapes == Wands.Shapes.rectangle;
            btn3_hollowRectangle.IsActive = GameMain.Wand_Shapes == Wands.Shapes.hollowRectangle;
            btn4_Solid.IsActive = GameMain.Wand_BlockType == WandAction.BlockType.Solid;
            btn4_HalfBlock.IsActive = GameMain.Wand_BlockType == WandAction.BlockType.HalfBlock;
            btn4_SlopeUpLeft.IsActive = GameMain.Wand_BlockType == WandAction.BlockType.SlopeUpLeft;
            btn4_SlopeUpRight.IsActive = GameMain.Wand_BlockType == WandAction.BlockType.SlopeUpRight;
            btn4_SlopeDownLeft.IsActive = GameMain.Wand_BlockType == WandAction.BlockType.SlopeDownLeft;
            btn4_SlopeDownRight.IsActive = GameMain.Wand_BlockType == WandAction.BlockType.SlopeDownRight;

            btn5_off.IsActive = GameMain.Wand_LiquidMode == GameMain.LiquidMode.None;
            btn5_absorb.IsActive = GameMain.Wand_LiquidMode == GameMain.LiquidMode.Absorb;
            btn5_clear.IsActive = GameMain.Wand_LiquidMode == GameMain.LiquidMode.Clear;
            btn5_water.IsActive = GameMain.Wand_LiquidMode == GameMain.LiquidMode.Water;
            btn5_lava.IsActive = GameMain.Wand_LiquidMode == GameMain.LiquidMode.Lava;
            btn5_honey.IsActive = GameMain.Wand_LiquidMode == GameMain.LiquidMode.Honey;
            btn5_shimmer.IsActive = GameMain.Wand_LiquidMode == GameMain.LiquidMode.Shimmer;
            btn5_infinite.IsActive = GameMain.Wand_InfiniteLiquid;

            btn6.SetTooltip($"建筑蓝图模式: {GameMain.Wand_StructureMode} [覆盖:{(GameMain.Wand_StructureOverwrite ? "开" : "关")}] [背景墙:{(GameMain.Wand_StructureIncludeWall ? "开" : "关")}] [材料消耗:{(GameMain.Wand_StructureConsumeMaterials ? "开" : "关")}]");
            btn6_off.IsActive = GameMain.Wand_StructureMode == GameMain.StructureMode.None;
            btn6_copy.IsActive = GameMain.Wand_StructureMode == GameMain.StructureMode.Copy;
            btn6_cut.IsActive = GameMain.Wand_StructureMode == GameMain.StructureMode.Cut;
            btn6_delete.IsActive = GameMain.Wand_StructureMode == GameMain.StructureMode.Delete;
            btn6_paste.IsActive = GameMain.Wand_StructureMode == GameMain.StructureMode.Paste;
            btn6_overwrite.IsActive = GameMain.Wand_StructureOverwrite;
            btn6_wall.IsActive = GameMain.Wand_StructureIncludeWall;
            btn6_consume.IsActive = GameMain.Wand_StructureConsumeMaterials;
            btn6_manager.IsActive = BlueprintManager.IsOpen;

            btn7_off.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.None;
            btn7_purity.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.Purity;
            btn7_hallow.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.Hallow;
            btn7_corruption.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.Corruption;
            btn7_crimson.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.Crimson;
            btn7_mushroom.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.Mushroom;
            btn7_desert.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.Desert;
            btn7_snow.IsActive = GameMain.Wand_BiomeMode == GameMain.BiomeMode.Snow;
            btn7_wall.IsActive = GameMain.Wand_BiomeIncludeWall;
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

            // 主轮盘 8 个按钮均匀环绕鼠标 (半径 48px，左右功能区对称)
            layoutCircle.Invoke(8, 0, btn1, 48f); // 正上方 (-90°): 放置/破坏切换
            layoutCircle.Invoke(8, 1, btn2, 48f); // 右上方 (-45°): 建筑行为与过滤
            layoutCircle.Invoke(8, 2, btn3, 48f); // 正右方 (0°): 几何形状
            layoutCircle.Invoke(8, 3, btn4, 48f); // 右下方 (45°): 坡度朝向
            layoutCircle.Invoke(8, 4, btn8, 48f); // 正下方 (90°): 电线与机关
            layoutCircle.Invoke(8, 5, btn5, 48f); // 左下方 (135°): 液体魔杖
            layoutCircle.Invoke(8, 6, btn6, 48f); // 正左方 (180°): 建筑蓝图与结构系统
            layoutCircle.Invoke(8, 7, btn7, 48f); // 左上方 (225°): 地形净化与改造

            // 子菜单 2 环绕 (半径 96px, 6 个按钮)
            layoutCircle.Invoke(6, 0, btn2_tile, 96f);
            layoutCircle.Invoke(6, 1, btn2_wall, 96f);
            layoutCircle.Invoke(6, 2, btn2_match_filter, 96f);
            layoutCircle.Invoke(6, 3, btn2_fill_empty, 96f);
            layoutCircle.Invoke(6, 4, btn2_replace_existing, 96f);
            layoutCircle.Invoke(6, 5, btn2_collect, 96f);

            // 子菜单 8 环绕 (半径 96px, 5 个按钮)
            layoutCircle.Invoke(5, 0, btn8_wire_red, 96f);
            layoutCircle.Invoke(5, 1, btn8_wire_green, 96f);
            layoutCircle.Invoke(5, 2, btn8_wire_blue, 96f);
            layoutCircle.Invoke(5, 3, btn8_wire_yellow, 96f);
            layoutCircle.Invoke(5, 4, btn8_wire_actuator, 96f);

            layoutCircle.Invoke(5, 0, btn3_line, 96f);
            layoutCircle.Invoke(5, 1, btn3_circular, 96f);
            layoutCircle.Invoke(5, 2, btn3_filledCircular, 96f);
            layoutCircle.Invoke(5, 3, btn3_rectangle, 96f);
            layoutCircle.Invoke(5, 4, btn3_hollowRectangle, 96f);
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

            layoutCircle.Invoke(11, 0, btn6_off, 96f);
            layoutCircle.Invoke(11, 1, btn6_copy, 96f);
            layoutCircle.Invoke(11, 2, btn6_cut, 96f);
            layoutCircle.Invoke(11, 3, btn6_delete, 96f);
            layoutCircle.Invoke(11, 4, btn6_paste, 96f);
            layoutCircle.Invoke(11, 5, btn6_flip_h, 96f);
            layoutCircle.Invoke(11, 6, btn6_flip_v, 96f);
            layoutCircle.Invoke(11, 7, btn6_overwrite, 96f);
            layoutCircle.Invoke(11, 8, btn6_wall, 96f);
            layoutCircle.Invoke(11, 9, btn6_consume, 96f);
            layoutCircle.Invoke(11, 10, btn6_manager, 96f);

            // 子菜单 7 环绕 (半径 96px, 9 个按钮)
            layoutCircle.Invoke(9, 0, btn7_off, 96f);
            layoutCircle.Invoke(9, 1, btn7_purity, 96f);
            layoutCircle.Invoke(9, 2, btn7_hallow, 96f);
            layoutCircle.Invoke(9, 3, btn7_corruption, 96f);
            layoutCircle.Invoke(9, 4, btn7_crimson, 96f);
            layoutCircle.Invoke(9, 5, btn7_mushroom, 96f);
            layoutCircle.Invoke(9, 6, btn7_desert, 96f);
            layoutCircle.Invoke(9, 7, btn7_snow, 96f);
            layoutCircle.Invoke(9, 8, btn7_wall, 96f);
        }

        private void OnClick(int index)
        {
            if (index == 0)
            {
                GameMain.Wand_isPlace = !GameMain.Wand_isPlace;
                return;
            }

            // 再次点击同一个母菜单按钮：收回（折叠）子菜单
            if (currentSubmenu == index)
            {
                btns.RemoveAllChildren();
                currentSubmenu = -1;
                return;
            }

            btns.RemoveAllChildren();
            currentSubmenu = index;

            switch (index)
            {
                case 1: btns.Append(btns_2); break; // 建筑行为与过滤
                case 2: btns.Append(btns_3); break; // 几何形状
                case 3: btns.Append(btns_4); break; // 坡度与朝向
                case 4: btns.Append(btns_8); break; // 电线与机关
                case 5: btns.Append(btns_5); break; // 液体魔杖
                case 6: // 建筑蓝图系统
                    btns.Append(btns_6);
                    if (GameMain.Wand_StructureMode == GameMain.StructureMode.None)
                    {
                        GameMain.Wand_StructureMode = GameMain.StructureMode.Copy;
                        GameMain.LastActiveStructureMode = GameMain.StructureMode.Copy;
                        Main.NewText("[魔杖] 蓝图模式已开启 (默认: 结构复制)", 255, 240, 100);
                    }
                    break;
                case 7: // 地形净化与改造
                    btns.Append(btns_7);
                    if (GameMain.Wand_BiomeMode == GameMain.BiomeMode.None)
                    {
                        GameMain.Wand_BiomeMode = GameMain.BiomeMode.Purity;
                        Main.NewText("[魔杖] 地形改造已开启 (默认: 纯净净化)", 100, 255, 120);
                    }
                    break;
                default: break;
            }
        }
    }
}
