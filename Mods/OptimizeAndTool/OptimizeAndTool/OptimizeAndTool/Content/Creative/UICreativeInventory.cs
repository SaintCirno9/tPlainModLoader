using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using tContentPatch.Content.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using UITextBox = tContentPatch.Content.UI.UITextBox;

namespace OptimizeAndTool.Content.Creative
{
    /// <summary>
    /// 创造模式物品浏览器 UI 窗口
    /// 作者: SaintCirno9
    /// </summary>
    public class UICreativeInventory : UIWindow
    {
        protected UIStackPanel panel_rows = null;
        protected UIPanel panel_items = null;//物品列表
        protected UIScrollViewer panel_items_sv = null;
        protected UIWrapPanel panel_items_wp = null;//物品列表项的容器
        //
        protected UIPanel panel_row1 = null;//搜索框行
        protected UITextBox panel_row1_tb = null;
        //
        protected UIPanel panel_row2 = null;//筛选
        protected UIStackPanel panel_row2_sp = null;
        protected UIStackPanel panel_row2_sp_row1 = null;//分类1级
        protected UIStackPanel panel_row2_sp_row2 = null;//分类2级
        protected UIRadioButton[][] panel_row2_sp_row2_rbs = null;//每个分类2级的单选框
        //
        protected ItemSort<int> itemsID = null;
        public string Search_Text = null;
        protected string Search_Text_new = null;
        protected int Search_Text_cd = 0;

        public UITextBox SearchTextBox => panel_row1_tb;
        public int MatchedCount => panel_items_wp?.Children?.Count() ?? 0;

