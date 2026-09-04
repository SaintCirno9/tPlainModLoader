using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 天然危险背景墙掉落门控与映射引擎
    /// 解决敲毁天然危险墙（蜘蛛墙、地牢墙、神庙墙、沙漠危险墙、天然洞穴岩壁等）原版不掉落或仅掉落安全版墙壁的问题，
    /// 精准掉落对应放置后保持危险特性的【危险版墙壁物品】。
    /// 作者: SaintCirno9
    /// </summary>
    public class UnsafeWallDropHooks : TPML.Content.ModSystem
    {
        private static bool _registered = false;
        private static readonly Dictionary<int, int> WallToUnsafeItemMap = new Dictionary<int, int>();
        private static readonly object SyncLock = new object();
        private static bool _initialized = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            EnsureInitialized();
            On_WorldGen.KillWall_DropItems += Hook_KillWall_DropItems;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_WorldGen.KillWall_DropItems -= Hook_KillWall_DropItems;
            _registered = false;
        }

        /// <summary>
        /// 初始化静态映射与动态补全表
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (SyncLock)
            {
                if (_initialized) return;

                // 1. 核心危险墙显式硬编码映射（确保最确定性与最高性能）
                // 蜘蛛墙
                WallToUnsafeItemMap[WallID.SpiderUnsafe] = ItemID.SpiderWallUnsafe; // 62 -> 5363

                // 地牢墙（蓝、粉、绿砖/石板/瓷砖变体）
                WallToUnsafeItemMap[WallID.BlueDungeonUnsafe] = ItemID.BlueBrickWallUnsafe; // 7 -> 5365
                WallToUnsafeItemMap[WallID.BlueDungeonSlabUnsafe] = ItemID.BlueSlabWallUnsafe; // 94 -> 5366
                WallToUnsafeItemMap[WallID.BlueDungeonTileUnsafe] = ItemID.BlueTiledWallUnsafe; // 95 -> 5367

                WallToUnsafeItemMap[WallID.PinkDungeonUnsafe] = ItemID.PinkBrickWallUnsafe; // 9 -> 5368
                WallToUnsafeItemMap[WallID.PinkDungeonSlabUnsafe] = ItemID.PinkSlabWallUnsafe; // 96 -> 5369
                WallToUnsafeItemMap[WallID.PinkDungeonTileUnsafe] = ItemID.PinkTiledWallUnsafe; // 97 -> 5370

                WallToUnsafeItemMap[WallID.GreenDungeonUnsafe] = ItemID.GreenBrickWallUnsafe; // 8 -> 5371
                WallToUnsafeItemMap[WallID.GreenDungeonSlabUnsafe] = ItemID.GreenSlabWallUnsafe; // 98 -> 5372
                WallToUnsafeItemMap[WallID.GreenDungeonTileUnsafe] = ItemID.GreenTiledWallUnsafe; // 99 -> 5373

                // 地下沙漠
                WallToUnsafeItemMap[WallID.Sandstone] = ItemID.SandstoneWallUnsafe; // 187 -> 5374
                WallToUnsafeItemMap[WallID.HardenedSand] = ItemID.HardenedSandWallUnsafe; // 216 -> 5375

                // 丛林神庙
                WallToUnsafeItemMap[WallID.LihzahrdBrickUnsafe] = ItemID.LihzahrdWallUnsafe; // 87 -> 5376

                // 泥土与活木
                WallToUnsafeItemMap[WallID.DirtUnsafe] = ItemID.DirtWallUnsafe; // 2 -> 5546
                WallToUnsafeItemMap[WallID.LivingWoodUnsafe] = ItemID.LivingWoodWallUnsafe; // 244 -> 5545

                // 2. 1.4.0 天然洞穴岩石与环境回声墙 (Wall 246~274, 276~303 -> Item 4486~4540)
                // 4486~4503 -> 246~263
                for (int itemId = 4486; itemId <= 4503; itemId++)
                {
                    int wallId = 246 + (itemId - 4486);
                    WallToUnsafeItemMap[wallId] = itemId;
                }
                // 4504~4505 -> 264~265
                WallToUnsafeItemMap[264] = 4504;
                WallToUnsafeItemMap[265] = 4505;
                // 4506~4507 -> 266~267
                WallToUnsafeItemMap[266] = 4506;
                WallToUnsafeItemMap[267] = 4507;
                // 4508 -> 268, 4509 -> 269
                WallToUnsafeItemMap[268] = 4508;
                WallToUnsafeItemMap[269] = 4509;
                // 4510~4511 -> 270~271
                WallToUnsafeItemMap[270] = 4510;
                WallToUnsafeItemMap[271] = 4511;
                // 4512 -> 274
                WallToUnsafeItemMap[274] = 4512;
                // 4513~4540 -> 276~303
                for (int itemId = 4513; itemId <= 4540; itemId++)
                {
                    int wallId = 276 + (itemId - 4513);
                    WallToUnsafeItemMap[wallId] = itemId;
                }

                // 3. 动态扫描 ContentSamples.ItemsByType，自动补充带危险标记 (DrawUnsafeIndicator) 的墙壁物品
                if (ContentSamples.ItemsByType != null && ContentSamples.ItemsByType.Count > 0)
                {
                    foreach (var kvp in ContentSamples.ItemsByType)
                    {
                        Item item = kvp.Value;
                        if (item == null || item.type <= 0 || item.createWall <= 0) continue;

                        if (item.type < ItemID.Sets.DrawUnsafeIndicator.Length && ItemID.Sets.DrawUnsafeIndicator[item.type])
                        {
                            if (!WallToUnsafeItemMap.ContainsKey(item.createWall))
                            {
                                WallToUnsafeItemMap[item.createWall] = item.type;
                            }
                        }
                    }
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// 尝试根据背景墙类型获取对应的危险墙物品 ID
        /// </summary>
        public static bool TryGetUnsafeWallDrop(int wallType, out int dropItemId)
        {
            EnsureInitialized();
            return WallToUnsafeItemMap.TryGetValue(wallType, out dropItemId);
        }

        /// <summary>
        /// 拦截背景墙掉落逻辑：
        /// 开启危险墙掉落时，若该墙为天然危险墙，则掉落对应的危险墙物品并拦截原版掉落；
        /// 否则正常执行原版掉落。
        /// </summary>
        private static void Hook_KillWall_DropItems(On_WorldGen.orig_KillWall_DropItems orig, int i, int j, Tile tileCache)
        {
            if (QoLValSet.unsafeWallDrops.val && tileCache != null)
            {
                if (TryGetUnsafeWallDrop(tileCache.wall, out int dropItemId))
                {
                    Item.NewItem(WorldGen.GetItemSource_FromWallBreak(i, j), new Vector2(i * 16, j * 16), dropItemId);
                    return;
                }
            }

            orig(i, j, tileCache);
        }
    }
}
