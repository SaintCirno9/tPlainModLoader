using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using RecipeBrowser.UIElements;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using TPML.Core.Pinyin;

namespace RecipeBrowser
{
    public class BestiaryUI
    {
        public static BestiaryUI instance;
        internal static Color color = new Color(28, 187, 180) * 0.5f;

        internal UIPanel npcGridPanel;
        internal UIGrid npcGrid;
        internal UIHorizontalGrid lootGrid;
        internal bool updateNeeded;
        internal UIPanel mainPanel;
        internal UIBestiaryQueryItemSlot queryItem;
        internal NewUITextBox npcNameFilter;
        internal bool EncounteredRadioButtonIsUnencountered;
        internal UICheckbox EncounteredRadioButton;
        internal UICheckbox HasLootRadioButton;
        internal UIRadioButtonGroup RadioButtonGroup;
        internal UIRadioButton BestiarySortRadioButton;
        internal UIRadioButton IDSortRadioButton;
        internal List<UINPCSlot> npcSlots;
        internal UINPCSlot queryLootNPC;

        internal static string RBText(string key, string category = "BestiaryUI")
        {
            return RBLanguage.GetText(category, key);
        }

        public BestiaryUI()
        {
            instance = this;
            npcSlots = new List<UINPCSlot>();
        }

