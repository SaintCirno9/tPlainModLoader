using Microsoft.Xna.Framework;
using System;
using System.IO;
using TPML.UI;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using UITextBox = TPML.UI.UITextBox;

namespace WandsTool.Content.Structure.UI
{
    /// <summary>
    /// 蓝图条目卡片 UI（支持载入放置、原地重命名与删除）
    /// </summary>
    public class UIBlueprintCard : UIPanel
    {
        private string filePath;
        private Action onReloadNeeded;
        private string currentTitleText;
        private StructureData _loadedData = null;
        private bool _isEditing = false;

        public UIBlueprintCard(string file, Action onReload)
        {
            filePath = file;
            onReloadNeeded = onReload;

            Width.Precent = 1;
            Height.Pixels = 68;
            SetPadding(8);
            BackgroundColor = new Color(30, 40, 70) * 0.85f;
            BorderColor = new Color(60, 90, 150) * 0.9f;

            RenderNormalView();
        }

        /// <summary>
        /// 渲染常规展示视图
        /// </summary>
        private void RenderNormalView()
        {
            RemoveAllChildren();
            _isEditing = false;

            string fileName = Path.GetFileName(filePath);
            currentTitleText = Path.GetFileNameWithoutExtension(filePath);
            string timeText = File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd HH:mm");

            // 尝试读取第一层 JSON 元数据以显示尺寸（引用保留供悬浮材料清单复用，避免额外 IO）
            string sizeInfo = "";
            try
            {
                StructureData quickData = StructureStorage.Load(filePath);
                _loadedData = quickData;
                if (quickData != null)
                {
                    if (!string.IsNullOrEmpty(quickData.Name)) currentTitleText = quickData.Name;
                    sizeInfo = $" ({quickData.Width}×{quickData.Height})";
                    if (!string.IsNullOrEmpty(quickData.BuildTime)) timeText = quickData.BuildTime;
                }
            }
            catch { }

            // 标题与尺寸（使用标准 MouseText 字体，字号 0.85f，非大标题）
            UIText uiTitle = new UIText($"{currentTitleText}{sizeInfo}", 0.85f, false)
            {
                TextColor = Color.Cyan,
                HAlign = 0,
                VAlign = 0
            };

            // 时间与文件名
            UIText uiSub = new UIText($"文件: {fileName}   时间: {timeText}", 0.75f)
            {
                TextColor = Color.LightGray * 0.75f,
                HAlign = 0,
                VAlign = 1
            };

            // 右侧操作按钮组
            UIStackPanel btnGroup = new UIStackPanel
            {
                HAlign = 1,
                VAlign = 0.5f,
                Horizontal = true,
                ItemMargin = 6,
                Height = { Pixels = 30 },
                IsAutoUpdateSize = true
            };

            // 【载入放置】按钮
            UIButton1 btnLoad = new UIButton1("载入放置", 0.8f)
            {
                Height = { Pixels = 30 },
                EnableColorBack = new Color(35, 140, 70) * 0.9f,
                MouseOverColorBack = new Color(45, 180, 90)
            };
            btnLoad.OnLeftClick += (e, s) =>
            {
                StructureData data = StructureStorage.Load(filePath);
                if (data != null)
                {
                    StructureStorage.Clipboard = data;
                    gameMain.LastActiveStructureMode = gameMain.StructureMode.Copy;
                    gameMain.Wand_StructureMode = gameMain.StructureMode.Paste;
                    Main.NewText($"[魔杖] 已载入蓝图: {data.Name}，请在世界中点击左键放置（右键取消）", 100, 255, 150);

                    // 标记放置后/取消后自动重新显示蓝图管理器
                    wandsPanel.AutoReopenManagerAfterPlacement = true;

                    // 主动隐藏蓝图管理器面板与魔杖轮盘，让出完整视野
                    wandsPanel.BlueprintManager.Close();
                    wandsPanel.Instance?.Close();
                }
                else
                {
                    Main.NewText("[魔杖] 蓝图解析失败，文件可能损坏！", 255, 80, 80);
                }
            };

            // 【重命名】按钮
            UIButton1 btnRename = new UIButton1("重命名", 0.8f)
            {
                Height = { Pixels = 30 },
                EnableColorBack = new Color(40, 90, 150) * 0.85f,
                MouseOverColorBack = new Color(50, 120, 200)
            };
            btnRename.OnLeftClick += (e, s) =>
            {
                RenderEditView();
            };

            // 【删除】按钮
            UIButton1 btnDelete = new UIButton1("删除", 0.8f)
            {
                Height = { Pixels = 30 },
                EnableColorBack = new Color(150, 40, 40) * 0.8f,
                MouseOverColorBack = new Color(200, 50, 50)
            };
            btnDelete.OnLeftClick += (e, s) =>
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Main.NewText($"[魔杖] 蓝图已删除: {fileName}", 255, 180, 100);
                        onReloadNeeded?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Main.NewText($"[魔杖] 删除失败: {ex.Message}", 255, 60, 60);
                }
            };

