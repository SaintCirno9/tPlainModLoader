using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Items;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 物品最大堆叠 9999 补丁
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Item))]
    internal class ItemMaxStackPatch
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("itemMaxStack", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "使所有可堆叠物品最大堆叠达到 9999（钱币除外）", "Images/Item_73", "物品最大堆叠 9999")
            };
        }

        public static bool IsCoin(int type)
        {
            return type == ItemID.CopperCoin ||
                   type == ItemID.SilverCoin ||
                   type == ItemID.GoldCoin ||
                   type == ItemID.PlatinumCoin;
        }

        [HarmonyPatch(nameof(Item.SetDefaults), typeof(int), typeof(ItemVariant))]
        [HarmonyPostfix]
        public static void SetDefaultsPostfix(Item __instance, int Type, ItemVariant variant)
        {
            if (__instance == null || !Enable.val) return;

            // 钱币保留原版堆叠（100），避免进位计算逻辑异常
            if (IsCoin(__instance.type)) return;

            // 任务鱼若开启堆叠则解除唯一限制并设为 9999
            if (__instance.questItem && AnglerQuestOptimization.EnableQuestFishStack.val)
            {
                __instance.maxStack = 9999;
                __instance.uniqueStack = false;
                return;
            }

            // 普通可堆叠物品设置为 9999
            if (__instance.maxStack > 1)
            {
                __instance.maxStack = 9999;
            }
        }
    }
}
