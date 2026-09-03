using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace WandsTool.Content.Structure
{
    /// <summary>
    /// 蓝图材料需求清单计算器与悬浮摘要面板（蓝图库卡片 hover 场景）
    /// 三态判定：满足(绿) / 可自动合成(金) / 缺少(红)，结果按背包指纹缓存避免高频重算。
    /// 面板为 UI 层纯 SpriteBatch 绘制，不进入 UIElement 树，不拦截鼠标。
    /// </summary>
    public static class StructureMaterialSummary
    {
        public enum MatState
        {
            Satisfied,
            Craftable,
            Missing
        }

        public class MaterialEntry
        {
            public int ItemId;
            public int Required;
            public int Owned;
            public MatState State;
        }

        // ---------- 悬浮通知（帧戳判定存活） ----------
        private static StructureData _hoverData = null;
        private static double _hoverStamp = -10;

        /// <summary>
        /// 蓝图卡片在 Update 中悬停时调用（每帧刷新帧戳）
        /// </summary>
        public static void NotifyHover(StructureData data)
        {
            if (data == null) return;
            if (_hoverData != data) _entries = null; // 换蓝图立即失效缓存
            _hoverData = data;
            _hoverStamp = Main.GameUpdateCount;
        }

        // ---------- 计算缓存 ----------
        private static List<MaterialEntry> _entries = null;
        private static StructureData _cacheData = null;
        private static int _cacheInvHash = -1;
        private static bool _cacheConsume = false;
        private static bool _cacheAutoCraft = false;
        private static bool _cacheReqStation = false;
        private static bool _computeFailed = false;

        private const int MaxRows = 9;

        private static void EnsureComputed(StructureData data)
        {
            Player player = Main.LocalPlayer;
            if (data == null || player == null) return;

            bool consume = GameMain.Wand_StructureConsumeMaterials && ModConfig.IsConsumablesItem();
            bool autoCraft = GameMain.Wand_StructureAutoCraft && ModConfig.IsAutoCraftMaterials();
            bool reqStation = GameMain.Wand_StructureAutoCraftRequireStation && ModConfig.IsAutoCraftRequireStation();
            int invHash = StructureCraftingEngine.GetInventoryHash(player);

            if (_entries != null && _cacheData == data && _cacheInvHash == invHash
                && _cacheConsume == consume && _cacheAutoCraft == autoCraft && _cacheReqStation == reqStation)
            {
                return;
            }

            _cacheData = data;
            _cacheInvHash = invHash;
            _cacheConsume = consume;
            _cacheAutoCraft = autoCraft;
            _cacheReqStation = reqStation;

            try
            {
                // 蓝图库场景无落点：不做世界差量免除，口径为总需求
                Dictionary<int, int> required = data.GetRequiredItems(null, true);
                Dictionary<int, int> stock = StructureCraftingEngine.GetPlayerInventorySnapshot(player);

                StructureCraftingEngine.CraftingPlan plan = null;
                if (consume)
                {
                    plan = StructureCraftingEngine.BuildPlan(data, player, autoCraft, reqStation, null, true);
                }

                List<MaterialEntry> list = new List<MaterialEntry>();
                foreach (var kvp in required)
                {
                    MaterialEntry e = new MaterialEntry
                    {
                        ItemId = kvp.Key,
                        Required = kvp.Value,
                        Owned = stock.TryGetValue(kvp.Key, out int owned) ? owned : 0
                    };

                    if (!consume || e.Owned >= e.Required)
                    {
                        e.State = MatState.Satisfied;
                    }
                    else if (plan != null && plan.CraftedCounts.ContainsKey(e.ItemId))
                    {
                        e.State = MatState.Craftable;
                    }
                    else
                    {
                        e.State = MatState.Missing;
                    }

                    list.Add(e);
                }

                // 缺少优先，其次可合成，同类按需求量降序
                _entries = list
                    .OrderByDescending(e => e.State)
                    .ThenByDescending(e => e.Required)
                    .ToList();
                _computeFailed = false;
            }
            catch
            {
                _entries = new List<MaterialEntry>();
                _computeFailed = true;
            }
        }

        // ---------- 悬浮面板绘制 ----------
        public static void DrawOverlay(SpriteBatch sb)
        {
            try
            {
                // 回主菜单：直接清空悬停与缓存引用，避免蓝图数据滞留内存
                if (Main.gameMenu)
                {
                    _hoverData = null;
                    _entries = null;
                    return;
                }

                if (_hoverData == null) return;

                // 帧戳失效（>2 帧未刷新视为已离开卡片）
                if (Main.GameUpdateCount - _hoverStamp > 2)
                {
                    _hoverData = null;
                    _entries = null;
                    return;
                }

                StructureData data = _hoverData;
                EnsureComputed(data);
                if (_entries == null) return;

                bool consume = _cacheConsume;
                List<MaterialEntry> entries = _entries;

                int missing = entries.Count(e => e.State == MatState.Missing);
                int craftable = entries.Count(e => e.State == MatState.Craftable);

                bool needMore = entries.Count > MaxRows;
                int rows = Math.Min(entries.Count, MaxRows);
                int extraRows = needMore ? 1 : 0;

                const float PanelWidth = 258f;
                const float HeaderH = 26f;
                const float RowH = 30f;
                const float FooterH = 26f;
                float panelH = HeaderH + (rows + extraRows) * RowH + FooterH;

                // 光标右侧锚定，屏幕边缘钳制（越界时翻转到左侧）
                float x = Main.mouseX + 18f;
                float y = Main.mouseY + 18f;
                if (x + PanelWidth > Main.screenWidth) x = Main.mouseX - PanelWidth - 18f;
                if (y + panelH > Main.screenHeight) y = Main.screenHeight - panelH - 4f;

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                if (pixel == null) return;

                Rectangle backRect = new Rectangle((int)x, (int)y, (int)PanelWidth, (int)panelH);
                sb.Draw(pixel, backRect, new Color(10, 14, 26) * 0.93f);
                Color border = Color.Gold * 0.85f;
                sb.Draw(pixel, new Rectangle(backRect.X, backRect.Y, backRect.Width, 2), border);
                sb.Draw(pixel, new Rectangle(backRect.X, backRect.Bottom - 2, backRect.Width, 2), border);
                sb.Draw(pixel, new Rectangle(backRect.X, backRect.Y, 2, backRect.Height), border);
                sb.Draw(pixel, new Rectangle(backRect.Right - 2, backRect.Y, 2, backRect.Height), border);

                // 标题行
                Vector2 titlePos = new Vector2(x + 10f, y + HeaderH * 0.5f);
                Utils.DrawBorderString(sb, $"材料清单 [{data.Width}×{data.Height}]", titlePos, Color.Cyan, 0.85f, 0f, 0.5f);

                // 材料行（图标 + 名称 + 需求/拥有）
                for (int i = 0; i < rows; i++)
                {
                    MaterialEntry e = entries[i];
                    float rowY = y + HeaderH + i * RowH + RowH * 0.5f;
                    DrawItemIconSafe(sb, e.ItemId, new Vector2(x + 24f, rowY));
                    Color lineColor = StateColor(e.State);
                    string line = $"{Lang.GetItemNameValue(e.ItemId)}: 需 {e.Required} (有 {e.Owned}){StateTag(e.State)}";
                    Utils.DrawBorderString(sb, line, new Vector2(x + 46f, rowY), lineColor, 0.78f, 0f, 0.5f);
                }

                // 折叠行
                float footTop = y + HeaderH + (rows + extraRows) * RowH;
                if (needMore)
                {
                    float foldY = footTop - RowH * 0.5f;
                    Utils.DrawBorderString(sb, $"... 及其余 {entries.Count - MaxRows} 种材料", new Vector2(x + 46f, foldY), Color.Gray, 0.78f, 0f, 0.5f);
                }

                // 汇总行
                string summary;
                Color summaryColor;
                if (_computeFailed && consume)
                {
                    summary = "材料清单计算失败，请稍后重试";
                    summaryColor = Color.Gray;
                }
                else if (!consume)
                {
                    summary = $"共 {entries.Count} 种材料 | 免消耗模式";
                    summaryColor = Color.LightCyan;
                }
                else if (missing == 0)
                {
                    summary = $"共 {entries.Count} 种材料 | 材料齐备";
                    summaryColor = Color.LightGreen;
                }
                else if (craftable > 0)
                {
                    summary = $"缺 {missing} 种 | 自动合成可补 {craftable} 种";
                    summaryColor = Color.Gold;
                }
                else
                {
                    summary = $"缺 {missing} 种材料，放置将被拒绝";
                    summaryColor = Color.Tomato;
                }
                Utils.DrawBorderString(sb, summary, new Vector2(x + 10f, footTop + FooterH * 0.5f), summaryColor, 0.8f, 0f, 0.5f);
            }
            catch
            {
                // 静默容错，避免异常冒泡到 XNA 绘制线程
            }
        }

        private static Color StateColor(MatState state)
        {
            if (state == MatState.Satisfied) return Color.LightGreen;
            if (state == MatState.Craftable) return Color.Gold;
            return Color.Tomato;
        }

        private static string StateTag(MatState state)
        {
            if (state == MatState.Satisfied) return "";
            if (state == MatState.Craftable) return " (可合成)";
            return " (缺少)";
        }

        /// <summary>
        /// 按原版光标物品渲染规范绘制单个物品图标（同 Wands.DrawCursorModeTooltip 管线）
        /// </summary>
        private static void DrawItemIconSafe(SpriteBatch sb, int itemId, Vector2 center)
        {
            if (itemId <= 0 || itemId >= TextureAssets.Item.Length) return;

            Main.instance.LoadItem(itemId);
            Item drawItem = (ContentSamples.ItemsByType != null && ContentSamples.ItemsByType.TryGetValue(itemId, out var sample)) ? sample : null;
            if (drawItem == null)
            {
                drawItem = new Item();
                drawItem.SetDefaults(itemId);
            }

            Color iconColor = Color.White;
            Terraria.UI.ItemSlot.GetItemLight(ref iconColor, itemId);
            Terraria.UI.ItemSlot.DrawItemIcon(drawItem, 21, sb, center, 0.85f, 32f, iconColor);
        }
    }
}
