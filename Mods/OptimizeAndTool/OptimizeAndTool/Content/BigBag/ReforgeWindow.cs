using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using TPML.Content;
using TPML.UI;
using OptimizeAndTool.Content.Storage.Core;

namespace OptimizeAndTool.Content.BigBag
{
    /// <summary>
    /// 便携智能重铸台 UI 窗口：
    /// 随时随地呼出原版重铸机制，支持装备单次重铸与一键极速重铸至白名单极品词条，
    /// 自动结算原版折扣折算费用，并具备防吞物自动安全返还。
    /// 作者: SaintCirno9
    /// </summary>
    public class ReforgeWindow : UIWindow
    {
        private static ReforgeWindow _instance;
        public static ReforgeWindow Instance => _instance ?? (_instance = new ReforgeWindow());

        private ReforgeSlotElement slotElement = null;
        private UIText itemInfoText = null;
        private UIText costText = null;
        private UIText playerMoneyText = null;
        private UIButton btnSingleReforge = null;
        private UIButton btnAutoReforge = null;

        public Item CurrentItem => slotElement?.Item;

        public ReforgeWindow() : base("便携重铸台 (白名单直达)", 340, 270)
        {
            _instance = this;

            MinWidth.Pixels = 320;
            MinHeight.Pixels = 250;

            // 移除右下角缩放手柄，保持原生紧凑自适应尺寸
            foreach (UIElement el in Elements)
            {
                if (el is UIImage img && el != Child)
                {
                    RemoveChild(el);
                    break;
                }
            }

            // 1. 重铸槽位（居中靠上，避让标题栏）
            slotElement = new ReforgeSlotElement();
            slotElement.HAlign = 0.5f;
            slotElement.Top.Set(14, 0);
            slotElement.OnItemChanged += UpdateDisplays;
            Child.Append(slotElement);

            // 2. 物品信息与词条状态展示
            itemInfoText = new UIText("请将需要重铸的武器/饰品/工具放入上方槽位", 0.75f);
            itemInfoText.HAlign = 0.5f;
            itemInfoText.Top.Set(74, 0);
            itemInfoText.TextColor = Color.LightGray;
            Child.Append(itemInfoText);

            // 3. 重铸单次费用
            costText = new UIText("重铸费用: 0 铜", 0.75f);
            costText.HAlign = 0.5f;
            costText.Top.Set(98, 0);
            costText.TextColor = Color.Gold;
            Child.Append(costText);

            // 4. 玩家当前持有钱币
            playerMoneyText = new UIText("持有钱币: 0 铜", 0.7f);
            playerMoneyText.HAlign = 0.5f;
            playerMoneyText.Top.Set(120, 0);
            playerMoneyText.TextColor = Color.Silver;
            Child.Append(playerMoneyText);

            // 5. 操作按钮栏
            UIStackPanel btnStack = new UIStackPanel();
            btnStack.HAlign = 0.5f;
            btnStack.Top.Set(148, 0);
            btnStack.Height.Set(28, 0);
            btnStack.Horizontal = true;
            btnStack.ItemMargin = 12;
            btnStack.IsAutoUpdateSize = true;
            Child.Append(btnStack);

            btnSingleReforge = new UIButton("重铸一次", 0.75f);
            btnSingleReforge.Height.Set(26, 0);
            btnSingleReforge.SetPadding(6);
            btnSingleReforge.OnLeftClick += (evt, el) => PerformSingleReforge();
            btnStack.Append(btnSingleReforge);

            btnAutoReforge = new UIButton("智能洗至白名单", 0.75f);
            btnAutoReforge.Height.Set(26, 0);
            btnAutoReforge.SetPadding(6);
            btnAutoReforge.OnLeftClick += (evt, el) => PerformAutoReforge();
            btnStack.Append(btnAutoReforge);

            OnOpen += () =>
            {
                UpdateDisplays();
            };

            OnClose += () =>
            {
                // 防吞物：窗口关闭时将槽位物品完整归还玩家个人背包
                slotElement?.ReturnItemToPlayer();
            };
        }

