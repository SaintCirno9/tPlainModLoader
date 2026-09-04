using System;
using MapAtlasTool.Content;
using MapAtlasTool.Content.UI;
using TPML;

namespace MapAtlasTool
{
    /// <summary>
    /// 地图图鉴工具设置持久化（面板即唯一 UI，无独立设置界面）
    /// 作者: SaintCirno9
    /// </summary>
    internal class SettingUIAtlas : ModSetting
    {
        public class Data
        {
            public bool MarkStructuresOnMap = true;
            public bool MarkPlanteraBulb = true;
            public bool MarkSwordShrine = true;
            public bool MarkBeeHive = true;
            public bool MarkTempleAltar = true;
            public bool MarkTempleDoor = true;
            public bool MarkEvilAltars = true;
            public bool MarkEvilOrbsHearts = true;
            public bool MarkShimmer = true;
            public bool MarkPyramid = true;
            public bool MarkFloatingIsland = true;
            public bool MarkDungeon = true;
            public bool MarkDungeonWaterBolt = true;
            public bool MarkMiniBiomes = true;
            public bool MarkMushroomBiome = true;
            public bool MarkAntlionHive = true;
            public bool MarkMossCaves = true;
            public bool MarkMeteorite = true;
            public bool MarkTrapsExplosives = true;
            public bool MarkTrappedNPCs = true;
            public bool MarkChestsAll = true;
            public bool MarkChestsShowEmpty = false;
            public bool MarkChestsSurfaceUnderground = true;
            public bool MarkChestsShadowBiome = true;
            public bool MarkChestsTrapped = true;
            public bool PanelOpen = false;
        }

        public override string Name => "设置";
        public override string Title => "地图图鉴工具: 设置";
        public override string FilePath => "setting.json";
        public override Type DataType => typeof(Data);
        public override bool HasUI => false;

        public override void Load(object v)
        {
            Data data = v as Data ?? new Data();

            AtlasValSet.markStructuresOnMap.val = data.MarkStructuresOnMap;
            AtlasValSet.markPlanteraBulb.val = data.MarkPlanteraBulb;
            AtlasValSet.markSwordShrine.val = data.MarkSwordShrine;
            AtlasValSet.markBeeHive.val = data.MarkBeeHive;
            AtlasValSet.markTempleAltar.val = data.MarkTempleAltar;
            AtlasValSet.markTempleDoor.val = data.MarkTempleDoor;
            AtlasValSet.markEvilAltars.val = data.MarkEvilAltars;
            AtlasValSet.markEvilOrbsHearts.val = data.MarkEvilOrbsHearts;
            AtlasValSet.markShimmer.val = data.MarkShimmer;
            AtlasValSet.markPyramid.val = data.MarkPyramid;
            AtlasValSet.markFloatingIsland.val = data.MarkFloatingIsland;
            AtlasValSet.markDungeon.val = data.MarkDungeon;
            AtlasValSet.markDungeonWaterBolt.val = data.MarkDungeonWaterBolt;
            AtlasValSet.markMiniBiomes.val = data.MarkMiniBiomes;
            AtlasValSet.markMushroomBiome.val = data.MarkMushroomBiome;
            AtlasValSet.markAntlionHive.val = data.MarkAntlionHive;
            AtlasValSet.markMossCaves.val = data.MarkMossCaves;
            AtlasValSet.markMeteorite.val = data.MarkMeteorite;
            AtlasValSet.markTrapsExplosives.val = data.MarkTrapsExplosives;
            AtlasValSet.markTrappedNPCs.val = data.MarkTrappedNPCs;
            AtlasValSet.markChestsAll.val = data.MarkChestsAll;
            AtlasValSet.markChestsShowEmpty.val = data.MarkChestsShowEmpty;
            AtlasValSet.markChestsSurfaceUnderground.val = data.MarkChestsSurfaceUnderground;
            AtlasValSet.markChestsShadowBiome.val = data.MarkChestsShadowBiome;
            AtlasValSet.markChestsTrapped.val = data.MarkChestsTrapped;

            MapAtlasPanel.PanelOpen.val = data.PanelOpen;

            // 变更自动保存
            AtlasValSet.markStructuresOnMap.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markPlanteraBulb.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markSwordShrine.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markBeeHive.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markTempleAltar.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markTempleDoor.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markEvilAltars.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markEvilOrbsHearts.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markShimmer.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markPyramid.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markFloatingIsland.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markDungeon.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markDungeonWaterBolt.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markMiniBiomes.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markMushroomBiome.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markAntlionHive.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markMossCaves.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markMeteorite.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markTrapsExplosives.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markTrappedNPCs.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markChestsAll.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markChestsShowEmpty.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markChestsSurfaceUnderground.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markChestsShadowBiome.OnValUpdate += _ => NeedSave = true;
            AtlasValSet.markChestsTrapped.OnValUpdate += _ => NeedSave = true;
            MapAtlasPanel.PanelOpen.OnValUpdate += _ => NeedSave = true;
        }

