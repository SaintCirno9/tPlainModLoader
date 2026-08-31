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
            long coinsInv = Utils.CoinsCount(out overFlowing, player.inventory, 58, 57, 56, 55, 54);
            long coinsBank = Utils.CoinsCount(out overFlowing, player.bank.item);
            long coinsBank2 = Utils.CoinsCount(out overFlowing, player.bank2.item);
            long coinsBank3 = Utils.CoinsCount(out overFlowing, player.bank3.item);
            long coinsBank4 = Utils.CoinsCount(out overFlowing, player.bank4.item);
            return Utils.CoinsCombineStacks(out overFlowing, coinsInv, coinsBank, coinsBank2, coinsBank3, coinsBank4) >= price;
        }
    }
}
