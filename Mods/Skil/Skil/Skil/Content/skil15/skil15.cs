using Microsoft.Xna.Framework;
using Skil.Utils;
using Skil.Utils.quickBuild;
using System.Collections.Generic;
using System.Diagnostics;
using tContentPatch;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.UI;

namespace Skil.Content.skil15
{
    //圣骑士锤相关
    internal class skil15 : PatchMain
    {
        private class patchProj : PatchProjectile
        {
            public override void SetDefaultsPostfix(Projectile This, int Type)
            {
                if (CanParticleSpawnList.IndexInRange(This.whoAmI) != true) return;
                CanParticleSpawnList[This.whoAmI] = false;

                if (Mode.val != 1) return;//不是应用于全部

                if (This.type != ProjectileID.PaladinsHammerFriendly) return;
                if (This.ai[0] != 0) return;//不是飞出去的状态

                CanParticleSpawnList[This.whoAmI] = true;
            }

            public override void NewProjectilePostfix(int result, IEntitySource spawnSource, float X, float Y, float SpeedX, float SpeedY, int Type, int Damage, float KnockBack, int Owner, float ai0, float ai1, float ai2, NewProjectileModifier modifer)
            {
                if (CanParticleSpawnList.IndexInRange(result) != true) return;
                CanParticleSpawnList[result] = false;

                if (Main.projectile[result] == null) return;
                if (Main.projectile[result].type != ProjectileID.PaladinsHammerFriendly) return;
                if (Main.projectile[result].ai[0] != 0) return;//不是飞出去的状态

                CanParticleSpawnList[result] = true;
            }
        }

        public static GetSetReset<bool> Enable = new GetSetReset<bool>();
        public static GetSetReset<int> Mode = new GetSetReset<int>(0, 0, GetSetReset.GetIntFunc(0, 1));//应用于
        public static GetSetReset<int> Color = new GetSetReset<int>(-1, -1);//闪电颜色
        private static readonly bool[] CanParticleSpawnList = new bool[Main.projectile.Length - 1];

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>()
            {
                UIBuild.get1(Enable, Mode, int.Parse, "0: 自己, 1: 全部<int>,", "Images/Item_1513", "触发闪电"),
                UIBuild.get6(Color, int.Parse, "颜色, 小于0随机<int>", "Images/Item_1513", "闪电颜色"),
            };
        }

        public override void OnEnterWorldPrefix()
        {
            for (int i = 0; i < CanParticleSpawnList.Length; i++)
            {
                CanParticleSpawnList[i] = false;
            }
        }

        public override void DoUpdateInWorldPostfix(Stopwatch sw)
        {
            if (Enable.val == false) return;

            for (int i = 0; i < CanParticleSpawnList.Length; i++)
            {
                if (CanParticleSpawnList[i] == false) continue;

                if (Main.projectile[i]?.active != true) continue;
                if (Main.projectile[i].ai[0] != 1) continue;
                //飞回来的状态

                CanParticleSpawnList[i] = false;

                ParticleSpawn(Main.projectile[i]);
            }
        }

        private static void ParticleSpawn(Projectile proj)
        {
            ParticleOrchestraType type = ParticleOrchestraType.StormLightning;
            int style = Utils.getRand(0, 1145);
            int color = GetLightningColor();

            ParticleOrchestrator.BroadcastOrRequestParticleSpawn(type, new ParticleOrchestraSettings
            {
                PositionInWorld = proj.Center,
                UniqueInfoPiece = color,
                MovementVector = new Vector2(style, 0f),
            });
        }

        private static int GetLightningColor()
        {
            if (Color.val < 0)
            {
                return (int)new Color(
                    Utils.getRand(byte.MinValue, byte.MaxValue),
                    Utils.getRand(byte.MinValue, byte.MaxValue),
                    Utils.getRand(byte.MinValue, byte.MaxValue)).PackedValue;
            }

            return Color.val;
        }
    }
}
