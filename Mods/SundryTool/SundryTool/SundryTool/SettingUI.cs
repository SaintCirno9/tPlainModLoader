using System;
using SundryTool.Content.HeldItemModify;
using SundryTool.Content.PlayerModify;
using SundryTool.Content.QoL;
using SundryTool.Utils.quickBuild;
using tContentPatch;
using Terraria.UI;

namespace SundryTool
{
    public class PlayerModifyData
    {
        public bool damage;
        public float damage_val;
        public bool armorPenetration;
        public int armorPenetration_val;
        public bool maxMinions;
        public int maxMinions_val;
        public bool endurance;
        public float endurance_val;
        public bool grabRange;
        public int grabRange_val = 20;
    }

    public class HeldItemModifyData
    {
        public bool useTime;
        public int useTime_val;
        public bool useAnimation;
        public int useAnimation_val;
        public bool shootSpeed;
        public float shootSpeed_val;
        public bool shoot;
        public int shoot_val;
        public bool tileBoost;
        public int tileBoost_val;
    }

    public class QoLConfigData
    {
        public bool pylonUnlimitedPlacement = true;
        public bool pylonFreeTeleport = true;
        public bool instantRecall = true;
        public bool quickRespawn = true;
        public int quickRespawnFrames = 90;
        public bool autoResummonMinions = true;
        public bool herbFastGrow = true;
        public bool herbBloomAnytime = true;
        public bool staffOfRegenAutoReplant = true;
        public bool treeFastGrow = true;
        public bool treeShakeGuaranteeFruit = true;
        public bool gemTreeFullGemDrops = true;
        public bool removeGraveyardVisuals = true;
        public bool antiGriefExplosions = true;
        public bool markStructuresOnMap = true;
        public bool markPlanteraBulb = true;
        public bool markSwordShrine = true;
        public bool markBeeHive = true;
        public bool markShimmer = true;
        public bool markPyramid = true;
        public bool markTempleAltar = true;
    }

    public class SundryConfigData
    {
        public PlayerModifyData player = new PlayerModifyData();
        public HeldItemModifyData heldItem = new HeldItemModifyData();
        public QoLConfigData qol = new QoLConfigData();
    }

    internal class SettingUI_player : ModSetting
    {
        public static SettingUI_player Instance { get; private set; }
        public override string Name => "玩家属性";
        public override string Title => "杂项功能: 玩家属性";
        public override string FilePath => "Config.json";
        public override Type DataType => typeof(SundryConfigData);

        private bool bound = false;

        public SettingUI_player()
        {
            Instance = this;
        }

        public override void Load(object v)
        {
            if (v is SundryConfigData config)
            {
                if (config.player != null)
                {
                    Content.PlayerModify.ValSet.damage.val = config.player.damage;
                    Content.PlayerModify.ValSet.damage_val.val = config.player.damage_val;
                    Content.PlayerModify.ValSet.armorPenetration.val = config.player.armorPenetration;
                    Content.PlayerModify.ValSet.armorPenetration_val.val = config.player.armorPenetration_val;
                    Content.PlayerModify.ValSet.maxMinions.val = config.player.maxMinions;
                    Content.PlayerModify.ValSet.maxMinions_val.val = config.player.maxMinions_val;
                    Content.PlayerModify.ValSet.endurance.val = config.player.endurance;
                    Content.PlayerModify.ValSet.endurance_val.val = config.player.endurance_val;
                    Content.PlayerModify.ValSet.grabRange.val = config.player.grabRange;
                    Content.PlayerModify.ValSet.grabRange_val.val = config.player.grabRange_val;
                }
                if (config.heldItem != null)
                {
                    Content.HeldItemModify.ValSet.useTime.val = config.heldItem.useTime;
                    Content.HeldItemModify.ValSet.useTime_val.val = config.heldItem.useTime_val;
                    Content.HeldItemModify.ValSet.useAnimation.val = config.heldItem.useAnimation;
                    Content.HeldItemModify.ValSet.useAnimation_val.val = config.heldItem.useAnimation_val;
                    Content.HeldItemModify.ValSet.shootSpeed.val = config.heldItem.shootSpeed;
                    Content.HeldItemModify.ValSet.shootSpeed_val.val = config.heldItem.shootSpeed_val;
                    Content.HeldItemModify.ValSet.shoot.val = config.heldItem.shoot;
                    Content.HeldItemModify.ValSet.shoot_val.val = config.heldItem.shoot_val;
                    Content.HeldItemModify.ValSet.tileBoost.val = config.heldItem.tileBoost;
                    Content.HeldItemModify.ValSet.tileBoost_val.val = config.heldItem.tileBoost_val;
                }
                if (config.qol != null)
                {
                    SettingUI_qol.Instance?.LoadQoLData(config.qol);
                }
            }
            BindUpdates();
        }

