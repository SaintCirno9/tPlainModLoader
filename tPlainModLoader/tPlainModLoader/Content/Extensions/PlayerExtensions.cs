using Terraria;
using Terraria.GameContent.UI;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// 对齐 tML 的 Player 便捷方法（仅方法调用语法；不含 DamageClass / 属性注入）。
    /// 作者: SaintCirno9
    /// </summary>
    public static class PlayerExtensions
    {
        private static readonly ILogger Logger = LogManager.GetLogger("PlayerExtensions");

        /// <summary>
        /// 对齐 tML <c>Player.HasBuff(int)</c>：当前是否拥有指定 buff。
        /// </summary>
        public static bool HasBuff(this Player player, int type)
        {
            if (player == null) return false;
            return player.FindBuffIndex(type) != -1;
        }

        /// <summary>
        /// 对齐 tML <c>Player.CanAfford</c>：检查背包 + 四银行合计货币是否足够，不扣款。
        /// <paramref name="customCurrency"/> 为 -1 时按金币计；否则按自定义货币计数（不调用 BuyItem）。
        /// </summary>
        public static bool CanAfford(this Player player, long price, int customCurrency = -1)
        {
            if (player == null) return false;
            if (price <= 0) return true;

            if (customCurrency != -1)
            {
                if (!CustomCurrencyManager._currencies.TryGetValue(customCurrency, out CustomCurrencySystem system) || system == null)
                {
                    Logger.Warn("CanAfford: 未知 customCurrency=" + customCurrency);
                    return false;
                }

                bool overflow;
                long inv = system.CountCurrency(out overflow, player.inventory, 58, 57, 56, 55, 54);
                long bank = system.CountCurrency(out overflow, player.bank.item);
                long bank2 = system.CountCurrency(out overflow, player.bank2.item);
                long bank3 = system.CountCurrency(out overflow, player.bank3.item);
                long bank4 = system.CountCurrency(out overflow, player.bank4.item);
                return system.CombineStacks(out overflow, inv, bank, bank2, bank3, bank4) >= price;
            }

            bool overFlowing;
            long coinsInv = Terraria.Utils.CoinsCount(out overFlowing, player.inventory, 58, 57, 56, 55, 54);
            long coinsBank = Terraria.Utils.CoinsCount(out overFlowing, player.bank.item);
            long coinsBank2 = Terraria.Utils.CoinsCount(out overFlowing, player.bank2.item);
            long coinsBank3 = Terraria.Utils.CoinsCount(out overFlowing, player.bank3.item);
            long coinsBank4 = Terraria.Utils.CoinsCount(out overFlowing, player.bank4.item);
            return Terraria.Utils.CoinsCombineStacks(out overFlowing, coinsInv, coinsBank, coinsBank2, coinsBank3, coinsBank4) >= price;
        }

        public static Terraria.DataStructures.IEntitySource GetSource_OpenItem(this Player player, int itemType) => new EntitySource_Misc("OpenItem");
        public static Terraria.DataStructures.IEntitySource GetSource_ItemUse(this Player player, Item item) => new Terraria.DataStructures.EntitySource_ItemUse(player, item);
        public static Terraria.DataStructures.IEntitySource GetSource_ItemUse(this Player player, int itemType) => new EntitySource_Misc("ItemUse");
        public static Terraria.DataStructures.IEntitySource GetSource_FromThis(this Player player, string context = null) => new EntitySource_Misc(context ?? "Player");
        public static Terraria.DataStructures.IEntitySource GetSource_Misc(this Player player, string context) => new EntitySource_Misc(context);
        public static Terraria.DataStructures.IEntitySource GetSource_Buff(this Player player, int buffIndex) => new EntitySource_Misc("Buff");
        public static int equippedWings(this Player player) => player.wings;

        public static StatModifier GetDamage(this Player player, DamageClass damageClass) => StatModifier.Default;
        public static StatModifier GetTotalDamage(this Player player, DamageClass damageClass) => StatModifier.Default;
        public static float GetTotalCritChance(this Player player, DamageClass damageClass) => player.GetCritChance(damageClass);
        public static float GetCritChance(this Player player, DamageClass damageClass) => 4f;
        public static float GetAttackSpeed(this Player player, DamageClass damageClass) => 1f;

        public static ExtraJumpState GetJumpState<T>(this Player player) where T : ExtraJump => default;
        public static ExtraJumpState GetJumpState(this Player player, ExtraJump jump) => default;
    }
}
