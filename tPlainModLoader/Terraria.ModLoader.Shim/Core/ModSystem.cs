using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace Terraria.ModLoader
{
    /// <summary>
    /// tModLoader 全局系统级生命周期基类
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

        public virtual void AddRecipes()
        {
        }

        public virtual void PostAddRecipes()
        {
        }
    }
}