        public override object GetSaveData()
        {
            return new Data
            {
                MarkStructuresOnMap = AtlasValSet.markStructuresOnMap.val,
                MarkPlanteraBulb = AtlasValSet.markPlanteraBulb.val,
                MarkSwordShrine = AtlasValSet.markSwordShrine.val,
                MarkBeeHive = AtlasValSet.markBeeHive.val,
                MarkTempleAltar = AtlasValSet.markTempleAltar.val,
                MarkTempleDoor = AtlasValSet.markTempleDoor.val,
                MarkEvilAltars = AtlasValSet.markEvilAltars.val,
                MarkEvilOrbsHearts = AtlasValSet.markEvilOrbsHearts.val,
                MarkShimmer = AtlasValSet.markShimmer.val,
                MarkPyramid = AtlasValSet.markPyramid.val,
                MarkFloatingIsland = AtlasValSet.markFloatingIsland.val,
                MarkDungeon = AtlasValSet.markDungeon.val,
                MarkDungeonWaterBolt = AtlasValSet.markDungeonWaterBolt.val,
                MarkMiniBiomes = AtlasValSet.markMiniBiomes.val,
                MarkMushroomBiome = AtlasValSet.markMushroomBiome.val,
                MarkAntlionHive = AtlasValSet.markAntlionHive.val,
                MarkMossCaves = AtlasValSet.markMossCaves.val,
                MarkMeteorite = AtlasValSet.markMeteorite.val,
                MarkTrapsExplosives = AtlasValSet.markTrapsExplosives.val,
                MarkTrappedNPCs = AtlasValSet.markTrappedNPCs.val,
                MarkChestsAll = AtlasValSet.markChestsAll.val,
                MarkChestsShowEmpty = AtlasValSet.markChestsShowEmpty.val,
                MarkChestsSurfaceUnderground = AtlasValSet.markChestsSurfaceUnderground.val,
                MarkChestsShadowBiome = AtlasValSet.markChestsShadowBiome.val,
                MarkChestsTrapped = AtlasValSet.markChestsTrapped.val,
                PanelOpen = MapAtlasPanel.PanelOpen.val,
            };
        }

        public override void SetDefault()
        {
            AtlasValSet.markStructuresOnMap.Reset();
            AtlasValSet.markPlanteraBulb.Reset();
            AtlasValSet.markSwordShrine.Reset();
            AtlasValSet.markBeeHive.Reset();
            AtlasValSet.markTempleAltar.Reset();
            AtlasValSet.markTempleDoor.Reset();
            AtlasValSet.markEvilAltars.Reset();
            AtlasValSet.markEvilOrbsHearts.Reset();
            AtlasValSet.markShimmer.Reset();
            AtlasValSet.markPyramid.Reset();
            AtlasValSet.markFloatingIsland.Reset();
            AtlasValSet.markDungeon.Reset();
            AtlasValSet.markDungeonWaterBolt.Reset();
            AtlasValSet.markMiniBiomes.Reset();
            AtlasValSet.markMushroomBiome.Reset();
            AtlasValSet.markAntlionHive.Reset();
            AtlasValSet.markMossCaves.Reset();
            AtlasValSet.markMeteorite.Reset();
            AtlasValSet.markTrapsExplosives.Reset();
            AtlasValSet.markTrappedNPCs.Reset();
            AtlasValSet.markChestsAll.Reset();
            AtlasValSet.markChestsShowEmpty.Reset();
            AtlasValSet.markChestsSurfaceUnderground.Reset();
            AtlasValSet.markChestsShadowBiome.Reset();
            AtlasValSet.markChestsTrapped.Reset();
            MapAtlasPanel.PanelOpen.Reset();
        }
    }
}
