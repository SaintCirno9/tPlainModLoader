using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Items;
using TPML.Content;

namespace tContentPatch
{
    /// <summary>
    /// 已废弃的物品 Patch 基类。请迁移至 <see cref="TPML.Content.GlobalItem"/> 或 <see cref="TPML.Content.ModItem"/>。
    /// 作者: SaintCirno9
    /// </summary>
    [Obsolete("TPML 现代体系已全面替代 Patch*，请继承 ModSystem / ModPlayer / Global*")]
    public abstract class PatchItem : TPML.Content.GlobalItem
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary/>
        public virtual void SetDefaultsPrefix(Item This, int Type, ItemVariant variant) { }
        /// <summary/>
        public virtual void SetDefaultsPostfix(Item This, int Type, ItemVariant variant) { }
        /// <summary/>
        public virtual void NewItemPostfix(int __result, IEntitySource source,
            Vector2 center, int type, int stack, int prefix,
            NewItemOwnership ownership,
            Vector2? velocity, Item.NewItemModifier modifier, bool noBroadcast)
        { }
    }
}
