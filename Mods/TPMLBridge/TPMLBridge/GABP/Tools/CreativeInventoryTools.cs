using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Terraria;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// 创造模式物品浏览器 UI 诊断、切换、搜索与输入焦点控制工具
    /// 作者: SaintCirno9
    /// </summary>
    public static class CreativeInventoryTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/test_creative_inventory",
                    Description = "诊断创造模式物品浏览器 UI 状态（是否打开、搜索关键词、当前匹配物品数、输入框 Focus 状态与尺寸）。",
                    Tags = new List<string> { "read-only", "ui", "diagnostic" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/toggle_creative_inventory",
                    Description = "打开或关闭创造模式物品浏览器 UI 窗口。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            open = new { type = "boolean", description = "true 为打开，false 为关闭；不传则切换开/关状态" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/set_creative_search",
                    Description = "向创造模式物品浏览器输入搜索关键词，并立即执行过滤返回匹配结果摘要。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        required = new[] { "query" },
                        properties = new
                        {
                            query = new { type = "string", description = "要搜索的物品名称（中文/英文）或纯数字 ItemID" }
                        }
                    }
                },
                new GABPToolDescriptor
                {
                    Name = "tpml/focus_creative_search",
                    Description = "切换或设置创造模式搜索框的键盘输入焦点 (Focus)。",
                    Tags = new List<string> { "write", "ui" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            focus = new { type = "boolean", description = "是否获得输入焦点（默认 true）" }
                        }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/test_creative_inventory":
                case "tpml_test_creative_inventory":
                    return await MainThreadQueue.EnqueueAsync(() => TestCreativeInventory());

                case "tpml/toggle_creative_inventory":
                case "tpml_toggle_inventory":
                case "tpml_toggle_creative_inventory":
                    {
                        bool? open = args?["open"]?.Value<bool?>();
                        return await MainThreadQueue.EnqueueAsync(() => ToggleCreativeInventory(open));
                    }

                case "tpml/set_creative_search":
                case "tpml_set_creative_search":
                    {
                        string query = args?["query"]?.ToString();
                        return await MainThreadQueue.EnqueueAsync(() => SetCreativeSearch(query));
                    }

                case "tpml/focus_creative_search":
                case "tpml_focus_creative_search":
                    {
                        bool focus = args?["focus"]?.Value<bool?>() ?? true;
                        return await MainThreadQueue.EnqueueAsync(() => FocusCreativeSearch(focus));
                    }

                default:
                    return null;
            }
        }

        public static Type GetCreativeInventoryType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = asm.GetType("OptimizeAndTool.Content.Creative.CreativeInventory");
                    if (type != null) return type;

                    foreach (var t in asm.GetTypes())
                    {
                        if (t.FullName == "OptimizeAndTool.Content.Creative.CreativeInventory" || t.Name == "CreativeInventory")
                        {
                            return t;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        public static object TestCreativeInventory()
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { isAvailable = false, message = "未检测到 OptimizeAndTool 模组程序集" };
            }

            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var isHoveringProp = type.GetProperty("IsHovering", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var uiProp = type.GetProperty("UI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            bool isOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            bool isHovering = (bool)(isHoveringProp?.GetValue(null) ?? false);
            string searchText = null;
            int matchedCount = 0;
            bool textBoxFocus = false;
            string textBoxText = null;

            var uiObj = uiProp?.GetValue(null);
            if (uiObj != null)
            {
                var uiType = uiObj.GetType();
                var searchField = uiType.GetField("Search_Text", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                searchText = searchField?.GetValue(uiObj)?.ToString();

                var matchedProp = uiType.GetProperty("MatchedCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                matchedCount = (int)(matchedProp?.GetValue(uiObj) ?? 0);

                var searchTextBoxProp = uiType.GetProperty("SearchTextBox", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tbObj = searchTextBoxProp?.GetValue(uiObj);
                if (tbObj != null)
                {
                    var tbType = tbObj.GetType();
                    var focusProp = tbType.GetProperty("Focus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var textProp = tbType.GetProperty("Text", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    textBoxFocus = (bool)(focusProp?.GetValue(tbObj) ?? false);
                    textBoxText = textProp?.GetValue(tbObj)?.ToString();
                }
            }

            return new
            {
                isAvailable = true,
                isOpen,
                isHovering,
                searchText,
                textBoxText,
                textBoxFocus,
                writingText = Terraria.GameInput.PlayerInput.WritingText,
                currentInputTaker = Main.CurrentInputTextTakerOverride != null ? Main.CurrentInputTextTakerOverride.GetType().Name : null,
                matchedCount
            };
        }

        public static object ToggleCreativeInventory(bool? open)
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { success = false, message = "未找到 OptimizeAndTool 模组程序集" };
            }

            var switchMethod = type.GetMethod("SwitchOpenOrClose", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool curOpen = (bool)(isOpenProp?.GetValue(null) ?? false);

            if (!open.HasValue || open.Value != curOpen)
            {
                switchMethod?.Invoke(null, null);
            }

            bool finalOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            return new
            {
                success = true,
                isOpen = finalOpen,
                message = finalOpen ? "创造模式物品浏览器已打开" : "创造模式物品浏览器已关闭"
            };
        }

        public static object SetCreativeSearch(string query)
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { success = false, message = "未找到创造模式物品浏览器" };
            }

            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool curOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            if (!curOpen)
            {
                var switchMethod = type.GetMethod("SwitchOpenOrClose", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                switchMethod?.Invoke(null, null);
            }

            var uiProp = type.GetProperty("UI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var uiObj = uiProp?.GetValue(null);
            if (uiObj != null)
            {
                var uiType = uiObj.GetType();
                var applyMethod = uiType.GetMethod("ApplySearchImmediate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                applyMethod?.Invoke(uiObj, new object[] { query });

                var matchedProp = uiType.GetProperty("MatchedCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                int matchedCount = (int)(matchedProp?.GetValue(uiObj) ?? 0);

                return new
                {
                    success = true,
                    query,
                    matchedCount,
                    message = $"已搜索 [{query}]，共匹配 {matchedCount} 个物品"
                };
            }

            return new { success = false, message = "创造模式物品浏览器 UI 实例为空" };
        }

        public static object FocusCreativeSearch(bool focus)
        {
            Type type = GetCreativeInventoryType();
            if (type == null)
            {
                return new { success = false, message = "未找到创造模式物品浏览器" };
            }

            var isOpenProp = type.GetProperty("IsOpen", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            bool curOpen = (bool)(isOpenProp?.GetValue(null) ?? false);
            if (!curOpen && focus)
            {
                var switchMethod = type.GetMethod("SwitchOpenOrClose", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                switchMethod?.Invoke(null, null);
            }

            var uiProp = type.GetProperty("UI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var uiObj = uiProp?.GetValue(null);
            if (uiObj != null)
            {
                var uiType = uiObj.GetType();
                var searchTextBoxProp = uiType.GetProperty("SearchTextBox", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tbObj = searchTextBoxProp?.GetValue(uiObj);
                if (tbObj != null)
                {
                    var tbType = tbObj.GetType();
                    var focusProp = tbType.GetProperty("Focus", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    focusProp?.SetValue(tbObj, focus);

                    return new
                    {
                        success = true,
                        focus,
                        writingText = Terraria.GameInput.PlayerInput.WritingText,
                        currentInputTaker = Main.CurrentInputTextTakerOverride != null ? Main.CurrentInputTextTakerOverride.GetType().Name : null,
                        message = focus ? "已激活搜索框输入焦点" : "已释放搜索框焦点"
                    };
                }
            }

            return new { success = false, message = "未找到创造模式物品浏览器搜索框" };
        }
    }
}
