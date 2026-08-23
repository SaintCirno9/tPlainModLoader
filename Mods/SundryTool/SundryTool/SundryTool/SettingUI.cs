using System;
using SundryTool.Content.HeldItemModify;
using SundryTool.Content.PlayerModify;
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

    internal class SettingUI_player : ModSetting
    {
        public static SettingUI_player Instance { get; private set; }
        public override string Name => "玩家属性";
        public override string Title => "杂项功能: 玩家属性";
        public override string FilePath => "Setting_player.json";
        public override Type DataType => typeof(PlayerModifyData);

        private bool bound = false;

        public SettingUI_player()
        {
            Instance = this;
        }

        public override void Load(object v)
        {
            if (v is PlayerModifyData data)
            {
                Content.PlayerModify.ValSet.damage.val = data.damage;
                Content.PlayerModify.ValSet.damage_val.val = data.damage_val;
                Content.PlayerModify.ValSet.armorPenetration.val = data.armorPenetration;
                Content.PlayerModify.ValSet.armorPenetration_val.val = data.armorPenetration_val;
                Content.PlayerModify.ValSet.maxMinions.val = data.maxMinions;
                Content.PlayerModify.ValSet.maxMinions_val.val = data.maxMinions_val;
                Content.PlayerModify.ValSet.endurance.val = data.endurance;
                Content.PlayerModify.ValSet.endurance_val.val = data.endurance_val;
                Content.PlayerModify.ValSet.grabRange.val = data.grabRange;
                Content.PlayerModify.ValSet.grabRange_val.val = data.grabRange_val;
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

        public override object GetSaveData()
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

    internal class SettingUI_item : ModSetting
    {
        public static SettingUI_item Instance { get; private set; }
        public override string Name => "手持物品属性";
        public override string Title => "杂项功能: 手持物品属性";
        public override string FilePath => "Setting_item.json";
        public override Type DataType => typeof(HeldItemModifyData);

        private bool bound = false;

        public SettingUI_item()
        {
            Instance = this;
        }

        public override void Load(object v)
        {
            if (v is HeldItemModifyData data)
            {
                Content.HeldItemModify.ValSet.useTime.val = data.useTime;
                Content.HeldItemModify.ValSet.useTime_val.val = data.useTime_val;
                Content.HeldItemModify.ValSet.useAnimation.val = data.useAnimation;
                Content.HeldItemModify.ValSet.useAnimation_val.val = data.useAnimation_val;
                Content.HeldItemModify.ValSet.shootSpeed.val = data.shootSpeed;
                Content.HeldItemModify.ValSet.shootSpeed_val.val = data.shootSpeed_val;
                Content.HeldItemModify.ValSet.shoot.val = data.shoot;
                Content.HeldItemModify.ValSet.shoot_val.val = data.shoot_val;
                Content.HeldItemModify.ValSet.tileBoost.val = data.tileBoost;
                Content.HeldItemModify.ValSet.tileBoost_val.val = data.tileBoost_val;
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

        public override object GetSaveData()
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