        public UICreativeInventory(string title, int width, int height) : base(title, width, height)
        {
            panel_rows = new UIStackPanel();
            panel_row1 = new UIPanel();
            panel_row1.SetPadding(0);
            panel_row1.BackgroundColor = Color.Transparent;
            panel_row1.BorderColor = Color.Transparent;

            panel_row1_tb = new UITextBox("搜索物品名称或 ID");
            panel_row2 = new UIPanel();
            panel_row2_sp = new UIStackPanel();
            panel_row2_sp_row1 = new UIStackPanel();
            panel_row2_sp_row2 = new UIStackPanel();
            panel_items = new UIPanel();
            panel_items_sv = new UIScrollViewer();
            panel_items_wp = new UIWrapPanel();

            //
            panel_row1.Height.Set(30, 0);

            panel_row1_tb.Width.Set(200, 0);
            panel_row1_tb.Height.Set(30, 0);
            panel_row1_tb.Left.Set(-panel_row1_tb.Width.Pixels, 1);
            panel_row1_tb.Text_MaxLength = 50;
            panel_row1_tb.OnTextChanged += (e) =>
            {
                Search_Text_cd = 3;
                Search_Text_new = e;
            };

            panel_row2.Height.Set(40 + 5 + 5, 0);
            panel_row2.MarginTop = 2;
            panel_row2.SetPadding(5);
            panel_row2.OverflowHidden = true;

            panel_row2_sp.Height.Set(40, 0);

            panel_row2_sp_row1.Height.Set(20, 0);
            panel_row2_sp_row1.ItemMargin = 2;
            panel_row2_sp_row1.Horizontal = true;

            panel_row2_sp_row2.Height.Set(20, 0);
            panel_row2_sp_row2.ItemMargin = 2;
            panel_row2_sp_row2.Horizontal = true;

            panel_items.SetPadding(0);
            panel_items.BorderColor = panel_items.BackgroundColor;
            panel_items.OverflowHidden = true;

            //
            Child.Append(panel_rows);
            Child.Append(panel_items);
            panel_items.Append(panel_items_sv);
            panel_items_sv.SetChild(panel_items_wp);
            //
            panel_rows.Append(panel_row1);
            panel_rows.Append(panel_row2);
            panel_row1.Append(panel_row1_tb);
            panel_row2.Append(panel_row2_sp);
            panel_row2_sp.Append(panel_row2_sp_row1);
            panel_row2_sp.Append(panel_row2_sp_row2);

            #region 每个分类2级的单选框
            panel_row2_sp_row2_rbs = new UIRadioButton[12][];
            int rbs_size = (int)panel_row2_sp_row2.Height.Pixels - 2;
            Func<string, string, int, int, UIRadioButton> action_rb2 = (path, text, id1, id2) =>
            {
                UIRadioButton rb =
                new UIRadioButton(Main.Assets.Request<Texture2D>($"Images/{path}", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value,
                rbs_size, rbs_size)
                {
                    MouseHoveringText = text
                };
                rb.OnChecked += () => update_item(id1, id2);

                return rb;
            };
            panel_row2_sp_row2_rbs[1] = new UIRadioButton[]
            {
                action_rb2.Invoke("Item_3507", "近战武器", itemsSort.ID_Weapon, itemsSort.ID_Weapon_Melee),
                action_rb2.Invoke("Item_39", "远程武器", itemsSort.ID_Weapon, itemsSort.ID_Weapon_Ranged),
                action_rb2.Invoke("Item_165", "魔法武器", itemsSort.ID_Weapon, itemsSort.ID_Weapon_Magic),
                action_rb2.Invoke("Item_1309", "召唤武器", itemsSort.ID_Weapon, itemsSort.ID_Weapon_Summon),
            };
            panel_row2_sp_row2_rbs[2] = new UIRadioButton[]
            {
                action_rb2.Invoke("Item_3509", "稿子", itemsSort.ID_Tool, itemsSort.ID_Tool_pick),
                action_rb2.Invoke("Item_3506", "斧头", itemsSort.ID_Tool, itemsSort.ID_Tool_axe),
                action_rb2.Invoke("Item_3505", "锤子", itemsSort.ID_Tool, itemsSort.ID_Tool_hammer),
            };
            panel_row2_sp_row2_rbs[3] = new UIRadioButton[]
            {
                action_rb2.Invoke("Item_2289", "钓竿", itemsSort.ID_ToolKit, itemsSort.ID_ToolKit_fishingPole),
                action_rb2.Invoke("Item_437", "钩爪", itemsSort.ID_ToolKit, itemsSort.ID_ToolKit_hook),
                action_rb2.Invoke("Item_2430", "坐骑", itemsSort.ID_ToolKit, itemsSort.ID_ToolKit_mount),
                action_rb2.Invoke("Item_5332", "宠物", itemsSort.ID_ToolKit, itemsSort.ID_ToolKit_pet),
            };
            panel_row2_sp_row2_rbs[4] = new UIRadioButton[]
            {
                action_rb2.Invoke("Item_894", "头盔", itemsSort.ID_Armor, itemsSort.ID_Armor_head),
                action_rb2.Invoke("Item_895", "胸甲", itemsSort.ID_Armor, itemsSort.ID_Armor_body),
                action_rb2.Invoke("Item_896", "护腿", itemsSort.ID_Armor, itemsSort.ID_Armor_leg),
                action_rb2.Invoke("Item_1746", "时装", itemsSort.ID_Armor, itemsSort.ID_Armor_vanity),
            };
            panel_row2_sp_row2_rbs[7] = new UIRadioButton[]
            {
                action_rb2.Invoke("Item_2", "方块", itemsSort.ID_Tile, itemsSort.ID_Tile_tile),
                action_rb2.Invoke("Item_130", "墙", itemsSort.ID_Tile, itemsSort.ID_Tile_wall),
            };
            #endregion

            #region 分类1级的单选框
            UIRadioButton firstRb = null;
            Action<string, string, int, int, int> action_rb = (string path, string text, int id1, int id2, int index) =>
            {
                UIRadioButton rb = new UIRadioButton(
                    Main.Assets.Request<Texture2D>($"Images/{path}", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value,
                    (int)panel_row2_sp_row1.Height.Pixels - 2, (int)panel_row2_sp_row1.Height.Pixels - 2);
                rb.VAlign = 0.5f;
                rb.MouseHoveringText = text;
                rb.OnChecked += () =>
                {
                    update_item(id1, id2);
                    switchItemSort1(index);
                };

                if (index == 0) firstRb = rb;
                panel_row2_sp_row1.Append(rb);
            };

            action_rb.Invoke("Item_2712", "全部", -1, -1, 0);
            action_rb.Invoke("Item_4", "武器", itemsSort.ID_Weapon, -1, 1);
            action_rb.Invoke("Item_1", "工具", itemsSort.ID_Tool, -1, 2);
            action_rb.Invoke("Item_324", "装备", itemsSort.ID_ToolKit, -1, 3);
            action_rb.Invoke("Item_82", "盔甲", itemsSort.ID_Armor, -1, 4);
            action_rb.Invoke("Item_1302", "弹药", itemsSort.ID_Ammo, -1, 5);
            action_rb.Invoke("Item_54", "饰品", itemsSort.ID_Accessorie, -1, 6);
            action_rb.Invoke("Item_2", "方块", itemsSort.ID_Tile, -1, 7);
            action_rb.Invoke("Item_296", "药水", itemsSort.ID_Buff, -1, 8);
            action_rb.Invoke("Item_557", "boss召唤物", itemsSort.ID_BossSpawn, -1, 9);
            action_rb.Invoke("Item_5", "消耗品", itemsSort.ID_Consumable, -1, 10);
            action_rb.Invoke("Item_9", "其他", itemsSort.ID_Other, -1, 11);

            if (firstRb != null) firstRb.IsChecked = true;
            #endregion

            // 初始化默认加载全量物品
            update_item(-1, -1);
        }

        public override void Update(GameTime time)
        {
            base.Update(time);

            panel_rows.Width_Stretch();
            panel_row1.Width_Stretch();
            panel_row2.Width_Stretch();
            panel_row2_sp.Width_Stretch();
            panel_row2_sp_row1.Width_Stretch();
            panel_row2_sp_row2.Width_Stretch();
            panel_items.Width_Stretch();
            panel_items_sv.Width.Precent = 1;
            panel_items_sv.Height.Precent = 1;
            panel_items_sv.VAlign = 0.5f;
            panel_items_wp.Width.Precent = 1;
            //
            panel_rows.UpdateSize_Height();
            panel_items_wp.UpdateSize_Height();
            panel_items.Top.Pixels = panel_rows.GetDimensions().Height + 10;//在rows下面
            panel_items.Height.Pixels = Child.GetInnerDimensions().Height - panel_rows.Height.Pixels - 10;//填满剩余空间

            //
            if (Search_Text_cd > 0)
            {
                --Search_Text_cd;

                if (Search_Text_cd == 0)
                {
                    Search_Text = Search_Text_new;
                    update_itemUI();
                }
            }

            // 鼠标悬停在物品浏览器窗口范围内（含 16px 外沿与滑条容差）时，拦截快捷栏与制造列表滚轮，并平滑驱动滚动条
            if (IsOpen && ModifyInterfaceLayers.IsHoveringWindow(this))
            {
                Main.LocalPlayer.mouseInterface = true;

                int delta = Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI;
                if (delta == 0) delta = Terraria.GameInput.PlayerInput.ScrollWheelDelta;

                if (delta != 0 && panel_items_sv?.Scrollbar != null)
                {
                    panel_items_sv.Scrollbar.ViewPosition -= delta;
                    Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = 0;
                    Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;
                }
            }
        }

        public override void ScrollWheel(UIScrollWheelEvent evt)
        {
            base.ScrollWheel(evt);
            if (panel_items_sv?.Scrollbar != null && evt.ScrollWheelValue != 0)
            {
                panel_items_sv.Scrollbar.ViewPosition -= evt.ScrollWheelValue;
                Terraria.GameInput.PlayerInput.ScrollWheelDeltaForUI = 0;
                Terraria.GameInput.PlayerInput.ScrollWheelDelta = 0;
            }
        }

        public void ApplySearchImmediate(string query)
        {
            Search_Text_new = query;
            Search_Text = query;
            Search_Text_cd = 0;
            if (panel_row1_tb != null)
            {
                panel_row1_tb.Text = query ?? string.Empty;
            }
            update_itemUI();
        }

        public void update_itemUI()
        {
            panel_items_wp.RemoveAllChildren();

            if (itemsID == null)
            {
                if (itemsSort.Loaded)
                {
                    itemsID = itemsSort.ID;
                }
                else
                {
                    return;
                }
            }

            itemsID.for_ItemsAll((i) =>
            {
                Item item = new Item();
                if (i < ItemID.Count)
                {
                    if (string.IsNullOrWhiteSpace(Lang.GetItemNameValue(i))) return;
                    item.SetDefaults(i);
                }
                else
                {
                    var modItem = TPML.Content.ItemLoader.GetItem(i);
                    if (modItem == null) return;
                    item.type = i;
                    TPML.Content.ItemLoader.SetDefaults(item);
                }
                if (item.type < 1) return;
                if (item.stack <= 0) item.stack = 1;

                // 根据搜索文本筛选
                if (!string.IsNullOrWhiteSpace(Search_Text))
                {
                    if (!searchItem(item, Search_Text)) return;
                }

                UIItemGrid it = new UIItemGrid(item, Terraria.UI.ItemSlot.Context.CreativeInfinite);
                panel_items_wp.Append(it);
            });
        }

        public void update_item(int id1, int id2)
        {
            if (!itemsSort.Loaded) return;

            ItemSort<int> old = itemsID;

            if (id1 == -1)//第1级全部
            {
                itemsID = itemsSort.ID;
            }
            else if (id2 == -1)//第2级全部
            {
                itemsID = itemsSort.ItemsSort_Gets(id1);
            }
            else
            {
                itemsID = itemsSort.ItemsSort_Gets(id1, id2);
            }

            if (itemsID != old || panel_items_wp.Children.Count() == 0)
            {
                update_itemUI();
            }
        }

        public void switchItemSort1(int index)
        {
            foreach (Terraria.UI.UIElement ui in panel_row2_sp_row2.Children)
            {
                UIRadioButton rb = ui as UIRadioButton;
                if (rb == null) continue;

                rb.IsChecked = false;
            }

            panel_row2_sp_row2.RemoveAllChildren();

            if (index >= panel_row2_sp_row2_rbs.Length) return;

            UIRadioButton[] rbs = panel_row2_sp_row2_rbs[index];

            for (int i = 0; i < rbs?.Length; ++i) panel_row2_sp_row2.Append(rbs[i]);
        }

        public bool searchItem(Item item, string s)
        {
            if (item == null || string.IsNullOrWhiteSpace(s)) return true;
            s = s.Trim();

            // 支持纯数字按 ItemID 检索
            if (int.TryParse(s, out int queryId) && item.type == queryId)
            {
                return true;
            }

            // 物品名称模糊匹配 (Name / NameOverride)
            if (!string.IsNullOrEmpty(item.Name) && item.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // 本地化名称匹配
            string localizedName = Lang.GetItemNameValue(item.type);
            if (!string.IsNullOrEmpty(localizedName) && localizedName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            // 模组物品元数据（内部类名、FullName、自定义 Tooltip）匹配
            if (item.type >= ItemID.Count)
            {
                var modItem = TPML.Content.ItemLoader.GetItem(item.type);
                if (modItem != null)
                {
                    if (!string.IsNullOrEmpty(modItem.Name) && modItem.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    if (!string.IsNullOrEmpty(modItem.FullName) && modItem.FullName.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }

                string modTooltip = TPML.Content.ItemLoader.GetTooltip(item.type);
                if (!string.IsNullOrEmpty(modTooltip) && modTooltip.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
