using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace OptimizeAndTool.Content.QoL.InfiniteBuff
{
    /// <summary>
    /// 单个 Buff 状态图标 UI 元素（支持左键切换黑名单、右键切换收藏置顶、悬停多行信息提示）
    /// 作者: SaintCirno9
    /// </summary>
    public class UIBuffIcon : UIPanel
    {
        public readonly int BuffType;
        public const float ICON_SIZE = 38f;

        private static Asset<Texture2D> starTexture = null;

        public UIBuffIcon(int buffType)
        {
            BuffType = buffType;
            Width.Set(ICON_SIZE, 0);
            Height.Set(ICON_SIZE, 0);
            SetPadding(0);

            if (starTexture == null)
            {
                starTexture = Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Icon_Rank_Light", AssetRequestMode.ImmediateLoad);
            }
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            InfiniteBuffStorage.ToggleBlacklist(BuffType);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            InfiniteBuffStorage.ToggleFavorite(BuffType);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }

        protected override void DrawSelf(SpriteBatch sb)
        {
            CalculatedStyle dims = GetDimensions();
            bool isBlacklisted = InfiniteBuffStorage.Blacklist.Contains(BuffType);
            bool isFavorite = InfiniteBuffStorage.Favorites.Contains(BuffType);

            // 1. 设置背景与边框颜色
            if (isFavorite)
            {
                BackgroundColor = IsMouseHovering ? new Color(60, 75, 140, 240) : new Color(40, 50, 95, 230);
                BorderColor = IsMouseHovering ? Color.White : Color.Gold;
            }
            else if (isBlacklisted)
            {
                BackgroundColor = IsMouseHovering ? new Color(50, 30, 30, 240) : new Color(30, 20, 20, 220);
                BorderColor = IsMouseHovering ? Color.Salmon : new Color(110, 45, 45);
            }
            else
            {
                BackgroundColor = IsMouseHovering ? new Color(55, 80, 150, 240) : new Color(35, 45, 80, 230);
                BorderColor = IsMouseHovering ? Color.White : new Color(60, 85, 140);
            }

            base.DrawSelf(sb);

            // 2. 绘制 Buff 贴图
            if (BuffType > 0 && BuffType < TextureAssets.Buff.Length && TextureAssets.Buff[BuffType]?.Value != null)
            {
                Texture2D tex = TextureAssets.Buff[BuffType].Value;
                Vector2 center = new Vector2(dims.X + dims.Width / 2f, dims.Y + dims.Height / 2f);
                Vector2 origin = tex.Size() / 2f;

                float maxSide = Math.Max(tex.Width, tex.Height);
                float scale = maxSide > 32f ? 32f / maxSide : 1f;

                Color buffColor = isBlacklisted ? (Color.White * 0.35f) : Color.White;
                sb.Draw(tex, center, null, buffColor, 0f, origin, scale, SpriteEffects.None, 0f);

                // 被禁用时叠加暗色遮罩与半透明叉号标识
                if (isBlacklisted)
                {
                    DynamicSpriteFont font = FontAssets.MouseText.Value;
                    string cross = "×";
                    Vector2 crossSize = font.MeasureString(cross) * 0.9f;
                    sb.DrawString(font, cross, center - crossSize / 2f, Color.Red * 0.8f, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
                }
            }

            // 3. 收藏状态绘制右上角金色星星
            if (isFavorite && starTexture?.Value != null)
            {
                Texture2D star = starTexture.Value;
                Vector2 starPos = new Vector2(dims.X + dims.Width - 10f, dims.Y + 2f);
                sb.Draw(star, starPos, null, Color.Gold, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

            // 4. 悬停提示
            if (IsMouseHovering)
            {
                Main.LocalPlayer.mouseInterface = true;

                string name = Lang.GetBuffName(BuffType);
                if (string.IsNullOrEmpty(name)) name = $"Buff #{BuffType}";

                string desc = Lang.GetBuffDescription(BuffType);
                string statusText = isBlacklisted ? "[c/FF6666:[已禁用]]" : "[c/66FF66:[已启用]]";
                string favText = isFavorite ? " [c/FFD700:★已收藏]" : "";

                string tooltip = $"{name} {statusText}{favText}";
                if (!string.IsNullOrWhiteSpace(desc))
                {
                    tooltip += $"\n{desc}";
                }
                tooltip += "\n[c/88FF88:[左键]] 切换启用/禁用\n[c/FFDD88:[右键]] 切换收藏置顶";

                Main.hoverItemName = tooltip;
            }
        }
    }
}
