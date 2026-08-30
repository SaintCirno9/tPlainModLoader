using Terraria.ModLoader;

namespace PotionSlots.Core
{
    public static class ModCompatibility
    {
        public static bool IsBankButtonsLoaded
        {
            get
            {
                return ModLoader.TryGetMod("BankButtons", out _);
            }
        }
    }
}
