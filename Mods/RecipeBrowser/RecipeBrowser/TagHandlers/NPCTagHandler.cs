using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    /// <summary>
    /// [npc] 标签处理器 —— 恢复原版 head 选项、身体贴图模式与范围校验
    /// 作者: SaintCirno9
    /// </summary>
    public class NPCTagHandler : ITagHandler
    {
        private class NPCSnippet : TextSnippet
        {
            private int npcType;
            private int netID;
            private bool head;

            public NPCSnippet(int netID, bool head)
                : base("")
            {
                this.netID = netID;
                this.head = head;
                CheckForHover = true;
                if (ContentSamples.NpcsByNetId != null && ContentSamples.NpcsByNetId.TryGetValue(netID, out var npc) && npc != null)
                {
                    npcType = npc.type;
                }
                else
                {
                    npcType = netID;
                }
            }

            public override void OnHover()
            {
                UICommon.TooltipMouseText(Lang.GetNPCNameValue(netID));
                // 箭头追踪：head 模式悬停时定位该 NPC（由 RecipeBrowserUI.HandleArrow 绘制）
                if (head && RecipeBrowserUI.instance != null)
                {
                    RecipeBrowserUI.instance.npcArrow = NPC.FindFirstNPC(npcType);
                }
            }

            public override void OnClick()
            {
                base.OnClick();
                // 点击时对 NPC 位置发 Ping（对齐原版）
                if (RecipeBrowserUI.instance != null && RecipeBrowserUI.instance.npcArrow != -1 && RecipeBrowserUI.instance.npcArrow < Main.npc.Length && Main.npc[RecipeBrowserUI.instance.npcArrow] != null)
                {
                    Main.Pings.Add(Main.npc[RecipeBrowserUI.instance.npcArrow].Center / 16f);
                }
            }

            public override bool UniqueDraw(bool justCheckingSize, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default(Vector2), Color color = default(Color), float scale = 1f)
            {
                float num = 24f * scale;
                Texture2D tex = null;
                Rectangle rect = default(Rectangle);

                if (npcType > 0 && npcType < NPCID.Count)
                {
                    Utilities.LoadNPC(npcType);
                    if (npcType < TextureAssets.Npc.Length && TextureAssets.Npc[npcType]?.Value != null)
                    {
                        tex = TextureAssets.Npc[npcType].Value;
                        int frameCount = (npcType < Main.npcFrameCount.Length && Main.npcFrameCount[npcType] > 0) ? Main.npcFrameCount[npcType] : 1;
                        rect = new Rectangle(0, 0, tex.Width, Math.Max(1, tex.Height / frameCount));
                        if (head)
                        {
                            int headIndex = NPC.TypeToDefaultHeadIndex(npcType);
                            if (headIndex >= 0 && headIndex < TextureAssets.NpcHead.Length && TextureAssets.NpcHead[headIndex]?.Value != null)
                            {
                                tex = TextureAssets.NpcHead[headIndex].Value;
                                rect = tex.Bounds;
                            }
                        }
                        if (rect.Width * scale > num || rect.Height * scale > num)
                        {
                            scale = (rect.Width <= rect.Height) ? (num / rect.Height) : (num / rect.Width);
                        }
                    }
                }

                if (!justCheckingSize && color != Color.Black && tex != null)
                {
                    Color white = Color.White;
                    if (ContentSamples.NpcsByNetId != null && ContentSamples.NpcsByNetId.TryGetValue(netID, out var val3) && val3 != null)
                    {
                        Color drawColor = (val3.alpha == 255) ? Color.White : val3.GetAlpha(white);
                        Main.spriteBatch.Draw(tex, position + new Vector2(num / 2f), rect, drawColor, 0f, Utils.Center(rect), scale, SpriteEffects.None, 0f);
                        if (val3.color != default(Color))
                        {
                            Main.spriteBatch.Draw(tex, position + new Vector2(num / 2f), rect, val3.GetColor(white), 0f, Utils.Center(rect), scale, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        Main.spriteBatch.Draw(tex, position + new Vector2(num / 2f), rect, Color.White, 0f, Utils.Center(rect), scale, SpriteEffects.None, 0f);
                    }
                }

                size = (rect.Width > 0 ? new Vector2(rect.Width * scale, rect.Height * scale) : new Vector2(32f * scale)) + new Vector2(2f, 0f);
                return true;
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            if (!int.TryParse(text, out var result) || result >= NPCID.Count || result <= -66)
            {
                return new TextSnippet(text);
            }
            bool head = options != null && options == "head";
            return new NPCSnippet(result, head)
            {
                Text = GenerateTag(result),
                CheckForHover = true,
                DeleteWhole = true
            };
        }

        public static string GenerateTag(int netID)
        {
            return $"[npc:{netID}]";
        }
    }
}