        public void BindUpdates()
        {
            if (bound) return;
            bound = true;
            Content.PlayerModify.ValSet.damage.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.damage_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.armorPenetration.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.armorPenetration_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.maxMinions.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.maxMinions_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.endurance.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.endurance_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.grabRange.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.PlayerModify.ValSet.grabRange_val.OnValUpdate += _ => { NeedSave = true; Save(); };
        }

        public PlayerModifyData GetPlayerSaveData()
        {
            return new PlayerModifyData
            {
                damage = Content.PlayerModify.ValSet.damage.val,
                damage_val = Content.PlayerModify.ValSet.damage_val.val,
                armorPenetration = Content.PlayerModify.ValSet.armorPenetration.val,
                armorPenetration_val = Content.PlayerModify.ValSet.armorPenetration_val.val,
                maxMinions = Content.PlayerModify.ValSet.maxMinions.val,
                maxMinions_val = Content.PlayerModify.ValSet.maxMinions_val.val,
                endurance = Content.PlayerModify.ValSet.endurance.val,
                endurance_val = Content.PlayerModify.ValSet.endurance_val.val,
                grabRange = Content.PlayerModify.ValSet.grabRange.val,
                grabRange_val = Content.PlayerModify.ValSet.grabRange_val.val,
            };
        }

        public override object GetSaveData()
        {
            return new SundryConfigData
            {
                player = GetPlayerSaveData(),
                heldItem = SettingUI_item.Instance?.GetHeldItemSaveData() ?? new HeldItemModifyData(),
                qol = SettingUI_qol.Instance?.GetQoLSaveData() ?? new QoLConfigData()
            };
        }

        public override void SetDefault()
        {
            Content.PlayerModify.ValSet.damage.Reset();
            Content.PlayerModify.ValSet.damage_val.Reset();
            Content.PlayerModify.ValSet.armorPenetration.Reset();
            Content.PlayerModify.ValSet.armorPenetration_val.Reset();
            Content.PlayerModify.ValSet.maxMinions.Reset();
            Content.PlayerModify.ValSet.maxMinions_val.Reset();
            Content.PlayerModify.ValSet.endurance.Reset();
            Content.PlayerModify.ValSet.endurance_val.Reset();
            Content.PlayerModify.ValSet.grabRange.Reset();
            Content.PlayerModify.ValSet.grabRange_val.Reset();
            NeedSave = true;
            Save();
        }

        public override UIElement GetUI()
        {
            BindUpdates();
            return UIBuild.get3(Content.PlayerModify.ValSet.GetUI());
        }
    }

    internal class SettingUI_item : ModSetting
    {
        public static SettingUI_item Instance { get; private set; }
        public override string Name => "手持物品属性";
        public override string Title => "杂项功能: 手持物品属性";
        public override string FilePath => "Config.json";
        public override Type DataType => typeof(SundryConfigData);

        private bool bound = false;

        public SettingUI_item()
        {
            Instance = this;
        }