        internal UIElement CreateBestiaryPanel()
        {
            mainPanel = new UIPanel();
            mainPanel.SetPadding(6f);
            mainPanel.BackgroundColor = color;
            mainPanel.Top.Set(20f, 0f);
            mainPanel.Height.Set(-20f, 1f);
            mainPanel.Width.Set(0f, 1f);

            npcGridPanel = new UIPanel();
            npcGridPanel.SetPadding(6f);
            npcGridPanel.Top.Pixels = 46f;
            npcGridPanel.Width.Set(0f, 1f);
            npcGridPanel.Left.Set(0f, 0f);
            npcGridPanel.Height.Set(-98f, 1f);
            npcGridPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;
            mainPanel.Append(npcGridPanel);

            npcGrid = new UIGrid();
            npcGrid.Width.Set(-20f, 1f);
            npcGrid.Height.Set(0f, 1f);
            npcGrid.ListPadding = 2f;
            npcGrid.alternateSort = CustomSort;
            npcGridPanel.Append(npcGrid);

            FixedUIScrollbar scrollbar = new FixedUIScrollbar(RecipeBrowserUI.instance.userInterface);
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(0f, 1f);
            scrollbar.Left.Set(-20f, 1f);
            npcGridPanel.Append(scrollbar);
            npcGrid.SetScrollbar(scrollbar);

            UIPanel lootPanel = new UIPanel();
            lootPanel.SetPadding(6f);
            lootPanel.Top.Set(-50f, 1f);
            lootPanel.Width.Set(0f, 0.5f);
            lootPanel.Height.Set(50f, 0f);
            lootPanel.BackgroundColor = Color.CornflowerBlue * 0.3f;
            mainPanel.Append(lootPanel);

            lootGrid = new UIHorizontalGrid();
            lootGrid.Width.Set(0f, 1f);
            lootGrid.Height.Set(0f, 1f);
            lootGrid.ListPadding = 2f;
            lootGrid.drawArrows = true;
            lootPanel.Append(lootGrid);

            InvisibleFixedUIHorizontalScrollbar lootScroll = new InvisibleFixedUIHorizontalScrollbar(RecipeBrowserUI.instance.userInterface);
            lootScroll.SetView(100f, 1000f);
            lootScroll.Width.Set(0f, 1f);
            lootScroll.Top.Set(-20f, 1f);
            lootGrid.SetScrollbar(lootScroll);

            queryItem = new UIBestiaryQueryItemSlot(new Item());
            queryItem.emptyHintText = RBText("EmptyQuerySlotHint");
            mainPanel.Append(queryItem);

            RadioButtonGroup = new UIRadioButtonGroup();
            RadioButtonGroup.Left.Pixels = 45f;
            RadioButtonGroup.Width.Set(180f, 0f);

            string bestiaryText = Language.GetTextValue("BestiaryInfo.Sort_BestiaryID");
            if (string.IsNullOrEmpty(bestiaryText) || bestiaryText == "BestiaryInfo.Sort_BestiaryID") bestiaryText = "图鉴顺序";
            string idText = Language.GetTextValue("BestiaryInfo.Sort_ID");
            if (string.IsNullOrEmpty(idText) || idText == "BestiaryInfo.Sort_ID") idText = "内部ID";

            BestiarySortRadioButton = new UIRadioButton(bestiaryText, 1f);
            BestiarySortRadioButton.SetHoverText("按怪物图鉴中的默认顺序排序");

            IDSortRadioButton = new UIRadioButton(idText, 1f);
            IDSortRadioButton.SetHoverText("按 NPC 内部网络 ID 排序");

            RadioButtonGroup.Add(BestiarySortRadioButton);
            RadioButtonGroup.Add(IDSortRadioButton);
            mainPanel.Append(RadioButtonGroup);
            BestiarySortRadioButton.Selected = true;

            BestiarySortRadioButton.OnSelectedChanged += (s, e) => { updateNeeded = true; };
            IDSortRadioButton.OnSelectedChanged += (s, e) => { updateNeeded = true; };

            npcNameFilter = new NewUITextBox(RBLanguage.GetText("Common", "FilterByName"));
            npcNameFilter.OnTextChanged += () => { updateNeeded = true; };
            npcNameFilter.Top.Set(0f, 0f);
            npcNameFilter.Left.Set(-150f, 1f);
            npcNameFilter.Width.Set(150f, 0f);
            npcNameFilter.Height.Set(25f, 0f);
            mainPanel.Append(npcNameFilter);

            EncounteredRadioButton = new UICheckbox(RBText("Encountered"), RBText("ShowOnlyNPCKilledAlready"));
            EncounteredRadioButton.Top.Set(-40f, 1f);
            EncounteredRadioButton.Left.Set(6f, 0.5f);
            EncounteredRadioButton.OnSelectedChanged += (s, e) => { updateNeeded = true; };
            EncounteredRadioButton.OnRightClick += (evt, el) =>
            {
                EncounteredRadioButtonIsUnencountered = !EncounteredRadioButtonIsUnencountered;
                if (EncounteredRadioButtonIsUnencountered)
                {
                    EncounteredRadioButton.SetText("   " + RBText("Unencountered"));
                    EncounteredRadioButton.SetHoverText(RBText("ShowOnlyNPCNotKilled"));
                }
                else
                {
                    EncounteredRadioButton.SetText("   " + RBText("Encountered"));
                    EncounteredRadioButton.SetHoverText(RBText("ShowOnlyNPCKilledAlready"));
                }
                updateNeeded = true;
            };
            mainPanel.Append(EncounteredRadioButton);

            HasLootRadioButton = new UICheckbox(RBText("HasLoot"), RBText("ShowOnlyNPCWithLoot"));
            HasLootRadioButton.Top.Set(-20f, 1f);
            HasLootRadioButton.Left.Set(6f, 0.5f);
            HasLootRadioButton.OnSelectedChanged += (s, e) => { updateNeeded = true; };
            mainPanel.Append(HasLootRadioButton);

            updateNeeded = true;
            return mainPanel;
        }

