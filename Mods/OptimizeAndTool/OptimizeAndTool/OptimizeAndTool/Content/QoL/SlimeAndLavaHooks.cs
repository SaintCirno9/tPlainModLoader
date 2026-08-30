using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 掉落与环境规则门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：
    /// 1. 史莱姆必定内含物品（关闭则遵循原版概率）；
    /// 2. 熔岩史莱姆/地狱蝙蝠不生成熔岩（专家/大师的熔岩史莱姆、GFB 种子的地狱蝙蝠与熔岩蝙蝠）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class SlimeAndLavaHooks
    {
        public static GetSetReset<bool> EnableSlimeExDrop = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableLavalessLavaSlime = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_NPC.AI_001_Slimes += Hook_AI_001_Slimes;
            On_NPC.HitEffect += Hook_HitEffect;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_NPC.AI_001_Slimes -= Hook_AI_001_Slimes;
            On_NPC.HitEffect -= Hook_HitEffect;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("slimeExDrop", EnableSlimeExDrop),
                CommandBuild.get2("lavalessLavaSlime", EnableLavalessLavaSlime)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableSlimeExDrop, "史莱姆被击杀时必定掉落内含的随机物品（关闭则遵循原版概率）", "Images/Item_11", "史莱姆必定含物品"),
                UIBuild.get2(EnableLavalessLavaSlime, "专家/大师熔岩史莱姆、GFB 种子地狱蝙蝠被击杀时不再生成熔岩", "Images/Item_2432", "熔岩史莱姆不生成熔岩")
            };
        }

        private static void Hook_AI_001_Slimes(On_NPC.orig_AI_001_Slimes orig, NPC self)
        {
            if (EnableSlimeExDrop.val && Main.netMode != 1 && NPCID.Sets.SlimeCanContainItems[self.type] &&
                self.ai[1] == 0f && self.value > 0f)
            {
                self.ai[1] = self.AI_001_Slimes_GenerateItemInsideBody(self.ai[0] == -999f);
                self.netUpdate = true;
            }

            orig(self);
        }

        private static void Hook_HitEffect(On_NPC.orig_HitEffect orig, NPC self, int hitDirection, double dmg)
        {
            bool savedRemixWorld = false;
            bool savedGetGoodWorld = false;
            bool modified = false;

            if (EnableLavalessLavaSlime.val)
            {
                if (self.type == 59 && Main.expertMode && !Main.remixWorld)
                {
                    savedRemixWorld = true;
                    Main.remixWorld = true;
                    modified = true;
                }
                else if ((self.type == 60 || self.type == 151) && Main.remixWorld && Main.getGoodWorld)
                {
                    savedGetGoodWorld = true;
                    Main.getGoodWorld = false;
                    modified = true;
                }
            }

            try
            {
                orig(self, hitDirection, dmg);
            }
            finally
            {
                if (modified)
                {
                    if (savedRemixWorld) Main.remixWorld = false;
                    if (savedGetGoodWorld) Main.getGoodWorld = true;
                }
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class SlimeAndLava
    {
        public static GetSetReset<bool> EnableSlimeExDrop => SlimeAndLavaHooks.EnableSlimeExDrop;
        public static GetSetReset<bool> EnableLavalessLavaSlime => SlimeAndLavaHooks.EnableLavalessLavaSlime;

        public static List<CommandObject> GetCO() => SlimeAndLavaHooks.GetCO();
        public static List<UIElement> GetUI() => SlimeAndLavaHooks.GetUI();
    }
}