        public override void Load(object v)
        {
            if (v is SundryConfigData config)
            {
                if (config.player != null)
                {
                    Content.PlayerModify.ValSet.damage.val = config.player.damage;
                    Content.PlayerModify.ValSet.damage_val.val = config.player.damage_val;
                    Content.PlayerModify.ValSet.armorPenetration.val = config.player.armorPenetration;
                    Content.PlayerModify.ValSet.armorPenetration_val.val = config.player.armorPenetration_val;
                    Content.PlayerModify.ValSet.maxMinions.val = config.player.maxMinions;
                    Content.PlayerModify.ValSet.maxMinions_val.val = config.player.maxMinions_val;
                    Content.PlayerModify.ValSet.endurance.val = config.player.endurance;
                    Content.PlayerModify.ValSet.endurance_val.val = config.player.endurance_val;
                    Content.PlayerModify.ValSet.grabRange.val = config.player.grabRange;
                    Content.PlayerModify.ValSet.grabRange_val.val = config.player.grabRange_val;
                }
                if (config.heldItem != null)
                {
                    Content.HeldItemModify.ValSet.useTime.val = config.heldItem.useTime;
                    Content.HeldItemModify.ValSet.useTime_val.val = config.heldItem.useTime_val;
                    Content.HeldItemModify.ValSet.useAnimation.val = config.heldItem.useAnimation;
                    Content.HeldItemModify.ValSet.useAnimation_val.val = config.heldItem.useAnimation_val;
                    Content.HeldItemModify.ValSet.shootSpeed.val = config.heldItem.shootSpeed;
                    Content.HeldItemModify.ValSet.shootSpeed_val.val = config.heldItem.shootSpeed_val;
                    Content.HeldItemModify.ValSet.shoot.val = config.heldItem.shoot;
                    Content.HeldItemModify.ValSet.shoot_val.val = config.heldItem.shoot_val;
                    Content.HeldItemModify.ValSet.tileBoost.val = config.heldItem.tileBoost;
                    Content.HeldItemModify.ValSet.tileBoost_val.val = config.heldItem.tileBoost_val;
                }
                if (config.qol != null)
                {
                    SettingUI_qol.Instance?.LoadQoLData(config.qol);
                }
            }
            BindUpdates();
        }

        public void BindUpdates()
        {
            if (bound) return;
            bound = true;
            Content.HeldItemModify.ValSet.useTime.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.useTime_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.useAnimation.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.useAnimation_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.shootSpeed.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.shootSpeed_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.shoot.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.shoot_val.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.tileBoost.OnValUpdate += _ => { NeedSave = true; Save(); };
            Content.HeldItemModify.ValSet.tileBoost_val.OnValUpdate += _ => { NeedSave = true; Save(); };
        }

        public HeldItemModifyData GetHeldItemSaveData()
        {
            return new HeldItemModifyData
            {
                useTime = Content.HeldItemModify.ValSet.useTime.val,
                useTime_val = Content.HeldItemModify.ValSet.useTime_val.val,
                useAnimation = Content.HeldItemModify.ValSet.useAnimation.val,
                useAnimation_val = Content.HeldItemModify.ValSet.useAnimation_val.val,
                shootSpeed = Content.HeldItemModify.ValSet.shootSpeed.val,
                shootSpeed_val = Content.HeldItemModify.ValSet.shootSpeed_val.val,
                shoot = Content.HeldItemModify.ValSet.shoot.val,
                shoot_val = Content.HeldItemModify.ValSet.shoot_val.val,
                tileBoost = Content.HeldItemModify.ValSet.tileBoost.val,
                tileBoost_val = Content.HeldItemModify.ValSet.tileBoost_val.val,
            };
        }

        public override object GetSaveData()
        {
            return new SundryConfigData
            {
                player = SettingUI_player.Instance?.GetPlayerSaveData() ?? new PlayerModifyData(),
                heldItem = GetHeldItemSaveData(),
                qol = SettingUI_qol.Instance?.GetQoLSaveData() ?? new QoLConfigData()
            };
        }

