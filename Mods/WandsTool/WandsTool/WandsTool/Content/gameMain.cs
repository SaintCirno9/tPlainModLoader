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
        public static Terraria.GameContent.UI.WiresUI.Settings.MultiToolMode Wand_ToolMode = 0;
        public static Wands.Shapes Wand_Shapes = Wands.Shapes.rectangle; // 几何形状（默认为矩形）
        public static WandAction.BlockType Wand_BlockType = WandAction.BlockType.Solid; // 坡度与方向

        /// <summary>
        /// 根据玩家当前手持物品智能自适应魔棒工作模式
        /// </summary>
        public static void AutoAdaptModeToHeldItem(Terraria.Player player)
        {
            if (player == null) return;
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

