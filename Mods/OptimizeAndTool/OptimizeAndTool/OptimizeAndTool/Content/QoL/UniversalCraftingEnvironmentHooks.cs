using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;
using TPML.Core.Logging;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 全环境自由制作门控（基于 HookGen 强类型 On_ 门控，零反射，100% 对齐规范）：<br/>
    /// 制作时无视生物群落环境（雪地、墓地、火炬神恩惠、Mechdusa）与液体环境（水、岩浆、蜂蜜），只要拥有所需材料和对应制作站图格即可直接合成。<br/>
    /// 作者: SaintCirno9
    /// </summary>
    internal static class UniversalCraftingEnvironmentHooks
    {
        private static readonly ILogger Logger = LogManager.GetLogger("UniversalCraftingEnvironment");
        private static bool _registered = false;

        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("universalCraftingEnvironment", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "制作时无视生物群落与液体环境要求（如雪地、墓地、火炬神恩惠、水/岩浆/蜂蜜），只要拥有对应制作站即可直接合成", "Images/Item_361", "全环境自由制作")
            };
        }

        public static void RegisterAll()
        {
            if (_registered) return;

            On_Recipe.PlayerMeetsEnvironmentConditions += Hook_PlayerMeetsEnvironmentConditions;
            _registered = true;
            Logger.Info("★ 全环境自由制作 (UniversalCraftingEnvironment) MonoMod On_ 门控已成功注册");
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;

            On_Recipe.PlayerMeetsEnvironmentConditions -= Hook_PlayerMeetsEnvironmentConditions;
            _registered = false;
            Logger.Info("全环境自由制作 (UniversalCraftingEnvironment) MonoMod On_ 门控已解绑");
        }

        private static bool Hook_PlayerMeetsEnvironmentConditions(On_Recipe.orig_PlayerMeetsEnvironmentConditions orig, Recipe self, Player player, List<string> missingObjects)
        {
            if (!Enable.val)
            {
                return orig(self, player, missingObjects);
            }

            // 无视生物群落（雪地、墓地、火炬神恩惠、天顶种子Mechdusa）与液体环境（水、岩浆、蜂蜜）
            // 仅保留制作站图格（requiredTile）的要求
            if (self.requiredTile >= 0 && (player.adjTile == null || !player.adjTile[self.requiredTile]))
            {
                if (missingObjects != null)
                {
                    missingObjects.Add(Recipe.GetRequiredTileName(self.requiredTile));
                }
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal static class UniversalCraftingEnvironment
    {
        public static GetSetReset<bool> Enable => UniversalCraftingEnvironmentHooks.Enable;
        public static List<CommandObject> GetCO() => UniversalCraftingEnvironmentHooks.GetCO();
        public static List<UIElement> GetUI() => UniversalCraftingEnvironmentHooks.GetUI();
    }
}
