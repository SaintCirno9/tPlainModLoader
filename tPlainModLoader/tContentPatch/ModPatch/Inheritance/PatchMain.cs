using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.UI;

namespace tContentPatch
{
    public abstract class PatchMain
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// 进入世界时, 仅在单人和客户端有效
        /// </summary>
        public virtual void OnEnterWorld() { }
        /// <summary>
        /// <see cref="Main.Update(GameTime)"/>前调用
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void UpdatePrefix(GameTime gameTime) { }
        /// <summary>
        /// <see cref="Main.Update(GameTime)"/>后调用
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void UpdatePostfix(GameTime gameTime) { }
        /// <summary>
        /// 用于修改或添加ui
        /// </summary>
        /// <param name="gameInterfaceLayers"></param>
        public virtual void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers) { }
        public virtual void UpdateUIStatesPrefix(GameTime gameTime) { }
        public virtual void UpdateUIStatesPostfix(GameTime gameTime) { }
        public virtual void DoUpdateInWorldPrefix(Stopwatch sw) { }
        public virtual void DoUpdateInWorldPostfix(Stopwatch sw) { }
        public virtual void DrawMapPostfix(GameTime gameTime) { }
        public virtual void DrawMenuPrefix(GameTime gameTime) { }
        public virtual void MouseText_DrawItemTooltip_GetLinesInfoPostfix(Item item, ref int yoyoLogo, ref int researchLine, ref float oldKB, ref int numLines, ref string[] toolTipLine, ref bool[] preFixLine, ref bool[] badPreFixLine) { }
    }
}