        public void OpenOrClose(UIState parentState)
        {
            if (IsOpen)
            {
                Close();
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
            else
            {
                Open(parentState);
            }
        }

        /// <summary>
        /// 原版重铸单次费用精确计算（支持打折卡折扣与 NPC 好感度折算）
        /// </summary>
        public static long CalculateReforgeCost(Item item, Player player)
        {
            if (item == null || item.IsAir || item.value <= 0) return 0;
            long cost = (long)item.value * (long)item.stack;
            if (player.discountAvailable)
            {
                cost = (long)((double)cost * 0.8);
            }
            cost = (long)((float)cost * player.currentShoppingSettings.PriceAdjustment);
            cost /= 3;
            if (cost < 1) cost = 1;
            return cost;
        }

        /// <summary>
        /// 获取玩家背包与钱币栏总持有金额（包含护卫熔炉、存钱罐、虚空袋与大背包等全部来源）
        /// </summary>
        public static long GetPlayerTotalCoins(Player player)
        {
            if (player == null) return 0;
            long total = 0;
            try
            {
                if (player.inventory != null)
                    total += Terraria.Utils.CoinsCount(out _, player.inventory);
                if (player.bank?.item != null)
                    total += Terraria.Utils.CoinsCount(out _, player.bank.item);
                if (player.bank2?.item != null)
                    total += Terraria.Utils.CoinsCount(out _, player.bank2.item);
                if (player.bank3?.item != null)
                    total += Terraria.Utils.CoinsCount(out _, player.bank3.item);
                if (player.bank4?.item != null)
                    total += Terraria.Utils.CoinsCount(out _, player.bank4.item);
                if (BigBag.Slots != null)
                    total += Terraria.Utils.CoinsCount(out _, BigBag.Slots);
            }
            catch
            {
            }
            return total;
        }

        public void UpdateDisplays()
        {
            Player player = Main.LocalPlayer;
            if (player == null) return;

            Item item = slotElement?.Item;
            long totalCoins = GetPlayerTotalCoins(player);
            if (playerMoneyText != null)
            {
                playerMoneyText.SetText($"持有钱币: {BigBag.FormatCoins(totalCoins)}");
            }

            if (item == null || item.IsAir)
            {
                if (itemInfoText != null)
                {
                    itemInfoText.SetText("请将需要重铸的武器/饰品/工具放入上方槽位");
                    itemInfoText.TextColor = Color.LightGray;
                }
                if (costText != null)
                {
                    costText.SetText("重铸费用: 0 铜");
                    costText.TextColor = Color.Gray;
                }
                return;
            }

            // 显示当前物品与修饰语
            string prefixName = item.prefix > 0 ? (Lang.prefix[item.prefix]?.Value ?? $"前缀{item.prefix}") : "无修饰语";
            bool isWhitelisted = item.prefix > 0 && PrefixWhitelistManager.IsWhitelisted(item.prefix);

            if (itemInfoText != null)
            {
                if (isWhitelisted)
                {
                    itemInfoText.SetText($"[{prefixName}] {item.Name} (★已命中白名单极品★)");
                    itemInfoText.TextColor = Color.Gold;
                }
                else
                {
                    itemInfoText.SetText($"[{prefixName}] {item.Name}");
                    itemInfoText.TextColor = Color.White;
                }
            }

            long cost = CalculateReforgeCost(item, player);
            if (costText != null)
            {
                costText.SetText($"重铸费用: {BigBag.FormatCoins(cost)}");
                costText.TextColor = totalCoins >= cost ? Color.Gold : Color.Red;
            }
        }

        /// <summary>
        /// 单次重铸操作
        /// </summary>
        private void PerformSingleReforge()
        {
            Player player = Main.LocalPlayer;
            if (player == null) return;

            Item item = slotElement?.Item;
            if (item == null || item.IsAir)
            {
                Main.NewText("[便携重铸台] 请先将需要重铸的装备放入槽位。", 220, 220, 100);
                return;
            }

            if (!item.CanHavePrefixes())
            {
                Main.NewText("[便携重铸台] 该物品无法拥有修饰语/前缀。", 255, 100, 100);
                return;
            }

            long cost = CalculateReforgeCost(item, player);
            if (!player.BuyItem(cost))
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                Main.NewText("[便携重铸台] 玩家钱币不足，无法重铸！", 255, 100, 100);
                return;
            }

            item.ResetPrefix();
            item.Prefix(-2, out bool isTopTier);

            bool hitWhitelist = item.prefix > 0 && PrefixWhitelistManager.IsWhitelisted(item.prefix);
            if (isTopTier || hitWhitelist)
            {
                SoundEngine.PlaySound(SoundID.BestReforge);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item37);
            }

