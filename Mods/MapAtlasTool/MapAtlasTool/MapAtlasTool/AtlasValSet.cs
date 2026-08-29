using System;
using MapAtlasTool.Content.UI;
using MapAtlasTool.Utils;
using MapAtlasTool.Utils.quickBuild;
using System.Collections.Generic;
using Terraria.UI;

namespace MapAtlasTool
{
    /// <summary>
    /// 地图图鉴工具: 关键结构与宝箱雷达显示开关定义
    /// 作者: SaintCirno9
    /// </summary>
    internal static class AtlasValSet
    {
        // 总开关
        public static GetSetReset<bool> markStructuresOnMap = new GetSetReset<bool>(true, true);

        // 世界关键遗迹与生态标记
        public static GetSetReset<bool> markPlanteraBulb = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markSwordShrine = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markBeeHive = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTempleAltar = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTempleDoor = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markEvilAltars = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markEvilOrbsHearts = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markShimmer = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markPyramid = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markFloatingIsland = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markDungeon = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markDungeonWaterBolt = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMiniBiomes = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMushroomBiome = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markAntlionHive = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMossCaves = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markMeteorite = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTrapsExplosives = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markTrappedNPCs = new GetSetReset<bool>(true, true);

        // 全量宝箱雷达标记
        public static GetSetReset<bool> markChestsAll = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markChestsShowEmpty = new GetSetReset<bool>(false, false);
        public static GetSetReset<bool> markChestsSurfaceUnderground = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markChestsShadowBiome = new GetSetReset<bool>(true, true);
        public static GetSetReset<bool> markChestsTrapped = new GetSetReset<bool>(true, true);

        public static event Action OnCategoryStateChanged;

        public static void NotifyCategoryChanged()
        {
            OnCategoryStateChanged?.Invoke();
        }

        public static bool IsStructuresEnabled =>
            markPlanteraBulb.val || markSwordShrine.val || markBeeHive.val || markTempleAltar.val ||
            markTempleDoor.val || markEvilAltars.val || markEvilOrbsHearts.val || markShimmer.val ||
            markPyramid.val || markFloatingIsland.val || markDungeon.val || markDungeonWaterBolt.val ||
            markMiniBiomes.val || markMushroomBiome.val || markAntlionHive.val || markMossCaves.val || markMeteorite.val;

        public static bool IsChestsEnabled =>
            markChestsAll.val || markChestsSurfaceUnderground.val || markChestsShadowBiome.val;

        public static bool IsNPCsEnabled =>
            markTrappedNPCs.val;

        public static bool IsTrapsEnabled =>
            markTrapsExplosives.val || markChestsTrapped.val;

        public static bool IsAnyMarkerEnabled =>
            IsStructuresEnabled || IsChestsEnabled || IsNPCsEnabled || IsTrapsEnabled;

        private static void UpdateMasterSwitch()
        {
            markStructuresOnMap.val = IsAnyMarkerEnabled;
        }

        public static void ToggleStructures()
        {
            bool target = !IsStructuresEnabled;
            markPlanteraBulb.val = target;
            markSwordShrine.val = target;
            markBeeHive.val = target;
            markTempleAltar.val = target;
            markTempleDoor.val = target;
            markEvilAltars.val = target;
            markEvilOrbsHearts.val = target;
            markShimmer.val = target;
            markPyramid.val = target;
            markFloatingIsland.val = target;
            markDungeon.val = target;
            markDungeonWaterBolt.val = target;
            markMiniBiomes.val = target;
            markMushroomBiome.val = target;
            markAntlionHive.val = target;
            markMossCaves.val = target;
            markMeteorite.val = target;
            UpdateMasterSwitch();
            NotifyCategoryChanged();
        }

        public static void ToggleChests()
        {
            bool target = !IsChestsEnabled;
            markChestsAll.val = target;
            markChestsSurfaceUnderground.val = target;
            markChestsShadowBiome.val = target;
            UpdateMasterSwitch();
            NotifyCategoryChanged();
        }

        public static void ToggleNPCs()
        {
            markTrappedNPCs.val = !markTrappedNPCs.val;
            UpdateMasterSwitch();
            NotifyCategoryChanged();
        }

        public static void ToggleTraps()
        {
            bool target = !IsTrapsEnabled;
            markTrapsExplosives.val = target;
            markChestsTrapped.val = target;
            UpdateMasterSwitch();
            NotifyCategoryChanged();
        }

        public static void SetAllStructureMarkers(bool enabled)
        {
            markStructuresOnMap.val = enabled;
            markPlanteraBulb.val = enabled;
            markSwordShrine.val = enabled;
            markBeeHive.val = enabled;
            markTempleAltar.val = enabled;
            markTempleDoor.val = enabled;
            markEvilAltars.val = enabled;
            markEvilOrbsHearts.val = enabled;
            markShimmer.val = enabled;
            markPyramid.val = enabled;
            markFloatingIsland.val = enabled;
            markDungeon.val = enabled;
            markDungeonWaterBolt.val = enabled;
            markMiniBiomes.val = enabled;
            markMushroomBiome.val = enabled;
            markAntlionHive.val = enabled;
            markMossCaves.val = enabled;
            markMeteorite.val = enabled;
            markTrapsExplosives.val = enabled;
            markTrappedNPCs.val = enabled;

            markChestsAll.val = enabled;
            markChestsSurfaceUnderground.val = enabled;
            markChestsShadowBiome.val = enabled;
            markChestsTrapped.val = enabled;

            NotifyCategoryChanged();
        }
    }
}
