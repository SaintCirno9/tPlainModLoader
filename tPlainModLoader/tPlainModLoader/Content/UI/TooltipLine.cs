using Microsoft.Xna.Framework;

namespace TPML.Content.UI
{
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
