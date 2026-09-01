using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// TPML 自定义生物群落 (ModBiome) 基类
    /// 遵循 tModLoader 经典 API 范式与图鉴/背景/音乐集成
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class ModBiome : ModType
    {
        public virtual SceneEffectPriority Priority => SceneEffectPriority.BiomeLow;
        public virtual int Music => -1;
        public virtual string BestiaryIcon => null;
        public virtual string BackgroundPath => null;
        public virtual Color? BackgroundColor => null;

        public virtual bool IsBiomeActive(Player player)
        {
            return false;
        }

        public virtual void SetStaticDefaults()
        {
        }
    }
}
