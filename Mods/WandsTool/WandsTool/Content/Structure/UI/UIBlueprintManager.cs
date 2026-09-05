using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using TPML.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using TPML.Core.Pinyin;
using UITextBox = TPML.UI.UITextBox;

namespace WandsTool.Content.Structure.UI
{
    /// <summary>
    /// 建筑蓝图管理器窗口（游戏内浏览、载入、保存、管理、即时搜索与打开文件夹）
    /// </summary>
    public class UIBlueprintManager : UIWindow
    {
        private UIScrollViewer2 scrollList = null;
        private UIText statusText = null;
        private UITextBox txtSearch = null;
        private string currentSearchFilter = string.Empty;

        public UIBlueprintManager() : base("建筑蓝图管理器 (Blueprint Library)", 560, 560)
        {
            BackgroundColor = new Color(20, 25, 45) * 0.95f;
            BorderColor = new Color(50, 80, 150) * 0.9f;

            // 1. 顶部操作工具栏
            UIStackPanel headerBar = new UIStackPanel
            {
                Width = { Precent = 1 },
                Height = { Pixels = 34 },
                Horizontal = true,
                ItemMargin = 8,
                IsAutoUpdateSize = true
            };

            UIButton btnRefresh = new UIButton("刷新", 0.85f)
            {
                Height = { Pixels = 34 },
                EnableColorBack = new Color(40, 70, 130) * 0.9f
            };
            btnRefresh.OnLeftClick += (e, s) => RefreshList();

            UIButton btnSaveClip = new UIButton("保存剪贴板", 0.85f)
            {
                Height = { Pixels = 34 },
                EnableColorBack = new Color(40, 120, 70) * 0.9f
            };
            btnSaveClip.OnLeftClick += (e, s) =>
            {
                if (StructureStorage.Clipboard != null)
                {
                    bool ok = StructureStorage.Save(StructureStorage.Clipboard);
                    if (ok)
                    {
                        Main.NewText($"[魔杖] 蓝图已保存: {StructureStorage.Clipboard.Name}", 100, 255, 100);
                        RefreshList();
                    }
                }
                else
                {
                    Main.NewText("[魔杖] 剪贴板为空，请先复制或剪切建筑", 255, 100, 100);
                }
            };

            UIButton btnFolder = new UIButton("打开文件夹", 0.85f)
            {
                Height = { Pixels = 34 },
                EnableColorBack = new Color(100, 80, 40) * 0.9f
            };
            btnFolder.OnLeftClick += (e, s) => StructureStorage.OpenInExplorer();

            // 顶部搜索过滤输入框（通用 UITextBox）
            txtSearch = new UITextBox("搜索蓝图...")
            {
                Height = { Pixels = 34 },
                Width = { Pixels = 180 },
                TextColor = Color.White,
                HintColor = Color.LightSlateGray,
                CursorColor = Color.Cyan,
                FocusedBorderColor = Color.Cyan,
                TextScale = 0.85f,
                BackgroundColor = new Color(15, 20, 38) * 0.9f
            };
            txtSearch.OnTextChanged += (filter) =>
            {
                currentSearchFilter = filter?.Trim() ?? string.Empty;
                RefreshList();
            };

            headerBar.Append(btnRefresh);
            headerBar.Append(btnSaveClip);
            headerBar.Append(btnFolder);
            headerBar.Append(txtSearch);

            // 2. 中间可滚动蓝图列表
            scrollList = new UIScrollViewer2(true);
            scrollList.Width.Precent = 1f;
            scrollList.Top.Pixels = 42;
            scrollList.Height.Set(-75, 1f);
            scrollList.ItemMargin = 6;

            // 3. 底部状态栏
            statusText = new UIText("蓝图库就绪", 0.8f)
            {
                VAlign = 1,
                HAlign = 0,
                TextColor = Color.LightCyan * 0.8f
            };

            Child.Append(headerBar);
            Child.Append(scrollList);
            Child.Append(statusText);
        }

        public override void Open(UIElement windowParent)
        {
            base.Open(windowParent);
            RefreshList();
        }

        /// <summary>
        /// 重新加载并刷新蓝图列表（支持即时名称过滤）
        /// </summary>
        public void RefreshList()
        {
            if (scrollList == null) return;

            scrollList.ClearChild();
            List<string> files = StructureStorage.GetSavedBlueprintFiles();

            // 搜索词过滤（支持中英文直接包含、全拼与拼音首字母缩写）
            if (!string.IsNullOrEmpty(currentSearchFilter))
            {
                files = files.FindAll(file =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (PinyinHelper.Matches(fileName, currentSearchFilter))
                        return true;

                    try
                    {
                        StructureData data = StructureStorage.Load(file);
                        if (data != null && !string.IsNullOrEmpty(data.Name))
                        {
                            return PinyinHelper.Matches(data.Name, currentSearchFilter);
                        }
                    }
                    catch { }

                    return false;
                });
            }

            string clipInfo = StructureStorage.Clipboard != null
                ? $"[{StructureStorage.Clipboard.Name} ({StructureStorage.Clipboard.Width}×{StructureStorage.Clipboard.Height})]"
                : "无";
            statusText.SetText($"共找到 {files.Count} 个蓝图文件   |   当前剪贴板: {clipInfo}");

            if (files.Count == 0)
            {
                UIPanel emptyPanel = new UIPanel
                {
                    Width = { Precent = 1 },
                    Height = { Pixels = 120 },
                    BackgroundColor = new Color(30, 35, 60) * 0.8f,
                    BorderColor = Color.Gray * 0.5f
                };

                string emptyMsg = string.IsNullOrEmpty(currentSearchFilter)
                    ? "暂无已保存的蓝图文件。\n\n" +
                      "• 在游戏中打开魔杖轮盘，选择【结构框选复制】抓取建筑并点击【保存剪贴板】；\n" +
                      "• 或点击上方【打开文件夹】，将 .wstruct 或 .json 蓝图文件放入其中后点击刷新。"
                    : $"未找到包含 \"{currentSearchFilter}\" 的蓝图文件。\n\n• 请尝试其他关键词或清空搜索框。";

                UIText emptyText = new UIText(emptyMsg, 0.85f)
                {
                    TextColor = Color.LightGoldenrodYellow,
                    HAlign = 0.5f,
                    VAlign = 0.5f
                };

                emptyPanel.Append(emptyText);
                scrollList.AddChild(emptyPanel);
                return;
            }

            foreach (string file in files)
            {
                UIBlueprintCard card = new UIBlueprintCard(file, () =>
                {
                    RefreshList();
                });
                scrollList.AddChild(card);
            }
        }
    }
}
