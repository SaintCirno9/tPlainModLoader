using System;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ID;
using TPML.Content;
using TPML.Core.Pinyin;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 通用容器物品 16 大细分类枚举
    /// 作者: SaintCirno9
    /// </summary>
    public enum BagItemCategory
    {
        /// <summary>全部物品</summary>
        All = 0,
        /// <summary>武器（近战/远程/魔法/召唤等）</summary>
        Weapon,
        /// <summary>工具（镐、斧、锤、钓竿、电线工具等）</summary>
        Tool,
        /// <summary>防具（头盔、胸甲、护腿）</summary>
        Armor,
        /// <summary>饰品（各类配饰、坐骑、宠物、钩爪等）</summary>
        Accessory,
        /// <summary>时装与染料（时装衣物、各色染料等）</summary>
        VanityDye,
        /// <summary>药水与食物（治疗、魔力、增益药剂、食物等）</summary>
        Potion,
        /// <summary>弹药（箭矢、子弹、火箭、飞镖等）</summary>
        Ammo,
        /// <summary>鱼饵（各类钓鱼诱饵、昆虫等）</summary>
        Bait,
        /// <summary>物块与建筑（物块、方块、墙壁、平台等）</summary>
        Tile,
        /// <summary>家具与装饰（桌椅床门、工作台、箱子、挂画等）</summary>
        Furniture,
        /// <summary>雕像（怪物/掉落/文字/功能/装饰雕像）</summary>
        Statue,
        /// <summary>召唤物与信物（Boss召唤物、天界符、事件召唤物等）</summary>
        Summon,
        /// <summary>光源与照明（火把、荧光棒、提灯、蜡烛、光源家具等）</summary>
        Light,
        /// <summary>合成素材（矿石、锭、灵魂、Boss材料、各类制作材料等）</summary>
        Material,
        /// <summary>杂项与消耗（钱币、宝藏袋、宝匣、礼包、其他）</summary>
        Misc
    }

    /// <summary>
    /// 通用容器物品分类与拼音/词条检索规则引擎
    /// 作者: SaintCirno9
    /// </summary>
    public static class BagCategoryHelper
    {
        /// <summary>
        /// 判定物品是否为工具（镐/斧/锤/钓竿/扳手/剪线钳/蓝图/促动魔杖/机械透镜/捕虫网/喷漆工具等）
        /// </summary>
        public static bool IsTool(Item item)
        {
            if (item == null || item.IsAir) return false;

            if (item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.fishingPole > 0)
                return true;

            int t = item.type;
            // 扳手系列（红/蓝/绿/黄/多彩扳手）、剪线钳、宏伟蓝图 (WireKite)、促动魔杖
            if (t == ItemID.Wrench || t == ItemID.BlueWrench || t == ItemID.GreenWrench || t == ItemID.YellowWrench ||
                t == ItemID.MulticolorWrench || t == ItemID.WireCutter || t == ItemID.WireKite || t == ItemID.ActuationRod)
                return true;

            // 施工与电线辅助工具（机械透镜、便携式水泥搅拌机、标尺、激光标尺等）
            if (t == ItemID.MechanicalLens || t == ItemID.PortableCementMixer || t == ItemID.Ruler || t == ItemID.LaserRuler)
                return true;

            // 捕虫网全系列
            if (t == ItemID.BugNet || t == ItemID.GoldenBugNet || t == ItemID.FireproofBugNet)
                return true;

            // 喷漆与漆铲工具全系列
            if (t == ItemID.Paintbrush || t == ItemID.PaintRoller || t == ItemID.PaintScraper ||
                t == ItemID.SpectrePaintbrush || t == ItemID.SpectrePaintRoller || t == ItemID.SpectrePaintScraper)
                return true;

            return false;
        }

        /// <summary>
        /// 判定物品是否为纯净 Boss/事件召唤物或活体信物（纯净显式白名单）
        /// </summary>
        public static bool IsBossOrEventSummon(Item item)
        {
            if (item == null || item.IsAir) return false;

            int t = item.type;
            // 活体信物
            if (t == ItemID.TruffleWorm || t == ItemID.EmpressButterfly ||
                t == ItemID.GuideVoodooDoll || t == ItemID.ClothierVoodooDoll)
                return true;

            // Boss 召唤物
            if (t == ItemID.SlimeCrown || t == ItemID.SuspiciousLookingEye ||
                t == ItemID.WormFood || t == ItemID.BloodySpine ||
                t == ItemID.Abeemination || t == ItemID.DeerThing ||
                t == ItemID.QueenSlimeCrystal ||
                t == ItemID.MechanicalEye || t == ItemID.MechanicalWorm || t == ItemID.MechanicalSkull ||
                t == ItemID.LihzahrdPowerCell || t == ItemID.CelestialSigil ||
                t == ItemID.MechdusaSummon)
                return true;

            // 事件信物
            if (t == ItemID.GoblinBattleStandard || t == ItemID.SnowGlobe ||
                t == ItemID.PirateMap || t == ItemID.PumpkinMoonMedallion ||
                t == ItemID.NaughtyPresent || t == ItemID.SolarTablet ||
                t == ItemID.DD2ElderCrystal || t == ItemID.DD2ElderCrystalStand ||
                t == ItemID.BloodMoonStarter)
                return true;

            return false;
        }

        /// <summary>
        /// 判定物品是否为雕像（怪物/掉落/文字/功能/装饰雕像）
        /// </summary>
        public static bool IsStatue(Item item)
        {
            if (item == null || item.IsAir) return false;
            if (item.createTile < 0) return false;

            int t = item.createTile;
            return t == TileID.Statues ||
                   t == TileID.MushroomStatue ||
                   t == TileID.AlphabetStatues ||
                   t == TileID.CatBast;
        }

        /// <summary>
        /// 判定物品是否为矿石、金属锭或宝石核心素材（前置于家具判定以防抢跑）
        /// </summary>
        public static bool IsOreBarOrGem(Item item)
        {
            if (item == null || item.IsAir) return false;

            // 1. 模组与原版金属锭泛化特征
            if (item.createTile == TileID.MetalBars && item.material)
                return true;

            // 2. 模组与原版矿石特征集合
            if (item.createTile >= 0 && item.createTile < TileID.Sets.Ore.Length && TileID.Sets.Ore[item.createTile])
                return true;

            int t = item.type;

            // 3. 全版本矿石补充白名单
            if (t == ItemID.CopperOre || t == ItemID.TinOre || t == ItemID.IronOre || t == ItemID.LeadOre ||
                t == ItemID.SilverOre || t == ItemID.TungstenOre || t == ItemID.GoldOre || t == ItemID.PlatinumOre ||
                t == ItemID.Meteorite || t == ItemID.DemoniteOre || t == ItemID.CrimtaneOre ||
                t == ItemID.Obsidian || t == ItemID.Hellstone ||
                t == ItemID.CobaltOre || t == ItemID.PalladiumOre || t == ItemID.MythrilOre || t == ItemID.OrichalcumOre ||
                t == ItemID.AdamantiteOre || t == ItemID.TitaniumOre ||
                t == ItemID.ChlorophyteOre || t == ItemID.LunarOre)
                return true;

            // 4. 全版本金属锭补充白名单
            if (t == ItemID.CopperBar || t == ItemID.TinBar || t == ItemID.IronBar || t == ItemID.LeadBar ||
                t == ItemID.SilverBar || t == ItemID.TungstenBar || t == ItemID.GoldBar || t == ItemID.PlatinumBar ||
                t == ItemID.MeteoriteBar || t == ItemID.DemoniteBar || t == ItemID.CrimtaneBar || t == ItemID.HellstoneBar ||
                t == ItemID.CobaltBar || t == ItemID.PalladiumBar || t == ItemID.MythrilBar || t == ItemID.OrichalcumBar ||
                t == ItemID.AdamantiteBar || t == ItemID.TitaniumBar ||
                t == ItemID.HallowedBar || t == ItemID.ChlorophyteBar || t == ItemID.SpectreBar ||
                t == ItemID.ShroomiteBar || t == ItemID.LunarBar)
                return true;

            // 5. 7 大宝石白名单
            if (t == ItemID.Amethyst || t == ItemID.Topaz || t == ItemID.Sapphire ||
                t == ItemID.Emerald || t == ItemID.Ruby || t == ItemID.Diamond || t == ItemID.Amber)
                return true;

            return false;
        }

        /// <summary>
        /// 判定物品是否为光源或照明设施（火把、荧光棒、提灯、蜡烛、营火、路灯等）
        /// </summary>
        public static bool IsLightSource(Item item)
        {
            if (item == null || item.IsAir) return false;

            int t = item.type;
            // 荧光棒全系列
            if (t == ItemID.Glowstick || t == ItemID.StickyGlowstick ||
                t == ItemID.BouncyGlowstick || t == ItemID.SpelunkerGlowstick ||
                t == ItemID.FairyGlowstick)
                return true;

            // 光源方块与设施
            if (item.createTile >= 0)
            {
                int tile = item.createTile;
                if (tile == TileID.Torches || tile == TileID.Candles ||
                    tile == TileID.Chandeliers || tile == TileID.Lamps ||
                    tile == TileID.HangingLanterns || tile == TileID.Candelabras ||
                    tile == TileID.Campfire || tile == TileID.Fireplace ||
                    tile == TileID.WaterCandle || tile == TileID.PeaceCandle ||
                    tile == TileID.ShadowCandle || tile == TileID.Jackolanterns ||
                    tile == TileID.ChineseLanterns || tile == TileID.SkullLanterns ||
                    tile == TileID.FireflyinaBottle || tile == TileID.LightningBuginaBottle ||
                    tile == TileID.SoulBottles || tile == TileID.Lampposts ||
                    tile == TileID.LavaLamp || tile == TileID.PlasmaLamp || tile == TileID.DjinnLamp)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 判定物品所属的主要分类（严谨 16 步优先级流水线）
        /// </summary>
        public static BagItemCategory GetCategory(Item item)
        {
            if (item == null || item.IsAir || item.type <= ItemID.None)
            {
                return BagItemCategory.Misc;
            }

            // 1. 工具（镐/斧/锤/钓竿/扳手/剪线钳/蓝图/促动魔杖/机械透镜/捕虫网/喷漆工具等）
            if (IsTool(item))
            {
                return BagItemCategory.Tool;
            }

            // 2. Boss 与事件召唤物、活体信物（纯净显式白名单，优先于鱼饵以确保松露虫归入召唤物）
            if (IsBossOrEventSummon(item))
            {
                return BagItemCategory.Summon;
            }

            // 3. 鱼饵（钓鱼诱饵、昆虫等）
            if (item.bait > 0)
            {
                return BagItemCategory.Bait;
            }

            // 4. 弹药（箭矢、子弹、火箭、飞镖等，排除钱币以防钱币枪弹药属性抢跑）
            if (item.ammo != AmmoID.None && !item.IsACoin && !(item.type >= ItemID.CopperCoin && item.type <= ItemID.PlatinumCoin))
            {
                return BagItemCategory.Ammo;
            }

            // 5. 雕像（怪物/掉落/文字/功能/装饰雕像）
            if (IsStatue(item))
            {
                return BagItemCategory.Statue;
            }

            // 6. 光源与照明（火把、荧光棒、提灯、蜡烛、营火等）
            if (IsLightSource(item))
            {
                return BagItemCategory.Light;
            }

            // 7. 时装与染料（时装衣物、各色染料等，精简冗余判定）
            if (item.dye > 0 || item.vanity)
            {
                return BagItemCategory.VanityDye;
            }

            // 8. 武器（近战/远程/魔法/召唤等，非配饰且有攻击力或武器类型）
            if (((item.damage > 0 && item.useStyle > 0 && !item.accessory) ||
                 item.melee || item.ranged || item.magic || item.summon) && !item.accessory)
            {
                return BagItemCategory.Weapon;
            }

            // 9. 防具（头/胸/腿，非纯时装）
            if ((item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) && !item.vanity)
            {
                return BagItemCategory.Armor;
            }

            // 10. 饰品与挂件（配饰/坐骑/宠物/钩爪）
            if (item.accessory || item.mountType >= 0 ||
                (item.shoot > 0 && item.shoot < Main.projHook.Length && Main.projHook[item.shoot]) ||
                (item.buffType > 0 && item.buffType < Main.vanityPet.Length && (Main.vanityPet[item.buffType] || Main.lightPet[item.buffType])))
            {
                return BagItemCategory.Accessory;
            }

            // 11. 药水与食物（治疗/魔力/增益/食物）
            if (item.buffType > 0 || item.healLife > 0 || item.healMana > 0 || item.potion)
            {
                return BagItemCategory.Potion;
            }

            // 12. 核心素材（矿石、金属锭、宝石等，前置于家具判定以防金属锭被误判为家具）
            if (IsOreBarOrGem(item))
            {
                return BagItemCategory.Material;
            }

            // 13. 家具与装饰（工作台、床、桌椅、门、箱子、挂画等）
            if (item.createTile >= 0)
            {
                int t = item.createTile;
                if (Main.tileFrameImportant[t] ||
                    t == TileID.Chairs || t == TileID.Tables || t == TileID.Beds || t == TileID.WorkBenches ||
                    t == TileID.Anvils || t == TileID.Furnaces || t == TileID.Containers || t == TileID.Dressers ||
                    t == TileID.OpenDoor || t == TileID.ClosedDoor || t == TileID.Banners ||
                    t == TileID.Painting3X3 || t == TileID.Painting4X3 || t == TileID.Painting6X4 ||
                    t == TileID.ItemFrame || t == TileID.WeaponsRack || t == TileID.Mannequin || t == TileID.Womannequin)
                {
                    return BagItemCategory.Furniture;
                }
            }

            // 14. 物块与建筑（纯方块/墙壁/平台）
            if (item.createTile >= 0 || item.createWall >= 0)
            {
                return BagItemCategory.Tile;
            }

            // 15. 合成素材（灵魂、Boss材料、纯制作材料）
            if (item.material)
            {
                return BagItemCategory.Material;
            }

            // 16. 杂项与消耗（钱币、宝藏袋、宝匣、礼包、杂物等）
            return BagItemCategory.Misc;
        }

        /// <summary>
        /// 检验物品是否满足目标分类筛选（单点收敛对齐 GetCategory，仅 Material 支持 item.material 宽容筛选）
        /// </summary>
        public static bool MatchesCategory(Item item, BagItemCategory category)
        {
            if (category == BagItemCategory.All) return true;
            if (item == null || item.IsAir) return false;

            // 唯一受控宽容筛选入口：Material 分类同时展示主分类为素材的物品 + 带有 material 标记的装备/道具
            if (category == BagItemCategory.Material)
            {
                return GetCategory(item) == BagItemCategory.Material || item.material;
            }

            // 其余所有分类严格单点对齐 GetCategory，彻底根治分类重叠、物块污染与松露虫冒出
            return GetCategory(item) == category;
        }

        /// <summary>
        /// 多维拼音、ID、中文名、英文名及 Tooltip 词条匹配
        /// </summary>
        public static bool MatchesSearch(Item item, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            if (item == null || item.IsAir || item.type <= ItemID.None) return false;

            query = query.Trim();

            // 0. 前缀专属检索语法：若以 '#' 或 '@' 开头，专门匹配物品前缀修饰语
            if (query.StartsWith("#") || query.StartsWith("@"))
            {
                if (item.prefix <= 0) return false;

                string prefixQuery = query.Substring(1).Trim();
                // 若仅输入 '#' 或 '@'，匹配所有带有修饰语的装备
                if (string.IsNullOrEmpty(prefixQuery)) return true;

                // 纯数字匹配前缀 ID
                if (int.TryParse(prefixQuery, out int targetPrefixId) && item.prefix == targetPrefixId)
                {
                    return true;
                }

                // 本地化前缀名称与拼音匹配
                if (item.prefix < Lang.prefix.Length)
                {
                    string prefixName = Lang.prefix[item.prefix]?.Value;
                    if (!string.IsNullOrEmpty(prefixName) && PinyinHelper.Matches(prefixName, prefixQuery))
                    {
                        return true;
                    }
                }

                return false;
            }

            // 1. 纯数字精确匹配 ItemID
            if (int.TryParse(query, out int queryId) && item.type == queryId)
            {
                return true;
            }

            // 2. 本地化中文名称与拼音/首字母缩写匹配
            string localizedName = Lang.GetItemNameValue(item.type);
            if (!string.IsNullOrEmpty(localizedName) && PinyinHelper.Matches(localizedName, query))
            {
                return true;
            }

            // 3. 原生英文/内部名称匹配
            if (!string.IsNullOrEmpty(item.Name) && PinyinHelper.Matches(item.Name, query))
            {
                return true;
            }

            if (item.type > 0 && item.type < ItemID.Count)
            {
                string internalName = ItemID.Search.GetName(item.type);
                if (!string.IsNullOrEmpty(internalName) && PinyinHelper.Matches(internalName, query))
                {
                    return true;
                }
            }

            // 4. 模组物品元数据匹配
            if (item.type >= ItemID.Count)
            {
                ModItem modItem = ItemLoader.GetItem(item.type);
                if (modItem != null)
                {
                    if (!string.IsNullOrEmpty(modItem.Name) && PinyinHelper.Matches(modItem.Name, query))
                    {
                        return true;
                    }
                    if (!string.IsNullOrEmpty(modItem.FullName) && PinyinHelper.Matches(modItem.FullName, query))
                    {
                        return true;
                    }
                }

                string modTooltip = ItemLoader.GetTooltip(item.type);
                if (!string.IsNullOrEmpty(modTooltip) && PinyinHelper.Matches(modTooltip, query))
                {
                    return true;
                }
            }

            // 5. 原版物品 Tooltip 属性与描述词条匹配
            if (item.ToolTip != null)
            {
                for (int i = 0; i < item.ToolTip.Lines; i++)
                {
                    string line = item.ToolTip.GetLine(i);
                    if (!string.IsNullOrEmpty(line) && PinyinHelper.Matches(line, query))
                    {
                        return true;
                    }
                }
            }

            // 6. 前缀修饰语名称与拼音匹配（常规搜索兼容）
            if (item.prefix > 0 && item.prefix < Lang.prefix.Length)
            {
                string prefixName = Lang.prefix[item.prefix]?.Value;
                if (!string.IsNullOrEmpty(prefixName) && PinyinHelper.Matches(prefixName, query))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
