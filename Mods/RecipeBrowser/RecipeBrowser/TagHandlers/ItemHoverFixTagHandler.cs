using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    /// <summary>
    /// [itemhover] 标签处理器 —— 双语法兼容：
    /// 1) 原版 tML options 语法： [itemhover/p5,s2,t:netID]（d/o/c/t/s/x/p 选项）
    /// 2) 移植版冒号语法：      [itemhover:type:stack:check:nameOverride]（向后兼容旧文本）
    /// 作者: SaintCirno9
    /// </summary>
    public class ItemHoverFixTagHandler : ITagHandler
    {
        private class ItemHoverFixSnippet : TextSnippet
        {
            private Item item;
            private bool check;
            private bool itemTooltip;

            public ItemHoverFixSnippet(Item item, bool check, bool itemTooltip)
                : base("")
            {
                this.item = item;
                this.check = check;
                this.itemTooltip = itemTooltip;
                base.Color = ItemRarity.GetColor(item.rare);
                CheckForHover = true;
            }

            public override void OnHover()
            {
                if (itemTooltip)
                {
                    Main.HoverItem = item.Clone();
                    Main.instance.MouseText(item.Name, item.rare, 0, -1, -1, -1, -1, 0);
                }
                else
                {
                    string text = ((item.stack > 1) ? $" ({item.stack}) " : "");
                    UICommon.TooltipMouseText(item.Name + text);
                }
            }

            public override bool UniqueDraw(bool justCheckingSize, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1f)
            {
                size = new Vector2(32f) * scale * 0.75f;

                if (!justCheckingSize && spriteBatch != null && item != null)
                {
                    Utilities.LoadItem(item.type);
                    Texture2D tex = (item.type < TextureAssets.Item.Length) ? TextureAssets.Item[item.type]?.Value : null;
                    if (tex != null)
                    {
                        Rectangle frame = (Main.itemAnimations != null && item.type < Main.itemAnimations.Length && Main.itemAnimations[item.type] != null)
                            ? Main.itemAnimations[item.type].GetFrame(tex, -1)
                            : Utils.Frame(tex, 1, 1, 0, 0, 0, 0);

                        float num = scale * 0.75f;
                        float inventoryScale = Main.inventoryScale;
                        Main.inventoryScale = num;
                        ItemSlot.Draw(spriteBatch, ref item, 14, position - new Vector2(10f) * scale * num, Color.White);
                        if (check)
                        {
                            ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, "✓", position + new Vector2(14f, 10f), Utilities.yesColor, 0f, Vector2.Zero, new Vector2(0.7f), -1f, 2f);
                        }
                        Main.inventoryScale = inventoryScale;
                    }
                }
                return true;
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            Item item = new Item();
            bool check = false;
            bool itemTooltip = false;
            int stack = 1;

            if (!string.IsNullOrEmpty(options))
            {
                // ---- 原版 tML options 语法 ----
                string[] opts = options.Split(',');
                for (int i = 0; i < opts.Length; i++)
                {
                    if (string.IsNullOrEmpty(opts[i])) continue;
                    switch (opts[i][0])
                    {
                        case 'd':
                            // tML ItemIO.FromBase64 完整物品（TPML 无 ItemIO），降级按数字 ID 解析
                            if (int.TryParse(opts[i].Substring(1), out var dType))
                            {
                                item.SetDefaults(dType);
                            }
                            break;
                        case 'o':
                            if (opts[i].Length > 1)
                            {
                                item.SetNameOverride(opts[i].Substring(1));
                            }
                            break;
                        case 'c':
                            check = true;
                            break;
                        case 't':
                            itemTooltip = true;
                            break;
                        case 's':
                        case 'x':
                            if (int.TryParse(opts[i].Substring(1), out var s))
                            {
                                stack = s;
                            }
                            break;
                        case 'p':
                            if (int.TryParse(opts[i].Substring(1), out var p))
                            {
                                try { item.Prefix(p); } catch { }
                            }
                            break;
                    }
                }
                if (int.TryParse(text, out var netType) && item.type <= 0)
                {
                    item.SetDefaults(netType);
                }
            }
            else if (text.IndexOf(':') >= 0)
            {
                // ---- 移植版冒号语法：type:stack:check:nameOverride ----
                string[] parts = text.Split(':');
                int itemType = 0;
                if (parts.Length > 0) int.TryParse(parts[0], out itemType);
                if (parts.Length > 1) int.TryParse(parts[1], out stack);
                if (parts.Length > 2) bool.TryParse(parts[2], out check);
                if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
                {
                    item.SetNameOverride(parts[3]);
                }
                item.SetDefaults(itemType);
            }
            else
            {
                if (int.TryParse(text, out var type))
                {
                    item.SetDefaults(type);
                }
            }

            if (item.type <= 0)
            {
                return new TextSnippet(text);
            }

            item.stack = stack;
            string text2 = "";
            if (item.stack > 1)
            {
                text2 = " (" + item.stack + ")";
            }
            return new ItemHoverFixSnippet(item, check, itemTooltip)
            {
                Text = "[" + item.AffixName() + text2 + "]",
                CheckForHover = true,
                DeleteWhole = true
            };
        }

        /// <summary>
        /// 原版签名重载（对齐原版 GenerateTag(Item, bool)）：生成 [itemhover/p..,s..,t:netID] 原版语法
        /// </summary>
        public static string GenerateTag(Item I, bool itemTooltip = false)
        {
            string text = "[itemhover";
            if (I.prefix != 0)
            {
                text += "/p" + I.prefix;
            }
            if (I.stack != 1)
            {
                text += "/s" + I.stack;
            }
            if (itemTooltip)
            {
                text += ((I.prefix != 0 || I.stack != 1) ? "," : "/") + "t";
            }
            return text + ":" + I.type + "]";
        }

        /// <summary>
        /// 移植版既有签名：输出原版 options 语法（对齐原版 GenerateTag(int,int,string,bool)），解析器双语法兼容
        /// </summary>
        public static string GenerateTag(int itemType, int stack = 1, string nameOverride = null, bool check = false)
        {
            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(nameOverride))
            {
                list.Add("o" + nameOverride);
            }
            if (check)
            {
                list.Add("c");
            }
            if (stack > 1)
            {
                list.Add("s" + stack);
            }
            if (list.Count > 0)
            {
                string value = "/" + string.Join(",", list);
                return $"[itemhover{value}:{itemType}]";
            }
            return $"[itemhover:{itemType}]";
        }
    }
}
