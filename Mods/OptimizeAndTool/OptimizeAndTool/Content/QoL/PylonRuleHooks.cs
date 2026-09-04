using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 晶塔传送细分规则门控（对齐 ImproveGame 语义，基于 HookGen 强类型 On_ 门控）：
    /// 1. 晶塔传送无需 NPC（TeleportPylonsSystem.DoesPositionHaveEnoughNPCs 恒真）；
    /// 2. 晶塔传送无视生物群落（DoesPylonAcceptTeleportation 恒真）。
    /// 注：原版晶塔传送本身没有"危险/Boss 在场"检查，无需实现"无视危险"。
    /// 作者: SaintCirno9
    /// </summary>
    internal static class PylonRuleHooks
    {
        public static GetSetReset<bool> EnableNoNPCCheck = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableIgnoreBiome = new GetSetReset<bool>(false, false);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_TeleportPylonsSystem.DoesPositionHaveEnoughNPCs += Hook_DoesPositionHaveEnoughNPCs;
            On_TeleportPylonsSystem.DoesPylonAcceptTeleportation += Hook_DoesPylonAcceptTeleportation;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_TeleportPylonsSystem.DoesPositionHaveEnoughNPCs -= Hook_DoesPositionHaveEnoughNPCs;
            On_TeleportPylonsSystem.DoesPylonAcceptTeleportation -= Hook_DoesPylonAcceptTeleportation;
            _registered = false;
        }

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

        private static bool Hook_DoesPositionHaveEnoughNPCs(On_TeleportPylonsSystem.orig_DoesPositionHaveEnoughNPCs orig, int numberOfNPCsRequiredToGenerateTeleportationMarker, Point16 centerTileCoords)
        {
            if (EnableNoNPCCheck.val)
            {
                return true;
            }

            return orig(numberOfNPCsRequiredToGenerateTeleportationMarker, centerTileCoords);
        }

        private static bool Hook_DoesPylonAcceptTeleportation(On_TeleportPylonsSystem.orig_DoesPylonAcceptTeleportation orig, TeleportPylonsSystem self, TeleportPylonInfo info, Player player)
        {
            if (EnableIgnoreBiome.val)
            {
                return true;
            }

            return orig(self, info, player);
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class PylonRules
    {
        public static GetSetReset<bool> EnableNoNPCCheck => PylonRuleHooks.EnableNoNPCCheck;
        public static GetSetReset<bool> EnableIgnoreBiome => PylonRuleHooks.EnableIgnoreBiome;

        public static List<CommandObject> GetCO() => PylonRuleHooks.GetCO();
        public static List<UIElement> GetUI() => PylonRuleHooks.GetUI();
    }
}