        private int CustomSort(UIElement x, UIElement y)
        {
            if (x is UINPCSlot s1 && y is UINPCSlot s2)
            {
                if (BestiarySortRadioButton.Selected)
                {
                    bool f1 = ContentSamples.NpcBestiarySortingId.TryGetValue(s1.npcType, out var v1);
                    bool f2 = ContentSamples.NpcBestiarySortingId.TryGetValue(s2.npcType, out var v2);
                    if (f1 && f2) return v1.CompareTo(v2);
                    if (f1) return -1;
                    if (f2) return 1;
                }
                int net1 = s1.npc.netID;
                int val1 = (net1 < 0) ? -net1 : (net1 >= 688 ? net1 : (net1 - 1000));
                int net2 = s2.npc.netID;
                int val2 = (net2 < 0) ? -net2 : (net2 >= 688 ? net2 : (net2 - 1000));
                return val1.CompareTo(val2);
            }
            return x.CompareTo(y);
        }

        internal void Update()
        {
            if (NPCID.Count - 2 + 66 != npcSlots.Count)
            {
                if (LootCache.instance == null || LootCache.instance.lootInfos == null || LootCache.instance.lootInfos.Count == 0)
                {
                    LootCacheManager.Setup();
                }

                npcSlots.Clear();
                for (int i = -65; i < NPCID.Count; i++)
                {
                    if (i != 0)
                    {
                        NPC npc = new NPC();
                        npc.SetDefaults(i);
                        npcSlots.Add(new UINPCSlot(npc));
                    }
                }
            }

            if (!updateNeeded) return;
            updateNeeded = false;

            npcGrid.Clear();
            foreach (var slot in npcSlots)
            {
                if (PassNPCFilters(slot))
                {
                    npcGrid._items.Add(slot);
                    npcGrid._innerList.Append(slot);
                }
            }
            npcGrid.UpdateOrder();
            npcGrid._innerList.Recalculate();

            lootGrid.Clear();
            if (queryLootNPC != null)
            {
                SortedSet<int> drops = queryLootNPC.GetDrops();
                foreach (int item in drops)
                {
                    Item dropItem = new Item();
                    dropItem.SetDefaults(item);
                    UIBestiaryItemSlot bSlot = new UIBestiaryItemSlot(dropItem);
                    lootGrid._items.Add(bSlot);
                    lootGrid._innerList.Append(bSlot);
                }
            }
            lootGrid.UpdateOrder();
            lootGrid._innerList.Recalculate();
        }

        internal void SetNPC(UINPCSlot slot)
        {
            foreach (var s in npcSlots) s.selected = false;
            slot.selected = true;
        }

        internal void CloseButtonClicked()
        {
            if (queryItem.real && queryItem.item.stack > 0)
            {
                queryItem.ReplaceWithFake(0);
            }
            updateNeeded = true;
        }

        private bool PassNPCFilters(UINPCSlot slot)
        {
            // Mod 过滤（对齐原版）：TPML 生态无模组 NPC，选中非 Terraria 模组时图鉴为空
            if (RecipeBrowserUI.ModIndex != 0 && RecipeBrowserUI.instance?.mods != null && RecipeBrowserUI.ModIndex < RecipeBrowserUI.instance.mods.Length)
            {
                string selectedMod = RecipeBrowserUI.instance.mods[RecipeBrowserUI.ModIndex];
                if (selectedMod != "Terraria") return false;
            }
            if (EncounteredRadioButton.Selected && (EncounteredRadioButtonIsUnencountered == RecipePath.NPCUnlocked(slot.npc.netID)))
            {
                return false;
            }
            if (HasLootRadioButton.Selected && slot.GetDrops().Count == 0)
            {
                return false;
            }
            if (!queryItem.item.IsAir && !slot.GetDrops().Contains(queryItem.item.type))
            {
                return false;
            }
            string filterText = npcNameFilter.currentString.Trim();
            if (filterText.Length > 0)
            {
                string npcName = Lang.GetNPCNameValue(slot.npc.netID);
                string internalNpcName = (slot.npc.netID > 0 && slot.npc.netID < NPCID.Count) ? NPCID.Search.GetName(slot.npc.netID) : "";
                if (!PinyinHelper.Matches(npcName, filterText) && !PinyinHelper.Matches(internalNpcName, filterText))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
