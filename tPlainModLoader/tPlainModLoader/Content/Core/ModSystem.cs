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
    }
}
