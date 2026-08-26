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
    /// 多线钓鱼系统
    /// 允许玩家单次抛竿发射多条鱼线（1~16 条对称散布鱼线），大幅提升垂钓效率
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    internal class MultipleFishingLines
    {
        public static GetSetReset<bool> EnableMultiLines = new GetSetReset<bool>(false, false);
        public static GetSetReset<int> MultiLineCount = new GetSetReset<int>(1, 1, GetSetReset.GetIntFunc(1, 32));

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get1("multiFishingLines", EnableMultiLines, MultiLineCount, new CommandInt())
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get1(EnableMultiLines, MultiLineCount, int.Parse, "单次抛竿发射的鱼线总数 (1~32 条分布鱼线)", "Images/Item_2421", "多线钓鱼数量")
            };
        }

        /// <summary>
        /// 拦截玩家手动抛竿发射逻辑，生成对称散射的额外鱼线
        /// </summary>
        [HarmonyPatch(typeof(Player), nameof(Player.ItemCheck_Shoot))]
        [HarmonyPostfix]
        public static void ItemCheck_ShootPostfix(Player __instance, int i, Item sItem, int weaponDamage, bool withAudioVisualFeedback)
        {
            if (__instance.whoAmI != Main.myPlayer || !__instance.active)
                return;
            if (sItem == null || sItem.fishingPole <= 0 || sItem.shoot <= 0)
                return;
            if (!EnableMultiLines.val || MultiLineCount.val <= 1)
                return;

            Vector2 baseVel = Main.MouseWorld - __instance.Center;
            if (baseVel == Vector2.Zero)
            {
                baseVel = new Vector2(__instance.direction * 10f, -5f);
            }
            baseVel.Normalize();
            baseVel *= sItem.shootSpeed <= 0f ? 12f : sItem.shootSpeed;

            IEntitySource source = __instance.GetItemSource_Item(sItem);
            SpawnExtraLines(__instance, sItem, baseVel, source, MultiLineCount.val);
        }

        /// <summary>
        /// 生成对称分布的多条鱼线浮标射弹
        /// </summary>
        public static void SpawnExtraLines(Player player, Item rod, Vector2 baseVelocity, IEntitySource source, int totalCount)
        {
            int extraCount = totalCount - 1;
            if (extraCount <= 0)
                return;

            // 与 AutoFisher 一致：线数越多角度范围越大，保持均匀分布
            float range = MathHelper.ToRadians((float)Math.Log(extraCount + 1) * 8f);
            float unit = range / extraCount;

            for (int i = 0; i < extraCount; i++)
            {
                float radians = (i % 2 == 0) ? (i / 2 + 1) * unit : -(i / 2 + 1) * unit;
                Vector2 vel = baseVelocity.RotatedBy(radians);
                Projectile.NewProjectile(source, player.Center, vel, rod.shoot, 0, 0f, player.whoAmI);
            }
        }
    }
}