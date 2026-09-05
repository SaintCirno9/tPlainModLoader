using CommandHelp;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Content.UI;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;
using TPML.UI.ModSet;

namespace OptimizeAndTool.Content.QoL
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

        // 2. 瞬传与微距传送
        public static GetSetReset<bool> instantRecall = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> altRightClickTeleport = new GetSetReset<bool>(true, true);

        // 3. 脱战 1.5s 快速复活 & 复活自动召回仆从
        public static GetSetReset<bool> quickRespawn = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> quickRespawnFrames = new GetSetReset<int>(90, 90, v => v < 1 ? 1 : v);
        public static GetSetReset<bool> autoResummonMinions = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> minionPhasing = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> minionRangeBoost = new GetSetReset<bool>(true, true);

        // 4. 生态与植被增强
        public static GetSetReset<bool> naturalGrowthBoost = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> naturalGrowthMultiplier = new GetSetReset<int>(5, 5, v => v < 1 ? 1 : (v > 30 ? 30 : v));
        public static GetSetReset<bool> mushroomWeightBoost = new GetSetReset<bool>(true, true);
        public static GetSetReset<int> mushroomWeightMultiplier = new GetSetReset<int>(10, 10, v => v < 1 ? 1 : (v > 50 ? 50 : v));
        public static GetSetReset<bool> evilMushroomWeightBoost = new GetSetReset<bool>(false, false);
        public static GetSetReset<int> evilMushroomWeightMultiplier = new GetSetReset<int>(5, 5, v => v < 1 ? 1 : (v > 25 ? 25 : v));
        public static GetSetReset<bool> wildHerbSpawnBoost = new GetSetReset<bool>(false, false);
        public static GetSetReset<int> wildHerbSpawnMultiplier = new GetSetReset<int>(3, 3, v => v < 1 ? 1 : (v > 10 ? 10 : v));

        public static GetSetReset<bool> herbFastGrow = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> herbBloomAnytime = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> staffOfRegenAutoReplant = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> treeFastGrow = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> treeShakeGuaranteeFruit = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> gemTreeFullGemDrops = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> removeGraveyardVisuals = new GetSetReset<bool>(true, true);

        // 5. 防非玩家爆炸物破坏地形
        public static GetSetReset<bool> antiGriefExplosions = new GetSetReset<bool>(true, true);

        // 6. 背景墙与掉落增强
        public static GetSetReset<bool> unsafeWallDrops = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get2("pylonUnlimitedPlacement", pylonUnlimitedPlacement),
                CommandBuild.get2("pylonFreeTeleport", pylonFreeTeleport),
                CommandBuild.get2("instantRecall", instantRecall),
                CommandBuild.get2("altRightClickTeleport", altRightClickTeleport),
                CommandBuild.get1("quickRespawn", quickRespawn, quickRespawnFrames, new CommandInt()),
                CommandBuild.get2("autoResummonMinions", autoResummonMinions),
                CommandBuild.get2("minionPhasing", minionPhasing),
                CommandBuild.get2("minionRangeBoost", minionRangeBoost),
                CommandBuild.get1("naturalGrowthBoost", naturalGrowthBoost, naturalGrowthMultiplier, new CommandInt()),
                CommandBuild.get1("mushroomWeightBoost", mushroomWeightBoost, mushroomWeightMultiplier, new CommandInt()),
                CommandBuild.get1("evilMushroomWeightBoost", evilMushroomWeightBoost, evilMushroomWeightMultiplier, new CommandInt()),
                CommandBuild.get1("wildHerbSpawnBoost", wildHerbSpawnBoost, wildHerbSpawnMultiplier, new CommandInt()),
                CommandBuild.get2("herbFastGrow", herbFastGrow),
                CommandBuild.get2("herbBloomAnytime", herbBloomAnytime),
                CommandBuild.get2("staffOfRegenAutoReplant", staffOfRegenAutoReplant),
                CommandBuild.get2("treeFastGrow", treeFastGrow),
                CommandBuild.get2("treeShakeGuaranteeFruit", treeShakeGuaranteeFruit),
                CommandBuild.get2("gemTreeFullGemDrops", gemTreeFullGemDrops),
                CommandBuild.get2("removeGraveyardVisuals", removeGraveyardVisuals),
                CommandBuild.get2("antiGriefExplosions", antiGriefExplosions),
                CommandBuild.get2("unsafeWallDrops", unsafeWallDrops),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                // 生态与植被增强
                new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_5", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "生态与植被增强 (蘑菇/草药/树木)"),
                UIBuild.get1(naturalGrowthBoost, naturalGrowthMultiplier, int.Parse, "全局作物与树木生长倍率(1x~30x)，平滑加速草药/树木/南瓜/竹子/蘑菇生长，隔离腐化蔓延<int>", "Images/Item_27", "自然生长倍率加速"),
                UIBuild.get1(mushroomWeightBoost, mushroomWeightMultiplier, int.Parse, "地表普通红蘑菇生成权重倍率(1x~50x)，将草坪生成的普通杂草按权重转化为红蘑菇(50x=100%全转红蘑菇)<int>", "Images/Item_5", "地表红蘑菇生成权重"),
                UIBuild.get1(evilMushroomWeightBoost, evilMushroomWeightMultiplier, int.Parse, "腐化魔菇/猩红毒蘑菇生成权重倍率(1x~25x)，大幅提升邪恶草坪植物转化为魔菇/毒蘑菇的比例<int>", "Images/Item_60", "邪恶蘑菇生成权重"),
                UIBuild.get1(wildHerbSpawnBoost, wildHerbSpawnMultiplier, int.Parse, "野生草药自然播种频率倍率(1x~10x)，加速世界随机自然生成野生草药幼苗的频率<int>", "Images/Item_313", "野生草药播种加速"),
                UIBuild.get2(herbFastGrow, "玩家周围草药（幼苗->成熟->开花两阶段）、仙人掌与竹子等平滑渐进生长", "Images/Item_313", "草药与作物极速生长"),
                UIBuild.get2(herbBloomAnytime, "草药在任意时刻均视为开花状态，收获必掉种子与额外草药", "Images/Item_309", "草药任意时刻开花"),
                UIBuild.get2(staffOfRegenAutoReplant, "使用再生法杖或再生之斧收获草药时自动原地重新播种", "Images/Item_213", "再生法杖收获自动补种"),
                UIBuild.get2(treeFastGrow, "全树种（森林/棕榈/宝石树/灰烬树/巨型发光蘑菇等）平滑极速生长，长成时自动锁定原版最高高度以最大化木材收获", "Images/Item_27", "树木极速生长(锁定最高)"),
                UIBuild.get2(treeShakeGuaranteeFruit, "摇树必定掉落当前树种对应的水果", "Images/Item_4009", "摇树必掉水果"),
                UIBuild.get2(gemTreeFullGemDrops, "破坏宝石树干全段方块必定掉落对应宝石", "Images/Item_182", "宝石树全段掉宝石"),
                UIBuild.get2(EcoGrowth.EnablePumpkinFastGrow, "南瓜藤生长速度大幅提升（每次生长直接推进至多 4 个阶段）", "Images/Item_1725", "南瓜迅速生长"),
                UIBuild.get2(EcoGrowth.EnableLifeFruitFastGrow, "生命果生成后以概率在附近补种，加快累积速度", "Images/Item_1291", "生命果迅速生长"),
                UIBuild.get2(removeGraveyardVisuals, "移除墓地环境屏幕暗角、迷雾滤镜与墓地背景音乐", "Images/Item_321", "移除墓地暗角与音乐"),

                // 晶塔与传送
                new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_4875", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "晶塔与传送便捷"),
                UIBuild.get2(pylonUnlimitedPlacement, "解除单世界同类型晶塔只能放置一个的限制", "Images/Item_4875", "晶塔无放置上限"),
                UIBuild.get2(pylonFreeTeleport, "全图晶塔传送无视危险、无视群落、无需靠近晶塔、无需周围有NPC", "Images/Item_4875", "晶塔无限制全图传送"),
                UIBuild.get2(instantRecall, "消除魔镜/冰雪镜/手机/海螺/回程药水等施法前摇延迟，点击瞬间传送", "Images/Item_50", "魔镜/回程药水瞬传"),
                UIBuild.get2(altRightClickTeleport, "在游戏世界中按住 Alt 并右击鼠标，瞬移至光标位置（自动智能吸附空位，便于钻入狭窄小角落）", "Images/Item_1326", "Alt+右键微距传送"),

                // 复活 & 仆从
                new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Buff_48", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "脱战复活与召唤物增强 (Minion & Sentry)"),
                UIBuild.get1(quickRespawn, quickRespawnFrames, int.Parse, "场上无存活Boss时的复活帧数(60帧=1秒，默认90帧=1.5s)<int>", "Images/Buff_48", "脱战极速复活"),
                UIBuild.get2(autoResummonMinions, "记录死亡前使用的召唤杖，复活后自动重新召唤仆从至上限", "Images/Buff_150", "复活自动召唤仆从"),
                UIBuild.get2(minionPhasing, "所有仆从、哨兵及衍生弹幕智能穿墙：飞行完全穿墙，走地仆从遇阻相位突进，发射物穿墙命中", "Images/Buff_150", "召唤物与弹幕智能穿墙"),
                UIBuild.get2(minionRangeBoost, "全屏级透视索敌与脱战防拉扯：索敌范围扩展至~1800像素(全屏视野)，脱战拉回上限放宽至2500像素，并优先集火威胁目标", "Images/Buff_150", "召唤物全屏索敌与防拉回"),

                // 防破坏与危险墙
                new UIItemTitle(Main.Assets.Request<Texture2D>("Images/Item_166", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value, "防地形破坏与危险墙掉落"),
                UIBuild.get2(antiGriefExplosions, "拦截小丑、机械骷髅王炸弹、陷阱爆炸物及非玩家敌怪爆炸破坏地图方块", "Images/Item_166", "防敌怪爆炸物破坏地形"),
                UIBuild.get2(unsafeWallDrops, "摧毁天然危险背景墙（蜘蛛墙、地牢墙、神庙墙、地下沙漠砂岩墙及天然环境岩壁等）必定掉落对应危险墙物品", "Images/Item_5363", "天然危险墙掉落对应物品"),
            };

            return uis;
        }
    }
}

