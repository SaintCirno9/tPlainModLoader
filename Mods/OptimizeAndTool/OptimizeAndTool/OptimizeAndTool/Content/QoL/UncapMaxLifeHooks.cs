using CommandHelp;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using TPML.Content.Engine;
using TPML.Content.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 解除血量上限门控（基于 HookGen 强类型 On_ / IL_ 门控）：
    /// 1. 生命水晶与生命果无限制自由使用（直至 short.MaxValue 32767）
    /// 2. 读档移除 500 HP 硬编码截断
    /// 3. Tooltip 显示当前生命上限与使用提示
    /// 
    /// 作者: SaintCirno9
    /// </summary>
    public class UncapMaxLifeHooks
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        private static bool _registered = false;

        static UncapMaxLifeHooks()
        {
            Initialize();
        }

        public static void Initialize()
        {
            ContentHookDispatcher.RegisterHookInstances(new ILoadable[] { new UncapMaxLifeGlobalItem() });
        }

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Player.ItemCheck_UseLifeCrystal += Hook_ItemCheck_UseLifeCrystal;
            On_Player.ItemCheck_UseLifeFruit += Hook_ItemCheck_UseLifeFruit;
            IL_Player.Deserialize += Hook_Deserialize_IL;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Player.ItemCheck_UseLifeCrystal -= Hook_ItemCheck_UseLifeCrystal;
            On_Player.ItemCheck_UseLifeFruit -= Hook_ItemCheck_UseLifeFruit;
            IL_Player.Deserialize -= Hook_Deserialize_IL;
            _registered = false;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("uncapMaxLife", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "解除生命水晶与生命果的使用门槛与上限限制（支持无限堆叠至 32767 HP，自动无损读档与 UI 缩放）", "Images/Item_29", "解除生命上限")
            };
        }

        /// <summary>
        /// 拦截生命水晶使用逻辑：开启时移除 statLifeMax < 400 限制
        /// </summary>
        private static void Hook_ItemCheck_UseLifeCrystal(On_Player.orig_ItemCheck_UseLifeCrystal orig, Player self, Item sItem)
        {
            if (!Enable.val)
            {
                orig(self, sItem);
                return;
            }

            if (sItem.type == ItemID.LifeCrystal && self.itemAnimation > 0 && self.statLifeMax < short.MaxValue && self.ItemTimeIsZero)
            {
                self.ApplyItemTime(sItem);
                self.statLifeMax += 20;
                self.statLifeMax2 += 20;
                self.statLife += 20;
                if (Main.myPlayer == self.whoAmI)
                {
                    self.HealEffect(20);
                }
                AchievementsHelper.HandleSpecialEvent(self, 0);
            }
        }

        /// <summary>
        /// 拦截生命果使用逻辑：开启时移除 statLifeMax >= 400 && statLifeMax < 500 限制
        /// </summary>
        private static void Hook_ItemCheck_UseLifeFruit(On_Player.orig_ItemCheck_UseLifeFruit orig, Player self, Item sItem)
        {
            if (!Enable.val)
            {
                orig(self, sItem);
                return;
            }

            if (sItem.type == ItemID.LifeFruit && self.itemAnimation > 0 && self.statLifeMax < short.MaxValue && self.ItemTimeIsZero)
            {
                self.ApplyItemTime(sItem);
                self.statLifeMax += 5;
                self.statLifeMax2 += 5;
                self.statLife += 5;
                if (Main.myPlayer == self.whoAmI)
                {
                    self.HealEffect(5);
                }
                AchievementsHelper.HandleSpecialEvent(self, 2);
            }
        }

        /// <summary>
        /// 拦截 Player.Deserialize 中的 if (newPlayer.statLifeMax > 500) 截断逻辑
        /// 将 500 常量重定向为动态上限（开启时为 short.MaxValue，关闭时为 500）
        /// </summary>
        private static void Hook_Deserialize_IL(ILContext il)
        {
            var cursor = new ILCursor(il);
            MethodInfo limitGetter = typeof(UncapMaxLifeHooks).GetMethod(nameof(GetDeserializedLifeLimit), BindingFlags.Public | BindingFlags.Static);

            while (cursor.TryGotoNext(MoveType.Before, instr => instr.MatchLdcI4(500)))
            {
                cursor.Remove();
                cursor.Emit(OpCodes.Call, limitGetter);
            }
        }

        public static int GetDeserializedLifeLimit()
        {
            return Enable.val ? short.MaxValue : 500;
        }
    }

    /// <summary>
    /// 全局物品 Tooltip 增强：显示当前生命上限与使用提示
    /// </summary>
    public class UncapMaxLifeGlobalItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!UncapMaxLifeHooks.Enable.val) return;

            Player player = Main.LocalPlayer;
            if (player == null || !player.active) return;

            if (item.type == ItemID.LifeCrystal)
            {
                tooltips.Add(new TooltipLine(null, "UncapLifeCrystal",
                    $"[c/FF7788:当前生命上限: ] [c/FFFFFF:{player.statLifeMax}] [c/88FF88:(已解除400上限，可直接使用 +20)]"));
            }
            else if (item.type == ItemID.LifeFruit)
            {
                tooltips.Add(new TooltipLine(null, "UncapLifeFruit",
                    $"[c/FFD700:当前生命上限: ] [c/FFFFFF:{player.statLifeMax}] [c/88FF88:(已解除500上限，可直接使用 +5)]"));
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    public class UncapMaxLifePatch : UncapMaxLifeHooks
    {
    }
}
