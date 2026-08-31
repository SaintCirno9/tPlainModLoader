using Terraria;

namespace TPML.Content
{
    /// <summary>
    /// 对齐 tML 的 Item 便捷方法（仅方法调用语法；<c>ModItem</c> 属性语法需 Prepatcher，本批不做）。
    /// 作者: SaintCirno9
    /// </summary>
    public static class ItemExtensions
    {
        /// <summary>
        /// 对齐 tML <c>Item.CloneDefaults</c>：克隆指定原版/已注册物品的默认属性，再写回自身 type。
        /// 不会复制配方、射击逻辑等额外行为。
        /// </summary>
        public static void CloneDefaults(this Item item, int typeToClone)
        {
            if (item == null) return;
            int originalType = item.type;
            bool originalMaterial = item.material;
            item.SetDefaults(typeToClone);
            item.type = originalType;
            item.material = originalMaterial;
        }

        /// <summary>
        /// 获取绑定在此 Item 实例上的 ModItem；原版物品返回 null。
        /// 对应 tML 属性 <c>item.ModItem</c> 的方法形式（扩展方法无法提供属性语法）。
        /// </summary>
        public static ModItem GetModItem(this Item item) => ItemLoader.GetModItem(item);

        /// <summary>
        /// 获取绑定在此 Item 实例上的指定类型 ModItem；失败返回 null。
        /// </summary>
        public static T GetModItem<T>(this Item item) where T : ModItem => ItemLoader.GetModItem<T>(item);

        /// <summary>
        /// 对齐 tML <c>Item.IsNotSameTypePrefixAndStack</c>，转发原版 <see cref="Item.IsNotTheSameAs"/>。
        /// </summary>
        public static bool IsNotSameTypePrefixAndStack(this Item item, Item compareItem)
        {
            if (item == null) return compareItem != null;
            return item.IsNotTheSameAs(compareItem);
        }
    }
}
