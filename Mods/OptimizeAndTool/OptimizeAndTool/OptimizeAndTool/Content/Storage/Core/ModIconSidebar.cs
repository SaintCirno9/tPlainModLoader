using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using OptimizeAndTool.Content.Storage.Core;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;
using TPML.Content;

namespace OptimizeAndTool.Content.Storage.Core
{
    /// <summary>
    /// 通用 Mod 分类筛选侧边栏
    /// 当容器中存有来自多个不同 Mod 的物品时自动展示，提供极速按 Mod 来源筛选能力
    /// 作者: SaintCirno9
    /// </summary>
    public class ModIconSidebar : UIPanel
    {
        public string CurrentFilter { get; private set; } = "All";
        public event Action<string> OnFilterChanged;
        public bool HasMultipleMods { get; private set; } = false;

        private IBagInventory bag;

        public ModIconSidebar(IBagInventory bag)
        {
            this.bag = bag;
            Width.Set(42, 0);
            Height.Set(0, 1);
            SetPadding(2);
            BackgroundColor = new Color(20, 25, 45) * 0.85f;
            BorderColor = new Color(43, 60, 120);
            OverflowHidden = true;
        }

        public void SetBag(IBagInventory bag)
        {
            this.bag = bag;
        }

        public void Rebuild()
        {
            Elements.Clear();
            if (bag?.Slots == null || !bag.ShowModSidebar)
            {
                HasMultipleMods = false;
                CurrentFilter = "All";
                return;
            }

            var modNames = new HashSet<string>();
            modNames.Add("All");

            Item[] slots = bag.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                Item it = slots[i];
                if (it != null && !it.IsAir)
                {
                    if (it.type >= ItemID.Count)
                    {
                        ModItem modIt = ItemLoader.GetModItem(it.type);
                        modNames.Add(modIt?.Mod?.Name ?? "TPML");
                    }
                    else
                    {
                        modNames.Add("Terraria");
                    }
                }
            }

            // 只有当存在除 All 之外至少 2 种不同来源时才开启侧边栏
            HasMultipleMods = modNames.Count >= 3;
            if (!HasMultipleMods)
            {
                CurrentFilter = "All";
                return;
            }

            var sortedList = modNames.OrderBy(n => n == "All" ? "0" : (n == "Terraria" ? "1" : n)).ToList();

            float top = 4f;
            foreach (var mod in sortedList)
            {
                ModIconOption btn = new ModIconOption(mod, mod == CurrentFilter);
                btn.Top.Set(top, 0);
                btn.Left.Set(2, 0);
                string captureName = mod;
                btn.OnClick += () =>
                {
                    CurrentFilter = captureName;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    OnFilterChanged?.Invoke(CurrentFilter);
                    Rebuild();
                };
                Append(btn);
                top += 36f;
            }
        }
    }

    internal class ModIconOption : UIPanel
    {
        public event Action OnClick;
        private readonly string modName;
        private readonly bool isSelected;

        public ModIconOption(string modName, bool isSelected)
        {
            this.modName = modName;
            this.isSelected = isSelected;

            Width.Set(34, 0);
            Height.Set(32, 0);
            SetPadding(0);

            BackgroundColor = isSelected ? new Color(60, 120, 200) : new Color(30, 40, 70);
            BorderColor = isSelected ? Color.Gold : new Color(60, 80, 140);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            OnClick?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            base.DrawSelf(sb);

            CalculatedStyle dim = GetDimensions();
            string shortLabel = modName == "All" ? "全" : (modName == "Terraria" ? "原" : modName.Substring(0, Math.Min(2, modName.Length)));

            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(shortLabel);
            Vector2 textPos = new Vector2(dim.X + (dim.Width - textSize.X * 0.8f) / 2f, dim.Y + (dim.Height - textSize.Y * 0.8f) / 2f);

            ChatManager.DrawColorCodedString(
                sb,
                FontAssets.MouseText.Value,
                shortLabel,
                textPos,
                isSelected ? Color.Yellow : Color.White,
                0f,
                Vector2.Zero,
                new Vector2(0.8f)
            );

            if (IsMouseHovering)
            {
                Main.hoverItemName = modName == "All" ? "显示全部模组物品" : $"按模组过滤: {modName}";
            }
        }
    }
}
