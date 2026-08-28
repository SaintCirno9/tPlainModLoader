using System.Diagnostics;
using Microsoft.Xna.Framework;
using RecipeBrowser.Common;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    /// <summary>
    /// [l] 链接标签处理器 —— 对齐原版参数顺序与悬停/打开行为
    /// 注：原版用 Utils.OpenToURL（tML 扩展），TPML 用 Process.Start 打开
    /// 作者: SaintCirno9
    /// </summary>
    public class LinkTagHandler : ITagHandler
    {
        private class LinkSnippet : TextSnippet
        {
            private string url;

            public LinkSnippet(string url, string text)
                : base(text, Color.LightBlue)
            {
                this.url = url;
                CheckForHover = true;
            }

            public override void OnHover()
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    UICommon.TooltipMouseText(url);
                }
            }

            public override void OnClick()
            {
                if (string.IsNullOrEmpty(url)) return;
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://" + url,
                        UseShellExecute = true
                    });
                }
                catch { }
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            return new LinkSnippet(options, text);
        }

        public static string GenerateTag(string url, string text)
        {
            return $"[l/{url}:{text}]";
        }
    }
}
