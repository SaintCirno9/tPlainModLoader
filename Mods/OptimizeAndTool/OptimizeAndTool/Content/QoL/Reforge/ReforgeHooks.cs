using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.Reforge
{
    /// <summary>
    /// 重铸逻辑门控（基于 HookGen 强类型 On_ / IL_ 门控）：
    /// 1. 当玩家在重铸优化面板中选中了目标前缀时，拦截原版单次重铸并执行瞬间模拟自动重铸；
    /// 2. 拦截存款绘制，重定位至前缀选择面板右侧、垃圾桶下方；
    /// 3. 修补 Main.DrawInventory 中重铸锤的坐标，移动到垃圾桶左侧。
    /// 作者: SaintCirno9
    /// </summary>
    public static class ReforgeHooks
    {
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Main.ReforgeItemInReforgeSlot += Hook_ReforgeItemInReforgeSlot;
            On_ItemSlot.DrawSavings += Hook_DrawSavings;
            IL_Main.DrawInventory += Hook_DrawInventory_IL;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Main.ReforgeItemInReforgeSlot -= Hook_ReforgeItemInReforgeSlot;
            On_ItemSlot.DrawSavings -= Hook_DrawSavings;
            IL_Main.DrawInventory -= Hook_DrawInventory_IL;
            _registered = false;
        }

        private static void Hook_ReforgeItemInReforgeSlot(On_Main.orig_ReforgeItemInReforgeSlot orig)
        {
            if (ReforgeOptimization.Enable.val &&
                ReforgeOptimization.SelectedPrefixId > 0 &&
                !Main.reforgeItem.IsAir &&
                Main.reforgeItem.prefix != ReforgeOptimization.SelectedPrefixId)
            {
                ReforgeOptimization.PerformAutoReforge(Main.reforgeItem, ReforgeOptimization.SelectedPrefixId);
                return; // 拦截原版单次 Roll，避免覆盖自动重铸结果
            }

            orig(); // 未选目标或已是目标词条时，执行原版单次重铸
        }

        private static void Hook_DrawSavings(On_ItemSlot.orig_DrawSavings orig, SpriteBatch sb, float shopx, float shopy, bool horizontal)
        {
            if (Main.InReforgeMenu && ReforgeOptimization.Enable.val)
            {
                // 重铸模式下将存款移至选择面板右侧、垃圾桶下方
                shopx = 425f;
                shopy = 328f;
                horizontal = false; // 垂直紧凑排列
            }

            orig(sb, shopx, shopy, horizontal);
        }

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

        public static int ModifyTalkNPCResult(int talkNPC)
        {
            if (talkNPC >= 0)
                return talkNPC;

            if (ReforgeOptimization.PortableReforgeActive)
                return 0; // 伪造非负索引，绕过 talkNPC == -1 退出判定

            return -1;
        }

        private static void Hook_DrawInventory_IL(ILContext il)
        {
            var cursor = new ILCursor(il);

            // 1. 便携重铸支持：拦截 InReforgeMenu 分支中对 talkNPC == -1 的阻断
            var inReforgeMenuField = typeof(Main).GetField(nameof(Main.InReforgeMenu), BindingFlags.Public | BindingFlags.Static);
            var getTalkNPCMethod = typeof(Player).GetProperty(nameof(Player.talkNPC))?.GetGetMethod()
                ?? typeof(Player).GetMethod("get_talkNPC", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var modifyTalkNPCMethod = typeof(ReforgeHooks).GetMethod(nameof(ModifyTalkNPCResult), BindingFlags.Public | BindingFlags.Static);

            if (inReforgeMenuField != null && getTalkNPCMethod != null && modifyTalkNPCMethod != null)
            {
                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdsfld(inReforgeMenuField)))
                {
                    if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCallvirt(getTalkNPCMethod)))
                    {
                        cursor.Emit(OpCodes.Call, modifyTalkNPCMethod);
                    }
                }
            }

            // 2. 移动游标到 DrawSavings 调用之后（重铸锤位置重定向）
            var drawSavingsMethod = typeof(ItemSlot).GetMethod(nameof(ItemSlot.DrawSavings), new[] { typeof(SpriteBatch), typeof(float), typeof(float), typeof(bool) });
            var modifyXMethod = typeof(ReforgeHooks).GetMethod(nameof(ModifyHammerX), BindingFlags.Public | BindingFlags.Static);
            var modifyYMethod = typeof(ReforgeHooks).GetMethod(nameof(ModifyHammerY), BindingFlags.Public | BindingFlags.Static);

            if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall(drawSavingsMethod)))
                return;

            // 匹配 70 + add 计算 num61
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(70), instr => instr.MatchAdd()))
            {
                cursor.Emit(OpCodes.Call, modifyXMethod);
            }

            // 匹配 40 + add 计算 num62
            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcI4(40), instr => instr.MatchAdd()))
            {
                cursor.Emit(OpCodes.Call, modifyYMethod);
            }
        }
    }

    /// <summary>
    /// 兼容别名类
    /// </summary>
    public static class Patch_Reforge
    {
    }

    public static class Patch_DrawSavings
    {
    }

    public static class Patch_DrawInventory_ReforgeHammer
    {
    }
}
