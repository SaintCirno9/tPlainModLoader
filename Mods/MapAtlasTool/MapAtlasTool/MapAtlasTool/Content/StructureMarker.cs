using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapAtlasTool.Content.UI;
using tContentPatch;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using TPML.Core.Diagnostics;
using TPML.Core.Logging;

namespace MapAtlasTool.Content
{
    public class StructurePin
    {
        public Vector2 PositionInTiles;
        public string Name;
        public Color Color;
        public string Category;
        public string CategoryLabel;
        public int ItemId;
        public int ChestIndex = -1;
        public bool IsTrapped = false;
        public Func<bool> CheckActive;
    }

    /// <summary>
    /// 搜索态视觉模式: 无搜索 / 命中(高亮) / 未命中(调暗)
    /// </summary>
    internal enum SearchVis
    {
        None,
        Hit,
        Dim,
    }

    /// <summary>
    /// 受困/特殊 NPC 动态标记定义(绘制与搜索共用)
    /// </summary>
    internal struct NpcMarkerInfo
    {
        public int[] NpcTypes;
        public string Name;
        public Color Color;
        public int IconItemId;
    }

    /// <summary>
    /// 箱子搜索结果展示信息
    /// </summary>
    internal struct ChestDisplayInfo
    {
        public string Title;
        public int Icon;
        public string Tooltip;
    }

    /// <summary>
    /// 全图关键结构与全量宝箱雷达标记系统
    /// 作者: SaintCirno9
    /// </summary>
    public class StructureMarker : PatchMain
    {
        private static readonly ILogger Logger = LogManager.GetLogger("MapAtlasTool");
        private static readonly object _pinsLock = new object();
        private static List<StructurePin> _pins = new List<StructurePin>();
        private static StructurePin[] _pinsSnapshot = Array.Empty<StructurePin>();
        private static volatile bool _isScanning = false;
        private static volatile bool _rescanQueued = false;
        private static int _cleanTick = 0;


