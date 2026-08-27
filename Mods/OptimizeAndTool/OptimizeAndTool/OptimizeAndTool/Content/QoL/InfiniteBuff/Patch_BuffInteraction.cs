using HarmonyLib;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Localization;
using Terraria.UI.Chat;

namespace OptimizeAndTool.Content.QoL.InfiniteBuff
{
    /// <summary>
    /// 原版 Buff 图标交互与提示渲染补丁：
    /// 1. 鼠标悬停左上角 Buff 图标时，底层通过 ChatManager 渲染富文本与彩色标签；
    /// 2. 在受害者名单（若有）或 Buff 描述最底部优雅自适应展示左键开窗提示；
    /// 3. 左键点击直接快捷呼出/关闭无限增益管理窗口，保留原版右键取消 Buff 机制。
    /// 作者: SaintCirno9
    /// </summary>
    [HarmonyPatch]
    public static class Patch_BuffInteraction
    {
        [HarmonyPatch(typeof(Main), nameof(Main.DrawBuffIcon))]
        [HarmonyPostfix]
        public static void DrawBuffIconPostfix(int drawBuffText, int buffSlotOnPlayer, int x, int y, ref int __result)
        {
            if (Main.netMode == 2 || Main.gameMenu) return;

            Player player = Main.LocalPlayer;
            if (player == null || buffSlotOnPlayer < 0 || buffSlotOnPlayer >= player.buffType.Length) return;

            int buffType = player.buffType[buffSlotOnPlayer];
            if (buffType <= 0 || buffType >= TextureAssets.Buff.Length) return;

            if (TextureAssets.Buff[buffType]?.Value == null) return;

            int iconWidth = TextureAssets.Buff[buffType].Width();
            int iconHeight = TextureAssets.Buff[buffType].Height();

            if (Main.mouseX >= x && Main.mouseX <= x + iconWidth &&
                Main.mouseY >= y && Main.mouseY <= y + iconHeight &&
                !PlayerInput.IgnoreMouseInterface)
            {
                player.mouseInterface = true;

                if (Main.mouseLeft && Main.mouseLeftRelease)
                {
                    Main.mouseLeftRelease = false;
                    InfiniteBuffWindow.Instance.Toggle();
                }
            }
        }

        [HarmonyPatch(typeof(Main), "MouseText_DrawBuffTooltip")]
        [HarmonyPrefix]
        public static bool MouseTextDrawBuffTooltipPrefix(Main __instance, string buffString, ref int X, ref int Y, Vector2 mouseTextSize)
        {
            Point p = new Point(X, Y);
            int colWidth = 220;
            int bottomMargin = 72;
            int stopBannerIndex = -1;
            float zoom = 1f;

            List<Vector2> boundsList = new List<Vector2>();
            Vector2 descSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, buffString, Vector2.One);
            boundsList.Add(descSize);
            boundsList.Add(mouseTextSize);

            // 每列可容纳的怪物名称行数
            int rowsPerCol = (int)((float)(Main.screenHeight - Y - 24 - bottomMargin) * zoom) / 20;
            if (rowsPerCol < 1) rowsPerCol = 1;

            int totalBanners = 0;
            if (Main.bannerMouseOver)
            {
                for (int i = 0; i < BannerSystem.MaxBannerTypes; i++)
                {
                    if (BannerSystem.BannerToNPC(i) != 0 && Main.player[Main.myPlayer].HasNPCBannerBuff(i))
                    {
                        totalBanners++;
                        string npcName = Lang.GetNPCNameValue(BannerSystem.BannerToNPC(i));
                        Vector2 bannerNameSize = FontAssets.MouseText.Value.MeasureString(npcName);
                        int itemX = X;
                        int itemY = Y + (int)bannerNameSize.Y + totalBanners * 20 + 10;
                        int colIndex = totalBanners / rowsPerCol;
                        for (int j = 0; j < colIndex; j++)
                        {
                            itemX += colWidth;
                            itemY -= rowsPerCol * 20;
                        }
                        if ((float)(itemX - 24 - colWidth) > (float)Main.screenWidth * zoom)
                        {
                            stopBannerIndex = totalBanners;
                            break;
                        }
                        boundsList.Add(new Vector2(itemX, itemY) + bannerNameSize - p.ToVector2());
                    }
                }
            }

