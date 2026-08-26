using CommandHelp;
using HarmonyLib;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.Fishing
{
    /// <summary>
    /// 全自动钓鱼核心系统
    /// 对齐 AutoFisher：
    /// 1. 自动咬钩与挂机结算：渔获在 FishingCheck 结果产生后直接交付，浮标继续留在水中挂机连钓，无需反复收竿重抛；
    /// 2. 极速咬钩：消除等待时间，瞬间触发判定；
    /// 3. 任意手持物品不断线保护：切换武器、工具、甚至空手时浮标与鱼线均不消失；
    /// 4. 微光/岩浆任意垂钓与无断线惩罚；
    /// 5. 消除时间、天气、月相与负幸运等环境折损惩罚。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class AutoFishingSystem
    {
        public static GetSetReset<bool> EnableAutoFish = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableInstantBite = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableHoldItemProtection = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableFishInShimmer = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableFishInLavaAnywhere = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableOnlyPositiveInfluences = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableIgnoreNegativeLuck = new GetSetReset<bool>(true, true);

        public struct BobberHoldState
        {
            public int OriginalSelected;
            public int OriginalPole;
            public int OriginalShoot;
            public int OriginalHoldStyle;
            public bool IsFaked;
        }

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("autoFish", EnableAutoFish),
                CommandBuild.get2("instantBite", EnableInstantBite),
                CommandBuild.get2("fishingHoldItemProtect", EnableHoldItemProtection),
                CommandBuild.get2("fishInShimmer", EnableFishInShimmer),
                CommandBuild.get2("fishInLavaAnywhere", EnableFishInLavaAnywhere),
                CommandBuild.get2("onlyPositiveFishingInfluences", EnableOnlyPositiveInfluences),
                CommandBuild.get2("ignoreNegativeLuck", EnableIgnoreNegativeLuck)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(EnableAutoFish, "浮标咬钩时直接结算渔获并留在水中继续挂机，无需手动收竿", "Images/Item_2291", "自动咬钩连钓"),
                UIBuild.get2(EnableInstantBite, "浮标入水后立刻完成咬钩判定，消除漫长的等待时间", "Images/Item_2373", "极速瞬间咬钩"),
                UIBuild.get2(EnableHoldItemProtection, "钓鱼抛出浮标后，切换任意手持武器/工具/空手均不会断线或丢失浮标", "Images/Item_2360", "切换物品不断线"),
                UIBuild.get2(EnableFishInShimmer, "允许在微光液体中正常垂钓，浮标不会被微光击碎销毁", "Images/Item_5358", "微光水体垂钓"),
                UIBuild.get2(EnableFishInLavaAnywhere, "无需防熔岩钓具或地狱鱼饵，任何普通钓竿均可直接在岩浆中垂钓", "Images/Item_4877", "全能岩浆垂钓"),
                UIBuild.get2(EnableOnlyPositiveInfluences, "消除时间、天气与月相的负面渔力惩罚，环境渔力倍率不低于 1.0x", "Images/Item_3095", "仅正向环境倍率"),
                UIBuild.get2(EnableIgnoreNegativeLuck, "消除负向幸运值对渔力扣除 10%~40% 的惩罚，保留正向幸运增益", "Images/Item_4381", "忽略负幸运惩罚")
            };
        }

        #region 1. 浮标挂机与切物保护

        [HarmonyPatch(typeof(Projectile), "AI_061_FishingBobber")]
        [HarmonyPrefix]
        public static bool AI_061_FishingBobberPrefix(Projectile __instance, out BobberHoldState __state)
        {
            __state = new BobberHoldState
            {
                OriginalSelected = -1,
                OriginalPole = 0,
                OriginalShoot = 0,
                OriginalHoldStyle = 0,
                IsFaked = false
            };

            if (__instance.owner != Main.myPlayer)
                return true;

            Player player = Main.player[__instance.owner];
            if (player == null || !player.active)
                return true;

            // 切物保护：玩家切换手持物品时，伪装手持钓竿属性，让原版浮标 AI 继续正常运作而不被 Kill
            if (EnableHoldItemProtection.val)
            {
                Item held = player.inventory[player.selectedItem];
                bool heldMatches = held != null && held.fishingPole > 0 &&
                                   (held.shoot == __instance.type || (__instance.type >= 986 && __instance.type <= 993));

                if (!heldMatches)
                {
                    int rodIndex = FindMatchingRod(player, __instance.type);
                    if (rodIndex >= 0)
                    {
                        __state.OriginalSelected = player.selectedItem;
                        player.selectedItemState.selected = rodIndex;
                    }
                    else if (held != null)
                    {
                        __state.IsFaked = true;
                        __state.OriginalPole = held.fishingPole;
                        __state.OriginalShoot = held.shoot;
                        __state.OriginalHoldStyle = held.holdStyle;
                        held.fishingPole = 1;
                        held.shoot = __instance.type;
                        held.holdStyle = 1;
                    }
                }
            }

            // 微光状态在 AI 入口前已经计算完，这里清掉后原版 AI 会按普通水体处理
            if (EnableFishInShimmer.val)
            {
                __instance.shimmerWet = false;
                __instance.wet = true;
            }

            // 极速咬钩：入水后直接推过 660，原版会立刻完成 FishingCheck
            if (EnableInstantBite.val && __instance.wet && __instance.ai[1] == 0f && __instance.localAI[1] <= 0f)
            {
                __instance.localAI[1] = 661f;
            }

            return true;
        }

        [HarmonyPatch(typeof(Projectile), "AI_061_FishingBobber")]
        [HarmonyPostfix]
        public static void AI_061_FishingBobberPostfix(Projectile __instance, BobberHoldState __state)
        {
            if (__instance.owner != Main.myPlayer)
                return;

            Player player = Main.player[__instance.owner];
            if (player == null)
                return;

            if (__state.OriginalSelected >= 0 && __state.OriginalSelected < player.inventory.Length)
            {
                player.selectedItemState.selected = __state.OriginalSelected;
            }

            if (__state.IsFaked && player.inventory[player.selectedItem] != null)
            {
                player.inventory[player.selectedItem].fishingPole = __state.OriginalPole;
                player.inventory[player.selectedItem].shoot = __state.OriginalShoot;
                player.inventory[player.selectedItem].holdStyle = __state.OriginalHoldStyle;
            }

            if (EnableFishInShimmer.val)
            {
                __instance.shimmerWet = false;
            }
        }

        /// <summary>
        /// 切物保护下鱼线正常绘制
        /// </summary>
        [HarmonyPatch(typeof(Main), nameof(Main.DrawProj), typeof(int))]
        [HarmonyPrefix]
        public static void DrawProjPrefix(int i, out int __state)
        {
            __state = -1;
            if (!EnableHoldItemProtection.val || i < 0 || i >= Main.maxProjectiles)
                return;

            Projectile proj = Main.projectile[i];
            if (proj == null || !proj.active || !proj.bobber || proj.owner < 0 || proj.owner >= Main.maxPlayers)
                return;

            Player player = Main.player[proj.owner];
            if (player == null || !player.active)
                return;

            Item held = player.inventory[player.selectedItem];
            if (held != null && held.holdStyle == 0)
            {
                __state = held.holdStyle;
                held.holdStyle = 1; // 临时设为 1 触发原版 DrawProj_FishingLine
            }
        }

        [HarmonyPatch(typeof(Main), nameof(Main.DrawProj), typeof(int))]
        [HarmonyPostfix]
        public static void DrawProjPostfix(int i, int __state)
        {
            if (__state >= 0 && i >= 0 && i < Main.maxProjectiles)
            {
                Projectile proj = Main.projectile[i];
                if (proj != null && proj.owner >= 0 && proj.owner < Main.maxPlayers)
                {
                    Player player = Main.player[proj.owner];
                    if (player != null && player.inventory[player.selectedItem] != null)
                    {
                        player.inventory[player.selectedItem].holdStyle = __state;
                    }
                }
            }
        }

        /// <summary>
        /// 全能岩浆垂钓：仅在 FishingCheck 判定期间临时标记玩家拥有熔岩鱼桶效果
        /// </summary>
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.FishingCheck))]
        [HarmonyPrefix]
        public static void FishingCheckPrefix(Projectile __instance, out bool __state)
        {
            __state = false;
            if (!EnableFishInLavaAnywhere.val || __instance.owner != Main.myPlayer)
                return;

            Player player = Main.player[__instance.owner];
            if (player != null && !player.accLavaFishing)
            {
                __state = true;
                player.accLavaFishing = true;
            }
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.FishingCheck))]
        [HarmonyPostfix]
        public static void FishingCheckPostfix(Projectile __instance, bool __state)
        {
            if (__state && __instance.owner == Main.myPlayer)
            {
                Player player = Main.player[__instance.owner];
                if (player != null)
                {
                    player.accLavaFishing = false;
                }
            }
        }

        /// <summary>
        /// 消除负向幸运惩罚：原版在 luck < 0 时会将 fishingLevel 削减 10%~40%
        /// </summary>
        [HarmonyPatch(typeof(Projectile), "TryBuildFishingContext")]
        [HarmonyPrefix]
        public static void TryBuildFishingContextPrefix(Projectile __instance, out float __state)
        {
            __state = 0f;
            if (!EnableIgnoreNegativeLuck.val || __instance.owner != Main.myPlayer)
                return;

            Player player = Main.player[__instance.owner];
            if (player != null && player.luck < 0f)
            {
                __state = player.luck;
                player.luck = 0f; // 判定期间临时清零负幸运
            }
        }

        [HarmonyPatch(typeof(Projectile), "TryBuildFishingContext")]
        [HarmonyPostfix]
        public static void TryBuildFishingContextPostfix(Projectile __instance, float __state)
        {
            if (__state < 0f && __instance.owner == Main.myPlayer)
            {
                Player player = Main.player[__instance.owner];
                if (player != null)
                {
                    player.luck = __state; // 恢复原幸运值
                }
            }
        }

        /// <summary>
        /// 消除时间、天气、月相的负面渔力影响（保底 >= 1.0x 倍率）
        /// </summary>
        [HarmonyPatch(typeof(Player), "Fishing_GetPowerMultiplier")]
        [HarmonyPostfix]
        public static void Fishing_GetPowerMultiplierPostfix(ref float __result)
        {
            if (EnableOnlyPositiveInfluences.val && __result < 1f)
            {
                __result = 1f;
            }
        }

        /// <summary>
        /// FishingCheck 的渔获结果产生后马上交付，无需等待咬钩/收竿动画，浮标继续挂机垂钓
        /// </summary>
        [HarmonyPatch(typeof(Projectile), "SetFishingCheckResults")]
        [HarmonyPostfix]
        public static void SetFishingCheckResultsPostfix(Projectile __instance, ref FishingAttempt fisher)
        {
            if (__instance.owner != Main.myPlayer || !EnableAutoFish.val)
                return;

            if (fisher.rolledEnemySpawn > 0)
            {
                FishingCatchProcessor.ProcessEnemyCatch(__instance, Main.player[__instance.owner], fisher.rolledEnemySpawn, fisher);
            }
            else if (fisher.rolledItemDrop > 0)
            {
                FishingCatchProcessor.ProcessCatch(__instance, Main.player[__instance.owner], fisher.rolledItemDrop, 1, fisher);
            }
            else
            {
                return;
            }

            // 清掉原版咬钩/收竿状态，让浮标留在水中继续下一次 FishingCheck
            __instance.ai[1] = 0f;
            __instance.localAI[1] = 0f;
            __instance.netUpdate = true;
        }

        private static int FindMatchingRod(Player player, int bobberType)
        {
            for (int i = 0; i < player.inventory.Length; i++)
            {
                Item item = player.inventory[i];
                if (item != null && !item.IsAir && item.fishingPole > 0 && item.shoot == bobberType)
                    return i;
            }
            return -1;
        }

        #endregion
    }
}