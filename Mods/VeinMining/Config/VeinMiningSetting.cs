using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.UI;

namespace VeinMining.Config
{
    /// <summary>
    /// 连锁挖矿 Mod 设置项管理（负责 JSON 存储与 UI 界面生成）
    /// </summary>
    internal class VeinMiningSetting : ModSetting
    {
        public static VeinMiningSetting Instance { get; private set; }

        public override string Name => "连锁挖矿设置";
        public override string Title => "简单连锁挖矿: 配置";
        public override string FilePath => "Config.json";
        public override Type DataType => typeof(VeinMiningData);

        private List<Action> updateUiCallbacks = new List<Action>();

        public VeinMiningSetting()
        {
            Instance = this;
        }

        public override void Load(object v)
        {
            if (v is VeinMiningData data)
            {
                VeinMiningConfig.Enable = data.enable;
                VeinMiningConfig.MaxTiles = data.maxTiles > 0 ? data.maxTiles : 200;
                VeinMiningConfig.MineTrashTiles = data.mineTrashTiles;
                VeinMiningConfig.MineGems = data.mineGems;
            }
            else
            {
                SetDefault();
                Save();
            }
        }

        public override object GetSaveData()
        {
            return new VeinMiningData
            {
                enable = VeinMiningConfig.Enable,
                maxTiles = VeinMiningConfig.MaxTiles,
                mineTrashTiles = VeinMiningConfig.MineTrashTiles,
                mineGems = VeinMiningConfig.MineGems
            };
        }

        public override void SetDefault()
        {
            VeinMiningConfig.Enable = true;
            VeinMiningConfig.MaxTiles = 200;
            VeinMiningConfig.MineTrashTiles = false;
            VeinMiningConfig.MineGems = true;

            NeedSave = true;
            Save();

            foreach (var update in updateUiCallbacks)
            {
                update?.Invoke();
            }
        }

        public override UIElement GetUI()
        {
            UIScrollViewer2 sv = new UIScrollViewer2();
            sv.Width.Precent = 1;
            sv.Height.Precent = 1;

            foreach (var ui in CreateSettingUIElements())
            {
                sv.AddChild(ui);
            }

            return sv;
        }

        /// <summary>
        /// 创建供 ModSetting 与 QuickSetting 共同使用的设置控件列表
        /// </summary>
        public List<UIElement> CreateSettingUIElements()
        {
            updateUiCallbacks.Clear();
            List<UIElement> list = new List<UIElement>();

            // 1. 连锁挖矿总开关
            Texture2D pickIco = Main.Assets.Request<Texture2D>("Images/Item_3507", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIItemSwitch uiEnable = new UIItemSwitch(pickIco, "启用简单连锁挖矿");
            uiEnable.SetVal(VeinMiningConfig.Enable);
            uiEnable.OnValUpdate += val =>
            {
                if (VeinMiningConfig.Enable == val) return;
                VeinMiningConfig.Enable = val;
                NeedSave = true;
                Save();
                VeinMiningConfig.OnConfigChanged?.Invoke();
            };
            updateUiCallbacks.Add(() => uiEnable.SetVal(VeinMiningConfig.Enable));
            list.Add(uiEnable);

            // 2. 最大连锁数量滑块 (10 ~ 500 格)
            Texture2D oreIco = Main.Assets.Request<Texture2D>("Images/Item_12", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIItemValueSlider uiMaxTiles = new UIItemValueSlider(10, 500, oreIco, "最大连锁破坏数量");
            uiMaxTiles.FloatToString = v => $"{(int)v} 格";
            uiMaxTiles.SetVal(VeinMiningConfig.MaxTiles);
            uiMaxTiles.OnValUpdate += val =>
            {
                int intVal = (int)val;
                if (VeinMiningConfig.MaxTiles == intVal) return;
                VeinMiningConfig.MaxTiles = intVal;
                NeedSave = true;
                Save();
                VeinMiningConfig.OnConfigChanged?.Invoke();
            };
            updateUiCallbacks.Add(() => uiMaxTiles.SetVal(VeinMiningConfig.MaxTiles));
            list.Add(uiMaxTiles);

            // 3. 连锁挖掘宝石/沙漠化石开关
            Texture2D gemIco = Main.Assets.Request<Texture2D>("Images/Item_181", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIItemSwitch uiGems = new UIItemSwitch(gemIco, "连锁破坏地下宝石与沙漠化石");
            uiGems.SetVal(VeinMiningConfig.MineGems);
            uiGems.OnValUpdate += val =>
            {
                if (VeinMiningConfig.MineGems == val) return;
                VeinMiningConfig.MineGems = val;
                NeedSave = true;
                Save();
                VeinMiningConfig.OnConfigChanged?.Invoke();
            };
            updateUiCallbacks.Add(() => uiGems.SetVal(VeinMiningConfig.MineGems));
            list.Add(uiGems);

            // 4. 连锁挖掘泥土/石头杂块开关
            Texture2D stoneIco = Main.Assets.Request<Texture2D>("Images/Item_3", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIItemSwitch uiTrash = new UIItemSwitch(stoneIco, "连锁破坏泥土/石头等杂块");
            uiTrash.SetVal(VeinMiningConfig.MineTrashTiles);
            uiTrash.OnValUpdate += val =>
            {
                if (VeinMiningConfig.MineTrashTiles == val) return;
                VeinMiningConfig.MineTrashTiles = val;
                NeedSave = true;
                Save();
                VeinMiningConfig.OnConfigChanged?.Invoke();
            };
            updateUiCallbacks.Add(() => uiTrash.SetVal(VeinMiningConfig.MineTrashTiles));
            list.Add(uiTrash);

            return list;
        }
    }
}
