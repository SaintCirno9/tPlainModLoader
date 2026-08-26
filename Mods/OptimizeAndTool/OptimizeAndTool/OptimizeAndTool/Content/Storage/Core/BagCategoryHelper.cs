using System;
using System.Text;
using Terraria;
using Terraria.ID;
using TPML.Content;
using TPML.Core.Pinyin;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 通用容器物品一级细分类枚举
    /// 作者: SaintCirno9
    /// </summary>
    public enum BagItemCategory
    {
        /// <summary>全部物品</summary>
        All = 0,
        /// <summary>武器（近战/远程/魔法/召唤等）</summary>
        Weapon,
        /// <summary>防具（头盔、胸甲、护腿、时装、染料等）</summary>
        Armor,
        /// <summary>饰品（各类配饰、坐骑、宠物、钩爪等）</summary>
        Accessory,
        /// <summary>工具（镐、斧、锤、钓竿、电线工具等）</summary>
        Tool,
        /// <summary>药水与增益（治疗、魔力、增益药剂、食物等）</summary>
        Potion,
        /// <summary>弹药（箭矢、子弹、火箭、飞镖等）</summary>
        Ammo,
        /// <summary>鱼饵（各类钓鱼诱饵、昆虫等）</summary>
        Bait,
        /// <summary>物块与建筑（物块、方块、墙壁、平台等）</summary>
        Tile,
        /// <summary>合成素材（矿石、锭、灵魂、Boss召唤物、各类制作材料等）</summary>
        Material,
        /// <summary>杂项与其他（家具、光源、钱币、NPC信物等）</summary>
        Other
    }

    /// <summary>
    /// 通用容器物品分类与拼音/词条检索规则引擎
    /// 作者: SaintCirno9
    /// </summary>
    public static class BagCategoryHelper
    {
        /// <summary>
        /// 判定物品所属的主要分类
        /// </summary>
        public static BagItemCategory GetCategory(Item item)
        {
            if (item == null || item.IsAir || item.type <= ItemID.None)
            {
                return BagItemCategory.Other;
            }

            // 1. 工具优先（部分稿斧带有伤害判定，优先按工具归类）
            if (item.pick > 0 || item.axe > 0 || item.hammer > 0 || item.fishingPole > 0 ||
                item.type == ItemID.Wrench || item.type == ItemID.BlueWrench || item.type == ItemID.GreenWrench ||
                item.type == ItemID.YellowWrench || item.type == ItemID.WireCutter || item.type == ItemID.WireKite)
            {
                return BagItemCategory.Tool;
            }

            // 2. 弹药
            if (item.ammo != AmmoID.None)
            {
                return BagItemCategory.Ammo;
            }

            // 3. 鱼饵（钓鱼诱饵、昆虫等）
            if (item.bait > 0)
            {
                return BagItemCategory.Bait;
            }

            // 4. 武器（带有攻击力且有挥动/使用方式）
            if ((item.damage > 0 && item.useStyle > 0 && !item.accessory) ||
                item.melee || item.ranged || item.magic || item.summon)
            {
                return BagItemCategory.Weapon;
            }

            // 4. 防具与时装（头/胸/腿/时装/染料）
            if (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0 || item.vanity || item.dye > 0)
            {
                return BagItemCategory.Armor;
            }

            // 5. 饰品与挂件（配饰/坐骑/宠物/钩爪）
            if (item.accessory || item.mountType >= 0 ||
                (item.shoot > 0 && item.shoot < Main.projHook.Length && Main.projHook[item.shoot]) ||
                (item.buffType > 0 && item.buffType < Main.vanityPet.Length && (Main.vanityPet[item.buffType] || Main.lightPet[item.buffType])))
            {
                return BagItemCategory.Accessory;
            }

            // 6. 药水与食物（治疗/魔力/增益/食物）
            if (item.buffType > 0 || item.healLife > 0 || item.healMana > 0 || item.potion)
            {
                return BagItemCategory.Potion;
            }

            // 7. 物块与墙壁建筑材料
            if (item.createTile >= 0 || item.createWall >= 0)
            {
                // 锭与矿石虽可放置但更偏向材料
                if (item.material && (item.type == ItemID.CopperOre || item.type == ItemID.IronOre || item.type == ItemID.GoldOre ||
                    item.type == ItemID.DemoniteOre || item.type == ItemID.CrimtaneOre || item.type == ItemID.Hellstone ||
                    item.type == ItemID.CobaltOre || item.type == ItemID.MythrilOre || item.type == ItemID.TitaniumOre ||
                    item.type == ItemID.ChlorophyteOre || item.type == ItemID.LunarOre ||
                    item.type == ItemID.CopperBar || item.type == ItemID.IronBar || item.type == ItemID.GoldBar ||
                    item.type == ItemID.DemoniteBar || item.type == ItemID.CrimtaneBar || item.type == ItemID.HellstoneBar ||
                    item.type == ItemID.HallowedBar || item.type == ItemID.ChlorophyteBar || item.type == ItemID.SpectreBar ||
                    item.type == ItemID.ShroomiteBar || item.type == ItemID.LunarBar))
                {
                    return BagItemCategory.Material;
                }

                return BagItemCategory.Tile;
            }

            // 8. 合成材料与素材（Boss召唤物、纯制作素材等）
            if (item.material || item.type == ItemID.SlimeCrown || item.type == ItemID.SuspiciousLookingEye ||
                item.type == ItemID.WormFood || item.type == ItemID.BloodySpine ||
                item.type == ItemID.Abeemination || item.type == ItemID.DeerThing ||
                item.type == ItemID.MechanicalEye || item.type == ItemID.MechanicalWorm ||
                item.type == ItemID.MechanicalSkull || item.type == ItemID.LihzahrdPowerCell ||
                item.type == ItemID.TruffleWorm || item.type == ItemID.CelestialSigil)
            {
                return BagItemCategory.Material;
            }

            return BagItemCategory.Other;
        }

        /// <summary>
        /// 检验物品是否满足目标分类筛选
        /// </summary>
        public static bool MatchesCategory(Item item, BagItemCategory category)
        {
            if (category == BagItemCategory.All) return true;
            if (item == null || item.IsAir) return false;

            // 针对 Bait 判定
            if (category == BagItemCategory.Bait && item.bait > 0)
            {
                return true;
            }

            // 针对 Material 智能宽泛命中所有纯合成素材
            if (category == BagItemCategory.Material && item.material && !item.accessory && item.damage == 0 && item.pick == 0 && item.axe == 0 && item.hammer == 0)
            {
                return true;
            }

            // 针对 Tile 智能宽泛命中所有可放置方块与墙体
            if (category == BagItemCategory.Tile && (item.createTile >= 0 || item.createWall >= 0))
            {
                return true;
            }

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

            return false;
        }
    }
}
