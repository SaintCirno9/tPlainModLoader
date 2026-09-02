using SuspiciousPlayer.Content.Event1;
using TPML;
using Terraria;
using Terraria.DataStructures;

namespace SuspiciousPlayer.Patch
{
    internal class PatchNPC : Mod
    {
        // M2：弃用 IAddPatch，改用 MonoMod.HookGen 的 On_ 门面（tML 标准做法）
        public override void Load()
        {
            On_NPC.NewNPC += (orig, source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target) =>
            {
                if (!CanSpawn(Type)) return -1;
                return orig(source, X, Y, Type, Start, ai0, ai1, ai2, ai3, Target);
            };
        }

        private static bool CanSpawn(int Type)
        {
            if (Event.CanSpawnNPC == false)
            {
                if (Type == 415) return false;
                if (Type == 416) return false;
                if (Type == 417) return false;
                if (Type == 418) return false;
                if (Type == 419) return false;
                if (Type == 518) return false;
            }
            if (Event.CanSpawnNPC_SolarCrawltipede == false)
            {
                if (Type == 412) return false;
                //if (Type == 413) return false;
                //if (Type == 414) return false;
            }
            return true;
        }

        public static bool NewNPC(IEntitySource source, int X, int Y, int Type, int Start = 0, float ai0 = 0f, float ai1 = 0f, float ai2 = 0f, float ai3 = 0f, int Target = 255)
        {
            return CanSpawn(Type);
        }
    }
}
