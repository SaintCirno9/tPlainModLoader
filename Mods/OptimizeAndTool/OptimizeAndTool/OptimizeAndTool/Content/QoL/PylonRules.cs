using CommandHelp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 晶塔传送细分规则（对齐 ImproveGame 语义，现有 pylonFreeTeleport 为整体开关，此处提供细分）：
    /// 1. 晶塔传送无需 NPC（TeleportPylonsSystem.DoesPositionHaveEnoughNPCs 恒真）；
    /// 2. 晶塔传送无视生物群落（DoesPylonAcceptTeleportation 恒真）。
    /// 注：原版晶塔传送本身没有"危险/Boss 在场"检查，无需实现"无视危险"。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class PylonRules
    {
        public static GetSetReset<bool> EnableNoNPCCheck = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableIgnoreBiome = new GetSetReset<bool>(false, false);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("pylonNoNPCNeeded", EnableNoNPCCheck),
                CommandBuild.get2("pylonIgnoreBiome", EnableIgnoreBiome)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableNoNPCCheck, "晶塔传送不再要求周围有足够城镇 NPC", "Images/Item_4875", "晶塔传送无需 NPC"),
                UIBuild.get2(EnableIgnoreBiome, "晶塔传送无视目标晶塔的生物群落限制（如雪山/丛林/神圣等）", "Images/Item_4875", "晶塔传送无视群落")
            };
        }
    }

    /// <summary>无需 NPC：DoesPositionHaveEnoughNPCs（TeleportPylonsSystem.cs:224）恒真</summary>
    [HarmonyPatch(typeof(TeleportPylonsSystem), nameof(TeleportPylonsSystem.DoesPositionHaveEnoughNPCs))]
    internal static class Patch_PylonNoNPC
    {
        [HarmonyPostfix]
        internal static void Postfix(ref bool __result)
        {
            if (!PylonRules.EnableNoNPCCheck.val) return;
            __result = true;
        }
    }

    /// <summary>无视群落：DoesPylonAcceptTeleportation（TeleportPylonsSystem.cs:254）恒真</summary>
    [HarmonyPatch(typeof(TeleportPylonsSystem), nameof(TeleportPylonsSystem.DoesPylonAcceptTeleportation))]
    internal static class Patch_PylonIgnoreBiome
    {
        [HarmonyPrefix]
        internal static bool Prefix(ref bool __result)
        {
            if (!PylonRules.EnableIgnoreBiome.val) return true;
            __result = true;
            return false;
        }
    }
}
