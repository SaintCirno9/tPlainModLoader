using CommandHelp;
using Microsoft.Xna.Framework;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.FishDropRules;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.Fishing
{
    /// <summary>
    /// 全自动钓鱼核心系统门控（基于 HookGen 强类型 On_ 门控）
    /// 对齐 AutoFisher：
    /// 1. 自动咬钩与挂机结算：渔获在 FishingCheck 结果产生后直接交付，浮标继续留在水中挂机连钓，无需反复收竿重抛；
    /// 2. 极速咬钩：消除等待时间，瞬间触发判定；
    /// 3. 任意手持物品不断线保护：切换武器、工具、甚至空手时浮标与鱼线均不消失；
    /// 4. 微光/岩浆任意垂钓与无断线惩罚；
    /// 5. 消除时间、天气、月相与负幸运等环境折损惩罚。
    /// 作者: SaintCirno9
    /// </summary>
    internal class AutoFishingSystemHooks
    {
        public static GetSetReset<bool> EnableAutoFish = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableInstantBite = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableHoldItemProtection = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableFishInShimmer = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableFishInLavaAnywhere = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> EnableOnlyPositiveInfluences = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> EnableIgnoreNegativeLuck = new GetSetReset<bool>(true, true);
        private static bool _registered = false;

        public static void RegisterAll()
        {
            if (_registered) return;
            On_Projectile.AI_061_FishingBobber += Hook_AI_061_FishingBobber;
            On_Main.DrawProj += Hook_DrawProj;
            On_Projectile.FishingCheck += Hook_FishingCheck;
            On_Projectile.TryBuildFishingContext += Hook_TryBuildFishingContext;
            On_Player.Fishing_GetPowerMultiplier += Hook_Fishing_GetPowerMultiplier;
            On_Projectile.SetFishingCheckResults += Hook_SetFishingCheckResults;
            _registered = true;
        }

        public static void UnregisterAll()
        {
            if (!_registered) return;
            On_Projectile.AI_061_FishingBobber -= Hook_AI_061_FishingBobber;
            On_Main.DrawProj -= Hook_DrawProj;
            On_Projectile.FishingCheck -= Hook_FishingCheck;
            On_Projectile.TryBuildFishingContext -= Hook_TryBuildFishingContext;
            On_Player.Fishing_GetPowerMultiplier -= Hook_Fishing_GetPowerMultiplier;
            On_Projectile.SetFishingCheckResults -= Hook_SetFishingCheckResults;
            _registered = false;
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

        private static void Hook_AI_061_FishingBobber(On_Projectile.orig_AI_061_FishingBobber orig, Projectile self)
        {
            if (self.owner != Main.myPlayer)
            {
                orig(self);
                return;
            }

            Player player = Main.player[self.owner];
            if (player == null || !player.active)
            {
                orig(self);
                return;
            }

            int originalSelected = -1;
            bool isFaked = false;
            int originalPole = 0;
            int originalShoot = 0;
            int originalHoldStyle = 0;
            Item fakedItem = null;

            // 切物保护：玩家切换手持物品时，伪装手持钓竿属性，让原版浮标 AI 继续正常运作而不被 Kill
            if (EnableHoldItemProtection.val)
            {
                Item held = player.inventory[player.selectedItem];
                bool heldMatches = held != null && held.fishingPole > 0 &&
                                   (held.shoot == self.type || (self.type >= 986 && self.type <= 993));

                if (!heldMatches)
                {
                    int rodIndex = FindMatchingRod(player, self.type);
                    if (rodIndex >= 0)
                    {
                        originalSelected = player.selectedItem;
                        player.selectedItemState.selected = rodIndex;
                    }
                    else if (held != null)
                    {
                        isFaked = true;
                        originalPole = held.fishingPole;
                        originalShoot = held.shoot;
                        originalHoldStyle = held.holdStyle;
                        fakedItem = held;
                        held.fishingPole = 1;
                        held.shoot = self.type;
                        held.holdStyle = 1;
                    }
                }
            }

            // 微光状态在 AI 入口前已经计算完，这里清掉后原版 AI 会按普通水体处理
            if (EnableFishInShimmer.val)
            {
                self.shimmerWet = false;
                self.wet = true;
            }

            // 极速咬钩：入水后直接推过 660，原版会立刻完成 FishingCheck
            if (EnableInstantBite.val && self.wet && self.ai[1] == 0f && self.localAI[1] <= 0f)
            {
                self.localAI[1] = 661f;
            }

            try
            {
                orig(self);
            }
            finally
            {
                if (originalSelected >= 0 && originalSelected < player.inventory.Length)
                {
                    player.selectedItemState.selected = originalSelected;
                }

                if (isFaked && fakedItem != null)
                {
                    fakedItem.fishingPole = originalPole;
                    fakedItem.shoot = originalShoot;
                    fakedItem.holdStyle = originalHoldStyle;
                }

                if (EnableFishInShimmer.val)
                {
                    self.shimmerWet = false;
                }
            }
        }

        /// <summary>
        /// 切物保护下鱼线正常绘制
        /// </summary>
        private static void Hook_DrawProj(On_Main.orig_DrawProj orig, Main self, int i)
        {
            if (!EnableHoldItemProtection.val || i < 0 || i >= Main.maxProjectiles)
            {
                orig(self, i);
                return;
            }

            Projectile proj = Main.projectile[i];
            if (proj == null || !proj.active || !proj.bobber || proj.owner < 0 || proj.owner >= Main.maxPlayers)
            {
                orig(self, i);
                return;
            }

            Player player = Main.player[proj.owner];
            if (player == null || !player.active)
            {
                orig(self, i);
                return;
            }

            Item held = player.inventory[player.selectedItem];
            int originalHoldStyle = 0;
            bool modifiedHoldStyle = false;

            if (held != null && held.holdStyle == 0)
            {
                originalHoldStyle = held.holdStyle;
                held.holdStyle = 1;
                modifiedHoldStyle = true;
            }

            try
            {
                orig(self, i);
            }
            finally
            {
                if (modifiedHoldStyle && held != null)
                {
                    held.holdStyle = originalHoldStyle;
                }
            }
        }

        /// <summary>
        /// 全能岩浆垂钓：仅在 FishingCheck 判定期间临时标记玩家拥有熔岩鱼桶效果
        /// </summary>
        private static void Hook_FishingCheck(On_Projectile.orig_FishingCheck orig, Projectile self)
        {
            bool modifiedLava = false;
            Player player = null;

            if (EnableFishInLavaAnywhere.val && self.owner == Main.myPlayer)
            {
                player = Main.player[self.owner];
                if (player != null && !player.accLavaFishing)
                {
                    player.accLavaFishing = true;
                    modifiedLava = true;
                }
            }

            try
            {
                orig(self);
            }
            finally
            {
                if (modifiedLava && player != null)
                {
                    player.accLavaFishing = false;
                }
            }
        }

        /// <summary>
        /// 消除负向幸运惩罚：原版在 luck < 0 时会将 fishingLevel 削减 10%~40%
        /// </summary>
        private static bool Hook_TryBuildFishingContext(On_Projectile.orig_TryBuildFishingContext orig, Projectile self, FishingContext context)
        {
            float originalLuck = 0f;
            bool modifiedLuck = false;
            Player player = null;

            if (EnableIgnoreNegativeLuck.val && self.owner == Main.myPlayer)
            {
                player = Main.player[self.owner];
                if (player != null && player.luck < 0f)
                {
                    originalLuck = player.luck;
                    player.luck = 0f;
                    modifiedLuck = true;
                }
            }

            try
            {
                return orig(self, context);
            }
            finally
            {
                if (modifiedLuck && player != null)
                {
                    player.luck = originalLuck;
                }
            }
        }

        /// <summary>
        /// 消除时间、天气、月相的负面渔力影响（保底 >= 1.0x 倍率）
        /// </summary>
        private static float Hook_Fishing_GetPowerMultiplier(On_Player.orig_Fishing_GetPowerMultiplier orig)
        {
            float result = orig();
            if (EnableOnlyPositiveInfluences.val && result < 1f)
            {
                return 1f;
            }
            return result;
        }

        /// <summary>
        /// FishingCheck 的渔获结果产生后马上交付，无需等待咬钩/收竿动画，浮标继续挂机垂钓
        /// </summary>
        private static void Hook_SetFishingCheckResults(On_Projectile.orig_SetFishingCheckResults orig, Projectile self, ref FishingAttempt fisher)
        {
            orig(self, ref fisher);

            if (self.owner != Main.myPlayer || !EnableAutoFish.val)
                return;

            if (fisher.rolledEnemySpawn > 0)
            {
                FishingCatchProcessor.ProcessEnemyCatch(self, Main.player[self.owner], fisher.rolledEnemySpawn, fisher);
            }
            else if (fisher.rolledItemDrop > 0)
            {
                FishingCatchProcessor.ProcessCatch(self, Main.player[self.owner], fisher.rolledItemDrop, 1, fisher);
            }
            else
            {
                return;
            }

            // 清掉原版咬钩/收竿状态，让浮标留在水中继续下一次 FishingCheck
            self.ai[1] = 0f;
            self.localAI[1] = 0f;
            self.netUpdate = true;
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

    /// <summary>
    /// 兼容别名类
    /// </summary>
    internal class AutoFishingSystem : AutoFishingSystemHooks
    {
    }
}