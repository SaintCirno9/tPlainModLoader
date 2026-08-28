using CommandHelp;
using HarmonyLib;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Terraria;
using Terraria.GameContent.Achievements;
using Terraria.ID;
using Terraria.IO;
using Terraria.UI;
using TPML.Content;
using TPML.Content.Engine;
using TPML.Content.UI;

namespace OptimizeAndTool.Content.QoL
{
    /// <summary>
    /// 解除血量上限补丁：
    /// 1. 生命水晶与生命果无限制自由使用（直至 short.MaxValue 32767）
    /// 2. 读档移除 500 HP 硬编码截断
    /// 3. Tooltip 显示当前生命上限与使用提示
    /// 
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    public class UncapMaxLifePatch
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        static UncapMaxLifePatch()
        {
            Initialize();
        }

        public static void Initialize()
        {
            ContentHookDispatcher.RegisterHookInstances(new ILoadable[] { new UncapMaxLifeGlobalItem() });
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
        [HarmonyPatch(typeof(Player), "ItemCheck_UseLifeCrystal", new Type[] { typeof(Item) })]
        [HarmonyPrefix]
        public static bool ItemCheck_UseLifeCrystal_Prefix(Player __instance, Item sItem)
        {
            if (!Enable.val) return true;

            if (sItem.type == ItemID.LifeCrystal && __instance.itemAnimation > 0 && __instance.statLifeMax < short.MaxValue && __instance.ItemTimeIsZero)
            {
                __instance.ApplyItemTime(sItem);
                __instance.statLifeMax += 20;
                __instance.statLifeMax2 += 20;
                __instance.statLife += 20;
                if (Main.myPlayer == __instance.whoAmI)
                {
                    __instance.HealEffect(20);
                }
                AchievementsHelper.HandleSpecialEvent(__instance, 0);
            }

            return false;
        }

        /// <summary>
        /// 拦截生命果使用逻辑：开启时移除 statLifeMax >= 400 && statLifeMax < 500 限制
        /// </summary>
        [HarmonyPatch(typeof(Player), "ItemCheck_UseLifeFruit", new Type[] { typeof(Item) })]
        [HarmonyPrefix]
        public static bool ItemCheck_UseLifeFruit_Prefix(Player __instance, Item sItem)
        {
            if (!Enable.val) return true;

            if (sItem.type == ItemID.LifeFruit && __instance.itemAnimation > 0 && __instance.statLifeMax < short.MaxValue && __instance.ItemTimeIsZero)
            {
                __instance.ApplyItemTime(sItem);
                __instance.statLifeMax += 5;
                __instance.statLifeMax2 += 5;
                __instance.statLife += 5;
                if (Main.myPlayer == __instance.whoAmI)
                {
                    __instance.HealEffect(5);
                }
                AchievementsHelper.HandleSpecialEvent(__instance, 2);
            }

            return false;
        }

        /// <summary>
        /// 拦截 Player.Deserialize 中的 if (newPlayer.statLifeMax > 500) 截断逻辑
        /// 将 500 常量重定向为动态上限（开启时为 short.MaxValue，关闭时为 500）
        /// </summary>
        [HarmonyPatch(typeof(Player), "Deserialize")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DeserializeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo limitGetter = typeof(UncapMaxLifePatch).GetMethod(nameof(GetDeserializedLifeLimit), BindingFlags.Public | BindingFlags.Static);

            foreach (CodeInstruction instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_I4 && (instr.operand is int val && val == 500))
                {
                    yield return new CodeInstruction(OpCodes.Call, limitGetter);
                }
                else
                {
                    yield return instr;
                }
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
            if (!UncapMaxLifePatch.Enable.val) return;

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
}
