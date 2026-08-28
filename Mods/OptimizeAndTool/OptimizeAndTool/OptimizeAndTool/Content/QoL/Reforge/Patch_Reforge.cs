using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Reflection.Emit;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.Reforge
{
    /// <summary>
    /// 重铸逻辑拦截补丁
    /// 当玩家在重铸优化面板中选中了目标前缀时，拦截原版单次重铸并执行瞬间模拟自动重铸；
    /// 未选中或已达到目标前缀时，保持原版单次重铸行为。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Main), nameof(Main.ReforgeItemInReforgeSlot))]
    public static class Patch_Reforge
    {
        [HarmonyPrefix]
        public static bool ReforgeItemInReforgeSlotPrefix()
        {
            if (ReforgeOptimization.Enable.val &&
                ReforgeOptimization.SelectedPrefixId > 0 &&
                !Main.reforgeItem.IsAir &&
                Main.reforgeItem.prefix != ReforgeOptimization.SelectedPrefixId)
            {
                ReforgeOptimization.PerformAutoReforge(Main.reforgeItem, ReforgeOptimization.SelectedPrefixId);
                return false; // 拦截原版单次 Roll，避免覆盖自动重铸结果
            }

            return true; // 未选目标或已是目标词条时，执行原版单次重铸
        }
    }

    /// <summary>
    /// 拦截存款绘制，重定位至前缀选择面板右侧、垃圾桶下方
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(ItemSlot), nameof(ItemSlot.DrawSavings), typeof(SpriteBatch), typeof(float), typeof(float), typeof(bool))]
    public static class Patch_DrawSavings
    {
        [HarmonyPrefix]
        public static void Prefix(ref float shopx, ref float shopy, ref bool horizontal)
        {
            if (Main.InReforgeMenu && ReforgeOptimization.Enable.val)
            {
                // 重铸模式下将存款移至选择面板右侧、垃圾桶下方
                shopx = 425f;
                shopy = 328f;
                horizontal = false; // 垂直紧凑排列
            }
        }
    }

    /// <summary>
    /// 修补 Main.DrawInventory 中重铸锤的坐标，移动到垃圾桶左侧
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch(typeof(Main), "DrawInventory")]
    public static class Patch_DrawInventory_ReforgeHammer
    {
        public static int ModifyHammerX(int defaultX)
        {
            if (ReforgeOptimization.Enable.val)
            {
                return 412; // 垃圾桶 (448) 左侧
            }
            return defaultX;
        }

        public static int ModifyHammerY(int defaultY)
        {
            if (ReforgeOptimization.Enable.val)
            {
                return 280; // 垃圾桶中心水平对齐
            }
            return defaultY;
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            var drawSavingsMethod = AccessTools.Method(typeof(ItemSlot), nameof(ItemSlot.DrawSavings), new[] { typeof(SpriteBatch), typeof(float), typeof(float), typeof(bool) });
            var reforgeField = AccessTools.Field(typeof(TextureAssets), nameof(TextureAssets.Reforge));
            var modifyXMethod = AccessTools.Method(typeof(Patch_DrawInventory_ReforgeHammer), nameof(ModifyHammerX));
            var modifyYMethod = AccessTools.Method(typeof(Patch_DrawInventory_ReforgeHammer), nameof(ModifyHammerY));

            int savingsIndex = -1;
            int reforgeIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (savingsIndex == -1 && list[i].Calls(drawSavingsMethod))
                {
                    savingsIndex = i;
                }
                if (savingsIndex != -1 && list[i].LoadsField(reforgeField))
                {
                    reforgeIndex = i;
                    break;
                }
            }

            if (savingsIndex != -1 && reforgeIndex != -1)
            {
                for (int i = savingsIndex; i < reforgeIndex; i++)
                {
                    // 匹配 70 + add 计算 num61
                    if ((list[i].LoadsConstant(70) || (list[i].opcode == OpCodes.Ldc_I4_S && (sbyte)list[i].operand == 70)) &&
                        i + 1 < list.Count && list[i + 1].opcode == OpCodes.Add)
                    {
                        list.Insert(i + 2, new CodeInstruction(OpCodes.Call, modifyXMethod));
                        reforgeIndex++;
                        i += 2;
                    }
                    // 匹配 40 + add 计算 num62
                    else if ((list[i].LoadsConstant(40) || (list[i].opcode == OpCodes.Ldc_I4_S && (sbyte)list[i].operand == 40)) &&
                             i + 1 < list.Count && list[i + 1].opcode == OpCodes.Add)
                    {
                        list.Insert(i + 2, new CodeInstruction(OpCodes.Call, modifyYMethod));
                        reforgeIndex++;
                        i += 2;
                    }
                }
            }

            return list;
        }
    }
}
