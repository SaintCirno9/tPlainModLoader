using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 掉落与环境规则（对齐 ImproveGame 语义）：
    /// 1. 史莱姆必定内含物品（关闭则遵循原版概率）；
    /// 2. 熔岩史莱姆/地狱蝙蝠不生成熔岩（专家/大师的熔岩史莱姆、GFB 种子的地狱蝙蝠与熔岩蝙蝠）。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class SlimeAndLava
    {
        public static GetSetReset<bool> EnableSlimeExDrop = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableLavalessLavaSlime = new GetSetReset<bool>(false, false);

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
    }

    /// <summary>
    /// 史莱姆必定含物品：在 AI_001_Slimes 首次初始化体内物品前（原版 NPC.cs:60951 概率块），
    /// 直接调用原版生成器 AI_001_Slimes_GenerateItemInsideBody 写入 ai[1]，绕过概率判定。
    /// 与原版一致的排除：netMode=客户端、非 SlimeCanContainItems、value&lt;=0（分裂体/特殊体）、已初始化。
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.AI_001_Slimes))]
    internal static class Patch_SlimeExDrop
    {
        [HarmonyPrefix]
        internal static void Prefix(NPC __instance)
        {
            if (!SlimeAndLava.EnableSlimeExDrop.val) return;
            if (Main.netMode == 1) return;
            if (!NPCID.Sets.SlimeCanContainItems[__instance.type]) return;
            if (__instance.ai[1] != 0f) return;
            if (__instance.value <= 0f) return;
            __instance.ai[1] = __instance.AI_001_Slimes_GenerateItemInsideBody(__instance.ai[0] == -999f);
            __instance.netUpdate = true;
        }
    }

    /// <summary>
    /// 熔岩史莱姆/GFB 蝙蝠不产熔岩：HitEffect 内专家熔岩史莱姆喷熔岩（NPC.cs:86561，条件含
    /// Main.expertMode 且 !Main.remixWorld）与 GFB 地狱/熔岩蝙蝠喷熔岩（NPC.cs:86602，条件含
    /// Main.getGoodWorld）。Main.expertMode 为只读属性不可改，故对 type 59 临时置
    /// Main.remixWorld=true（86561 的 !remixWorld 判 false，且 86602 对 type 59 直接 return，
    /// 无副作用）；对 60/151 临时置 Main.getGoodWorld=false。Postfix 恢复。
    /// </summary>
    [HarmonyPatch(typeof(NPC), nameof(NPC.HitEffect))]
    internal static class Patch_LavalessLavaSlime
    {
        private static bool savedRemixWorld = false;
        private static bool savedGetGoodWorld = false;
        private static bool modified = false;

        [HarmonyPrefix]
        internal static void Prefix(NPC __instance)
        {
            if (!SlimeAndLava.EnableLavalessLavaSlime.val) return;
            savedRemixWorld = false;
            savedGetGoodWorld = false;
            modified = false;

            // 专家/大师熔岩史莱姆：使 86561 的 !Main.remixWorld 判 false，跳过熔岩生成。
            // 仅在原本 !remixWorld 时才改写并登记恢复，避免 GFB（remixWorld 本就为 true）被误恢复。
            if (__instance.type == 59 && Main.expertMode && !Main.remixWorld)
            {
                savedRemixWorld = true;
                Main.remixWorld = true;
                modified = true;
            }
            // GFB 种子地狱蝙蝠/熔岩蝙蝠：使 86602 提前 return，跳过熔岩生成
            else if ((__instance.type == 60 || __instance.type == 151) && Main.remixWorld && Main.getGoodWorld)
            {
                savedGetGoodWorld = true;
                Main.getGoodWorld = false;
                modified = true;
            }
        }

        [HarmonyPostfix]
        internal static void Postfix()
        {
            if (!modified) return;
            if (savedRemixWorld) Main.remixWorld = false;
            if (savedGetGoodWorld) Main.getGoodWorld = true;
            modified = false;
        }
    }
}