        internal static readonly NpcMarkerInfo[] NpcMarkerTable = new NpcMarkerInfo[]
        {
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.BoundGoblin }, Name = "受困的哥布林工匠", Color = Color.DodgerBlue, IconItemId = ItemID.TinkerersWorkshop },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.BoundMechanic }, Name = "受困的机械师", Color = Color.HotPink, IconItemId = ItemID.Wrench },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.BoundWizard }, Name = "受困的巫师", Color = Color.MediumPurple, IconItemId = ItemID.CrystalBall },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.WebbedStylist }, Name = "被蛛网缠住的发型师", Color = Color.Pink, IconItemId = ItemID.StylistKilLaKillScissorsIWish },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.BartenderUnconscious }, Name = "昏迷的酒馆老板", Color = Color.SandyBrown, IconItemId = ItemID.Ale },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.GolferRescue }, Name = "受困的高尔夫球手", Color = Color.LightGreen, IconItemId = ItemID.GolfClubIron },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.DemonTaxCollector }, Name = "折磨之魂 (税收官)", Color = Color.IndianRed, IconItemId = ItemID.PurificationPowder },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.LostGirl }, Name = "迷失女孩 (宁芙)", Color = Color.Silver, IconItemId = ItemID.MetalDetector },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.OldMan }, Name = "地牢老人", Color = Color.SkyBlue, IconItemId = ItemID.BoneKey },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.CultistDevote, NPCID.CultistArcherBlue }, Name = "地牢神秘拜月教信徒", Color = Color.DarkCyan, IconItemId = ItemID.CelestialSigil },
            new NpcMarkerInfo { NpcTypes = new int[] { NPCID.BoundTownSlimeOld, NPCID.BoundTownSlimePurple, NPCID.BoundTownSlimeYellow }, Name = "受困的城镇史莱姆", Color = Color.Lime, IconItemId = ItemID.Gel },
        };

        /// <summary>扫描结果快照(搜索侧只读)</summary>
        internal static StructurePin[] GetPinsSnapshot() => _pinsSnapshot;

        private static readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

        /// <summary>把动作调度到主线程下一帧执行（UpdatePostfix 消费）</summary>
        private static void RunOnMainThread(Action action)
        {
            if (action != null)
            {
                _mainThreadActions.Enqueue(action);
            }
        }

        public override void UpdatePostfix(GameTime gameTime)
        {
            while (_mainThreadActions.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Logger.Error("主线程任务异常", ex);
                }
            }
        }

        public override void OnEnterWorld()
        {
            TriggerRescan();
        }

        public override void OnLeaveWorld()
        {
            _rescanQueued = false;
            lock (_pinsLock)
            {
                _pins.Clear();
                _pinsSnapshot = Array.Empty<StructurePin>();
            }
            UI.MapAtlasPanel.ClearSearchState();
            ChestItemIndex.Clear();
        }

        public static void TriggerRescan()
        {
            if (_isScanning)
            {
                _rescanQueued = true;
                return;
            }
            _isScanning = true;
            _rescanQueued = false;

            // ---- 主线程内快照（读取游戏状态，避免后台线程并发读取引发撕裂）----
            int maxX = Main.maxTilesX;
            int maxY = Main.maxTilesY;
            float worldSurface = (float)Main.worldSurface;
            float rockLayer = (float)Main.rockLayer;
            int dungeonX = Main.dungeonX;
            int dungeonY = Main.dungeonY;
            Chest[] chestSnapshot = Main.chest;

            _ = Task.Run(() =>
            {
                try
                {
                    using (new ScopedTimer("StructureMarker.ScanWorldStructures", LogManager.GetLogger("MapAtlasTool"), LogLevel.Info))
                    {
                        ScanWorldStructures(maxX, maxY, worldSurface, rockLayer, dungeonX, dungeonY, chestSnapshot);
                    }
                }
                catch (Exception ex)
                {
                    RunOnMainThread(() => Main.NewText($"[结构标记] 扫描失败: {ex.Message}"));
                }
                finally
                {
                    _isScanning = false;
                    if (_rescanQueued)
                    {
                        _rescanQueued = false;
                        RunOnMainThread(TriggerRescan);
                    }
                }
            });
        }

        private class GridCell
        {
            public int MarbleCount;
            public long MarbleSumX, MarbleSumY;

            public int GraniteCount;
            public long GraniteSumX, GraniteSumY;

            public int SpiderCount;
            public long SpiderSumX, SpiderSumY;

            public int SkyLakeCount;
            public long SkyLakeSumX, SkyLakeSumY;

            public int PyramidCount;
            public long PyramidSumX, PyramidSumY;

            public int ShimmerCount;
            public long ShimmerSumX, ShimmerSumY;

            public int MushroomCount;
            public long MushroomSumX, MushroomSumY;

            public int AntlionCount;
            public long AntlionSumX, AntlionSumY;

            public int MossCount;
            public long MossSumX, MossSumY;

            public int MeteoriteCount;
            public long MeteoriteSumX, MeteoriteSumY;
        }

        private struct ChestStyleInfo
        {
            public readonly string Name;
            public readonly Color Color;
            public readonly string Category;
            public readonly string CategoryLabel;
            public readonly int ItemId;
            public readonly bool IsTrapped;

            public ChestStyleInfo(string name, Color color, string category, string catLabel, int itemId, bool isTrapped = false)
            {
                Name = name;
                Color = color;
                Category = category;
                CategoryLabel = catLabel;
                ItemId = itemId;
                IsTrapped = isTrapped;
            }
        }

        // Containers (tile 21 / TileID.Containers), style = frameX / 36 (0~51)
        private static readonly ChestStyleInfo[] ContainersStyles = new ChestStyleInfo[]
        {
            /* 0 */ new ChestStyleInfo("木质宝箱", Color.Tan, "ChestNormal", "地表/地下宝箱", ItemID.Chest),
            /* 1 */ new ChestStyleInfo("地下黄金宝箱", Color.Gold, "ChestNormal", "地下宝箱", ItemID.GoldChest),
            /* 2 */ new ChestStyleInfo("地牢: 锁住的黄金宝箱", Color.Gold, "ChestNormal", "地牢宝箱", ItemID.GoldenKey),
            /* 3 */ new ChestStyleInfo("地狱废墟: 暗影箱", Color.DarkOrchid, "ChestShadowBiome", "地狱暗影宝藏", ItemID.ShadowChest),
            /* 4 */ new ChestStyleInfo("地狱废墟: 锁住的暗影箱", Color.DarkOrchid, "ChestShadowBiome", "地狱暗影宝藏", ItemID.ShadowKey),
            /* 5 */ new ChestStyleInfo("木桶", Color.SaddleBrown, "ChestNormal", "地下储物容器", ItemID.Barrel),
            /* 6 */ new ChestStyleInfo("垃圾桶", Color.Gray, "ChestNormal", "地下储物容器", ItemID.TrashCan),
            /* 7 */ new ChestStyleInfo("腐化: 乌木宝箱", Color.MediumPurple, "ChestNormal", "腐化宝箱", ItemID.EbonwoodChest),
            /* 8 */ new ChestStyleInfo("丛林: 红木宝箱", Color.IndianRed, "ChestNormal", "丛林宝箱", ItemID.RichMahoganyChest),
            /* 9 */ new ChestStyleInfo("神圣: 珍珠木宝箱", Color.LightYellow, "ChestNormal", "神圣宝箱", ItemID.PearlwoodChest),
            /* 10 */ new ChestStyleInfo("地下丛林: 常春藤宝箱", Color.MediumSeaGreen, "ChestNormal", "地下丛林宝箱", ItemID.IvyChest),
            /* 11 */ new ChestStyleInfo("地下冰雪: 冰雪宝箱", Color.Cyan, "ChestNormal", "地下冰雪宝箱", ItemID.IceChest),
            /* 12 */ new ChestStyleInfo("巨型生命树: 生命木宝箱", Color.LawnGreen, "ChestNormal", "生命树宝箱", ItemID.LivingWoodChest),
            /* 13 */ new ChestStyleInfo("空岛: 天域建筑宝箱", Color.SkyBlue, "ChestNormal", "高空浮岛宝箱", ItemID.SkywareChest),
            /* 14 */ new ChestStyleInfo("猩红: 阴森木宝箱", Color.DimGray, "ChestNormal", "猩红木宝箱", ItemID.ShadewoodChest),
            /* 15 */ new ChestStyleInfo("地下蛛巢: 蛛网宝箱", Color.SlateGray, "ChestNormal", "地下蛛巢宝箱", ItemID.WebCoveredChest),
            /* 16 */ new ChestStyleInfo("丛林神庙: 神庙宝箱", Color.OrangeRed, "ChestShadowBiome", "神庙遗迹宝箱", ItemID.LihzahrdChest),
            /* 17 */ new ChestStyleInfo("沉没宝藏: 水下宝箱", Color.CornflowerBlue, "ChestNormal", "水下宝箱", ItemID.WaterChest),
            /* 18 */ new ChestStyleInfo("地牢: 丛林宝箱", Color.LimeGreen, "ChestShadowBiome", "地牢环境宝箱", 1528),
            /* 19 */ new ChestStyleInfo("地牢: 腐化宝箱", Color.MediumPurple, "ChestShadowBiome", "地牢环境宝箱", 1529),
            /* 20 */ new ChestStyleInfo("地牢: 猩红宝箱", Color.Crimson, "ChestShadowBiome", "地牢环境宝箱", 1530),
            /* 21 */ new ChestStyleInfo("地牢: 神圣宝箱", Color.HotPink, "ChestShadowBiome", "地牢环境宝箱", 1531),
            /* 22 */ new ChestStyleInfo("地牢: 冰霜宝箱", Color.LightCyan, "ChestShadowBiome", "地牢环境宝箱", 1532),
            /* 23 */ new ChestStyleInfo("地牢: 锁住的丛林神器箱", Color.LimeGreen, "ChestShadowBiome", "地牢神器宝箱", ItemID.JungleKey),
            /* 24 */ new ChestStyleInfo("地牢: 锁住的腐化神器箱", Color.MediumPurple, "ChestShadowBiome", "地牢神器宝箱", ItemID.CorruptionKey),
            /* 25 */ new ChestStyleInfo("地牢: 锁住的猩红神器箱", Color.Crimson, "ChestShadowBiome", "地牢神器宝箱", ItemID.CrimsonKey),
            /* 26 */ new ChestStyleInfo("地牢: 锁住的神圣神器箱", Color.HotPink, "ChestShadowBiome", "地牢神器宝箱", ItemID.HallowedKey),
            /* 27 */ new ChestStyleInfo("地牢: 锁住的冰霜神器箱", Color.LightCyan, "ChestShadowBiome", "地牢神器宝箱", ItemID.FrozenKey),
            /* 28 */ new ChestStyleInfo("王朝宝箱", Color.Peru, "ChestNormal", "常规宝箱", 2230),
            /* 29 */ new ChestStyleInfo("地下丛林: 蜂蜜宝箱", Color.Goldenrod, "ChestNormal", "蜂巢宝箱", 2249),
            /* 30 */ new ChestStyleInfo("蒸汽朋克宝箱", Color.Orange, "ChestNormal", "常规宝箱", 2250),
            /* 31 */ new ChestStyleInfo("海洋: 棕榈木宝箱", Color.SandyBrown, "ChestNormal", "海洋宝箱", 2526),
            /* 32 */ new ChestStyleInfo("发光蘑菇地: 蘑菇宝箱", Color.DeepSkyBlue, "ChestNormal", "发光蘑菇宝箱", ItemID.MushroomChest),
            /* 33 */ new ChestStyleInfo("雪原: 针叶木宝箱", Color.CornflowerBlue, "ChestNormal", "雪原宝箱", 2559),
            /* 34 */ new ChestStyleInfo("史莱姆宝箱", Color.RoyalBlue, "ChestNormal", "常规宝箱", 2574),
            /* 35 */ new ChestStyleInfo("地牢: 绿地牢宝箱", Color.ForestGreen, "ChestNormal", "地牢宝箱", 2612),
            /* 36 */ new ChestStyleInfo("地牢: 锁住的绿地牢宝箱", Color.ForestGreen, "ChestNormal", "地牢宝箱", ItemID.GoldenKey),
            /* 37 */ new ChestStyleInfo("地牢: 粉地牢宝箱", Color.HotPink, "ChestNormal", "地牢宝箱", 2613),
            /* 38 */ new ChestStyleInfo("地牢: 锁住的粉地牢宝箱", Color.HotPink, "ChestNormal", "地牢宝箱", ItemID.GoldenKey),
            /* 39 */ new ChestStyleInfo("地牢: 蓝地牢宝箱", Color.DodgerBlue, "ChestNormal", "地牢宝箱", 2614),
            /* 40 */ new ChestStyleInfo("地牢: 锁住的蓝地牢宝箱", Color.DodgerBlue, "ChestNormal", "地牢宝箱", ItemID.GoldenKey),
            /* 41 */ new ChestStyleInfo("地牢: 骨头宝箱", Color.LightGray, "ChestNormal", "地牢宝箱", ItemID.BoneChest),
            /* 42 */ new ChestStyleInfo("沙漠: 仙人掌宝箱", Color.YellowGreen, "ChestNormal", "沙漠宝箱", 2616),
            /* 43 */ new ChestStyleInfo("猩红: 血肉宝箱", Color.Crimson, "ChestNormal", "猩红宝箱", 2617),
            /* 44 */ new ChestStyleInfo("地狱: 黑曜石宝箱", Color.Purple, "ChestNormal", "地狱宝箱", 2618),
            /* 45 */ new ChestStyleInfo("万圣节: 南瓜宝箱", Color.OrangeRed, "ChestNormal", "常规宝箱", 2619),
            /* 46 */ new ChestStyleInfo("万圣节: 阴森宝箱", Color.DarkViolet, "ChestNormal", "常规宝箱", 2620),
            /* 47 */ new ChestStyleInfo("玻璃宝箱", Color.LightBlue, "ChestNormal", "常规宝箱", 2748),
            /* 48 */ new ChestStyleInfo("火星宝箱", Color.MediumTurquoise, "ChestNormal", "常规宝箱", 2814),
            /* 49 */ new ChestStyleInfo("陨石宝箱", Color.Firebrick, "ChestNormal", "陨石宝箱", 3180),
            /* 50 */ new ChestStyleInfo("花岗岩洞: 花岗岩宝箱", Color.CornflowerBlue, "ChestNormal", "花岗岩宝箱", ItemID.GraniteChest),
            /* 51 */ new ChestStyleInfo("大理石洞: 大理石宝箱", Color.GhostWhite, "ChestNormal", "大理石宝箱", ItemID.MarbleChest),
        };

        // Containers2 (tile 467 / TileID.Containers2), style = frameX / 36 (0~37)
        private static readonly ChestStyleInfo[] Containers2Styles = new ChestStyleInfo[]
        {
            /* 0 */ new ChestStyleInfo("水晶宝箱", Color.LightPink, "ChestNormal", "地下宝箱", 3884),
            /* 1 */ new ChestStyleInfo("地下黄金宝箱", Color.Gold, "ChestNormal", "地下宝箱", 3885),
            /* 2 */ new ChestStyleInfo("地下蛛巢: 蜘蛛宝箱", Color.SlateGray, "ChestNormal", "地下蛛巢宝箱", ItemID.SpiderChest),
            /* 3 */ new ChestStyleInfo("腐化: 病变宝箱", Color.Purple, "ChestNormal", "腐化宝箱", ItemID.LesionChest),
            /* 4 */ new ChestStyleInfo("致命陷阱: 死人宝箱！", Color.Red, "ChestTrapped", "死人陷阱宝箱", ItemID.DeadMansChest, isTrapped: true),
            /* 5 */ new ChestStyleInfo("日耀宝箱", Color.OrangeRed, "ChestNormal", "四柱宝箱", 4153),
            /* 6 */ new ChestStyleInfo("漩涡宝箱", Color.Teal, "ChestNormal", "四柱宝箱", 4174),
            /* 7 */ new ChestStyleInfo("星云宝箱", Color.Magenta, "ChestNormal", "四柱宝箱", 4195),
            /* 8 */ new ChestStyleInfo("星尘宝箱", Color.Cyan, "ChestNormal", "四柱宝箱", 4216),
            /* 9 */ new ChestStyleInfo("高尔夫宝箱", Color.LightGreen, "ChestNormal", "常规宝箱", ItemID.GolfChest),
            /* 10 */ new ChestStyleInfo("地下沙漠: 沙漠宝箱", Color.SandyBrown, "ChestNormal", "地下沙漠宝箱", 4267),
            /* 11 */ new ChestStyleInfo("竹宝箱", Color.GreenYellow, "ChestNormal", "常规宝箱", ItemID.BambooChest),
            /* 12 */ new ChestStyleInfo("地牢: 沙漠宝箱", Color.Goldenrod, "ChestShadowBiome", "地牢环境宝箱", 4712),
            /* 13 */ new ChestStyleInfo("地牢: 锁住的沙漠神器箱", Color.Goldenrod, "ChestShadowBiome", "地牢神器宝箱", 4714),
            /* 14 */ new ChestStyleInfo("海洋: 珊瑚宝箱", Color.Coral, "ChestNormal", "海洋宝箱", 5156),
            /* 15 */ new ChestStyleInfo("气球宝箱", Color.HotPink, "ChestNormal", "常规宝箱", 5177),
            /* 16 */ new ChestStyleInfo("地狱: 灰烬木宝箱", Color.DimGray, "ChestNormal", "地狱宝箱", ItemID.AshWoodChest),
            /* 17 */ new ChestStyleInfo("以太微光: 以太宝箱", Color.MediumPurple, "ChestNormal", "以太宝箱", 5556),
            /* 18 */ new ChestStyleInfo("陨星宝箱", Color.Yellow, "ChestNormal", "常规宝箱", 5609),
            /* 19 */ new ChestStyleInfo("仙灵木宝箱", Color.Plum, "ChestNormal", "常规宝箱", 5697),
            /* 20 */ new ChestStyleInfo("神圣家具宝箱", Color.LightGoldenrodYellow, "ChestNormal", "神圣宝箱", 5720),
            /* 21 */ new ChestStyleInfo("哥特宝箱", Color.DarkGray, "ChestNormal", "地牢宝箱", 5745),
            /* 22 */ new ChestStyleInfo("魔金宝箱", Color.MediumPurple, "ChestNormal", "腐化宝箱", 5763),
            /* 23 */ new ChestStyleInfo("猩红矿宝箱", Color.Crimson, "ChestNormal", "猩红宝箱", 5784),
            /* 24 */ new ChestStyleInfo("雪原宝箱", Color.AliceBlue, "ChestNormal", "雪原宝箱", 5805),
            /* 25 */ new ChestStyleInfo("弗林克斯毛宝箱", Color.WhiteSmoke, "ChestNormal", "雪原宝箱", 5826),
            /* 26 */ new ChestStyleInfo("松木宝箱", Color.ForestGreen, "ChestNormal", "常规宝箱", 5846),
            /* 27 */ new ChestStyleInfo("复活节宝箱", Color.LightPink, "ChestNormal", "常规宝箱", 5865),
            /* 28 */ new ChestStyleInfo("石宝箱", Color.Gray, "ChestNormal", "地下宝箱", 5886),
            /* 29 */ new ChestStyleInfo("海洋: 水母宝箱", Color.Aquamarine, "ChestNormal", "海洋宝箱", 5905),
            /* 30 */ new ChestStyleInfo("空岛: 鸟身女妖宝箱", Color.SkyBlue, "ChestNormal", "高空浮岛宝箱", 5939),
            /* 31 */ new ChestStyleInfo("空岛: 云宝箱", Color.White, "ChestNormal", "高空浮岛宝箱", 5962),
            /* 32 */ new ChestStyleInfo("月面宝箱", Color.DarkCyan, "ChestNormal", "常规宝箱", 5982),
            /* 33 */ new ChestStyleInfo("图书管理员宝箱", Color.SandyBrown, "ChestNormal", "常规宝箱", 6005),
            /* 34 */ new ChestStyleInfo("地牢: 尖刺宝箱", Color.DarkSlateGray, "ChestNormal", "地牢宝箱", 6028),
            /* 35 */ new ChestStyleInfo("办公室宝箱", Color.LightSteelBlue, "ChestNormal", "常规宝箱", 6051),
            /* 36 */ new ChestStyleInfo("地下沙漠: 禁忌宝箱", Color.Goldenrod, "ChestNormal", "地下沙漠宝箱", 6074),
            /* 37 */ new ChestStyleInfo("巨石宝箱", Color.SlateGray, "ChestNormal", "地下宝箱", 6118),
        };

        private static ChestStyleInfo GetChestInfo(ushort tileType, int style)
        {
            if (tileType == TileID.Containers)
            {
                if (style >= 0 && style < ContainersStyles.Length)
                {
                    return ContainersStyles[style];
                }
                int dropItem = WorldGen.GetItemDrop_Chests(style, secondType: false);
                if (dropItem <= 0) dropItem = ItemID.Chest;
                return new ChestStyleInfo($"宝箱 (样式 {style})", Color.Tan, "ChestNormal", "常规宝箱", dropItem);
            }
            if (tileType == TileID.Containers2)
            {
                if (style >= 0 && style < Containers2Styles.Length)
                {
                    return Containers2Styles[style];
                }
                int dropItem = WorldGen.GetItemDrop_Chests(style, secondType: true);
                if (dropItem <= 0) dropItem = ItemID.Chest;
                return new ChestStyleInfo($"宝箱2 (样式 {style})", Color.Tan, "ChestNormal", "常规宝箱", dropItem);
            }
            if (tileType == TileID.FakeContainers || tileType == TileID.FakeContainers2)
            {
                return new ChestStyleInfo("伪装陷阱: 陷阱宝箱！", Color.Red, "ChestTrapped", "机关陷阱宝箱", 3625, isTrapped: true);
            }
            return new ChestStyleInfo($"未知宝箱 (Type {tileType})", Color.Tan, "ChestNormal", "常规宝箱", ItemID.Chest);
        }

        private static bool IsTileActiveAndType(Vector2 pos, params ushort[] types)
        {
            int x = (int)pos.X;
            int y = (int)pos.Y;
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return false;
            Tile t = Main.tile[x, y];
            if (t == null || !t.active()) return false;
            for (int i = 0; i < types.Length; i++)
            {
                if (t.type == types[i]) return true;
            }
            return false;
        }

        private static void ScanWorldStructures(int maxX, int maxY, float worldSurface, float rockLayer, int dungeonX, int dungeonY, Chest[] chestSnapshot)
        {
            List<StructurePin> list = new List<StructurePin>();

            if (Main.tile == null || maxX <= 0 || maxY <= 0) return;

            // 1. 地牢主入口探测
            if (dungeonX > 0 && dungeonY > 0)
            {
                list.Add(new StructurePin
                {
                    PositionInTiles = new Vector2(dungeonX, dungeonY),
                    Name = "地牢: 地表主入口",
                    Color = Color.DeepSkyBlue,
                    Category = "DungeonEntrance",
                    CategoryLabel = "地牢主入口",
                    ItemId = ItemID.BoneKey
                });
            }

            // 2. 世界宝箱扫描 (全量宝箱雷达)
            if (chestSnapshot != null)
            {
                for (int i = 0; i < chestSnapshot.Length; i++)
                {
                    Chest c = chestSnapshot[i];
                    if (c == null || c.x <= 0 || c.x >= maxX || c.y <= 0 || c.y >= maxY) continue;

                    Tile t = Main.tile[c.x, c.y];
                    if (t == null || !t.active()) continue;

                    if (t.type != TileID.Containers && t.type != TileID.Containers2 &&
                        t.type != TileID.FakeContainers && t.type != TileID.FakeContainers2)
                    {
                        continue;
                    }

                    Vector2 chestPos = new Vector2(c.x, c.y);
                    int style = t.frameX / 36;
                    ChestStyleInfo info = GetChestInfo(t.type, style);

                    list.Add(new StructurePin
                    {
                        PositionInTiles = chestPos,
                        Name = info.Name,
                        Color = info.Color,
                        Category = info.Category,
                        CategoryLabel = info.CategoryLabel,
                        ItemId = info.ItemId,
                        ChestIndex = i,
                        IsTrapped = info.IsTrapped,
                        CheckActive = () => IsTileActiveAndType(chestPos, TileID.Containers, TileID.Containers2, TileID.FakeContainers, TileID.FakeContainers2)
                    });
                }
            }

            // 3. 空间统计网格初始化 (CellSize = 30)
            const int cellSize = 30;
            int gridW = (maxX + cellSize - 1) / cellSize;
            int gridH = (maxY + cellSize - 1) / cellSize;
            GridCell[] grid = new GridCell[gridW * gridH];

            // 4. 单次遍历全图图格进行精确匹配与空间聚类统计
            for (int x = 10; x < maxX - 10; x++)
            {
                int gx = x / cellSize;
                for (int y = 10; y < maxY - 10; y++)
                {
                    Tile tile = Main.tile[x, y];
                    int gy = y / cellSize;
                    int gIndex = gy * gridW + gx;

                    // 4.1 世纪之花花苞
                    if (tile.active() && tile.type == TileID.PlanteraBulb)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "世纪之花花苞",
                                Color = Color.Magenta,
                                Category = "Plantera",
                                CategoryLabel = "关键首领生成物",
                                ItemId = ItemID.PlanteraBossBag,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.PlanteraBulb)
                            });
                        }
                    }

                    // 4.2 附魔剑冢
                    if (tile.active() && tile.type == TileID.LargePiles2)
                    {
                        if (tile.frameX >= 17 * 18 && tile.frameX <= 19 * 18 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "附魔剑冢",
                                Color = Color.Cyan,
                                Category = "SwordShrine",
                                CategoryLabel = "珍贵自然生成物",
                                ItemId = ItemID.EnchantedSword,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.LargePiles2)
                            });
                        }
                    }

                    // 4.3 蜂巢幼虫
                    if (tile.active() && tile.type == TileID.Larva)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "蜂巢幼虫",
                                Color = Color.Gold,
                                Category = "Larva",
                                CategoryLabel = "首领生成物",
                                ItemId = ItemID.Abeemination,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.Larva)
                            });
                        }
                    }

                    // 4.4 丛林神庙祭坛
                    if (tile.active() && tile.type == TileID.LihzahrdAltar)
                    {
                        if (tile.frameX == 0 && tile.frameY == 0)
                        {
                            Vector2 pos = new Vector2(x, y);
                            list.Add(new StructurePin
                            {
                                PositionInTiles = pos,
                                Name = "丛林神庙石巨人祭坛",
                                Color = Color.OrangeRed,
                                Category = "TempleAltar",
                                CategoryLabel = "核心遗迹祭坛",
                                ItemId = ItemID.LihzahrdPowerCell,
                                CheckActive = () => IsTileActiveAndType(pos, TileID.LihzahrdAltar)
                            });
                        }
                    }

                    // 4.5 丛林神庙大门 (Locked Lihzahrd Door, ClosedDoor style 11; 仅取左列子格避免重复 pin)
                    if (tile.active() && tile.type == TileID.ClosedDoor && tile.frameY / 54 == 11 && tile.frameY % 54 == 0 && tile.frameX % 36 == 0)
                    {
                        Vector2 pos = new Vector2(x, y);
                        list.Add(new StructurePin
                        {
                            PositionInTiles = pos,
                            Name = "丛林神庙大门",
                            Color = Color.SandyBrown,
                            Category = "TempleDoor",
                            CategoryLabel = "神庙遗迹入口",
                            ItemId = ItemID.TempleKey,
                            CheckActive = () => IsTileActiveAndType(pos, TileID.ClosedDoor, TileID.OpenDoor)
                        });
                    }

                    // 4.6 恶魔祭坛 / 猩红祭坛 (Demon Altar / Crimson Altar)
                    if (tile.active() && tile.type == TileID.DemonAltar && tile.frameX % 54 == 0 && tile.frameY == 0)
                    {
                        bool isCrimson = tile.frameX >= 54;
                        Vector2 pos = new Vector2(x, y);
                        list.Add(new StructurePin
                        {
                            PositionInTiles = pos,
                            Name = isCrimson ? "猩红祭坛" : "恶魔祭坛",
                            Color = isCrimson ? Color.Crimson : Color.MediumPurple,
                            Category = "EvilAltars",
                            CategoryLabel = "邪恶生物祭坛",
                            ItemId = isCrimson ? ItemID.CrimtaneOre : ItemID.DemoniteOre,
                            CheckActive = () => IsTileActiveAndType(pos, TileID.DemonAltar)
                        });
                    }

                    // 4.7 暗影珠 / 猩红之心 (Shadow Orb / Crimson Heart)
                    if (tile.active() && tile.type == TileID.ShadowOrbs && tile.frameX % 36 == 0 && tile.frameY == 0)
                    {
                        bool isHeart = tile.frameX >= 36;
                        Vector2 pos = new Vector2(x, y);
                        list.Add(new StructurePin
                        {
                            PositionInTiles = pos,
                            Name = isHeart ? "猩红之心" : "暗影珠",
                            Color = isHeart ? Color.Crimson : Color.DarkViolet,
                            Category = "EvilOrbsHearts",
                            CategoryLabel = "邪恶生物核心",
                            ItemId = isHeart ? ItemID.CrimsonHeart : ItemID.ShadowOrb,
                            CheckActive = () => IsTileActiveAndType(pos, TileID.ShadowOrbs)
                        });
                    }

                    // 4.8 地牢水之书藏书 (Water Bolt on Dungeon Bookshelf)
                    // 原版约定: 水之书书架固定 frameX == 90 (见 WorldGen.GenerateDungeonBook 与 WorldGen 掉落逻辑 case 50)
                    if (tile.active() && tile.type == TileID.Books && tile.frameX == 90)
                    {
                        Vector2 pos = new Vector2(x, y);
                        list.Add(new StructurePin
                        {
                            PositionInTiles = pos,
                            Name = "地牢藏书: 《水之书》",
                            Color = Color.DodgerBlue,
                            Category = "DungeonWaterBolt",
                            CategoryLabel = "地牢魔法藏书",
                            ItemId = ItemID.WaterBolt,
                            CheckActive = () => IsTileActiveAndType(pos, TileID.Books)
                        });
                    }

                    // 4.9 炸药陷阱 (Explosives Trap)
                    if (tile.active() && tile.type == TileID.Explosives && tile.frameX == 0 && tile.frameY == 0)
                    {
                        Vector2 pos = new Vector2(x, y);
                        list.Add(new StructurePin
                        {
                            PositionInTiles = pos,
                            Name = "危险机关: 炸药陷阱",
                            Color = Color.OrangeRed,
                            Category = "Traps",
                            CategoryLabel = "地下致命陷阱",
                            ItemId = ItemID.Explosives,
                            CheckActive = () => IsTileActiveAndType(pos, TileID.Explosives)
                        });
                    }

                    // 4.10 微光液体统计
                    if (tile.liquidType() == LiquidID.Shimmer && tile.liquid > 150)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].ShimmerCount++;
                        grid[gIndex].ShimmerSumX += x;
                        grid[gIndex].ShimmerSumY += y;
                    }

                    // 4.11 沙漠金字塔砖块 (放宽至岩石层以上，兼容深埋金字塔)
                    if (tile.active() && tile.type == TileID.SandstoneBrick && y < rockLayer)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].PyramidCount++;
                        grid[gIndex].PyramidSumX += x;
                        grid[gIndex].PyramidSumY += y;
                    }

                    // 4.12 高空天湖水池统计 (高空且包含大量水)
                    if (y < worldSurface * 0.38f && tile.liquid > 180 && tile.liquidType() == LiquidID.Water)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].SkyLakeCount++;
                        grid[gIndex].SkyLakeSumX += x;
                        grid[gIndex].SkyLakeSumY += y;
                    }

                    // 4.13 地下大理石群落
                    if (tile.active() && (tile.type == TileID.Marble || tile.type == TileID.MarbleBlock) && y > worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].MarbleCount++;
                        grid[gIndex].MarbleSumX += x;
                        grid[gIndex].MarbleSumY += y;
                    }

                    // 4.14 地下花岗岩群落
                    if (tile.active() && (tile.type == TileID.Granite || tile.type == TileID.GraniteBlock) && y > worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].GraniteCount++;
                        grid[gIndex].GraniteSumX += x;
                        grid[gIndex].GraniteSumY += y;
                    }

                    // 4.15 地下蛛巢墙体
                    if (tile.wall == WallID.SpiderUnsafe && y > worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].SpiderCount++;
                        grid[gIndex].SpiderSumX += x;
                        grid[gIndex].SpiderSumY += y;
                    }

                    // 4.16 地下发光蘑菇地
                    if (tile.active() && (tile.type == TileID.MushroomGrass || tile.type == TileID.MushroomBlock) && y > worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].MushroomCount++;
                        grid[gIndex].MushroomSumX += x;
                        grid[gIndex].MushroomSumY += y;
                    }

                    // 4.17 地下沙漠蚁穴群落
                    if (tile.active() && tile.type == TileID.Sandstone && y > worldSurface && y < rockLayer + 200)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].AntlionCount++;
                        grid[gIndex].AntlionSumX += x;
                        grid[gIndex].AntlionSumY += y;
                    }

                    // 4.18 地下发光苔藓洞
                    if (tile.active() && (tile.type == TileID.ArgonMoss || tile.type == TileID.KryptonMoss || tile.type == TileID.XenonMoss || tile.type == TileID.VioletMoss || tile.type == TileID.RainbowMoss) && y > worldSurface)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].MossCount++;
                        grid[gIndex].MossSumX += x;
                        grid[gIndex].MossSumY += y;
                    }

                    // 4.19 陨石撞击坑
                    if (tile.active() && tile.type == TileID.Meteorite)
                    {
                        if (grid[gIndex] == null) grid[gIndex] = new GridCell();
                        grid[gIndex].MeteoriteCount++;
                        grid[gIndex].MeteoriteSumX += x;
                        grid[gIndex].MeteoriteSumY += y;
                    }
                }
            }

            // 5. 空间连通区域聚类与质心提取
            ClusterFeature(grid, gridW, gridH,
                cell => cell?.ShimmerCount ?? 0,
                cell => cell.ShimmerSumX,
                cell => cell.ShimmerSumY,
                minCellCount: 5, minTotalCount: 20,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "以太微光湖",
                        Color = Color.MediumPurple,
                        Category = "Shimmer",
                        CategoryLabel = "特殊液体群落",
                        ItemId = ItemID.BottomlessShimmerBucket
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.PyramidCount ?? 0,
                cell => cell.PyramidSumX,
                cell => cell.PyramidSumY,
                minCellCount: 15, minTotalCount: 80,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "沙漠金字塔",
                        Color = Color.SandyBrown,
                        Category = "Pyramid",
                        CategoryLabel = "地表沙漠遗迹",
                        ItemId = ItemID.FlyingCarpet
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.SkyLakeCount ?? 0,
                cell => cell.SkyLakeSumX,
                cell => cell.SkyLakeSumY,
                minCellCount: 6, minTotalCount: 30,
                (centroid, count) =>
                {
                    if (!HasNearbyPin(list, centroid, 80f, "FloatingIsland"))
                    {
                        list.Add(new StructurePin
                        {
                            PositionInTiles = centroid,
                            Name = "空岛: 高空天湖",
                            Color = Color.DeepSkyBlue,
                            Category = "FloatingIsland",
                            CategoryLabel = "高空浮岛遗迹",
                            ItemId = ItemID.BottomlessBucket
                        });
                    }
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.MarbleCount ?? 0,
                cell => cell.MarbleSumX,
                cell => cell.MarbleSumY,
                minCellCount: 8, minTotalCount: 70,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "地下大理石洞",
                        Color = Color.GhostWhite,
                        Category = "MiniBiomes",
                        CategoryLabel = "地下微群落",
                        ItemId = ItemID.MarbleBlock
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.GraniteCount ?? 0,
                cell => cell.GraniteSumX,
                cell => cell.GraniteSumY,
                minCellCount: 8, minTotalCount: 70,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "地下花岗岩洞",
                        Color = Color.CornflowerBlue,
                        Category = "MiniBiomes",
                        CategoryLabel = "地下微群落",
                        ItemId = ItemID.GraniteBlock
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.SpiderCount ?? 0,
                cell => cell.SpiderSumX,
                cell => cell.SpiderSumY,
                minCellCount: 10, minTotalCount: 60,
                (centroid, count) =>
                {
                    if (!HasNearbyPin(list, centroid, 60f, "MiniBiomes"))
                    {
                        list.Add(new StructurePin
                        {
                            PositionInTiles = centroid,
                            Name = "地下蛛巢",
                            Color = Color.SlateGray,
                            Category = "MiniBiomes",
                            CategoryLabel = "地下微群落",
                            ItemId = ItemID.Cobweb
                        });
                    }
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.MushroomCount ?? 0,
                cell => cell.MushroomSumX,
                cell => cell.MushroomSumY,
                minCellCount: 10, minTotalCount: 80,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "地下发光蘑菇地",
                        Color = Color.RoyalBlue,
                        Category = "MushroomBiome",
                        CategoryLabel = "发光蘑菇群落",
                        ItemId = ItemID.GlowingMushroom
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.AntlionCount ?? 0,
                cell => cell.AntlionSumX,
                cell => cell.AntlionSumY,
                minCellCount: 15, minTotalCount: 100,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "地下沙漠蚁穴群落",
                        Color = Color.Goldenrod,
                        Category = "AntlionHive",
                        CategoryLabel = "沙漠地下微群落",
                        ItemId = ItemID.AntlionMandible
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.MossCount ?? 0,
                cell => cell.MossSumX,
                cell => cell.MossSumY,
                minCellCount: 6, minTotalCount: 25,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "地下发光苔藓洞",
                        Color = Color.MediumSpringGreen,
                        Category = "MossCaves",
                        CategoryLabel = "稀有发光苔藓群落",
                        ItemId = ItemID.ArgonMoss
                    });
                });

            ClusterFeature(grid, gridW, gridH,
                cell => cell?.MeteoriteCount ?? 0,
                cell => cell.MeteoriteSumX,
                cell => cell.MeteoriteSumY,
                minCellCount: 5, minTotalCount: 20,
                (centroid, count) =>
                {
                    list.Add(new StructurePin
                    {
                        PositionInTiles = centroid,
                        Name = "陨石撞击坑",
                        Color = Color.Firebrick,
                        Category = "Meteorite",
                        CategoryLabel = "天外陨石矿藏",
                        ItemId = ItemID.Meteorite
                    });
                });

            lock (_pinsLock)
            {
                _pins = list;
                _pinsSnapshot = list.ToArray();
            }

            // 构建箱子物品索引(同在后台线程, 供面板搜索与 N/M 统计)
            ChestItemIndex.Build(chestSnapshot);

            int totalPins = _pinsSnapshot.Length;
            RunOnMainThread(() =>
            {
                Main.NewText($"[结构与宝箱标记] 扫描完成，共索引 {totalPins} 处世界结构与宝箱！");
                MapAtlasPanel.RefreshStatsOnMainThread();
            });
        }

        private static bool HasNearbyPin(List<StructurePin> list, Vector2 pos, float maxDistance, string category)
        {
            float maxDistSq = maxDistance * maxDistance;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Category == category && Vector2.DistanceSquared(list[i].PositionInTiles, pos) <= maxDistSq)
                {
                    return true;
                }
            }
            return false;
        }

        private static void ClusterFeature(GridCell[] grid, int gridW, int gridH,
            Func<GridCell, int> getCount, Func<GridCell, long> getSumX, Func<GridCell, long> getSumY,
            int minCellCount, int minTotalCount, Action<Vector2, int> onClusterFound)
        {
            bool[] visited = new bool[gridW * gridH];
            Queue<Point> queue = new Queue<Point>();

            for (int y = 0; y < gridH; y++)
            {
                for (int x = 0; x < gridW; x++)
                {
                    int index = y * gridW + x;
                    if (visited[index]) continue;

                    GridCell c = grid[index];
                    int count = getCount(c);
                    if (count < minCellCount) continue;

                    int totalCount = 0;
                    long totalSumX = 0;
                    long totalSumY = 0;

                    visited[index] = true;
                    queue.Enqueue(new Point(x, y));

                    while (queue.Count > 0)
                    {
                        Point p = queue.Dequeue();
                        int currIndex = p.Y * gridW + p.X;
                        GridCell curr = grid[currIndex];

                        int cellCount = getCount(curr);
                        totalCount += cellCount;
                        totalSumX += getSumX(curr);
                        totalSumY += getSumY(curr);

                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = p.X + dx;
                                int ny = p.Y + dy;
                                if (nx < 0 || nx >= gridW || ny < 0 || ny >= gridH) continue;

                                int nIndex = ny * gridW + nx;
                                if (visited[nIndex]) continue;

                                GridCell nextCell = grid[nIndex];
                                if (getCount(nextCell) >= minCellCount)
                                {
                                    visited[nIndex] = true;
                                    queue.Enqueue(new Point(nx, ny));
                                }
                            }
                        }
                    }

                    if (totalCount >= minTotalCount)
                    {
                        Vector2 centroid = new Vector2((float)totalSumX / totalCount, (float)totalSumY / totalCount);
                        onClusterFound(centroid, totalCount);
                    }
                }
            }
        }

        private static bool IsCategoryEnabled(StructurePin pin)
        {
            switch (pin.Category)
            {
                case "ChestNormal":
                    return AtlasValSet.markChestsAll.val && AtlasValSet.markChestsSurfaceUnderground.val;
                case "ChestShadowBiome":
                    return AtlasValSet.markChestsAll.val && AtlasValSet.markChestsShadowBiome.val;
                case "ChestTrapped":
                    return AtlasValSet.markChestsTrapped.val;
                case "Plantera":
                    return AtlasValSet.markPlanteraBulb.val;
                case "SwordShrine":
                    return AtlasValSet.markSwordShrine.val;
                case "Larva":
                    return AtlasValSet.markBeeHive.val;
                case "TempleAltar":
                    return AtlasValSet.markTempleAltar.val;
                case "TempleDoor":
                    return AtlasValSet.markTempleDoor.val;
                case "EvilAltars":
                    return AtlasValSet.markEvilAltars.val;
                case "EvilOrbsHearts":
                    return AtlasValSet.markEvilOrbsHearts.val;
                case "DungeonEntrance":
                    return AtlasValSet.markDungeon.val;
                case "DungeonWaterBolt":
                    return AtlasValSet.markDungeonWaterBolt.val;
                case "Traps":
                    return AtlasValSet.markTrapsExplosives.val;
                case "Shimmer":
                    return AtlasValSet.markShimmer.val;
                case "Pyramid":
                    return AtlasValSet.markPyramid.val;
                case "FloatingIsland":
                    return AtlasValSet.markFloatingIsland.val;
                case "MiniBiomes":
                    return AtlasValSet.markMiniBiomes.val;
                case "MushroomBiome":
                    return AtlasValSet.markMushroomBiome.val;
                case "AntlionHive":
                    return AtlasValSet.markAntlionHive.val;
                case "MossCaves":
                    return AtlasValSet.markMossCaves.val;
                case "Meteorite":
                    return AtlasValSet.markMeteorite.val;
                case "TrappedNPC":
                    return AtlasValSet.markTrappedNPCs.val;
                default:
                    return true;
            }
        }

        private static bool ShouldDrawPin(StructurePin pin, out string tooltip, bool buildTooltip = true, bool ignoreToggles = false)
        {
            tooltip = null;
            if (!ignoreToggles && !IsCategoryEnabled(pin)) return false;

            if (pin.ChestIndex >= 0)
            {
                if (pin.ChestIndex >= Main.chest.Length) return false;
                Chest c = Main.chest[pin.ChestIndex];
                if (c == null) return false;

                int validItemsCount = CountValidItems(c, out List<string> itemLines);

                bool isEmpty = (validItemsCount == 0);
                if (isEmpty && !ignoreToggles && !AtlasValSet.markChestsShowEmpty.val)
                {
                    return false;
                }

                if (buildTooltip)
                {
                    tooltip = BuildChestContentTooltip(pin.Name, pin.CategoryLabel, pin.PositionInTiles, c, validItemsCount, itemLines);
                }
                return true;
            }

            if (buildTooltip)
            {
                tooltip = BuildBasicTooltip(pin.Name, pin.CategoryLabel, pin.PositionInTiles);
            }
            return true;
        }

        private static int CountValidItems(Chest c, out List<string> itemLines)
        {
            itemLines = new List<string>();
            int validItemsCount = 0;
            for (int j = 0; j < c.item.Length; j++)
            {
                Item it = c.item[j];
                if (it != null && !it.IsAir && it.stack > 0)
                {
                    validItemsCount++;
                    if (itemLines.Count < 7)
                    {
                        string itName = it.AffixName();
                        if (it.stack > 1)
                            itemLines.Add($" • {itName} x{it.stack}");
                        else
                            itemLines.Add($" • {itName}");
                    }
                }
            }
            return validItemsCount;
        }

        internal static string BuildBasicTooltip(string name, string catLabel, Vector2 pos)
        {
            return $"[c/FFE45E:{name}]\n类型: {catLabel}\n坐标: [X: {(int)pos.X}, Y: {(int)pos.Y}]";
        }

        private static string BuildChestContentTooltip(string typeName, string catLabel, Vector2 pos, Chest c, int validItemsCount, List<string> itemLines)
        {
            string chestTitle = string.IsNullOrEmpty(c.name) ? typeName : $"{typeName} (\"{c.name}\")";
            string contentSummary;
            if (validItemsCount == 0)
            {
                contentSummary = "[c/888888:(已清空/空箱)]";
            }
            else
            {
                contentSummary = string.Join("\n", itemLines);
                if (validItemsCount > itemLines.Count)
                {
                    contentSummary += $"\n[c/AAAAAA:...等共 {validItemsCount} 件物品]";
                }
            }

            return $"[c/FFE45E:{chestTitle}]\n类型: {catLabel}\n坐标: [X: {(int)pos.X}, Y: {(int)pos.Y}]\n{contentSummary}";
        }

        internal static string BuildChestContentTooltip(string typeName, string catLabel, Vector2 pos, Chest c)
        {
            int validItemsCount = CountValidItems(c, out List<string> itemLines);
            return BuildChestContentTooltip(typeName, catLabel, pos, c, validItemsCount, itemLines);
        }

        /// <summary>搜索是否命中该图钉(无搜索时视为全部命中; 搜索权威, 无视分类开关)</summary>
        internal static bool IsSearchHit(StructurePin pin)
        {
            if (!MapAtlasPanel.HasActiveQuery) return true;
            if (pin.ChestIndex >= 0) return MapAtlasPanel.HitChestIndexes.Contains(pin.ChestIndex);
            return MapAtlasPanel.HitTexts.Contains(pin.Name);
        }

        /// <summary>面板结果列表: 图钉详情 tooltip</summary>
        internal static string BuildSearchTooltip(StructurePin pin)
        {
            if (pin.ChestIndex >= 0 && pin.ChestIndex < Main.chest.Length && Main.chest[pin.ChestIndex] != null)
            {
                return BuildChestContentTooltip(pin.Name, pin.CategoryLabel, pin.PositionInTiles, Main.chest[pin.ChestIndex]);
            }
            return BuildBasicTooltip(pin.Name, pin.CategoryLabel, pin.PositionInTiles);
        }

        /// <summary>面板结果列表: 箱子条目展示信息(标题/图标/内容 tooltip)</summary>
        internal static ChestDisplayInfo BuildChestSearchInfo(int chestIndex, string matchText)
        {
            Chest c = Main.chest[chestIndex];
            if (c == null) return default;

            string typeName = $"宝箱 #{chestIndex}";
            int icon = ItemID.Chest;
            Tile t = Main.tile[c.x, c.y];
            if (t != null && t.active())
            {
                ChestStyleInfo info = GetChestInfo(t.type, t.frameX / 36);
                typeName = info.Name;
                icon = info.ItemId;
            }

            string chestTitle = string.IsNullOrEmpty(c.name) ? typeName : $"{typeName} (\"{c.name}\")";
            if (!string.IsNullOrEmpty(matchText))
            {
                chestTitle += $"  [c/9BE87C:命中: {matchText}]";
            }

            return new ChestDisplayInfo
            {
                Title = chestTitle,
                Icon = icon,
                Tooltip = BuildChestContentTooltip(typeName, "宝箱雷达", new Vector2(c.x, c.y), c),
            };
        }

        public override void DrawMapPostfix(GameTime gameTime)
        {
            bool searching = MapAtlasPanel.HasActiveQuery;
            if (!searching && !AtlasValSet.IsAnyMarkerEnabled) return;
            if (!Main.mapEnabled || !Main.mapReady) return;

            // 动态生命周期清理：每隔 60 帧对标记存活状态进行一次检查
            _cleanTick++;
            if (_cleanTick % 60 == 0)
            {
                lock (_pinsLock)
                {
                    int removed = _pins.RemoveAll(p => p.CheckActive != null && !p.CheckActive());
                    if (removed > 0)
                    {
                        _pinsSnapshot = _pins.ToArray();
                    }
                }
            }

            string hoveredTooltip = null;

            // 1. 绘制静态扫描缓存的结构与宝箱
            //    搜索态分两轮: 先画未命中(调暗, 仍受分类开关过滤), 再画命中(高亮置顶, 无视分类开关)
            StructurePin[] currentPins = _pinsSnapshot;
            if (currentPins != null && currentPins.Length > 0)
            {
                if (searching)
                {
                    for (int i = 0; i < currentPins.Length; i++)
                    {
                        StructurePin pin = currentPins[i];
                        if (pin == null || IsSearchHit(pin)) continue;

                        // 视口预裁剪: 视野外的图钉跳过开关判定与战利品扫描 (上千图钉时的性能关键路径)
                        if (!IsPositionInViewport(pin.PositionInTiles)) continue;
                        if (!ShouldDrawPin(pin, out _, buildTooltip: false)) continue;

                        RenderSinglePinOnCurrentMap(pin, null, ref hoveredTooltip, SearchVis.Dim);
                    }
                }

                for (int i = 0; i < currentPins.Length; i++)
                {
                    StructurePin pin = currentPins[i];
                    if (pin == null) continue;
                    if (searching && !IsSearchHit(pin)) continue;

                    if (!IsPositionInViewport(pin.PositionInTiles)) continue;
                    if (!ShouldDrawPin(pin, out string pinTooltip, buildTooltip: true, ignoreToggles: searching)) continue;

                    RenderSinglePinOnCurrentMap(pin, pinTooltip, ref hoveredTooltip, searching ? SearchVis.Hit : SearchVis.None);
                }
            }

            // 2. 绘制动态 NPC 实时标记 (受困NPC/老人/邪教徒等)
            DrawDynamicNPCs(ref hoveredTooltip, searching);

            if (hoveredTooltip != null)
            {
                Main.instance.MouseText(hoveredTooltip);
            }
        }

        private static void DrawDynamicNPCs(ref string hoveredTooltip, bool searching)
        {
            if (!AtlasValSet.markTrappedNPCs.val) return;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc == null || !npc.active) continue;

                NpcMarkerInfo info = default;
                bool matched = false;
                for (int t = 0; t < NpcMarkerTable.Length; t++)
                {
                    int[] types = NpcMarkerTable[t].NpcTypes;
                    for (int k = 0; k < types.Length; k++)
                    {
                        if (npc.type == types[k])
                        {
                            info = NpcMarkerTable[t];
                            matched = true;
                            break;
                        }
                    }
                    if (matched) break;
                }

                if (!matched) continue;

                Vector2 npcPosInTiles = npc.Center / 16f;
                if (!IsPositionInViewport(npcPosInTiles)) continue;

                // 搜索权威: 命中的 NPC 无视开关高亮, 未命中调暗(整体仍受 markTrappedNPCs 开关控制)
                SearchVis vis = SearchVis.None;
                if (searching)
                {
                    vis = MapAtlasPanel.HitTexts.Contains(info.Name) ? SearchVis.Hit : SearchVis.Dim;
                }

                StructurePin pin = new StructurePin
                {
                    PositionInTiles = npcPosInTiles,
                    Name = info.Name,
                    Color = info.Color,
                    Category = "TrappedNPC",
                    CategoryLabel = "受困/特殊NPC",
                    ItemId = info.IconItemId
                };

                string tooltip = vis == SearchVis.Dim ? null : BuildBasicTooltip(pin.Name, pin.CategoryLabel, pin.PositionInTiles);
                RenderSinglePinOnCurrentMap(pin, tooltip, ref hoveredTooltip, vis);
            }
        }

        /// <summary>
        /// 视口预裁剪: 判断图钉 Tile 坐标是否落在当前活动地图 (全屏大地图/右上角小地图/中央覆盖地图) 视野内。
        /// 只做廉价几何判断, 不构建任何字符串, 用于在战利品扫描与 Tooltip 构建之前剔除视野外图钉。
        /// </summary>
        private static bool IsPositionInViewport(Vector2 posInTiles)
        {
            if (Main.mapFullscreen)
            {
                Vector2 centerPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                Vector2 screenTopLeft = (Main.mapFullscreenPos * Main.mapFullscreenScale - centerPos) / Main.mapFullscreenScale;
                Vector2 screenBottomRight = screenTopLeft + new Vector2(Main.screenWidth, Main.screenHeight) / Main.mapFullscreenScale;

                const float buffer = 10f;
                return posInTiles.X >= screenTopLeft.X - buffer && posInTiles.X <= screenBottomRight.X + buffer &&
                       posInTiles.Y >= screenTopLeft.Y - buffer && posInTiles.Y <= screenBottomRight.Y + buffer;
            }

            if (Main.mapStyle == 1)
            {
                Vector2 worldCenter = Main.screenPosition;
                worldCenter.X += Main.screenWidth / 2f;
                worldCenter.Y += Main.screenHeight / 2f;

                Vector2 offset = posInTiles * 16f - worldCenter;
                Vector2 drawPos = new Vector2(Main.miniMapX + Main.miniMapWidth / 2f, Main.miniMapY + Main.miniMapHeight / 2f);
                drawPos += (offset / 16f) * Main.mapMinimapScale;

                return drawPos.X > Main.miniMapX + 4 &&
                       drawPos.X < Main.miniMapX + Main.miniMapWidth - 4 &&
                       drawPos.Y > Main.miniMapY + 4 &&
                       drawPos.Y < Main.miniMapY + Main.miniMapHeight - 4;
            }

            if (Main.mapStyle == 2)
            {
                Vector2 worldCenter = Main.screenPosition;
                worldCenter.X += Main.screenWidth / 2f;
                worldCenter.Y += Main.screenHeight / 2f;

                Vector2 offset = posInTiles * 16f - worldCenter;
                Vector2 drawPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                drawPos += (offset / 16f) * Main.mapOverlayScale;

                return drawPos.X > -20 && drawPos.X < Main.screenWidth + 20 &&
                       drawPos.Y > -20 && drawPos.Y < Main.screenHeight + 20;
            }

            return true;
        }

        private static void RenderSinglePinOnCurrentMap(StructurePin pin, string pinTooltip, ref string hoveredTooltip, SearchVis vis = SearchVis.None)
        {
            // 1. 全屏大地图
            if (Main.mapFullscreen)
            {
                Vector2 centerPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                Vector2 screenTopLeft = (Main.mapFullscreenPos * Main.mapFullscreenScale - centerPos) / Main.mapFullscreenScale;
                Vector2 screenBottomRight = screenTopLeft + new Vector2(Main.screenWidth, Main.screenHeight) / Main.mapFullscreenScale;

                float buffer = 10f;
                if (pin.PositionInTiles.X < screenTopLeft.X - buffer || pin.PositionInTiles.X > screenBottomRight.X + buffer ||
                    pin.PositionInTiles.Y < screenTopLeft.Y - buffer || pin.PositionInTiles.Y > screenBottomRight.Y + buffer)
                {
                    return; // 视口外快速裁剪
                }

                Vector2 drawPos = centerPos - Main.mapFullscreenPos * Main.mapFullscreenScale;
                drawPos += pin.PositionInTiles * Main.mapFullscreenScale;

                bool isHovered = IsMouseHovering(drawPos, 14f * Main.UIScale);
                DrawPinMarker(drawPos, pin, 1.25f, isHovered, vis);

                if (isHovered && pinTooltip != null && hoveredTooltip == null)
                {
                    hoveredTooltip = pinTooltip;
                }
            }
            // 2. 右上角小地图
            else if (Main.mapStyle == 1)
            {
                Vector2 worldCenter = Main.screenPosition;
                worldCenter.X += Main.screenWidth / 2f;
                worldCenter.Y += Main.screenHeight / 2f;

                Vector2 offset = pin.PositionInTiles * 16f - worldCenter;
                Vector2 drawPos = new Vector2(Main.miniMapX + Main.miniMapWidth / 2f, Main.miniMapY + Main.miniMapHeight / 2f);
                drawPos += (offset / 16f) * Main.mapMinimapScale;

                if (drawPos.X > Main.miniMapX + 4 &&
                    drawPos.X < Main.miniMapX + Main.miniMapWidth - 4 &&
                    drawPos.Y > Main.miniMapY + 4 &&
                    drawPos.Y < Main.miniMapY + Main.miniMapHeight - 4)
                {
                    float scale = (Main.mapMinimapScale * 0.25f * 2f + 1f) / 3f;
                    if (scale > 1f) scale = 1f;

                    bool isHovered = IsMouseHovering(drawPos, 10f);
                    DrawPinMarker(drawPos, pin, scale, isHovered, vis);

                    if (isHovered && pinTooltip != null && hoveredTooltip == null)
                    {
                        hoveredTooltip = pinTooltip;
                    }
                }
            }
            // 3. 屏幕中央半透明覆盖地图
            else if (Main.mapStyle == 2)
            {
                Vector2 worldCenter = Main.screenPosition;
                worldCenter.X += Main.screenWidth / 2f;
                worldCenter.Y += Main.screenHeight / 2f;

                Vector2 offset = pin.PositionInTiles * 16f - worldCenter;
                Vector2 drawPos = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
                drawPos += (offset / 16f) * Main.mapOverlayScale;

                if (drawPos.X > -20 && drawPos.X < Main.screenWidth + 20 &&
                    drawPos.Y > -20 && drawPos.Y < Main.screenHeight + 20)
                {
                    float scale = (Main.mapOverlayScale * 0.2f * 2f + 1f) / 3f;
                    if (scale > 1f) scale = 1f;
                    scale *= Main.UIScale;

                    bool isHovered = IsMouseHovering(drawPos, 12f * scale);
                    DrawPinMarker(drawPos, pin, scale, isHovered, vis);

                    if (isHovered && pinTooltip != null && hoveredTooltip == null)
                    {
                        hoveredTooltip = pinTooltip;
                    }
                }
            }
        }

        private static void DrawPinMarker(Vector2 pos, StructurePin pin, float scale, bool isHovered, SearchVis vis = SearchVis.None)
        {
            Texture2D magicPixel = TextureAssets.MagicPixel.Value;
            if (magicPixel == null) return;

            // 1. 尺寸计算 (图标底框)
            int baseSize = 22;
            int size = (int)(baseSize * Math.Max(0.75f, scale));
            if (isHovered) size += 4;
            Rectangle rect = new Rectangle((int)pos.X - size / 2, (int)pos.Y - size / 2, size, size);

            // 搜索未命中: 整体调暗
            float visAlpha = vis == SearchVis.Dim ? 0.4f : 1f;

            // 2. 绘制深色半透明底衬与发光外边框
            Color borderColor = pin.IsTrapped ? (Color.Crimson * ((float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.25f + 0.75f)) : (pin.Color * (isHovered ? 1f : 0.85f));
            Color bgColor = Color.Black * (isHovered ? 0.85f : 0.72f);
            borderColor *= visAlpha;
            bgColor *= visAlpha;

            // 搜索命中: 白色脉动光圈(最外层)
            if (vis == SearchVis.Hit)
            {
                float pulse = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.25f + 0.75f;
                int glow = 4 + (int)(pulse * 3f);
                Main.spriteBatch.Draw(magicPixel, new Rectangle(rect.X - glow, rect.Y - glow, rect.Width + glow * 2, rect.Height + glow * 2), Color.White * pulse);
            }

            // 外边框发光
            Main.spriteBatch.Draw(magicPixel, new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), borderColor);
            // 内部深色背景
            Main.spriteBatch.Draw(magicPixel, rect, bgColor);

            // 3. 绘制代表性图标
            Texture2D iconTex = null;
            if (pin.ItemId > 0)
            {
                try
                {
                    Main.instance.LoadItem(pin.ItemId);
                    iconTex = TextureAssets.Item[pin.ItemId]?.Value;
                }
                catch { }
            }

            if (iconTex != null)
            {
                float maxDim = Math.Max(iconTex.Width, iconTex.Height);
                float iconScale = (size - 6f) / maxDim;
                Vector2 iconOrigin = new Vector2(iconTex.Width / 2f, iconTex.Height / 2f);
                Main.spriteBatch.Draw(iconTex, pos, null, Color.White * ((isHovered ? 1f : 0.95f) * visAlpha), 0f, iconOrigin, iconScale, SpriteEffects.None, 0f);
            }
            else
            {
                // 兜底几何方块
                Rectangle coreRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
                Main.spriteBatch.Draw(magicPixel, coreRect, pin.Color * 0.9f * visAlpha);
            }
        }

        private static bool IsMouseHovering(Vector2 pos, float size)
        {
            return Main.mouseX >= pos.X - size && Main.mouseX <= pos.X + size &&
                   Main.mouseY >= pos.Y - size && Main.mouseY <= pos.Y + size;
        }
    }
}



