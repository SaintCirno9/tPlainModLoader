using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;

namespace TPML.Content
{
    /// <summary>
    /// TPML 全局物块行为修饰基类
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class GlobalTile : ModType
    {
        public virtual int[] AdjTiles(int type) => null;
        public virtual void MouseOver(int i, int j, int type) {}
        public virtual void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem) {}
        public virtual void NearbyEffects(int i, int j, int type, bool closer) {}
        public virtual void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b) {}
        public virtual void NumDust(int i, int j, int type, bool fail, ref int num) {}
    }
}