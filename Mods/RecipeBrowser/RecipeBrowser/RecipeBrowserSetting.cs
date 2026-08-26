using System;
using Newtonsoft.Json;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.UI;

namespace RecipeBrowser
{
    /// <summary>
    /// RecipeBrowser 模组设置面板 (接入 TPML ModSetting)
    /// 作者: SaintCirno9
    /// </summary>
    public class RecipeBrowserSetting : ModSetting
    {
        public override string Name => "设置";
        public override string Title => "合成表与图鉴: 设置";
        public override string FilePath => "Config.json";
        public override Type DataType => typeof(RecipeBrowserClientConfig);

        public override void Load(object v)
        {
            if (v is RecipeBrowserClientConfig cfg)
            {
                RecipeBrowserClientConfig.Instance = cfg;
            }
            else
            {
                RecipeBrowserClientConfig.Load();
            }
        }

        public override void SetDefault()
        {
            RecipeBrowserClientConfig.Instance = new RecipeBrowserClientConfig();
            NeedSave = true;
            Save();
        }

        public override object GetSaveData() => RecipeBrowserClientConfig.Instance;

        public override UIElement GetUI()
        {
            UIScrollViewer2 sv = new UIScrollViewer2();
            sv.Width.Set(0, 1);
            sv.Height.Set(0, 1);

            // 1. 性能剖析器 (Profiler) 开关 (默认关闭)
            UIItemSwitch profilerSwitch = new UIItemSwitch(null, "性能剖析器 (Profiler)");
            profilerSwitch.OnUpdate += _ =>
            {
                profilerSwitch.SetVal(RecipeBrowserClientConfig.Instance.EnableProfiler);
                if (profilerSwitch.IsMouseHovering)
                {
                    Main.instance.MouseText("是否开启合成表性能剖析器并向日志/聊天框输出分段耗时 (默认关闭)");
                }
            };
            profilerSwitch.OnValUpdate += v =>
            {
                if (RecipeBrowserClientConfig.Instance.EnableProfiler == v) return;
                RecipeBrowserClientConfig.Instance.EnableProfiler = v;
                RecipeBrowserClientConfig.Save();
                NeedSave = true;
            };
            sv.AddChild(profilerSwitch);

            // 2. 显示配方模组过滤器
            UIItemSwitch modFilterSwitch = new UIItemSwitch(null, "配方模组过滤器");
            modFilterSwitch.OnUpdate += _ =>
            {
                modFilterSwitch.SetVal(RecipeBrowserClientConfig.Instance.ShowRecipeModFilter);
                if (modFilterSwitch.IsMouseHovering) Main.instance.MouseText("是否在配方面板中显示 Mod 来源筛选下拉框");
            };
            modFilterSwitch.OnValUpdate += v =>
            {
                RecipeBrowserClientConfig.Instance.ShowRecipeModFilter = v;
                RecipeBrowserClientConfig.Save();
                NeedSave = true;
            };
            sv.AddChild(modFilterSwitch);

            // 3. 记住最后选中的配方
            UIItemSwitch saveLastRecipeSwitch = new UIItemSwitch(null, "记住最后选中的配方");
            saveLastRecipeSwitch.OnUpdate += _ =>
            {
                saveLastRecipeSwitch.SetVal(RecipeBrowserClientConfig.Instance.SaveLastSelectedRecipe);
                if (saveLastRecipeSwitch.IsMouseHovering) Main.instance.MouseText("关闭并重新打开合成表时保留上次查询的配方");
            };
            saveLastRecipeSwitch.OnValUpdate += v =>
            {
                RecipeBrowserClientConfig.Instance.SaveLastSelectedRecipe = v;
                RecipeBrowserClientConfig.Save();
                NeedSave = true;
            };
            sv.AddChild(saveLastRecipeSwitch);

            // 4. 点击物品槽位时自动隐藏
            UIItemSwitch autoHideSwitch = new UIItemSwitch(null, "选中物品时自动关闭");
            autoHideSwitch.OnUpdate += _ =>
            {
                autoHideSwitch.SetVal(RecipeBrowserClientConfig.Instance.AutomaticallyHideWhenItemSlotClicked);
                if (autoHideSwitch.IsMouseHovering) Main.instance.MouseText("在合成表中点击具体物品/制作目标时自动收起面板");
            };
            autoHideSwitch.OnValUpdate += v =>
            {
                RecipeBrowserClientConfig.Instance.AutomaticallyHideWhenItemSlotClicked = v;
                RecipeBrowserClientConfig.Save();
                NeedSave = true;
            };
            sv.AddChild(autoHideSwitch);

            return sv;
        }
    }
}