        public override void SetDefault()
        {
            Content.HeldItemModify.ValSet.useTime.Reset();
            Content.HeldItemModify.ValSet.useTime_val.Reset();
            Content.HeldItemModify.ValSet.useAnimation.Reset();
            Content.HeldItemModify.ValSet.useAnimation_val.Reset();
            Content.HeldItemModify.ValSet.shootSpeed.Reset();
            Content.HeldItemModify.ValSet.shootSpeed_val.Reset();
            Content.HeldItemModify.ValSet.shoot.Reset();
            Content.HeldItemModify.ValSet.shoot_val.Reset();
            Content.HeldItemModify.ValSet.tileBoost.Reset();
            Content.HeldItemModify.ValSet.tileBoost_val.Reset();
            NeedSave = true;
            Save();
        }

        public override UIElement GetUI()
        {
            BindUpdates();
            return UIBuild.get3(Content.HeldItemModify.ValSet.GetUI());
        }
    }

    internal class SettingUI_qol : ModSetting
    {
        public static SettingUI_qol Instance { get; private set; }
        public override string Name => "便捷与生态增强";
        public override string Title => "杂项功能: 便捷与生态增强";
        public override string FilePath => "Config.json";
        public override Type DataType => typeof(SundryConfigData);

        private bool bound = false;

        public SettingUI_qol()
        {
            Instance = this;
        }

        public void LoadQoLData(QoLConfigData data)
        {
            if (data == null) return;
            QoLValSet.pylonUnlimitedPlacement.val = data.pylonUnlimitedPlacement;
            QoLValSet.pylonFreeTeleport.val = data.pylonFreeTeleport;
            QoLValSet.instantRecall.val = data.instantRecall;
            QoLValSet.quickRespawn.val = data.quickRespawn;
            QoLValSet.quickRespawnFrames.val = data.quickRespawnFrames;
            QoLValSet.autoResummonMinions.val = data.autoResummonMinions;
            QoLValSet.herbFastGrow.val = data.herbFastGrow;
            QoLValSet.herbBloomAnytime.val = data.herbBloomAnytime;
            QoLValSet.staffOfRegenAutoReplant.val = data.staffOfRegenAutoReplant;
            QoLValSet.treeFastGrow.val = data.treeFastGrow;
            QoLValSet.treeShakeGuaranteeFruit.val = data.treeShakeGuaranteeFruit;
            QoLValSet.gemTreeFullGemDrops.val = data.gemTreeFullGemDrops;
            QoLValSet.removeGraveyardVisuals.val = data.removeGraveyardVisuals;
            QoLValSet.antiGriefExplosions.val = data.antiGriefExplosions;
            QoLValSet.markStructuresOnMap.val = data.markStructuresOnMap;
            QoLValSet.markPlanteraBulb.val = data.markPlanteraBulb;
            QoLValSet.markSwordShrine.val = data.markSwordShrine;
            QoLValSet.markBeeHive.val = data.markBeeHive;
            QoLValSet.markShimmer.val = data.markShimmer;
            QoLValSet.markPyramid.val = data.markPyramid;
            QoLValSet.markTempleAltar.val = data.markTempleAltar;
        }

        public override void Load(object v)
        {
            if (v is SundryConfigData config)
            {
                if (config.player != null)
                {
                    SettingUI_player.Instance?.Load(config);
                }
                if (config.heldItem != null)
                {
                    SettingUI_item.Instance?.Load(config);
                }
                if (config.qol != null)
                {
                    LoadQoLData(config.qol);
                }
            }
            BindUpdates();
        }

        public void BindUpdates()
        {
            if (bound) return;
            bound = true;
            QoLValSet.pylonUnlimitedPlacement.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.pylonFreeTeleport.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.instantRecall.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.quickRespawn.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.quickRespawnFrames.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.autoResummonMinions.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.herbFastGrow.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.herbBloomAnytime.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.staffOfRegenAutoReplant.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.treeFastGrow.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.treeShakeGuaranteeFruit.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.gemTreeFullGemDrops.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.removeGraveyardVisuals.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.antiGriefExplosions.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.markStructuresOnMap.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.markPlanteraBulb.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.markSwordShrine.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.markBeeHive.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.markShimmer.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.markPyramid.OnValUpdate += _ => { NeedSave = true; Save(); };
            QoLValSet.markTempleAltar.OnValUpdate += _ => { NeedSave = true; Save(); };
        }

