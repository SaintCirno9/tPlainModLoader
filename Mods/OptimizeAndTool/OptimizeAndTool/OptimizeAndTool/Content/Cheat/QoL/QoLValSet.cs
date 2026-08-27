using CommandHelp;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.QoL
{
    /// <summary>
    /// 便捷与生态增强配置项与UI绑定定义
    /// 作者: SaintCirno9
    /// </summary>
    internal static class QoLValSet
    {
        // 1. 全图晶塔无限制传送
        public static GetSetReset<bool> pylonUnlimitedPlacement = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> pylonFreeTeleport = new GetSetReset<bool>(true, true);

        // 2. 魔镜 / 回程药水瞬传
        public static GetSetReset<bool> instantRecall = new GetSetReset<bool>(true, true);

        // 3. 脱战 1.5s 快速复活 & 复活自动召回仆从
        public static GetSetReset<bool> quickRespawn = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> quickRespawnFrames = new GetSetReset<int>(90, 90, v => v < 1 ? 1 : v);
        public static GetSetReset<bool> autoResummonMinions = new GetSetReset<bool>(true, true);

        // 4. 生态与植被增强
        public static GetSetReset<bool> herbFastGrow = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> herbBloomAnytime = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> staffOfRegenAutoReplant = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> treeFastGrow = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> treeShakeGuaranteeFruit = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> gemTreeFullGemDrops = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> removeGraveyardVisuals = new GetSetReset<bool>(true, true);

        // 5. 防非玩家爆炸物破坏地形
        public static GetSetReset<bool> antiGriefExplosions = new GetSetReset<bool>(true, true);

        // 6. 关键结构与世界标记
        public static GetSetReset<bool> markStructuresOnMap = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markPlanteraBulb = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markSwordShrine = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markBeeHive = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTempleAltar = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTempleDoor = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markEvilAltars = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markEvilOrbsHearts = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markShimmer = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markPyramid = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markFloatingIsland = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markDungeon = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markDungeonWaterBolt = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMiniBiomes = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMushroomBiome = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markAntlionHive = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMossCaves = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMeteorite = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTrapsExplosives = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTrappedNPCs = new GetSetReset<bool>(true, true);

        // 7. 全量宝箱雷达标记
        public static GetSetReset<bool> markChestsAll = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markChestsShowEmpty = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> markChestsSurfaceUnderground = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markChestsShadowBiome = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markChestsTrapped = new GetSetReset<bool>(true, true);

        public static void SetAllStructureMarkers(bool enabled)
        {
            markStructuresOnMap.val = enabled;
            markPlanteraBulb.val = enabled;
            markSwordShrine.val = enabled;
            markBeeHive.val = enabled;
            markTempleAltar.val = enabled;
            markTempleDoor.val = enabled;
            markEvilAltars.val = enabled;
            markEvilOrbsHearts.val = enabled;
            markShimmer.val = enabled;
            markPyramid.val = enabled;
            markFloatingIsland.val = enabled;
            markDungeon.val = enabled;
            markDungeonWaterBolt.val = enabled;
            markMiniBiomes.val = enabled;
            markMushroomBiome.val = enabled;
            markAntlionHive.val = enabled;
            markMossCaves.val = enabled;
            markMeteorite.val = enabled;
            markTrapsExplosives.val = enabled;
            markTrappedNPCs.val = enabled;

            markChestsAll.val = enabled;
            markChestsSurfaceUnderground.val = enabled;
            markChestsShadowBiome.val = enabled;
            markChestsTrapped.val = enabled;
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("pylonUnlimitedPlacement", pylonUnlimitedPlacement),
                CommandBuild.get2("pylonFreeTeleport", pylonFreeTeleport),
                CommandBuild.get2("instantRecall", instantRecall),
                CommandBuild.get1("quickRespawn", quickRespawn, quickRespawnFrames, new CommandInt()),
                CommandBuild.get2("autoResummonMinions", autoResummonMinions),
                CommandBuild.get2("herbFastGrow", herbFastGrow),
                CommandBuild.get2("herbBloomAnytime", herbBloomAnytime),
                CommandBuild.get2("staffOfRegenAutoReplant", staffOfRegenAutoReplant),
                CommandBuild.get2("treeFastGrow", treeFastGrow),
                CommandBuild.get2("treeShakeGuaranteeFruit", treeShakeGuaranteeFruit),
                CommandBuild.get2("gemTreeFullGemDrops", gemTreeFullGemDrops),
                CommandBuild.get2("removeGraveyardVisuals", removeGraveyardVisuals),
                CommandBuild.get2("antiGriefExplosions", antiGriefExplosions),
                CommandBuild.get2("markStructuresOnMap", markStructuresOnMap),
                CommandBuild.get2("markPlanteraBulb", markPlanteraBulb),
                CommandBuild.get2("markSwordShrine", markSwordShrine),
                CommandBuild.get2("markBeeHive", markBeeHive),
                CommandBuild.get2("markTempleAltar", markTempleAltar),
                CommandBuild.get2("markTempleDoor", markTempleDoor),
                CommandBuild.get2("markEvilAltars", markEvilAltars),
                CommandBuild.get2("markEvilOrbsHearts", markEvilOrbsHearts),
                CommandBuild.get2("markShimmer", markShimmer),
                CommandBuild.get2("markPyramid", markPyramid),
                CommandBuild.get2("markFloatingIsland", markFloatingIsland),
                CommandBuild.get2("markDungeon", markDungeon),
                CommandBuild.get2("markDungeonWaterBolt", markDungeonWaterBolt),
                CommandBuild.get2("markMiniBiomes", markMiniBiomes),
                CommandBuild.get2("markMushroomBiome", markMushroomBiome),
                CommandBuild.get2("markAntlionHive", markAntlionHive),
                CommandBuild.get2("markMossCaves", markMossCaves),
                CommandBuild.get2("markMeteorite", markMeteorite),
                CommandBuild.get2("markTrapsExplosives", markTrapsExplosives),
                CommandBuild.get2("markTrappedNPCs", markTrappedNPCs),
                CommandBuild.get2("markChestsAll", markChestsAll),
                CommandBuild.get2("markChestsShowEmpty", markChestsShowEmpty),
                CommandBuild.get2("markChestsSurfaceUnderground", markChestsSurfaceUnderground),
                CommandBuild.get2("markChestsShadowBiome", markChestsShadowBiome),
                CommandBuild.get2("markChestsTrapped", markChestsTrapped),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                // 晶塔
                UIBuild.get2(pylonUnlimitedPlacement, "解除单世界同类型晶塔只能放置一个的限制", "Images/Item_4875", "晶塔无放置上限"),
                UIBuild.get2(pylonFreeTeleport, "全图晶塔传送无视危险、无视群落、无需靠近晶塔、无需周围有NPC", "Images/Item_4875", "晶塔无限制全图传送"),

                // 瞬传
                UIBuild.get2(instantRecall, "消除魔镜/冰雪镜/手机/海螺/回程药水等施法前摇延迟，点击瞬间传送", "Images/Item_50", "魔镜/回程药水瞬传"),

                // 复活 & 仆从
                UIBuild.get1(quickRespawn, quickRespawnFrames, int.Parse, "场上无存活Boss时的复活帧数(60帧=1秒，默认90帧=1.5s)<int>", "Images/Buff_48", "脱战极速复活"),
                UIBuild.get2(autoResummonMinions, "记录死亡前使用的召唤杖，复活后自动重新召唤仆从至上限", "Images/Buff_150", "复活自动召唤仆从"),

                // 生态与植被增强
                UIBuild.get2(herbFastGrow, "草药极速跃迁生长至开花阶段", "Images/Item_313", "草药极速生长"),
                UIBuild.get2(herbBloomAnytime, "草药在任意时刻均视为开花状态，收获必掉种子与额外草药", "Images/Item_309", "草药任意时刻开花"),
                UIBuild.get2(staffOfRegenAutoReplant, "使用再生法杖或再生之斧收获草药时自动原地重新播种", "Images/Item_213", "再生法杖收获自动补种"),
                UIBuild.get2(treeFastGrow, "树苗与宝石树苗极速生长成树木", "Images/Item_27", "树木极速生长"),
                UIBuild.get2(treeShakeGuaranteeFruit, "摇树必定掉落当前树种对应的水果", "Images/Item_4009", "摇树必掉水果"),
                UIBuild.get2(gemTreeFullGemDrops, "破坏宝石树干全段方块必定掉落对应宝石", "Images/Item_182", "宝石树全段掉宝石"),
                UIBuild.get2(removeGraveyardVisuals, "移除墓地环境屏幕暗角、迷雾滤镜与墓地背景音乐", "Images/Item_321", "移除墓地暗角与音乐"),

                // 防爆炸
                UIBuild.get2(antiGriefExplosions, "拦截小丑、机械骷髅王炸弹、陷阱爆炸物及非玩家敌怪爆炸破坏地图方块", "Images/Item_166", "防敌怪爆炸物破坏地形"),
            };

            // 结构与宝箱标记总抽屉
            UIDrawer structureDrawer = new UIDrawer("Images/Item_5358", "地图标记关键结构与宝箱雷达 (展开/折叠)");
            structureDrawer.Add(UIBuild.get2(markStructuresOnMap, "在全图与小地图上高亮标记世界关键结构与宝箱雷达", "Images/Item_5358", "总开关: 地图标记关键结构与宝箱"));
            structureDrawer.Add(UIBuild.get4("全部开启", () => SetAllStructureMarkers(true), "一键开启所有关键结构与宝箱标记", "Images/Item_5358", "一键全部开启"));
            structureDrawer.Add(UIBuild.get4("全部关闭", () => SetAllStructureMarkers(false), "一键关闭所有关键结构与宝箱标记", "Images/Item_5358", "一键全部关闭"));
            structureDrawer.Add(UIBuild.get4("重新扫描", () => StructureMarker.TriggerRescan(), "立即遍历世界方块与宝箱重新扫描位置", "Images/Item_5358", "重新扫描世界结构与宝箱"));

            // 1. 全量宝箱雷达子分组
            UIDrawer chestDrawer = new UIDrawer("Images/Item_48", "全量宝箱雷达 (展开/折叠)");
            chestDrawer.Add(UIBuild.get2(markChestsAll, "地图标记所有宝箱总开关（悬停可查看箱内战利品清单）", "Images/Item_48", "总开关: 宝箱雷达"));
            chestDrawer.Add(UIBuild.get2(markChestsShowEmpty, "是否标记已被清空的空宝箱（关闭后仅显示有物品的箱子）", "Images/Item_48", "显示已清空的空宝箱"));
            chestDrawer.Add(UIBuild.get2(markChestsSurfaceUnderground, "小地图标记地表与地下常规宝箱（木箱/黄金箱/冰雪/常春藤/水箱/沙岩等）", "Images/Item_306", "标记: 常规地表与地下宝箱"));
            chestDrawer.Add(UIBuild.get2(markChestsShadowBiome, "小地图标记特殊与环境宝箱（地狱暗影箱/神庙箱/地牢五大环境神器箱/沙漠神器箱）", "Images/Item_327", "标记: 环境神器与暗影宝箱"));
            chestDrawer.Add(UIBuild.get2(markChestsTrapped, "小地图标记伪装陷阱宝箱与致命死人宝箱（红框高亮警示）", "Images/Item_4712", "标记: 陷阱与死人宝箱"));
            structureDrawer.Add(chestDrawer);

            // 2. 世界关键遗迹与生态子分组
            UIDrawer biomeDrawer = new UIDrawer("Images/Item_5334", "世界遗迹与微群落 (展开/折叠)");
            biomeDrawer.Add(UIBuild.get2(markPlanteraBulb, "小地图标记世纪之花花苞坐标", "Images/Item_3324", "标记: 世纪之花花苞"));
            biomeDrawer.Add(UIBuild.get2(markSwordShrine, "小地图标记附魔剑冢坐标", "Images/Item_72", "标记: 附魔剑冢"));
            biomeDrawer.Add(UIBuild.get2(markBeeHive, "小地图标记蜂巢与幼虫坐标", "Images/Item_1133", "标记: 蜂巢幼虫"));
            biomeDrawer.Add(UIBuild.get2(markTempleAltar, "小地图标记丛林神庙石巨人祭坛坐标", "Images/Item_1293", "标记: 神庙石巨人祭坛"));
            biomeDrawer.Add(UIBuild.get2(markTempleDoor, "小地图标记丛林神庙大门入口坐标", "Images/Item_1153", "标记: 丛林神庙大门"));
            biomeDrawer.Add(UIBuild.get2(markShimmer, "小地图标记以太微光湖坐标", "Images/Item_5334", "标记: 以太微光湖"));
            biomeDrawer.Add(UIBuild.get2(markPyramid, "小地图标记沙漠金字塔坐标", "Images/Item_857", "标记: 沙漠金字塔"));
            biomeDrawer.Add(UIBuild.get2(markFloatingIsland, "小地图标记空岛天域建筑与高空天湖坐标", "Images/Item_831", "标记: 空岛与天湖"));
            biomeDrawer.Add(UIBuild.get2(markDungeon, "小地图标记地牢地表主入口坐标", "Images/Item_1531", "标记: 地牢主入口"));
            biomeDrawer.Add(UIBuild.get2(markDungeonWaterBolt, "小地图标记地牢书架上的《水之书》法术藏书", "Images/Item_165", "标记: 地牢《水之书》"));
            biomeDrawer.Add(UIBuild.get2(markMiniBiomes, "小地图标记地下蛛巢、大理石洞与花岗岩洞坐标", "Images/Item_939", "标记: 蛛巢/大理石/花岗岩洞"));
            biomeDrawer.Add(UIBuild.get2(markMushroomBiome, "小地图标记地下发光蘑菇地群落", "Images/Item_183", "标记: 地下发光蘑菇地"));
            biomeDrawer.Add(UIBuild.get2(markAntlionHive, "小地图标记地下沙漠蚁穴核心群落", "Images/Item_323", "标记: 地下沙漠蚁穴"));
            biomeDrawer.Add(UIBuild.get2(markMossCaves, "小地图标记地下发光苔藓洞群落", "Images/Item_4387", "标记: 地下发光苔藓洞"));
            biomeDrawer.Add(UIBuild.get2(markMeteorite, "小地图标记陨石撞击坑坐标", "Images/Item_117", "标记: 陨石撞击坑"));
            structureDrawer.Add(biomeDrawer);

            // 3. 邪恶核心、危险机关与受困NPC
            UIDrawer dangerDrawer = new UIDrawer("Images/Item_26", "邪恶核心/机关/受困NPC (展开/折叠)");
            dangerDrawer.Add(UIBuild.get2(markEvilAltars, "小地图标记恶魔祭坛与猩红祭坛坐标", "Images/Item_26", "标记: 恶魔/猩红祭坛"));
            dangerDrawer.Add(UIBuild.get2(markEvilOrbsHearts, "小地图标记暗影珠与猩红之心坐标", "Images/Item_29", "标记: 暗影珠/猩红之心"));
            dangerDrawer.Add(UIBuild.get2(markTrapsExplosives, "小地图标记地下炸药陷阱与致命机关", "Images/Item_166", "标记: 炸药与致命机关"));
            dangerDrawer.Add(UIBuild.get2(markTrappedNPCs, "小地图实时动态追踪受困NPC（哥布林/机械师/巫师/税收官/酒馆老板/发型师/高尔夫/迷失女孩/老人/信徒）", "Images/Item_267", "标记: 受困与特殊NPC实时追踪"));
            structureDrawer.Add(dangerDrawer);

            uis.Add(structureDrawer);

            return uis;
        }
    }
}

