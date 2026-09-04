using CommandHelp;
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
    /// 物品最大堆叠 9999 门控（基于 HookGen 强类型 On_ 门控）
    /// 作者: SaintCirno9
    /// </summary>
    internal class ItemMaxStackHooks
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Item.SetDefaults += Hook_SetDefaults;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Item.SetDefaults -= Hook_SetDefaults;
            _registered = false;
        }

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

        private static void Hook_SetDefaults(On_Item.orig_SetDefaults orig, Item self, int Type, ItemVariant variant)
        {
            orig(self, Type, variant);

            if (self == null || !Enable.val) return;

            // 钱币保留原版堆叠（100），避免进位计算逻辑异常
            if (IsCoin(self.type)) return;

            // 任务鱼若开启堆叠则解除唯一限制并设为 9999
            if (self.questItem && AnglerQuestOptimizationHooks.EnableQuestFishStack.val)
            {
                self.maxStack = 9999;
                self.uniqueStack = false;
                return;
            }

            // 普通可堆叠物品设置为 9999
            if (self.maxStack > 1)
            {
                self.maxStack = 9999;
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal class ItemMaxStackPatch : ItemMaxStackHooks
    {
    }
}