            // 计算左键开窗操作指引的排版位置
            const string hintText = "[c/88FF88:[左键]] 打开/关闭增益管理窗口";
            Vector2 hintSize = ChatManager.GetStringSize(FontAssets.MouseText.Value, hintText, Vector2.One);

            int hintRelativeY;
            if (Main.bannerMouseOver && totalBanners > 0)
            {
                // 存在旗帜名单时，将提示置于第一列最下方
                int displayedRows = Math.Min(totalBanners, rowsPerCol);
                hintRelativeY = (int)mouseTextSize.Y + displayedRows * 20 + 10 + 24;
            }
            else
            {
                // 普通 Buff 提示紧随描述文字下方
                hintRelativeY = (int)mouseTextSize.Y + (int)descSize.Y + 4;
            }

            boundsList.Add(new Vector2(hintSize.X, hintRelativeY + hintSize.Y));

            // 防出界自适应贴边
            Vector2 maxBound = Vector2.Zero;
            foreach (Vector2 bound in boundsList)
            {
                if (maxBound.X < bound.X) maxBound.X = bound.X;
                if (maxBound.Y < bound.Y) maxBound.Y = bound.Y;
            }

            if ((float)X + maxBound.X + 24f > (float)Main.screenWidth * zoom)
            {
                X = (int)((float)Main.screenWidth * zoom - maxBound.X - 24f);
            }
            if ((float)Y + maxBound.Y + 4f > (float)Main.screenHeight * zoom)
            {
                Y = (int)((float)Main.screenHeight * zoom - maxBound.Y - 4f);
            }

            // 1. 使用 ChatManager 渲染 Buff 描述（原生支持 [c/HEX:...] 富文本彩色代码与阴影）
            Color textColor = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, buffString, new Vector2(X, Y + (int)mouseTextSize.Y), textColor, 0f, Vector2.Zero, Vector2.One);

            // 2. 渲染旗帜受害者名单列表
            if (Main.bannerMouseOver && totalBanners > 0)
            {
                int drawIndex = 0;
                float colorMult = (float)(int)Main.mouseTextColor / 255f;
                Color bannerTextColor = new Color((byte)(80f * colorMult), (byte)(255f * colorMult), (byte)(120f * colorMult), Main.mouseTextColor);

                for (int l = 0; l < BannerSystem.MaxBannerTypes; l++)
                {
                    if (BannerSystem.BannerToNPC(l) == 0 || !Main.player[Main.myPlayer].HasNPCBannerBuff(l))
                    {
                        continue;
                    }

                    drawIndex++;
                    bool isEllipsis = false;
                    int curCol = (drawIndex - 1) / rowsPerCol;
                    int bannerX = X + colWidth * curCol;
                    int bannerY = Y + (int)mouseTextSize.Y + drawIndex * 20 + 10 - rowsPerCol * 20 * curCol;

                    string text = Lang.GetNPCNameValue(BannerSystem.BannerToNPC(l));
                    if (stopBannerIndex == drawIndex)
                    {
                        text = Language.GetTextValue("UI.Ellipsis");
                        isEllipsis = true;
                    }

                    ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, text, new Vector2(bannerX, bannerY), bannerTextColor, 0f, Vector2.Zero, Vector2.One);

                    if (isEllipsis) break;
                }
            }

            // 3. 在最底部渲染操作提示
            ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, hintText, new Vector2(X, Y + hintRelativeY), textColor, 0f, Vector2.Zero, Vector2.One);

            return false; // 拦截原版单色绘制
        }
    }
}
