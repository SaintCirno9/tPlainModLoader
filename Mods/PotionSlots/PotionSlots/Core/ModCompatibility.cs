using TPML.Content;

namespace PotionSlots.Core
{
    public static class ModCompatibility
    {
        public static bool IsBankButtonsLoaded
        {
            get
            {
                return ModContent.TryGetMod("BankButtons", out _);
            }
        }
    }
}
