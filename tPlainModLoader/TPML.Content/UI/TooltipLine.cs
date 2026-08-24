using Microsoft.Xna.Framework;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 物品悬浮提示行对象
    /// </summary>
    public class TooltipLine
    {
        public readonly Mod Mod;
        public readonly string Name;
        public string Text;
        public bool IsModifier;
        public bool IsModifierBad;
        public bool OneDropLogo;
        public Color? OverrideColor;

        public TooltipLine(Mod mod, string name, string text)
        {
            Mod = mod;
            Name = name;
            Text = text;
        }
    }
}
