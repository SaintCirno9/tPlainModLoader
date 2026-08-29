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

        // 2. 瞬传与微距传送
        public static GetSetReset<bool> instantRecall = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> altRightClickTeleport = new GetSetReset<bool>(true, true);

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
                CommandBuild.get2("herbFastGrow", herbFastGrow),
                CommandBuild.get2("herbBloomAnytime", herbBloomAnytime),
                CommandBuild.get2("staffOfRegenAutoReplant", staffOfRegenAutoReplant),
                CommandBuild.get2("treeFastGrow", treeFastGrow),
                CommandBuild.get2("treeShakeGuaranteeFruit", treeShakeGuaranteeFruit),
                CommandBuild.get2("gemTreeFullGemDrops", gemTreeFullGemDrops),
                CommandBuild.get2("removeGraveyardVisuals", removeGraveyardVisuals),
                CommandBuild.get2("antiGriefExplosions", antiGriefExplosions),
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

                // 瞬传与微距传送
                UIBuild.get2(instantRecall, "消除魔镜/冰雪镜/手机/海螺/回程药水等施法前摇延迟，点击瞬间传送", "Images/Item_50", "魔镜/回程药水瞬传"),
                UIBuild.get2(altRightClickTeleport, "在游戏世界中按住 Alt 并右击鼠标，瞬移至光标位置（自动智能吸附空位，便于钻入狭窄小角落）", "Images/Item_1326", "Alt+右键微距传送"),

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

            return uis;
        }
    }
}

