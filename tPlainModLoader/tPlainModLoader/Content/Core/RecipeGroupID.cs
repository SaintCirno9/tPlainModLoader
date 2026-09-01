using TPML.Content;

namespace Terraria.ID
{
    /// <summary>
    /// RecipeGroupID 兼容映射门面
    /// 作者: SaintCirno9
    /// </summary>
    public static class RecipeGroupID
    {
        public static int Birds => RecipeGroups.Birds?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Birds");
        public static int Scorpions => RecipeGroups.Scorpions?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Scorpions");
        public static int Bugs => RecipeGroups.Bugs?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Bugs");
        public static int Ducks => RecipeGroups.Ducks?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Ducks");
        public static int Squirrels => RecipeGroups.Squirrels?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Squirrels");
        public static int Butterflies => RecipeGroups.Butterflies?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Butterflies");
        public static int Fireflies => RecipeGroups.Fireflies?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Fireflies");
        public static int Snails => RecipeGroups.Snails?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Snails");
        public static int FishForDinner => RecipeGroups.FishForDinner?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("FishForDinner");
        public static int GoldenCritter => RecipeGroups.GoldenCritter?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("GoldenCritter");
        public static int Dragonflies => RecipeGroups.Dragonflies?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Dragonflies");
        public static int Turtles => RecipeGroups.Turtles?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Turtles");
        public static int Fruit => RecipeGroups.Fruit?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Fruit");
        public static int Balloons => RecipeGroups.Balloons?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Balloons");
        public static int Wood => RecipeGroups.Wood?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Wood");
        public static int Sand => RecipeGroups.Sand?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Sand");
        public static int IronBar => RecipeGroups.IronBar?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("IronBar");
        public static int Fragment => RecipeGroups.Fragment?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Fragment");
        public static int PressurePlate => RecipeGroups.PressurePlate?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("PressurePlate");
        public static int Macaws => RecipeGroups.Macaws?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Macaws");
        public static int Cockatiels => RecipeGroups.Cockatiels?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Cockatiels");
        public static int CloudBalloons => RecipeGroups.CloudBalloons?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("CloudBalloons");
        public static int BlizzardBalloons => RecipeGroups.BlizzardBalloons?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("BlizzardBalloons");
        public static int SandstormBalloons => RecipeGroups.SandstormBalloons?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("SandstormBalloons");
        public static int CritterGuides => RecipeGroups.CritterGuides?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("CritterGuides");
        public static int NatureGuides => RecipeGroups.NatureGuides?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("NatureGuides");
        public static int Seashells => RecipeGroups.Seashells?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Seashells");
        public static int Stone => RecipeGroups.Stone?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Stone");
        public static int CobaltBar => RecipeGroups.CobaltBar?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("CobaltBar");
        public static int MythrilBar => RecipeGroups.MythrilBar?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("MythrilBar");
        public static int AdamantiteBar => RecipeGroups.AdamantiteBar?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("AdamantiteBar");
        public static int GemCritter => RecipeGroups.GemCritter?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("GemCritter");
        public static int MagicMirror => RecipeGroups.MagicMirror?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("MagicMirror");
        public static int Jellyfish => RecipeGroups.Jellyfish?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("Jellyfish");
        public static int SilverBar => RecipeGroups.SilverBar?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("SilverBar");
        public static int GoldBar => RecipeGroups.GoldBar?.RegisteredId ?? RecipeLoader.GetRecipeGroupId("GoldBar");
    }
}
