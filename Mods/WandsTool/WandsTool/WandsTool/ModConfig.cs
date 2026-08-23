using Newtonsoft.Json;
using System;
using tContentPatch;
using tContentPatch.Content.UI;
using tContentPatch.Content.UI.ModSet;
using Terraria;
using Terraria.UI;
using WandsTool.Content;

namespace WandsTool
{
    internal class ModConfig : ModSetting
    {
        public class Data
        {
            [JsonProperty("消耗物品")]
            public bool ConsumablesItem = true;

            [JsonProperty("方块替换模式")]
            public bool BlockReplace = false;

            [JsonProperty("破坏掉落物自动吸附")]
            public bool CollectDrops = true;

            [JsonProperty("无限液体模式")]
            public bool InfiniteLiquid = false;

            [JsonProperty("单次批量处理速率")]
            public int BatchSize = 64;
        }

        public override string Name => "设置";
        public override string Title => "魔杖工具: 设置";
        public override string FilePath => "Config.json";
        public override Type DataType => typeof(Data);
        private static Data data = null;

        public override void Load(object v)
        {
            if (v is Data data)
            {
                ModConfig.data = data;
            }
            else
            {
                SetDefault();
                Save();
            }

            SyncToGameMain();
        }

        public override void SetDefault()
        {
            data = new Data();
            NeedSave = true;
            Save();
            SyncToGameMain();
        }

        private static void SyncToGameMain()
        {
            if (data == null) return;
            gameMain.Wand_BlockReplace = data.BlockReplace;
            gameMain.Wand_CollectDrops = data.CollectDrops;
            gameMain.Wand_InfiniteLiquid = data.InfiniteLiquid;
            if (data.BatchSize > 0) gameMain.Wand_BatchSize = data.BatchSize;
        }

        public override object GetSaveData() => data;

        public override UIElement GetUI()
        {
            UIScrollViewer2 sv = new UIScrollViewer2();
            sv.Width.Set(0, 1);
            sv.Height.Set(0, 1);

            // 1. 消耗物品
            UIItemSwitch s1 = new UIItemSwitch(null, "消耗物品");
            s1.OnUpdate += _ =>
            {
                if (data == null) return;
                s1.SetVal(data.ConsumablesItem);
                if (s1.IsMouseHovering) Main.instance.MouseText("放置方块、墙壁或液体时是否扣除背包对应物品/桶");
            };
            s1.OnValUpdate += v =>
            {
                if (data == null) return;
                if (data.ConsumablesItem == v) return;
                data.ConsumablesItem = v;
                NeedSave = true;
                Save();
            };
            sv.AddChild(s1);

            // 2. 方块替换模式
            UIItemSwitch s2 = new UIItemSwitch(null, "方块替换模式");
            s2.OnUpdate += _ =>
            {
                if (data == null) return;
                s2.SetVal(data.BlockReplace);
                if (s2.IsMouseHovering) Main.instance.MouseText("放置方块或背景墙时，自动替换区域内原有不同方块/墙壁");
            };
            s2.OnValUpdate += v =>
            {
                if (data == null) return;
                if (data.BlockReplace == v) return;
                data.BlockReplace = v;
                gameMain.Wand_BlockReplace = v;
                NeedSave = true;
                Save();
            };
            sv.AddChild(s2);

            // 3. 破坏掉落物自动吸附
            UIItemSwitch s3 = new UIItemSwitch(null, "破坏掉落物自动吸附");
            s3.OnUpdate += _ =>
            {
                if (data == null) return;
                s3.SetVal(data.CollectDrops);
                if (s3.IsMouseHovering) Main.instance.MouseText("星爆/区域破坏方块与墙壁时，自动将掉落物吸附至玩家背包");
            };
            s3.OnValUpdate += v =>
            {
                if (data == null) return;
                if (data.CollectDrops == v) return;
                data.CollectDrops = v;
                gameMain.Wand_CollectDrops = v;
                NeedSave = true;
                Save();
            };
            sv.AddChild(s3);

            // 4. 无限液体模式
            UIItemSwitch s4 = new UIItemSwitch(null, "无限液体模式");
            s4.OnUpdate += _ =>
            {
                if (data == null) return;
                s4.SetVal(data.InfiniteLiquid);
                if (s4.IsMouseHovering) Main.instance.MouseText("铺设水、岩浆、蜂蜜、微光时无需消耗背包液体桶");
            };
            s4.OnValUpdate += v =>
            {
                if (data == null) return;
                if (data.InfiniteLiquid == v) return;
                data.InfiniteLiquid = v;
                gameMain.Wand_InfiniteLiquid = v;
                NeedSave = true;
                Save();
            };
            sv.AddChild(s4);

            return sv;
        }

        public static bool IsConsumablesItem()
        {
            if (data == null) data = new Data();
            return data.ConsumablesItem;
        }
    }
}
