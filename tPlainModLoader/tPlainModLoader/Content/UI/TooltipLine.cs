using Microsoft.Xna.Framework;

namespace TPML.Content
{
    /// <summary>
    /// 物品提示行对象（对齐 tModLoader TooltipLine）
    /// 作者: SaintCirno9
    /// </summary>
    public class TooltipLine
    {
        public Mod Mod { get; }
        public string Name { get; }
        public string Text { get; set; }
        public Color? OverrideColor { get; set; }
        public bool IsModifier { get; set; }
        public bool IsModifierBad { get; set; }

        public TooltipLine(Mod mod, string name, string text)
        {
            Mod = mod;
            Name = name;
            Text = text;
        }
    }
}

namespace TPML.Content.UI
{
    /// <summary>
    /// 兼容存根以确保历史 using TPML.Content.UI 正常解析
    /// </summary>
    internal static class NamespaceDoc
    {
    }
}