            btnGroup.Append(btnLoad);
            btnGroup.Append(btnRename);
            btnGroup.Append(btnDelete);

            Append(uiTitle);
            Append(uiSub);
            Append(btnGroup);
        }

        /// <summary>
        /// 渲染原地重命名输入编辑视图
        /// </summary>
        private void RenderEditView()
        {
            RemoveAllChildren();
            _isEditing = true;

            // 1. 输入框
            TPML.UI.UITextBox txtName = new TPML.UI.UITextBox("输入新名称...")
            {
                Width = { Precent = 0.58f },
                Height = { Pixels = 32 },
                HAlign = 0,
                VAlign = 0.5f,
                BackgroundColor = new Color(15, 20, 38) * 0.95f,
                BorderColor = Color.Cyan * 0.9f,
                FocusedBorderColor = Color.Gold,
                TextColor = Color.White,
                HintColor = Color.LightSlateGray,
                CursorColor = Color.Cyan,
                TextScale = 0.85f
            };
            txtName.Text = currentTitleText;
            txtName.Focus = true;

            void SaveRenameAction(string name)
            {
                string newName = name?.Trim();
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    bool ok = StructureStorage.Rename(filePath, newName);
                    if (ok)
                    {
                        onReloadNeeded?.Invoke();
                        return;
                    }
                }
                RenderNormalView();
            }

            txtName.OnSubmit += (name) => SaveRenameAction(name);
            txtName.OnCancel += () => RenderNormalView();

            // 2. 确认与取消按钮组
            UIStackPanel btnGroup = new UIStackPanel
            {
                HAlign = 1,
                VAlign = 0.5f,
                Horizontal = true,
                ItemMargin = 6,
                Height = { Pixels = 32 },
                IsAutoUpdateSize = true
            };

            // 【确认保存】按钮
            UIButton1 btnSave = new UIButton1("保存", 0.8f)
            {
                Height = { Pixels = 32 },
                EnableColorBack = new Color(35, 140, 70) * 0.95f,
                MouseOverColorBack = new Color(45, 180, 90)
            };
            btnSave.OnLeftClick += (e, s) =>
            {
                SaveRenameAction(txtName.Text);
            };

            // 【取消】按钮
            UIButton1 btnCancel = new UIButton1("取消", 0.8f)
            {
                Height = { Pixels = 32 },
                EnableColorBack = new Color(100, 100, 110) * 0.85f,
                MouseOverColorBack = new Color(130, 130, 140)
            };
            btnCancel.OnLeftClick += (e, s) =>
            {
                RenderNormalView();
            };

            btnGroup.Append(btnSave);
            btnGroup.Append(btnCancel);

            Append(txtName);
            Append(btnGroup);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsMouseHovering)
            {
                BackgroundColor = new Color(45, 60, 100) * 0.95f;
                BorderColor = Color.Gold * 0.8f;

                // 悬停时通知材料清单悬浮面板（重命名编辑态不触发）
                if (!_isEditing && _loadedData != null)
                {
                    StructureMaterialSummary.NotifyHover(_loadedData);
                }
            }
            else
            {
                BackgroundColor = new Color(30, 40, 70) * 0.85f;
                BorderColor = new Color(60, 90, 150) * 0.9f;
            }
        }
    }
}
