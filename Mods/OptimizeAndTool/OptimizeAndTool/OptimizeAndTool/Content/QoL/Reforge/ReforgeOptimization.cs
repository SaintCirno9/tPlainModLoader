using CommandHelp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using Terraria.UI.Chat;

namespace OptimizeAndTool.Content.QoL.Reforge
{
    /// <summary>
    /// 重铸功能优化系统
    /// 在哥布林重铸槽下方平铺全部适用前缀标签，支持点击锁定目标词条，
    /// 点击原版重铸锤触发瞬间模拟重铸与真实扣费，包含 1000 次安全迭代保护。
    /// 作者: SaintCirno9
    /// </summary>
    public static class ReforgeOptimization
    {
        public static GetSetReset<bool> Enable = new GetSetReset<bool>(true, true);

        /// <summary>
        /// 当前玩家选中的目标前缀 ID（0 表示未选择，维持原版单次重铸）
        /// </summary>
        public static int SelectedPrefixId = 0;

        /// <summary>
        /// 记录重铸槽物品类型与前缀，以便物品更换时重置选择
        /// </summary>
        private static int lastItemType = -1;

        /// <summary>
        /// 缓存的前缀选项列表
        /// </summary>
        private static List<PrefixOption> cachedOptions = new List<PrefixOption>();
        private static int cachedForType = -1;

        public static List<CommandObject> GetCO()
        {
            return new List<CommandObject>
            {
                CommandBuild.get2("reforgeOptimization", Enable)
            };
        }

        public static List<UIElement> GetUI()
        {
            return new List<UIElement>
            {
                UIBuild.get2(Enable, "在哥布林工匠重铸界面展示所有可用前缀，点击选定目标词条后自动连续重铸直到获得", "Images/UI/Reforge_0", "重铸前缀选择优化")
            };
        }

        /// <summary>
        /// 单个前缀条目模型
        /// </summary>
        public class PrefixOption
        {
            public int PrefixId { get; set; }
            public string Name { get; set; }
            public bool IsTopTier { get; set; }
            public float ValueMultiplier { get; set; }
            public List<string> Tooltips { get; set; } = new List<string>();
            public List<Color> TooltipColors { get; set; } = new List<Color>();
        }

        /// <summary>
        /// 获取并解析当前物品可用的所有合法前缀，并按品质强度降序排序
        /// </summary>
        public static List<PrefixOption> GetAvailablePrefixes(Item item)
        {
            if (item == null || item.IsAir || !item.CanHavePrefixes())
                return new List<PrefixOption>();

            if (item.type == cachedForType && cachedOptions.Count > 0)
                return cachedOptions;

            int[] rollable = item.GetRollablePrefixes();
            if (rollable == null || rollable.Length == 0)
                return new List<PrefixOption>();

            float bestValue = item.BestPrefixValue();
            HashSet<int> seen = new HashSet<int>();
            List<PrefixOption> result = new List<PrefixOption>();

            foreach (int p in rollable)
            {
                if (p <= 0 || !seen.Add(p)) continue;

                if (!item.TryGetPrefixStatMultipliersForItem(p, out float dmg, out float kb, out float spd, out float size, out float shtspd, out float mcst, out int crt, out int tagdmg, out int arpen, out float value))
                {
                    continue;
                }

                // 判定是否为顶级前缀
                bool isTop = (Math.Abs(value - bestValue) < 0.001f);
                if (item.accessory)
                {
                    // 饰品满级词条均为 TopTier
                    if (p == PrefixID.Warding || p == PrefixID.Menacing || p == PrefixID.Lucky || p == PrefixID.Quick2 || p == PrefixID.Violent || p == PrefixID.Arcane)
                    {
                        isTop = true;
                    }
                }
                else
                {
                    // 武器经典顶级前缀
                    if (p == PrefixID.Legendary || p == PrefixID.Unreal || p == PrefixID.Mythical || p == PrefixID.Legendary2 || p == PrefixID.Godly || p == PrefixID.Demonic || p == PrefixID.Ruthless)
                    {
                        if (value >= bestValue * 0.95f)
                        {
                            isTop = true;
                        }
                    }
                }

                string name = (p < Lang.prefix.Length && Lang.prefix[p] != null) ? Lang.prefix[p].Value : $"Prefix_{p}";
                PrefixOption opt = new PrefixOption
                {
                    PrefixId = p,
                    Name = name,
                    IsTopTier = isTop,
                    ValueMultiplier = value
                };

                // 生成词条详细属性列表
                GeneratePrefixTooltips(opt, p, dmg, kb, spd, size, shtspd, mcst, crt, tagdmg, arpen, item);
                result.Add(opt);
            }

            // 排序：顶级前缀置顶 -> 价值倍率降序 -> ID 升序
            result.Sort((a, b) =>
            {
                if (a.IsTopTier != b.IsTopTier)
                    return b.IsTopTier ? 1 : -1;
                int cmpVal = b.ValueMultiplier.CompareTo(a.ValueMultiplier);
                if (cmpVal != 0)
                    return cmpVal;
                return a.PrefixId.CompareTo(b.PrefixId);
            });

            cachedForType = item.type;
            cachedOptions = result;
            return result;
        }