            PopupText.NewText(isTopTier ? PopupTextContext.ItemReforge_Best : PopupTextContext.ItemReforge, item, player.Center, item.stack, noStack: true);
            UpdateDisplays();
        }

        /// <summary>
        /// 智能极速重铸至白名单极品词条
        /// </summary>
        private void PerformAutoReforge()
        {
            Player player = Main.LocalPlayer;
            if (player == null) return;

            Item item = slotElement?.Item;
            if (item == null || item.IsAir)
            {
                Main.NewText("[便携重铸台] 请先将需要重铸的装备放入槽位。", 220, 220, 100);
                return;
            }

            if (!item.CanHavePrefixes())
            {
                Main.NewText("[便携重铸台] 该物品无法拥有修饰语/前缀。", 255, 100, 100);
                return;
            }

            if (item.prefix > 0 && PrefixWhitelistManager.IsWhitelisted(item.prefix))
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.NewText($"[便携重铸台] 当前物品已经是白名单极品词条 [{Lang.prefix[item.prefix]?.Value}]，无需重复重铸。", 100, 255, 100);
                return;
            }

            int attempts = 0;
            long totalSpent = 0;
            bool success = false;

            // 单次最多连续重铸 100 次以防死循环或过度扣费
            while (attempts < 100)
            {
                long currentCost = CalculateReforgeCost(item, player);
                if (!player.BuyItem(currentCost))
                {
                    Main.NewText($"[便携重铸台] 钱币不足！在第 {attempts} 次重铸后停止，共消耗 {BigBag.FormatCoins(totalSpent)}。", 255, 120, 120);
                    break;
                }

                attempts++;
                totalSpent += currentCost;
                item.ResetPrefix();
                item.Prefix(-2, out _);

                if (item.prefix > 0 && PrefixWhitelistManager.IsWhitelisted(item.prefix))
                {
                    success = true;
                    break;
                }
            }

            if (success)
            {
                SoundEngine.PlaySound(SoundID.BestReforge);
                string pName = Lang.prefix[item.prefix]?.Value ?? "极品";
                Main.NewText($"[便携重铸台] ★恭喜！历经 {attempts} 次重铸，成功洗出白名单极品 [{pName}]，共消耗 {BigBag.FormatCoins(totalSpent)}！", 100, 255, 100);
            }
            else if (attempts >= 100)
            {
                SoundEngine.PlaySound(SoundID.Item37);
                Main.NewText($"[便携重铸台] 已连续重铸 100 次保护性暂停，共消耗 {BigBag.FormatCoins(totalSpent)}。可再次点击继续。", 255, 200, 50);
            }

