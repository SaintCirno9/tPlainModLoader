using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Terraria.UI.Chat;

namespace RecipeBrowser.TagHandlers
{
    public class LinkTagHandler : ITagHandler
    {
        private class LinkSnippet : TextSnippet
        {
            private string url;

            public LinkSnippet(string text, string url, Color color)
                : base(text, color)
            {
                this.url = url;
                CheckForHover = true;
            }

            public override void OnClick()
            {
                if (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }
        }

        TextSnippet ITagHandler.Parse(string text, Color baseColor, string options)
        {
            string url = options ?? text;
            return new LinkSnippet(text, url, Color.SkyBlue);
        }

        public static string GenerateTag(string text, string url)
        {
            return $"[l/{url}:{text}]";
        }
    }
}
