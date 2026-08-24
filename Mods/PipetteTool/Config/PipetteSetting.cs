using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using PipetteTool.Input;

namespace PipetteTool.Config
{
    /// <summary>
    /// 吸管工具配置管理（负责 JSON 持久化与 UI 生成）
    /// </summary>
    public class PipetteSetting : ModSetting
    {
        public static PipetteSetting Instance { get; private set; }

        public override string Name => "吸管工具设置";
        public override string Title => "吸管工具: 配置";
        public override string FilePath => "Config.json";
        public override Type DataType => typeof(PipetteConfigData);

        private readonly List<Action> updateUiCallbacks = new List<Action>();

        public PipetteSetting()
        {
            Instance = this;
        }

        public override void Load(object v)
        {
            if (v is PipetteConfigData data)
            {
                PipetteConfig.KeyBind = !string.IsNullOrEmpty(data.keyBind) ? data.keyBind : "Q";
                PipetteConfig.Enable = data.enable;
                PipetteConfig.PickWall = data.pickWall;
                PipetteConfig.ShowNotification = data.showNotification;
            }
            else
            {
                SetDefault();
                Save();
            }
        }

        public override object GetSaveData()
        {
            return new PipetteConfigData
            {
                keyBind = PipetteConfig.KeyBind,
                enable = PipetteConfig.Enable,
                pickWall = PipetteConfig.PickWall,
                showNotification = PipetteConfig.ShowNotification
            };
        }

        public override void SetDefault()
        {
            PipetteConfig.KeyBind = "Q";
            PipetteConfig.Enable = true;
            PipetteConfig.PickWall = true;
            PipetteConfig.ShowNotification = true;

            NeedSave = true;
            Save();

            foreach (var update in updateUiCallbacks)
            {
                update?.Invoke();
            }
            PipetteConfig.OnConfigChanged?.Invoke();
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

            // 1. 快捷按键绑定控件 (提示前往原版控件界面设置)
            Texture2D keyIco = Main.Assets.Request<Texture2D>("Images/UI/Camera_1", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIKeyBindItem uiKey = new UIKeyBindItem(keyIco, "吸管快捷键 (可前往控件设置修改)");
            uiKey.SetKey(PipetteKeyHandler.GetCurrentBoundKey());
            updateUiCallbacks.Add(() => uiKey.SetKey(PipetteKeyHandler.GetCurrentBoundKey()));
            list.Add(uiKey);

            // 2. 吸管工具总开关
            Texture2D switchIco = Main.Assets.Request<Texture2D>("Images/Item_3611", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIItemSwitch uiEnable = new UIItemSwitch(switchIco, "启用吸管工具 (Pick Block)");
            uiEnable.SetVal(PipetteConfig.Enable);
            uiEnable.OnValUpdate += val =>
            {
                if (PipetteConfig.Enable == val) return;
                PipetteConfig.Enable = val;
                NeedSave = true;
                Save();
                PipetteConfig.OnConfigChanged?.Invoke();
            };
            updateUiCallbacks.Add(() => uiEnable.SetVal(PipetteConfig.Enable));
            list.Add(uiEnable);

            // 3. 背景墙吸取开关
            Texture2D wallIco = Main.Assets.Request<Texture2D>("Images/Item_131", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIItemSwitch uiWall = new UIItemSwitch(wallIco, "无物块时吸取背景墙");
            uiWall.SetVal(PipetteConfig.PickWall);
            uiWall.OnValUpdate += val =>
            {
                if (PipetteConfig.PickWall == val) return;
                PipetteConfig.PickWall = val;
                NeedSave = true;
                Save();
                PipetteConfig.OnConfigChanged?.Invoke();
            };
            updateUiCallbacks.Add(() => uiWall.SetVal(PipetteConfig.PickWall));
            list.Add(uiWall);

            // 4. 提示浮字开关
            Texture2D msgIco = Main.Assets.Request<Texture2D>("Images/UI/ButtonPlay", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            UIItemSwitch uiNotify = new UIItemSwitch(msgIco, "显示头顶状态提示文字");
            uiNotify.SetVal(PipetteConfig.ShowNotification);
            uiNotify.OnValUpdate += val =>
            {
                if (PipetteConfig.ShowNotification == val) return;
                PipetteConfig.ShowNotification = val;
                NeedSave = true;
                Save();
                PipetteConfig.OnConfigChanged?.Invoke();
            };
            updateUiCallbacks.Add(() => uiNotify.SetVal(PipetteConfig.ShowNotification));
            list.Add(uiNotify);

            return list;
        }
    }

    /// <summary>
    /// 自定义按键绑定 UI 项
    /// </summary>
    internal class UIKeyBindItem : UIItem
    {
        private string currentKey;
        private readonly UIText uiText;

        public UIKeyBindItem(Texture2D ico = null, string text = null) : base(ico, text)
        {
            uiText = new UIText("未绑定");
            uiText.HAlign = 1f;
            uiText.VAlign = 0.5f;
            uiText.TextColor = Color.LightGray;

            OnLeftMouseUp += (e, s) =>
            {
                SetKey(PipetteKeyHandler.GetCurrentBoundKey());
                Main.NewText("提示：可在游戏主菜单或 ESC 设置中的 [控件 (Controls)] 统一自定义所有模组快捷键与手柄绑定。", Color.Gold);
            };

            Append(uiText);
        }

        public void SetKey(string key)
        {
            currentKey = key;

            if (string.IsNullOrEmpty(currentKey))
            {
                uiText.SetText("未绑定");
                uiText.TextColor = Color.Gray;
            }
            else
            {
                uiText.SetText($"[ {currentKey} ]");
                uiText.TextColor = Color.White;
            }
        }
    }
}