        /// <summary>
        /// 生成前缀属性 Tooltip 文本与颜色
        /// </summary>
        private static void GeneratePrefixTooltips(PrefixOption opt, int prefixId, float dmg, float kb, float spd, float size, float shtspd, float mcst, int crt, int tagdmg, int arpen, Item item)
        {
            Color green = new Color(120, 190, 120);
            Color red = new Color(235, 100, 100);
            Color gold = new Color(255, 215, 0);

            if (item.accessory)
            {
                switch (prefixId)
                {
                    case PrefixID.Hard: opt.Tooltips.Add("+1 防御力"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Guarding: opt.Tooltips.Add("+2 防御力"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Armored: opt.Tooltips.Add("+3 防御力"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Warding: opt.Tooltips.Add("+4 防御力"); opt.TooltipColors.Add(gold); break;
                    case PrefixID.Jagged: opt.Tooltips.Add("+1% 伤害"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Spiked: opt.Tooltips.Add("+2% 伤害"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Angry: opt.Tooltips.Add("+3% 伤害"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Menacing: opt.Tooltips.Add("+4% 伤害"); opt.TooltipColors.Add(gold); break;
                    case PrefixID.Precise: opt.Tooltips.Add("+1% 暴击率"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Lucky: opt.Tooltips.Add("+4% 暴击率"); opt.TooltipColors.Add(gold); break;
                    case PrefixID.Brisk: opt.Tooltips.Add("+1% 移动速度"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Fleeting: opt.Tooltips.Add("+2% 移动速度"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Hasty2: opt.Tooltips.Add("+3% 移动速度"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Quick2: opt.Tooltips.Add("+4% 移动速度"); opt.TooltipColors.Add(gold); break;
                    case PrefixID.Wild: opt.Tooltips.Add("+1% 近战速度"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Rash: opt.Tooltips.Add("+2% 近战速度"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Intrepid: opt.Tooltips.Add("+3% 近战速度"); opt.TooltipColors.Add(green); break;
                    case PrefixID.Violent: opt.Tooltips.Add("+4% 近战速度"); opt.TooltipColors.Add(gold); break;
                    case PrefixID.Arcane: opt.Tooltips.Add("+20 最大魔力"); opt.TooltipColors.Add(gold); break;
                }
                return;
            }

            if (dmg != 1f)
            {
                int diff = (int)Math.Round((dmg - 1f) * 100f);
                if (diff != 0)
                {
                    opt.Tooltips.Add($"{(diff > 0 ? "+" : "")}{diff}% 伤害");
                    opt.TooltipColors.Add(diff > 0 ? green : red);
                }
            }

            if (spd != 1f)
            {
                // spd 越小越快 (useTime 乘数)
                int diff = (int)Math.Round((1f - spd) * 100f);
                if (diff != 0)
                {
                    opt.Tooltips.Add($"{(diff > 0 ? "+" : "")}{diff}% 速度");
                    opt.TooltipColors.Add(diff > 0 ? green : red);
                }
            }

            if (crt != 0)
            {
                opt.Tooltips.Add($"{(crt > 0 ? "+" : "")}{crt}% 暴击率");
                opt.TooltipColors.Add(crt > 0 ? green : red);
            }

            if (size != 1f)
            {
                int diff = (int)Math.Round((size - 1f) * 100f);
                if (diff != 0)
                {
                    opt.Tooltips.Add($"{(diff > 0 ? "+" : "")}{diff}% 大小");
                    opt.TooltipColors.Add(diff > 0 ? green : red);
                }
            }

            if (kb != 1f)
            {
                int diff = (int)Math.Round((kb - 1f) * 100f);
                if (diff != 0)
                {
                    opt.Tooltips.Add($"{(diff > 0 ? "+" : "")}{diff}% 击退");
                    opt.TooltipColors.Add(diff > 0 ? green : red);
                }
            }

            if (mcst != 1f)
            {
                int diff = (int)Math.Round((mcst - 1f) * 100f);
                if (diff != 0)
                {
                    opt.Tooltips.Add($"{(diff < 0 ? "" : "+")}{diff}% 魔力消耗");
                    opt.TooltipColors.Add(diff < 0 ? green : red);
                }
            }

            if (shtspd != 1f)
            {
                int diff = (int)Math.Round((shtspd - 1f) * 100f);
                if (diff != 0)
                {
                    opt.Tooltips.Add($"{(diff > 0 ? "+" : "")}{diff}% 射速");
                    opt.TooltipColors.Add(diff > 0 ? green : red);
                }
            }

            if (arpen != 0)
            {
                opt.Tooltips.Add($"{(arpen > 0 ? "+" : "")}{arpen} 护甲穿透");
                opt.TooltipColors.Add(arpen > 0 ? green : red);
            }

            if (tagdmg != 0)
            {
                opt.Tooltips.Add($"{(tagdmg > 0 ? "+" : "")}{tagdmg} 仆从标记伤害");
                opt.TooltipColors.Add(tagdmg > 0 ? green : red);
            }
        }

        /// <summary>
        /// 计算当前物品单次重铸所需金币费用（与原版折算公式完全一致）
        /// </summary>
        public static long GetSingleReforgeCost(Item item, Player player)
        {
            if (item == null || item.IsAir) return 0;
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
        /// 绘制重铸前缀网格面板与悬停 Tooltip
        /// </summary>
        public static void DrawReforgeUI(SpriteBatch spriteBatch)
        {
            if (!Enable.val || !Main.InReforgeMenu || !Main.playerInventory)
            {
                SelectedPrefixId = 0;
                lastItemType = -1;
                return;
            }

            Item item = Main.reforgeItem;
            if (item == null || item.IsAir || !item.CanHavePrefixes())
            {
                SelectedPrefixId = 0;
                lastItemType = -1;
                return;
            }

            // 当重铸槽内物品更换时，重置选择
            if (item.type != lastItemType)
            {
                SelectedPrefixId = 0;
                lastItemType = item.type;
                cachedForType = -1;
            }

            List<PrefixOption> options = GetAvailablePrefixes(item);
            if (options.Count == 0) return;

            // UI 布局参数：重铸槽位于 (50, 270)，在 Y = 338 处展开前缀列表
            int startX = 50;
            int startY = 338;
            int columns = 4;
            int btnW = 84;
            int btnH = 26;
            int gapX = 4;
            int gapY = 4;

            int rows = (options.Count + columns - 1) / columns;
            int totalW = columns * btnW + (columns - 1) * gapX;
            int totalH = rows * btnH + (rows - 1) * gapY;

            // 绘制主背景底板
            Rectangle panelRect = new Rectangle(startX - 6, startY - 24, totalW + 12, totalH + 30);
            DrawPanel(spriteBatch, panelRect, new Color(16, 20, 36, 225), new Color(50, 68, 120, 220), 2);

            // 绘制顶部标题
            string headerText = SelectedPrefixId > 0
                ? $"目标: [c/FFD700:{Lang.prefix[SelectedPrefixId].Value}] (点击重铸锤自动重铸)"
                : "选择目标前缀 (点击选定 / 点击重铸锤自动)";
            Terraria.Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, headerText, startX, startY - 20, Color.White, Color.Black, Vector2.Zero, 0.75f);

            PrefixOption hoveredOpt = null;
            Rectangle hoveredBtnRect = Rectangle.Empty;

            for (int i = 0; i < options.Count; i++)
            {
                PrefixOption opt = options[i];
                int col = i % columns;
                int row = i / columns;
                int x = startX + col * (btnW + gapX);
                int y = startY + row * (btnH + gapY);
                Rectangle btnRect = new Rectangle(x, y, btnW, btnH);

                bool isHover = btnRect.Contains(Main.mouseX, Main.mouseY);
                bool isSelected = (SelectedPrefixId == opt.PrefixId);
                bool isCurrent = (item.prefix == opt.PrefixId);

                // 背景与边框颜色
                Color bgColor = new Color(26, 32, 54, 210);
                Color borderColor = new Color(45, 60, 100, 180);
                int borderWidth = 1;

                if (opt.IsTopTier)
                {
                    bgColor = new Color(38, 36, 20, 220);
                    borderColor = new Color(210, 175, 40, 220);
                }

                if (isCurrent)
                {
                    bgColor = new Color(30, 48, 40, 220);
                    borderColor = new Color(70, 160, 90, 200);
                }

                if (isHover)
                {
                    bgColor = Color.Lerp(bgColor, Color.White, 0.15f);
                    borderColor = Color.Lerp(borderColor, Color.White, 0.4f);
                    borderWidth = 2;
                }

                if (isSelected)
                {
                    bgColor = new Color(60, 50, 20, 240);
                    borderColor = Color.Gold;
                    borderWidth = 2;
                }

                DrawPanel(spriteBatch, btnRect, bgColor, borderColor, borderWidth);

                // 绘制前缀文字
                Color textColor = Color.White;
                if (opt.IsTopTier) textColor = Color.Gold;
                else if (opt.ValueMultiplier < 1.0f) textColor = Color.LightCoral;

                if (isCurrent) textColor = new Color(130, 240, 150);

                string displayText = opt.Name;
                if (opt.IsTopTier) displayText = "★" + displayText;
                if (isCurrent) displayText += "(当前)";

                Vector2 textSize = FontAssets.MouseText.Value.MeasureString(displayText) * 0.72f;
                Vector2 textPos = new Vector2(btnRect.X + (btnRect.Width - textSize.X) / 2f, btnRect.Y + (btnRect.Height - textSize.Y) / 2f - 1f);
                Terraria.Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.MouseText.Value, displayText, textPos.X, textPos.Y, textColor, Color.Black, Vector2.Zero, 0.72f);

                // 交互响应
                if (isHover && !PlayerInput.IgnoreMouseInterface)
                {
                    Main.LocalPlayer.mouseInterface = true;
                    hoveredOpt = opt;
                    hoveredBtnRect = btnRect;

                    if (Main.mouseLeft && Main.mouseLeftRelease)
                    {
                        SoundEngine.PlaySound(SoundID.MenuTick);
                        if (SelectedPrefixId == opt.PrefixId)
                        {
                            SelectedPrefixId = 0; // 取消选中
                        }
                        else
                        {
                            SelectedPrefixId = opt.PrefixId; // 选中
                        }
                    }
                }
            }

            // 绘制悬停 Tooltip 详情面板
            if (hoveredOpt != null)
            {
                DrawPrefixTooltip(spriteBatch, hoveredOpt, item, hoveredBtnRect);
            }
        }

        /// <summary>
        /// 绘制前缀浮动详细属性面板
        /// </summary>
        private static void DrawPrefixTooltip(SpriteBatch spriteBatch, PrefixOption opt, Item item, Rectangle btnRect)
        {
            List<string> lines = new List<string>();
            List<Color> colors = new List<Color>();

            // 1. 标题行
            string title = opt.Name;
            if (opt.IsTopTier) title += " [c/FFD700:(顶级品质)]";
            else if (opt.ValueMultiplier > 1f) title += " [c/80E080:(正向品质)]";
            else if (opt.ValueMultiplier < 1f) title += " [c/FFA0A0:(负向品质)]";
            lines.Add(title);
            colors.Add(opt.IsTopTier ? Color.Gold : Color.White);

            // 2. 词条属性行
            if (opt.Tooltips.Count > 0)
            {
                for (int i = 0; i < opt.Tooltips.Count; i++)
                {
                    lines.Add(opt.Tooltips[i]);
                    colors.Add(opt.TooltipColors[i]);
                }
            }
            else
            {
                lines.Add("无额外属性变动");
                colors.Add(Color.LightGray);
            }

            // 3. 提示行
            lines.Add("----------------");
            colors.Add(Color.Gray * 0.7f);

            long cost = GetSingleReforgeCost(item, Main.LocalPlayer);
            lines.Add($"单次费用: {GetCoinString(cost)}");
            colors.Add(Color.LightGoldenrodYellow);

            if (SelectedPrefixId == opt.PrefixId)
            {
                lines.Add("[已锁定为目标] 点击重铸锤自动重铸");
                colors.Add(Color.Gold);
            }
            else
            {
                lines.Add("左键点击锁定该前缀为目标");
                colors.Add(Color.LightSkyBlue);
            }

            // 计算面板尺寸
            float maxW = 0f;
            float totalH = 0f;
            float lineH = 20f;
            for (int i = 0; i < lines.Count; i++)
            {
                Vector2 sz = FontAssets.MouseText.Value.MeasureString(lines[i]) * 0.75f;
                if (sz.X > maxW) maxW = sz.X;
                totalH += lineH;
            }

            int tipX = Main.mouseX + 16;
            int tipY = Main.mouseY + 16;
            int pad = 8;
            Rectangle tipRect = new Rectangle(tipX, tipY, (int)maxW + pad * 2, (int)totalH + pad * 2);

            // 屏幕边缘防越界修正
            if (tipRect.Right > Main.screenWidth - 10)
                tipRect.X = Main.screenWidth - tipRect.Width - 10;
            if (tipRect.Bottom > Main.screenHeight - 10)
                tipRect.Y = Main.screenHeight - tipRect.Height - 10;

            DrawPanel(spriteBatch, tipRect, new Color(14, 18, 30, 240), new Color(70, 90, 140, 230), 2);

            float curY = tipRect.Y + pad;
            for (int i = 0; i < lines.Count; i++)
            {
                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch,
                    FontAssets.MouseText.Value,
                    lines[i],
                    new Vector2(tipRect.X + pad, curY),
                    colors[i],
                    0f,
                    Vector2.Zero,
                    new Vector2(0.75f)
                );
                curY += lineH;
            }
        }

        /// <summary>
        /// 格式化金币文本
        /// </summary>
        private static string GetCoinString(long money)
        {
            long platinum = money / 1000000;
            money %= 1000000;
            long gold = money / 10000;
            money %= 10000;
            long silver = money / 100;
            long copper = money % 100;

            string res = "";
            if (platinum > 0) res += $"[i/s1:{ItemID.PlatinumCoin}]{platinum} ";
            if (gold > 0 || platinum > 0) res += $"[i/s1:{ItemID.GoldCoin}]{gold} ";
            if (silver > 0 || gold > 0 || platinum > 0) res += $"[i/s1:{ItemID.SilverCoin}]{silver} ";
            res += $"[i/s1:{ItemID.CopperCoin}]{copper}";
            return res;
        }

        /// <summary>
        /// 执行自动连续重铸模拟（真实计费 + 1000 次安全迭代上限）
        /// </summary>
        public static void PerformAutoReforge(Item item, int targetPrefix)
        {
            if (item == null || item.IsAir || targetPrefix <= 0) return;

            long singleCost = GetSingleReforgeCost(item, Main.LocalPlayer);
            int rolls = 0;
            bool reachedTarget = false;
            bool outOfMoney = false;
            bool hitLimit = false;

            while (rolls < 1000)
            {
                // 第 1 次重铸已经在原版点击重铸锤的那一刻扣除了单次费用；
                // 仅从第 2 次开始需要手动调用 BuyItem 扣费。
                if (rolls > 0)
                {
                    if (!Main.LocalPlayer.BuyItem(singleCost))
                    {
                        outOfMoney = true;
                        break;
                    }
                }

                item.ResetPrefix();
                bool isTop;
                item.Prefix(-2, out isTop);
                rolls++;

                if (item.prefix == targetPrefix)
                {
                    reachedTarget = true;
                    break;
                }
            }

            if (!reachedTarget && rolls >= 1000)
            {
                hitLimit = true;
            }

            // 结算反馈与音效
            if (reachedTarget)
            {
                string targetName = (targetPrefix < Lang.prefix.Length && Lang.prefix[targetPrefix] != null) ? Lang.prefix[targetPrefix].Value : $"Prefix_{targetPrefix}";
                bool isTopTier = false;
                List<PrefixOption> opts = GetAvailablePrefixes(item);
                PrefixOption match = opts.Find(o => o.PrefixId == targetPrefix);
                if (match?.IsTopTier == true) isTopTier = true;

                PopupText.NewText(isTopTier ? PopupTextContext.ItemReforge_Best : PopupTextContext.ItemReforge, item, Main.LocalPlayer.Center, item.stack, noStack: true);

                if (isTopTier)
                {
                    SoundEngine.PlaySound(SoundID.BestReforge);
                    Main.reforgeCooldown = 60;
                    ParticleOrchestrator.RequestParticleSpawn(clientOnly: true, ParticleOrchestraType.BestReforge, new ParticleOrchestraSettings
                    {
                        PositionInWorld = Main.LocalPlayer.Bottom
                    });
                }
                else
                {
                    SoundEngine.PlaySound(SoundID.Item37);
                }

                Main.NewText($"[i:{item.type}] 自动重铸成功！经过 {rolls} 次尝试，成功获得【{targetName}】！", new Color(255, 215, 0));
            }
            else if (outOfMoney)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                string currentPrefixName = item.prefix > 0 && item.prefix < Lang.prefix.Length ? Lang.prefix[item.prefix].Value : "无前缀";
                Main.NewText($"[i:{item.type}] 金币不足！已连续重铸 {rolls} 次，金币耗尽，停留在当前前缀【{currentPrefixName}】。", new Color(255, 160, 50));
            }
            else if (hitLimit)
            {
                SoundEngine.PlaySound(SoundID.MenuClose);
                string currentPrefixName = item.prefix > 0 && item.prefix < Lang.prefix.Length ? Lang.prefix[item.prefix].Value : "无前缀";
                Main.NewText($"[i:{item.type}] 已达到单次安全重铸上限 (1000 次)，自动停止保护，停留在【{currentPrefixName}】。", new Color(255, 215, 80));
            }
        }

        /// <summary>
        /// 绘制纯色边框面板辅助方法
        /// </summary>
        private static void DrawPanel(SpriteBatch sb, Rectangle rect, Color bgColor, Color borderColor, int borderWidth = 1)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel, rect, bgColor);
            if (borderWidth > 0 && borderColor.A > 0)
            {
                sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, borderWidth), borderColor);
                sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - borderWidth, rect.Width, borderWidth), borderColor);
                sb.Draw(pixel, new Rectangle(rect.X, rect.Y, borderWidth, rect.Height), borderColor);
                sb.Draw(pixel, new Rectangle(rect.Right - borderWidth, rect.Y, borderWidth, rect.Height), borderColor);
            }
        }
    }
}
