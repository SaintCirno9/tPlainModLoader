using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TPML.Content.IO;
using Terraria.UI;

namespace TPML.Content
{
    /// <summary>
    /// TPML 全局系统级生命周期基类
    /// </summary>
    public abstract class ModSystem : ModType
    {
        public virtual void PostUpdateEverything()
        {
        }

        public virtual void PreUpdateWorld()
        {
        }

        public virtual void PostUpdateWorld()
        {
        }

        public virtual void UpdateUI(GameTime gameTime)
        {
        }

        public virtual void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
        }

        public virtual void PostDrawInterface(SpriteBatch spriteBatch)
        {
        }

        public virtual void OnWorldLoad()
        {
        }

        public virtual void OnWorldUnload()
        {
        }

        public virtual void SaveWorldData(TagCompound tag)
        {
        }

        public virtual void LoadWorldData(TagCompound tag)
        {
        }

        public virtual void PreWorldGen()
        {
        }

        public virtual void NetReceive(System.IO.BinaryReader reader)
        {
        }

        public virtual void NetSend(System.IO.BinaryWriter writer)
        {
        }

        public virtual void TileCountsAvailable(System.ReadOnlySpan<int> tileCounts)
        {
        }

        public virtual void AddRecipeGroups()
        {
        }

        public virtual void AddRecipes()
        {
        }

        public virtual void PostAddRecipes()
        {
        }

        public virtual void UpdatePrefix(GameTime gameTime) { }
        public virtual void UpdatePostfix(GameTime gameTime) { }
        public virtual void SetupDrawInterfaceLayersPostfix(List<GameInterfaceLayer> gameInterfaceLayers) { }
        public virtual void UpdateUIStatesPrefix(GameTime gameTime) { }
        public virtual void UpdateUIStatesPostfix(GameTime gameTime) { }
        public virtual void DoUpdateInWorldPrefix() { }
        public virtual void DoUpdateInWorldPostfix() { }
        public virtual void DrawMapPostfix(GameTime gameTime) { }
        public virtual void DrawMenuPrefix(GameTime gameTime) { }
        public virtual void DrawMenuPostfix(GameTime gameTime) { }
        public virtual void OnEnterWorld() { }
        public virtual void OnLeaveWorld() { }
        public virtual void DoDrawPrefix(GameTime gameTime) { }
        public virtual void DoDrawPostfix(GameTime gameTime) { }
        public virtual Vector2 PlayerFocusedScreenPosition(Vector2 origin, Vector2 modifi) => modifi;

        public override void Load()
        {
            base.Load();
            Initialize();
        }

        public virtual void Initialize() { }
        public virtual void MouseText_DrawItemTooltip_GetLinesInfoPostfix(Terraria.Item item, ref int yoyoLogo, ref float oldKB, ref int numLines, ref string[] toolTipLine, ref Color[] lineColors) { }
    }
}
