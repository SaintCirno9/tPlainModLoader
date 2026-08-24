namespace WandsTool.Content
{
    public partial class gameMain
    {
        /// <summary>
        /// 液体操作模式定义
        /// </summary>
        public enum LiquidMode
        {
            /// <summary>
            /// 无液体操作（常规方块/墙壁/电线操作）
            /// </summary>
            None,
            /// <summary>
            /// 吸收液体（抽取并尝试装入背包空桶）
            /// </summary>
            Absorb,
            /// <summary>
            /// 一键清空/蒸发液体
            /// </summary>
            Clear,
            /// <summary>
            /// 放置水
            /// </summary>
            Water,
            /// <summary>
            /// 放置岩浆
            /// </summary>
            Lava,
            /// <summary>
            /// 放置蜂蜜
            /// </summary>
            Honey,
            /// <summary>
            /// 放置微光
            /// </summary>
            Shimmer
        }

        /// <summary>
        /// 建筑结构/蓝图操作模式
        /// </summary>
        public enum StructureMode
        {
            /// <summary>
            /// 无蓝图操作（常规几何魔杖模式）
            /// </summary>
            None,
            /// <summary>
            /// 结构框选复制模式
            /// </summary>
            Copy,
            /// <summary>
            /// 结构框选剪切模式（抓取后清除原区域，实现一键搬家移动）
            /// </summary>
            Cut,
            /// <summary>
            /// 结构区域删除模式（同时破坏区域内物块与背景墙）
            /// </summary>
            Delete,
            /// <summary>
            /// 结构蓝图粘贴模式
            /// </summary>
            Paste
        }

        public static bool UI_WandsPanel1_isOpen = false;

        public static bool Wand_isEnable = false;
        public static int Wand_UpdateCount = 1;         // 更新循环次数
        public static int Wand_BatchSize = 64;          // 单次批量操作图格数
        public static bool Wand_isPlace = true;         // 是否为放置模式（false 为破坏模式）
        public static bool Wand_Tile = true;            // 操作方块
        public static bool Wand_Wall = false;           // 操作墙壁
        public static bool Wand_BlockReplace = false;   // 方块/墙壁替换模式
        public static bool Wand_CollectDrops = true;    // 破坏时自动吸附收集掉落物
        public static LiquidMode Wand_LiquidMode = LiquidMode.None; // 当前液体操作模式
        public static bool Wand_InfiniteLiquid = false; // 无限液体模式（免消耗背包桶）
        public static StructureMode Wand_StructureMode = StructureMode.None; // 当前蓝图/结构操作模式
        public static StructureMode LastActiveStructureMode = StructureMode.Copy; // 上一个激活的蓝图工具模式（取消放置时精准回退，默认 Copy）
        public static bool Wand_StructureOverwrite = true; // 蓝图放置时是否覆盖已有物块
        public static bool Wand_StructureConsumeMaterials = true; // 蓝图放置是否消耗材料（默认开启）
        public static bool Wand_StructureAutoCraft = true; // 缺材料时是否自动消耗背包原材料制造（默认开启）
        public static bool Wand_StructureAutoCraftRequireStation = false; // 自动制造时是否严格要求附近有对应工作台（默认关闭，随身便携加工）
        public static Microsoft.Xna.Framework.Rectangle? CutSourceRect = null; // 剪切平移的原建筑区域（确认放置时才执行原子转移，取消则不修改世界）
        public static Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode Wand_ToolMode = 0;
        public static Wands.Shapes Wand_Shapes = Wands.Shapes.rectangle; // 几何形状（默认为矩形）
        public static WandAction.BlockType Wand_BlockType = WandAction.BlockType.Solid; // 坡度与方向

        /// <summary>
        /// 切换魔杖模式开关并弹出提示
        /// </summary>
        public static void ToggleWand()
        {
            SetWandEnabled(!Wand_isEnable);
        }

        /// <summary>
        /// 设置魔杖启用状态并弹出提示消息
        /// </summary>
        public static void SetWandEnabled(bool enable, bool showMsg = true)
        {
            Wand_isEnable = enable;
            Terraria.Player player = Terraria.Main.LocalPlayer;

            if (Wand_isEnable)
            {
                AutoAdaptModeToHeldItem(player);
                if (showMsg)
                {
                    Terraria.Main.NewText("[魔杖] 魔杖模式已开启", 100, 255, 120);
                    if (player != null)
                    {
                        Terraria.CombatText.NewText(player.getRect(), Microsoft.Xna.Framework.Color.LimeGreen, "魔杖模式：已开启", true, false);
                    }
                }
            }
            else
            {
                // 关闭魔杖时清空全部临时追踪、选区与操作队列
                Wands.Reset();
                WandAction.Clear();
                Wand_StructureMode = StructureMode.None;
                LastActiveStructureMode = StructureMode.Copy;
                CutSourceRect = null;
                wandsPanel.BlueprintManager?.Close();

                if (showMsg)
                {
                    Terraria.Main.NewText("[魔杖] 魔杖模式已关闭", 255, 170, 100);
                    if (player != null)
                    {
                        Terraria.CombatText.NewText(player.getRect(), Microsoft.Xna.Framework.Color.Orange, "魔杖模式：已关闭", true, false);
                    }
                }
            }
        }

        /// <summary>
        /// 根据玩家当前手持物品智能自适应魔棒工作模式（切换快捷栏时自动退出蓝图模式）
        /// </summary>
        public static void AutoAdaptModeToHeldItem(Terraria.Player player)
        {
            if (player == null) return;

            // 切换快捷栏手持物品时，自动退出蓝图与剪切模式，恢复常规魔杖工作流
            if (Wand_StructureMode != StructureMode.None)
            {
                Wand_StructureMode = StructureMode.None;
                CutSourceRect = null;
                Wands.Reset();
                wandsPanel.BlueprintManager?.Close();
                Terraria.CombatText.NewText(player.getRect(), Microsoft.Xna.Framework.Color.Orange, "已退出蓝图模式", true, false);
            }

            Terraria.Item item = player.HeldItem;
            if (item == null || item.IsAir || item.type == Terraria.ID.ItemID.None) return;

            // 1. 镐子 / 斧头 / 纯锤子 -> 破坏/挖掘模式
            if (item.pick > 0 || item.axe > 0 || (item.hammer > 0 && item.createTile < 0 && item.createWall <= 0))
            {
                Wand_isPlace = false;
                Wand_Tile = true;
                Wand_Wall = item.hammer > 0 && item.pick <= 0;
                Wand_LiquidMode = LiquidMode.None;
                Wand_ToolMode = 0;
                return;
            }

            // 2. 放置物块 -> 物块放置模式
            if (item.createTile >= 0)
            {
                Wand_isPlace = true;
                Wand_Tile = true;
                Wand_Wall = false;
                Wand_LiquidMode = LiquidMode.None;
                Wand_ToolMode = 0;
                return;
            }

            // 3. 放置背景墙 -> 背景墙放置模式
            if (item.createWall > 0)
            {
                Wand_isPlace = true;
                Wand_Tile = false;
                Wand_Wall = true;
                Wand_LiquidMode = LiquidMode.None;
                Wand_ToolMode = 0;
                return;
            }

            // 4. 液体桶 / 无底桶 / 吸收海绵 -> 液体魔杖模式
            if (item.type == Terraria.ID.ItemID.WaterBucket || item.type == Terraria.ID.ItemID.BottomlessBucket)
            {
                Wand_LiquidMode = LiquidMode.Water;
                Wand_isPlace = true;
                return;
            }
            if (item.type == Terraria.ID.ItemID.LavaBucket || item.type == Terraria.ID.ItemID.BottomlessLavaBucket)
            {
                Wand_LiquidMode = LiquidMode.Lava;
                Wand_isPlace = true;
                return;
            }
            if (item.type == Terraria.ID.ItemID.HoneyBucket || item.type == Terraria.ID.ItemID.BottomlessHoneyBucket)
            {
                Wand_LiquidMode = LiquidMode.Honey;
                Wand_isPlace = true;
                return;
            }
            if (item.type == Terraria.ID.ItemID.BottomlessShimmerBucket)
            {
                Wand_LiquidMode = LiquidMode.Shimmer;
                Wand_isPlace = true;
                return;
            }
            if (item.type == Terraria.ID.ItemID.EmptyBucket ||
                item.type == Terraria.ID.ItemID.SuperAbsorbantSponge ||
                item.type == Terraria.ID.ItemID.LavaAbsorbantSponge ||
                item.type == Terraria.ID.ItemID.HoneyAbsorbantSponge ||
                item.type == Terraria.ID.ItemID.UltraAbsorbantSponge)
            {
                Wand_LiquidMode = LiquidMode.Absorb;
                Wand_isPlace = true;
                return;
            }

            // 5. 电线与制动器工具
            if (item.type == Terraria.ID.ItemID.Wrench)
            {
                Wand_ToolMode = Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red;
                Wand_isPlace = true;
                Wand_Tile = false;
                Wand_Wall = false;
                Wand_LiquidMode = LiquidMode.None;
                return;
            }
            if (item.type == Terraria.ID.ItemID.BlueWrench)
            {
                Wand_ToolMode = Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue;
                Wand_isPlace = true;
                Wand_Tile = false;
                Wand_Wall = false;
                Wand_LiquidMode = LiquidMode.None;
                return;
            }
            if (item.type == Terraria.ID.ItemID.GreenWrench)
            {
                Wand_ToolMode = Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green;
                Wand_isPlace = true;
                Wand_Tile = false;
                Wand_Wall = false;
                Wand_LiquidMode = LiquidMode.None;
                return;
            }
            if (item.type == Terraria.ID.ItemID.YellowWrench)
            {
                Wand_ToolMode = Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow;
                Wand_isPlace = true;
                Wand_Tile = false;
                Wand_Wall = false;
                Wand_LiquidMode = LiquidMode.None;
                return;
            }
            if (item.type == Terraria.ID.ItemID.WireCutter)
            {
                Wand_ToolMode = Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Red |
                                Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Green |
                                Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Blue |
                                Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Yellow;
                Wand_isPlace = false;
                Wand_Tile = false;
                Wand_Wall = false;
                Wand_LiquidMode = LiquidMode.None;
                return;
            }
            if (item.type == Terraria.ID.ItemID.Actuator)
            {
                Wand_ToolMode = Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode.Actuator;
                Wand_isPlace = true;
                Wand_Tile = false;
                Wand_Wall = false;
                Wand_LiquidMode = LiquidMode.None;
                return;
            }
        }
    }
}

