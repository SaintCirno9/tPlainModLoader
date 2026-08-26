using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RecipeBrowser.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace RecipeBrowser.UIElements
{
    public class UIItemCatalogueItemSlot : UIItemSlot
    {
        internal bool selected;

        public UIItemCatalogueItemSlot(Item item, float scale = 0.75f) : base(item, scale)
        {
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            ItemCatalogueUI.instance.SetItem(this);
            CraftUI.instance.SetItem(item.type);
            ItemCatalogueUI.instance.PopulateItemDropViewerPanel(item.type);
        }

        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            RecipeCatalogueUI.instance.itemDescriptionFilter?.SetText("");
            RecipeCatalogueUI.instance.itemNameFilter?.SetText("");
            RecipeCatalogueUI.instance.queryItem?.ReplaceWithFake(item.type);
            RecipeBrowserUI.instance.tabController.SetPanel(0);
        }

        public override void RightDoubleClick(UIMouseEvent evt)
        {
            BestiaryUI.instance.npcNameFilter?.SetText("");
            BestiaryUI.instance.queryItem?.ReplaceWithFake(item.type);
            RecipeBrowserUI.instance.tabController.SetPanel(3);
        }

        internal override void DrawAdditionalOverlays(SpriteBatch spriteBatch, Vector2 vector2, float scale)
        {
            base.DrawAdditionalOverlays(spriteBatch, vector2, scale);
            if (selected && UIRecipeSlot.selectedBackgroundTexture != null)
            {
                spriteBatch.Draw(UIRecipeSlot.selectedBackgroundTexture, vector2, null, Color.White * Main.essScale, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        internal override void DrawAdditionalBadges(SpriteBatch spriteBatch, Vector2 vector2, float scale)
        {
            base.DrawAdditionalBadges(spriteBatch, vector2, scale);
            if (ItemCatalogueUI.instance != null)
            {
                if (item.type < ItemCatalogueUI.instance.isLoot.Length && ItemCatalogueUI.instance.isLoot[item.type] && TextureAssets.Wire2?.Value != null)
                {
                    spriteBatch.Draw(TextureAssets.Wire2.Value, vector2 + new Vector2(40f, 10f) * scale, new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);
                }
                if (item.type < ItemCatalogueUI.instance.craftResults.Length && ItemCatalogueUI.instance.craftResults[item.type] && TextureAssets.Wire3?.Value != null)
                {
                    spriteBatch.Draw(TextureAssets.Wire3.Value, vector2 + new Vector2(10f, 10f) * scale, new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);
                }
            }
            if (RecipeBrowserUI.instance != null && RecipeBrowserUI.instance.foundItems != null && item.type < RecipeBrowserUI.instance.foundItems.Length && !RecipeBrowserUI.instance.foundItems[item.type] && TextureAssets.Wire4?.Value != null)
            {
                spriteBatch.Draw(TextureAssets.Wire4.Value, vector2 + new Vector2(10f, 40f) * scale, new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);
            }
            if (Main.GameMode == 3 && RecipePath.ItemFullyResearched(item.type) && TextureAssets.Wire?.Value != null)
            {
                spriteBatch.Draw(TextureAssets.Wire.Value, vector2 + new Vector2(40f, 40f) * scale, new Rectangle(4, 58, 8, 8), Color.White, 0f, new Vector2(4f), 1f, SpriteEffects.None, 0f);
            }
        }
    }

    public class UIBestiaryItemSlot : UIItemSlot
    {
        public UIBestiaryItemSlot(Item item, float scale = 0.75f) : base(item, scale)
        {
        }

        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            RecipeCatalogueUI.instance.itemDescriptionFilter?.SetText("");
            RecipeCatalogueUI.instance.itemNameFilter?.SetText("");
            RecipeCatalogueUI.instance.queryItem?.ReplaceWithFake(item.type);
            RecipeBrowserUI.instance.tabController.SetPanel(0);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            BestiaryUI.instance.queryItem?.ReplaceWithFake(item.type);
        }
    }

    public class UIItemNoSlot : UIElement
    {
        internal float scale = 0.75f;
        public int itemType;
        public Item item;

        public UIItemNoSlot(Item item, float scale = 0.75f)
        {
            this.scale = scale;
            this.item = item;
            itemType = item?.type ?? 0;
            Width.Set(32f * scale * 0.65f, 0f);
            Height.Set(32f * scale * 0.65f, 0f);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            if (item == null || item.IsAir) return;
            CalculatedStyle innerDimensions = GetInnerDimensions();
            Vector2 pos = innerDimensions.Position();

            Texture2D tex = (item.type < TextureAssets.Item.Length) ? TextureAssets.Item[item.type]?.Value : null;
            if (tex == null) return;

            DrawAnimation anim = (item.type < Main.itemAnimations.Length) ? Main.itemAnimations[item.type] : null;
            Rectangle frame = anim != null ? anim.GetFrame(tex, -1) : tex.Bounds;

            float drawScale = 1f;
            if (frame.Height > 32) drawScale = 32f / frame.Height;
            drawScale *= scale;
            if (drawScale > 0.75f) drawScale = 0.75f;

            float origInvScale = Main.inventoryScale;
            Main.inventoryScale = scale * drawScale;
            ItemSlot.Draw(spriteBatch, ref item, 14, pos - new Vector2(10f) * scale * drawScale, Color.White);
            Main.inventoryScale = origInvScale;

            if (IsMouseHovering)
            {
                Main.hoverItemName = item.Name;
            }
        }
    }

    public class UINPCSlot : UIElement
    {
        public static Texture2D SelectedBackgroundTexture => TextureAssets.InventoryBack15?.Value ?? TextureAssets.InventoryBack.Value;
        public static Texture2D BackgroundTexture => TextureAssets.InventoryBack9?.Value ?? TextureAssets.InventoryBack.Value;

        private float scale = 0.75f;
        public int npcType;
        public NPC npc;
        public bool selected;
        private int clickIndicatorTime;
        internal int frameCounter;
        internal int frameTimer;

        public UINPCSlot(NPC npc)
        {
            this.npc = npc;
            npcType = npc?.type ?? 0;
            var bg = BackgroundTexture;
            Width.Set((bg != null ? bg.Width : 52) * scale, 0f);
            Height.Set((bg != null ? bg.Height : 52) * scale, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (npcType <= 0) return;
            Utilities.LoadNPC(npcType);
            Texture2D tex = (npcType < TextureAssets.Npc.Length) ? TextureAssets.Npc[npcType]?.Value : null;
            if (tex == null) return;

            int frameCount = (npcType < Main.npcFrameCount.Length) ? Main.npcFrameCount[npcType] : 1;
            if (frameCount < 1) frameCount = 1;

            if (++frameTimer > 7)
            {
                frameCounter++;
                frameTimer = 0;
                if (frameCounter > frameCount - 1) frameCounter = 0;
            }

            int frameHeight = tex.Height / frameCount;
            Rectangle frame = new Rectangle(0, frameHeight * frameCounter, tex.Width, frameHeight);

            CalculatedStyle innerDimensions = GetInnerDimensions();
            var bg = BackgroundTexture;
            if (bg != null)
            {
                spriteBatch.Draw(bg, innerDimensions.Position(), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            DrawAdditionalOverlays(spriteBatch, innerDimensions.Position(), scale);

            int width = tex.Width;
            float drawScale = 2f;
            float bgWidth = (bg != null ? bg.Width : 52) * scale - 6f;
            if (width * drawScale > bgWidth || frameHeight * drawScale > bgWidth)
            {
                drawScale = (width <= frameHeight) ? (bgWidth / frameHeight) : (bgWidth / width);
            }
            drawScale = Math.Min(drawScale, 0.8f);

            Vector2 drawPos = innerDimensions.Position();
            drawPos.X += (bg != null ? bg.Width : 52) * scale / 2f - width * drawScale / 2f;
            drawPos.Y += (bg != null ? bg.Height : 52) * scale / 2f - frameHeight * drawScale / 2f;

            Color color = (npc != null && npc.color != Color.Transparent) ? npc.color : Color.White;
            spriteBatch.Draw(tex, drawPos, frame, color, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

            if (IsMouseHovering && npc != null)
            {
                UICommon.DrawHoverStringInBounds(spriteBatch, Lang.GetNPCNameValue(npc.netID));
            }
        }

        public override int CompareTo(object obj)
        {
            if (obj is UINPCSlot other) return npcType.CompareTo(other.npcType);
            return 0;
        }

        public SortedSet<int> GetDrops()
        {
            SortedSet<int> drops = new SortedSet<int>();
            if (LootCache.instance != null && LootCache.instance.lootInfos != null && npc != null)
            {
                foreach (var lootInfo in LootCache.instance.lootInfos)
                {
                    if (lootInfo.Value.Contains(npc.type))
                    {
                        drops.Add(lootInfo.Key);
                    }
                }
            }
            return drops;
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            clickIndicatorTime = 30;
            var drops = GetDrops();
            if (RecipeBrowserUI.instance.CurrentPanel == 0)
            {
                string npcName = npc != null ? Lang.GetNPCNameValue(npc.netID) : "";
                string text = RBLanguage.GetText("BestiaryUI", "NPCDrops").Replace("{0}", npcName);
                foreach (int item in drops)
                {
                    text += $"[i:{item}]";
                }
                Main.NewText(text, 255, 255, 255);
            }
            else if (RecipeBrowserUI.instance.CurrentPanel == 3 && BestiaryUI.instance != null && BestiaryUI.instance.npcSlots.Contains(this))
            {
                BestiaryUI.instance.queryLootNPC = this;
                BestiaryUI.instance.updateNeeded = true;
                BestiaryUI.instance.SetNPC(this);
            }
        }

        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            if (RecipeBrowserUI.instance.CurrentPanel != 0) return;
            RecipeBrowserUI.instance.tabController.SetPanel(3);
            BestiaryUI.instance.npcNameFilter?.SetText("");
            BestiaryUI.instance.queryItem?.ReplaceWithFake(0);
            BestiaryUI.instance.updateNeeded = true;
            BestiaryUI.instance.Update();
            BestiaryUI.instance.npcGrid?.Recalculate();
            BestiaryUI.instance.npcGrid?.Goto(el =>
            {
                if (el is UINPCSlot slot && slot.npcType == npcType)
                {
                    BestiaryUI.instance.queryLootNPC = slot;
                    BestiaryUI.instance.updateNeeded = true;
                    BestiaryUI.instance.SetNPC(slot);
                    return true;
                }
                return false;
            }, center: true);
        }

        internal void DrawAdditionalOverlays(SpriteBatch spriteBatch, Vector2 vector2, float scale)
        {
            var selTex = SelectedBackgroundTexture;
            if (selTex == null) return;

            if (selected)
            {
                spriteBatch.Draw(selTex, vector2, null, Color.White * Main.essScale, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            if (clickIndicatorTime > 0)
            {
                clickIndicatorTime--;
                spriteBatch.Draw(selTex, vector2, null, Color.White * (clickIndicatorTime / 30f), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }
    }

    public class UITileSlot : UIElement
    {
        internal float scale = 0.75f;
        public int order;
        public int tile;
        public bool selected;
        private Texture2D texture;

        public Texture2D BackgroundTexture
        {
            get
            {
                if (selected && RecipeCatalogueUI.instance != null && RecipeCatalogueUI.instance.tileIsItemsThatPlaceThisTileInstead)
                {
                    return TextureAssets.InventoryBack11?.Value ?? TextureAssets.InventoryBack.Value;
                }
                return TextureAssets.InventoryBack9?.Value ?? TextureAssets.InventoryBack.Value;
            }
        }

        public UITileSlot(int tile, int order, float scale = 0.75f)
        {
            this.scale = scale;
            this.order = order;
            this.tile = tile;
            var bg = BackgroundTexture;
            Width.Set((bg != null ? bg.Width : 52) * scale, 0f);
            Height.Set((bg != null ? bg.Height : 52) * scale, 0f);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            if (selected && !RecipeCatalogueUI.instance.tileIsItemsThatPlaceThisTileInstead)
            {
                RecipeCatalogueUI.instance.Tile = -1;
                return;
            }
            if (RecipeCatalogueUI.instance.tileIsItemsThatPlaceThisTileInstead)
            {
                RecipeCatalogueUI.instance.updateNeeded = true;
            }
            RecipeCatalogueUI.instance.Tile = tile;
        }

        public override void RightClick(UIMouseEvent evt)
        {
            if (selected && RecipeCatalogueUI.instance.tileIsItemsThatPlaceThisTileInstead)
            {
                RecipeCatalogueUI.instance.Tile = -1;
                return;
            }
            RecipeCatalogueUI.instance.pendingQueryHowToCraftTileShouldGoto = false;
            RecipeCatalogueUI.instance.pendingQueryHowToCraftTile = tile;
        }

        public override int CompareTo(object obj)
        {
            if (obj is UITileSlot other) return -order.CompareTo(other.order);
            return 0;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (texture == null)
            {
                if (!Utilities.tileTextures.ContainsKey(tile))
                {
                    Utilities.GenerateTileTexture(tile);
                }
                Utilities.tileTextures.TryGetValue(tile, out texture);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (texture == null) return;
            CalculatedStyle innerDimensions = GetInnerDimensions();
            var bg = BackgroundTexture;
            if (bg != null)
            {
                spriteBatch.Draw(bg, innerDimensions.Position(), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            if (selected && UIRecipeSlot.selectedBackgroundTexture != null)
            {
                spriteBatch.Draw(UIRecipeSlot.selectedBackgroundTexture, innerDimensions.Position(), null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            int height = texture.Height;
            int width = texture.Width;
            float drawScale = 1f;
            float bgWidth = (bg != null ? bg.Width : 52) * scale;
            if (width > bgWidth || height > bgWidth)
            {
                drawScale = (width <= height) ? (bgWidth / height) : (bgWidth / width);
            }
            drawScale *= scale;

            Vector2 slotSize = (bg != null ? new Vector2(bg.Width, bg.Height) : new Vector2(52, 52)) * scale;
            Vector2 drawPos = innerDimensions.Position() + slotSize / 2f - new Vector2(texture.Width, texture.Height) * drawScale / 2f;
            spriteBatch.Draw(texture, drawPos, null, Color.White, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

            if (IsMouseHovering)
            {
                string text = Utilities.GetTileName(tile);
                if (selected && RecipeCatalogueUI.instance != null)
                {
                    if (RecipeCatalogueUI.instance.tileIsItemsThatPlaceThisTileInstead)
                    {
                        text += "\n" + RBLanguage.GetText("RecipeCatalogueUI", "ShowRecipesForCraftingStationItself");
                        text += "\n" + RBLanguage.GetText("RecipeCatalogueUI", "ShowRecipesThatNeedThisCraftingStationHint");
                    }
                    else
                    {
                        text += "\n" + (RecipeCatalogueUI.instance.uniqueCheckbox?.CurrentState != 0 ? RBLanguage.GetText("RecipeCatalogueUI", "ShowRecipesThatNeedThisCraftingStationNoInherited") : RBLanguage.GetText("RecipeCatalogueUI", "ShowRecipesThatNeedThisCraftingStation"));
                        text += "\n" + RBLanguage.GetText("RecipeCatalogueUI", "ShowRecipesForCraftingStationItselfHint");
                    }
                }
                UICommon.TooltipMouseText(text);
            }
        }
    }

    public class UITileNoSlot : UIElement
    {
        internal float scale = 0.75f;
        public int order;
        public int tile;
        private Texture2D texture;

        public UITileNoSlot(int tile, int order, float scale = 0.75f)
        {
            this.scale = scale;
            this.order = order;
            this.tile = tile;
            var bg = TextureAssets.InventoryBack9?.Value ?? TextureAssets.InventoryBack.Value;
            Width.Set((bg != null ? bg.Width : 52) * scale, 0f);
            Height.Set((bg != null ? bg.Height : 52) * scale, 0f);
        }

        public override int CompareTo(object obj)
        {
            if (obj is UITileSlot other) return -order.CompareTo(other.order);
            return 0;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (texture == null)
            {
                if (!Utilities.tileTextures.ContainsKey(tile))
                {
                    Utilities.GenerateTileTexture(tile);
                }
                Utilities.tileTextures.TryGetValue(tile, out texture);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (texture == null) return;
            CalculatedStyle innerDimensions = GetInnerDimensions();
            var bg = TextureAssets.InventoryBack9?.Value ?? TextureAssets.InventoryBack.Value;
            int height = texture.Height;
            int width = texture.Width;
            float drawScale = 1f;
            float bgWidth = (bg != null ? bg.Width : 52) * scale;
            if (width > bgWidth || height > bgWidth)
            {
                drawScale = (width <= height) ? (bgWidth / height) : (bgWidth / width);
            }
            drawScale *= scale;

            Vector2 slotSize = (bg != null ? new Vector2(bg.Width, bg.Height) : new Vector2(52, 52)) * scale;
            Vector2 drawPos = innerDimensions.Position() + slotSize / 2f - new Vector2(texture.Width, texture.Height) * drawScale / 2f;
            spriteBatch.Draw(texture, drawPos, null, Color.White, 0f, Vector2.Zero, drawScale, SpriteEffects.None, 0f);

            if (IsMouseHovering)
            {
                Main.hoverItemName = Utilities.GetTileName(tile);
            }
        }
    }

    public class UIArmorSetCatalogueItemSlot : UIItemCatalogueItemSlot
    {
        internal Tuple<Item, Item, Item, string, int> set;
        internal Item compareItem;
        private bool drawError;
        private UIItemCatalogueItemSlot headSlot;
        private UIItemCatalogueItemSlot bodySlot;
        private UIItemCatalogueItemSlot legsSlot;
        internal bool needsUpdate;
        internal static Player drawPlayer;
        internal static bool useDye;
        internal static bool animate;
        internal static bool accessories;
        internal static bool showItems = true;
        private static uint lastUpdate;

        public UIArmorSetCatalogueItemSlot(Tuple<Item, Item, Item, string, int> set, float scale = 0.75f)
            : base(set.Item1 ?? set.Item2 ?? new Item(), scale)
        {
            this.set = set;
            compareItem = set.Item1 ?? set.Item2 ?? new Item();
            var bg = UIItemSlot.DefaultBackgroundTexture;
            Width.Set((bg != null ? bg.Width : 52) * scale, 0f);
            Height.Set((bg != null ? bg.Height : 52) * 4.6f * scale, 0f);

            if (set.Item1 != null)
            {
                Item h = new Item();
                h.SetDefaults(set.Item1.type);
                headSlot = new UIItemCatalogueItemSlot(h, scale);
                headSlot.Top.Set(60f, 0f);
            }
            if (set.Item2 != null)
            {
                Item b = new Item();
                b.SetDefaults(set.Item2.type);
                bodySlot = new UIItemCatalogueItemSlot(b, scale);
                bodySlot.Top.Set(100f, 0f);
            }
            if (set.Item3 != null)
            {
                Item l = new Item();
                l.SetDefaults(set.Item3.type);
                legsSlot = new UIItemCatalogueItemSlot(l, scale);
                legsSlot.Top.Set(140f, 0f);
            }

            if (showItems)
            {
                if (headSlot != null) Append(headSlot);
                if (bodySlot != null) Append(bodySlot);
                if (legsSlot != null) Append(legsSlot);
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (needsUpdate)
            {
                if (headSlot != null) { RemoveChild(headSlot); if (showItems) Append(headSlot); }
                if (bodySlot != null) { RemoveChild(bodySlot); if (showItems) Append(bodySlot); }
                if (legsSlot != null) { RemoveChild(legsSlot); if (showItems) Append(legsSlot); }

                var bg = UIItemSlot.DefaultBackgroundTexture;
                Height.Set((bg != null ? bg.Height : 52) * (showItems ? 4.6f : 1.6f) * scale, 0f);
                needsUpdate = false;
            }

            if (Main.GameUpdateCount == lastUpdate) return;
            lastUpdate = Main.GameUpdateCount;

            if (drawPlayer == null) drawPlayer = new Player();
            drawPlayer.skinVariant = Main.LocalPlayer.skinVariant;
            drawPlayer.Male = Main.LocalPlayer.Male;
            drawPlayer.eyeColor = Main.LocalPlayer.eyeColor;
            drawPlayer.hairColor = Main.LocalPlayer.hairColor;
            drawPlayer.skinColor = Main.LocalPlayer.skinColor;
            drawPlayer.shirtColor = Main.LocalPlayer.shirtColor;
            drawPlayer.underShirtColor = Main.LocalPlayer.underShirtColor;
            drawPlayer.shoeColor = Main.LocalPlayer.shoeColor;
            drawPlayer.pantsColor = Main.LocalPlayer.pantsColor;
            drawPlayer.direction = 1;
            drawPlayer.gravDir = 1f;
            drawPlayer.head = -1;
            drawPlayer.body = -1;
            drawPlayer.legs = -1;

            if (useDye)
            {
                for (int i = 0; i < 10; i++) drawPlayer.dye[i] = Main.LocalPlayer.dye[i].Clone();
            }
            else
            {
                for (int j = 0; j < 10; j++) { drawPlayer.dye[j].TurnToAir(); drawPlayer.dye[j].dye = 0; }
            }
            drawPlayer.UpdateDyes();

            if (accessories)
            {
                for (int k = 0; k < 20; k++)
                {
                    drawPlayer.armor[k] = Main.LocalPlayer.armor[k].Clone();
                    if (k < 10) drawPlayer.hideVisibleAccessory[k] = Main.LocalPlayer.hideVisibleAccessory[k];
                }
            }
            else
            {
                for (int l = 0; l < 20; l++)
                {
                    drawPlayer.armor[l].TurnToAir();
                    if (l < 10) drawPlayer.hideVisibleAccessory[l] = true;
                }
            }
            drawPlayer.PlayerFrame();
            drawPlayer.socialIgnoreLight = true;
            if (animate)
            {
                drawPlayer.bodyFrame = Main.LocalPlayer.bodyFrame;
                drawPlayer.legFrame = Main.LocalPlayer.legFrame;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (drawPlayer == null) return;
            drawPlayer.head = set.Item1?.headSlot ?? -1;
            drawPlayer.body = set.Item2?.bodySlot ?? -1;
            drawPlayer.legs = set.Item3?.legSlot ?? -1;

            CalculatedStyle innerDimensions = GetInnerDimensions();
            Rectangle rect = innerDimensions.ToRectangle();
            var bg = BackgroundTexture ?? DefaultBackgroundTexture;
            if (bg != null)
            {
                spriteBatch.Draw(bg, innerDimensions.Position(), null, Color.White, 0f, Vector2.Zero, new Vector2(scale, scale * 1.5f), SpriteEffects.None, 0f);
            }

            if (!drawError)
            {
                Vector2 center = new Vector2(rect.Center.X, rect.Y + 38);
                drawPlayer.direction = 1;
                drawPlayer.Bottom = Main.screenPosition + center + new Vector2(0f, 15f);
                try
                {
                    Main.PlayerRenderer.DrawPlayer(Main.Camera, drawPlayer, drawPlayer.position, drawPlayer.fullRotation, drawPlayer.fullRotationOrigin, 0f, 1f);
                }
                catch
                {
                    drawError = true;
                }

                if (IsMouseHovering)
                {
                    string defText = RBLanguage.GetText("UIArmorSetCatalogue", "TotalSetDefense").Replace("{0}", set.Item5.ToString());
                    UICommon.DrawHoverStringInBounds(spriteBatch, $"{set.Item4}\n{defText}");
                }
            }
        }
    }
}