            UpdateDisplays();
        }
    }

    /// <summary>
    /// 便携重铸台专属单件装备槽 UIElement：
    /// 支持鼠标左键拿起/放下，右键快速取回，悬停属性浮窗与关闭防吞物返还
    /// </summary>
    public class ReforgeSlotElement : UIElement
    {
        public Item Item = new Item();
        public event Action OnItemChanged;

        public ReforgeSlotElement()
        {
            Width.Set(52f, 0f);
            Height.Set(52f, 0f);
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            Player player = Main.LocalPlayer;
            if (player == null) return;

            Item mouse = Main.mouseItem;
            if (!mouse.IsAir)
            {
                // 手中有物品：校验是否为可重铸装备
                if (!mouse.CanHavePrefixes())
                {
                    SoundEngine.PlaySound(SoundID.MenuClose);
                    Main.NewText("[便携重铸台] 该物品无法拥有修饰语/前缀。", 255, 100, 100);
                    return;
                }

                // 交换或放入
                Item temp = Item.Clone();
                Item = mouse.Clone();
                Main.mouseItem = temp;
                SoundEngine.PlaySound(SoundID.Grab);
                OnItemChanged?.Invoke();
            }
            else if (!Item.IsAir)
            {
                // 手中无物且槽位有物：拿起物品
                Main.mouseItem = Item.Clone();
                Item.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                OnItemChanged?.Invoke();
            }
        }

        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            // 右键直接取回物品到玩家背包
            if (!Item.IsAir)
            {
                ReturnItemToPlayer();
                SoundEngine.PlaySound(SoundID.Grab);
                OnItemChanged?.Invoke();
            }
        }

        /// <summary>
        /// 安全返还槽位中的物品到玩家背包（背包满则快速掉落在脚下）
        /// </summary>
        public void ReturnItemToPlayer()
        {
            if (Item == null || Item.IsAir) return;

            Player player = Main.LocalPlayer;
            if (player != null && player.active)
            {
                player.QuickSpawnItem(null, Item);
            }
            Item = new Item();
            OnItemChanged?.Invoke();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dim = GetDimensions();
            Rectangle slotRect = dim.ToRectangle();

            // 1. 绘制槽位底框 (原版 InventoryBack 贴图)
            Texture2D backTex = TextureAssets.InventoryBack.Value;
            Color backColor = IsMouseHovering ? new Color(220, 235, 255) : Color.White * 0.95f;
            spriteBatch.Draw(backTex, slotRect, backColor);

            // 2. 绘制物品本身 (标准 0.68f 黄金比例居中等比自适应缩放)
            if (Item != null && !Item.IsAir && Item.type > ItemID.None)
            {
                if (!TextureAssets.Item[Item.type].IsLoaded)
                {
                    Main.instance.LoadItem(Item.type);
                }

                Texture2D itemTex = TextureAssets.Item[Item.type].Value;
                if (itemTex != null && !itemTex.IsDisposed)
                {
                    Rectangle frame = Main.itemAnimations[Item.type]?.GetFrame(itemTex) ?? itemTex.Bounds;
                    float maxBound = (float)slotRect.Width * 0.68f;
                    float scale = 1f;
                    if ((float)frame.Width > maxBound || (float)frame.Height > maxBound)
                    {
                        scale = frame.Width > frame.Height ? maxBound / (float)frame.Width : maxBound / (float)frame.Height;
                    }

                    Vector2 origin = frame.Size() / 2f;
                    Vector2 itemCenter = new Vector2(slotRect.X + slotRect.Width / 2f, slotRect.Y + slotRect.Height / 2f);
                    Color itemColor = Item.GetAlpha(Color.White);

                    spriteBatch.Draw(itemTex, itemCenter, frame, itemColor, 0f, origin, scale, SpriteEffects.None, 0f);
                }

                // 鼠标悬停显示完整原版物品 Tooltip 属性浮窗
                if (IsMouseHovering)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.HoverItem = Item.Clone();
                }
            }
            else if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;
                Main.instance.MouseText("将需要重铸的装备放置于此 (右键取回)");
            }
        }
    }
}
