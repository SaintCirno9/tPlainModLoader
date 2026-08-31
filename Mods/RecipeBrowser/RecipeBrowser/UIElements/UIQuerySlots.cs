using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.UIElements
{
    public class UIQueryItemSlot : UIItemSlot
    {
        public bool real;
        public string emptyHintText;

        public UIQueryItemSlot(Item item) : base(item, 0.85f)
        {
        }

        public virtual void ReplaceWithFake(int type)
        {
            if (real && item != null && !item.IsAir)
            {
                // 归还原物品本体（保留词缀等数据；对齐原版 GetItem 语义的单机等价）
                Main.LocalPlayer.QuickSpawnItem(null, item);
            }
            real = false;
            item = new Item();
            if (type > 0)
            {
                item.SetDefaults(type);
            }
            if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
            if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (Main.mouseItem != null && !Main.mouseItem.IsAir)
            {
                if (real && item != null && !item.IsAir)
                {
                    Main.LocalPlayer.QuickSpawnItem(null, item);
                }
                real = true;
                item = Main.mouseItem.Clone();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
            else if (real && item != null && !item.IsAir)
            {
                Main.mouseItem = item.Clone();
                item.TurnToAir();
                real = false;
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
            else if (!real && item != null && !item.IsAir)
            {
                item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
        }

        public override void RightClick(UIMouseEvent evt)
        {
            if (item != null && !item.IsAir)
            {
                if (real)
                {
                    Main.LocalPlayer.QuickSpawnItem(null, item);
                }
                item.TurnToAir();
                real = false;
                SoundEngine.PlaySound(SoundID.Grab);
                if (RecipeCatalogueUI.instance != null) RecipeCatalogueUI.instance.updateNeeded = true;
                if (BestiaryUI.instance != null) BestiaryUI.instance.updateNeeded = true;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            if (item.IsAir && IsMouseHovering && !string.IsNullOrEmpty(emptyHintText))
            {
                UICommon.TooltipMouseText(emptyHintText);
            }
        }
    }

    public class UIRecipeCatalogueQueryItemSlot : UIQueryItemSlot
    {
        /// <summary>
        /// 配方组规范化物品 ID（对齐原版 CanonicalItemType 映射表）
        /// </summary>
        public int CanonicalItemType
        {
            get
            {
                int num = item?.type ?? 0;
                switch (num)
                {
                    case 5358:
                    case 5359:
                    case 5360:
                    case 5361:
                        return 5437;
                    case 5453:
                        return 4767;
                    case 5454:
                        return 5309;
                    case 5455:
                        return 5323;
                    case 5325:
                        return 4131;
                    case 5391:
                        return 4346;
                    case 5329:
                    case 5330:
                        return 5324;
                    default:
                        return num;
                }
            }
        }

        // 查询历史（对齐原版 UIRecipeCatalogueQueryItemSlot）
        internal List<int> history = new List<int>();
        internal int historyCursor;
        private bool skipHistory;

        public UIRecipeCatalogueQueryItemSlot(Item item) : base(item)
        {
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            ReplaceWithFake(item.type);
            RecipeCatalogueUI.instance.queryLootItem = (item.type == 0) ? null : item;
            RecipeCatalogueUI.instance.updateNeeded = true;
            if (SharedUI.instance?.categories != null && SharedUI.instance.categories.Count > 0)
            {
                SharedUI.instance.SelectedCategory = SharedUI.instance.categories[0];
            }
        }

        public override void ReplaceWithFake(int type)
        {
            base.ReplaceWithFake(type);
            RecipeCatalogueUI.instance.queryLootItem = item;
            RecipeCatalogueUI.instance.updateNeeded = true;
            RecipeCatalogueUI.instance.Tile = -1;
            if (RecipeCatalogueUI.instance.TileLookupRadioButton != null)
            {
                RecipeCatalogueUI.instance.TileLookupRadioButton.Selected = false;
            }
            if (SharedUI.instance?.categories != null && SharedUI.instance.categories.Count > 0)
            {
                SharedUI.instance.SelectedCategory = SharedUI.instance.categories[0];
            }
            AddToHistory(type);
        }

        internal void AddToHistory(int type)
        {
            if (!skipHistory && type != 0)
            {
                for (int i = history.Count - 1; i >= 0; i--)
                {
                    if (history[i] == type)
                    {
                        history.RemoveAt(i);
                        if (i < historyCursor) historyCursor--;
                    }
                }
                history.RemoveRange(historyCursor, history.Count - historyCursor);
                history.Add(type);
                historyCursor++;
            }
            skipHistory = false;
        }

        internal void GoBackInHistory()
        {
            skipHistory = true;
            if (real)
            {
                if (historyCursor > 0)
                {
                    ReplaceWithFake(history[historyCursor - 1]);
                }
                else if (history.Count == 0)
                {
                    Main.NewText(RecipeCatalogueUI.RBText("HistoryEmpty"), 255, 255, 255);
                }
                else
                {
                    Main.NewText(RecipeCatalogueUI.RBText("HistoryReachedStart"), 255, 255, 255);
                }
            }
            else if (historyCursor > 1)
            {
                historyCursor--;
                ReplaceWithFake(history[historyCursor - 1]);
            }
            else if (historyCursor == 1)
            {
                historyCursor--;
                ReplaceWithFake(0);
            }
            else
            {
                Main.NewText("Error: GoBackInHistory, not real, historyCursor is 0", 255, 255, 255);
            }
            skipHistory = false;
        }

        internal void GoForwardInHistory()
        {
            skipHistory = true;
            if (real)
            {
                if (history.Count > 0)
                {
                    if (historyCursor == 0) historyCursor++;
                    ReplaceWithFake(history[historyCursor - 1]);
                }
                else
                {
                    Main.NewText(RecipeCatalogueUI.RBText("HistoryEmpty"), 255, 255, 255);
                }
            }
            else if (historyCursor < history.Count)
            {
                int type = history[historyCursor];
                historyCursor++;
                ReplaceWithFake(type);
            }
            else
            {
                Main.NewText(RecipeCatalogueUI.RBText("HistoryReachedEnd"), 255, 255, 255);
            }
            skipHistory = false;
        }
    }

    public class UICraftQueryItemSlot : UIQueryItemSlot
    {
        public UICraftQueryItemSlot(Item item) : base(item)
        {
        }
    }

    public class UIBestiaryQueryItemSlot : UIQueryItemSlot
    {
        public UIBestiaryQueryItemSlot(Item item) : base(item)
        {
        }
    }
}