        public QoLConfigData GetQoLSaveData()
        {
            return new QoLConfigData
            {
                pylonUnlimitedPlacement = QoLValSet.pylonUnlimitedPlacement.val,
                pylonFreeTeleport = QoLValSet.pylonFreeTeleport.val,
                instantRecall = QoLValSet.instantRecall.val,
                quickRespawn = QoLValSet.quickRespawn.val,
                quickRespawnFrames = QoLValSet.quickRespawnFrames.val,
                autoResummonMinions = QoLValSet.autoResummonMinions.val,
                herbFastGrow = QoLValSet.herbFastGrow.val,
                herbBloomAnytime = QoLValSet.herbBloomAnytime.val,
                staffOfRegenAutoReplant = QoLValSet.staffOfRegenAutoReplant.val,
                treeFastGrow = QoLValSet.treeFastGrow.val,
                treeShakeGuaranteeFruit = QoLValSet.treeShakeGuaranteeFruit.val,
                gemTreeFullGemDrops = QoLValSet.gemTreeFullGemDrops.val,
                removeGraveyardVisuals = QoLValSet.removeGraveyardVisuals.val,
                antiGriefExplosions = QoLValSet.antiGriefExplosions.val,
                markStructuresOnMap = QoLValSet.markStructuresOnMap.val,
                markPlanteraBulb = QoLValSet.markPlanteraBulb.val,
                markSwordShrine = QoLValSet.markSwordShrine.val,
                markBeeHive = QoLValSet.markBeeHive.val,
                markShimmer = QoLValSet.markShimmer.val,
                markPyramid = QoLValSet.markPyramid.val,
                markTempleAltar = QoLValSet.markTempleAltar.val,
            };
        }

        public override object GetSaveData()
        {
            return new SundryConfigData
            {
                player = SettingUI_player.Instance?.GetPlayerSaveData() ?? new PlayerModifyData(),
                heldItem = SettingUI_item.Instance?.GetHeldItemSaveData() ?? new HeldItemModifyData(),
                qol = GetQoLSaveData()
            };
        }

        public override void SetDefault()
        {
            QoLValSet.pylonUnlimitedPlacement.Reset();
            QoLValSet.pylonFreeTeleport.Reset();
            QoLValSet.instantRecall.Reset();
            QoLValSet.quickRespawn.Reset();
            QoLValSet.quickRespawnFrames.Reset();
            QoLValSet.autoResummonMinions.Reset();
            QoLValSet.herbFastGrow.Reset();
            QoLValSet.herbBloomAnytime.Reset();
            QoLValSet.staffOfRegenAutoReplant.Reset();
            QoLValSet.treeFastGrow.Reset();
            QoLValSet.treeShakeGuaranteeFruit.Reset();
            QoLValSet.gemTreeFullGemDrops.Reset();
            QoLValSet.removeGraveyardVisuals.Reset();
            QoLValSet.antiGriefExplosions.Reset();
            QoLValSet.markStructuresOnMap.Reset();
            QoLValSet.markPlanteraBulb.Reset();
            QoLValSet.markSwordShrine.Reset();
            QoLValSet.markBeeHive.Reset();
            QoLValSet.markShimmer.Reset();
            QoLValSet.markPyramid.Reset();
            QoLValSet.markTempleAltar.Reset();
            NeedSave = true;
            Save();
        }

        public override UIElement GetUI()
        {
            BindUpdates();
            return UIBuild.get3(QoLValSet.GetUI());
        }
    }

    internal class SettingUI_function1 : ModSetting
    {
        public override string Name => "其它功能1";
        public override string Title => "杂项功能: 其它功能1";

        public override UIElement GetUI()
        {
            return UIBuild.get3(Content.Function1.Function.GetUI());
        }
    }

    internal class SettingUI_function2 : ModSetting
    {
        public override string Name => "其它功能2";
        public override string Title => "杂项功能: 其它功能2";

        public override UIElement GetUI()
        {
            return UIBuild.get3(Content.Function2.Function.GetUI());
        }
    }
}
