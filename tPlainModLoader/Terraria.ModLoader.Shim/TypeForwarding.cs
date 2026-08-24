using System.Runtime.CompilerServices;

// 将官方 tModLoader.dll 中所包含或合并的原版 Terraria 类型自动转发至 Terraria.exe 程序集
[assembly: TypeForwardedTo(typeof(Terraria.Main))]
[assembly: TypeForwardedTo(typeof(Terraria.Player))]
[assembly: TypeForwardedTo(typeof(Terraria.Item))]
[assembly: TypeForwardedTo(typeof(Terraria.NPC))]
[assembly: TypeForwardedTo(typeof(Terraria.Projectile))]
[assembly: TypeForwardedTo(typeof(Terraria.Dust))]
[assembly: TypeForwardedTo(typeof(Terraria.Tile))]
[assembly: TypeForwardedTo(typeof(Terraria.WorldGen))]
[assembly: TypeForwardedTo(typeof(Terraria.Recipe))]
[assembly: TypeForwardedTo(typeof(Terraria.Chest))]
[assembly: TypeForwardedTo(typeof(Terraria.Lighting))]
[assembly: TypeForwardedTo(typeof(Terraria.Lang))]
[assembly: TypeForwardedTo(typeof(Terraria.Utils))]
[assembly: TypeForwardedTo(typeof(Terraria.Collision))]
[assembly: TypeForwardedTo(typeof(Terraria.Gore))]
[assembly: TypeForwardedTo(typeof(Terraria.NetMessage))]
[assembly: TypeForwardedTo(typeof(Terraria.MessageBuffer))]
[assembly: TypeForwardedTo(typeof(Terraria.RemoteClient))]

// Terraria.ID
[assembly: TypeForwardedTo(typeof(Terraria.ID.ItemID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.NPCID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.ProjectileID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.TileID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.WallID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.BuffID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.SoundID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.PrefixID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.Colors))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.MessageID))]
[assembly: TypeForwardedTo(typeof(Terraria.ID.SetFactory))]

// Terraria.UI
[assembly: TypeForwardedTo(typeof(Terraria.UI.UIState))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.UIElement))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.UserInterface))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.GameInterfaceLayer))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.LegacyGameInterfaceLayer))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.GameInterfaceDrawMethod))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.InterfaceScaleType))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.ItemSlot))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.ItemTooltip))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.StyleDimension))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.CalculatedStyle))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.UIMouseEvent))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.UIAlign))]
[assembly: TypeForwardedTo(typeof(Terraria.UI.SnapPoint))]

// Terraria.GameContent
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.TextureAssets))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.FontAssets))]

// Terraria.GameContent.UI.Elements
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIItemSlot))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIImage))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIImageButton))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIPanel))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIText))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UITextPanel<>))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIList))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIScrollbar))]
[assembly: TypeForwardedTo(typeof(Terraria.GameContent.UI.Elements.UIKeybindingListItem))]

// Terraria.Audio
[assembly: TypeForwardedTo(typeof(Terraria.Audio.SoundEngine))]
[assembly: TypeForwardedTo(typeof(Terraria.Audio.SoundStyle))]
[assembly: TypeForwardedTo(typeof(Terraria.Audio.LegacySoundStyle))]

// Terraria.DataStructures
[assembly: TypeForwardedTo(typeof(Terraria.DataStructures.PlayerDeathReason))]
[assembly: TypeForwardedTo(typeof(Terraria.DataStructures.DrawData))]
[assembly: TypeForwardedTo(typeof(Terraria.DataStructures.Point16))]

// Terraria.GameInput
[assembly: TypeForwardedTo(typeof(Terraria.GameInput.PlayerInput))]
[assembly: TypeForwardedTo(typeof(Terraria.GameInput.TriggersSet))]
[assembly: TypeForwardedTo(typeof(Terraria.GameInput.TriggersPack))]
[assembly: TypeForwardedTo(typeof(Terraria.GameInput.KeyConfiguration))]

// Terraria.Localization
[assembly: TypeForwardedTo(typeof(Terraria.Localization.Language))]
[assembly: TypeForwardedTo(typeof(Terraria.Localization.LanguageManager))]
[assembly: TypeForwardedTo(typeof(Terraria.Localization.LocalizedText))]
[assembly: TypeForwardedTo(typeof(Terraria.Localization.GameCulture))]

// Terraria.Graphics.Shaders
[assembly: TypeForwardedTo(typeof(Terraria.Graphics.Shaders.GameShaders))]
[assembly: TypeForwardedTo(typeof(Terraria.Graphics.Shaders.ArmorShaderData))]
[assembly: TypeForwardedTo(typeof(Terraria.Graphics.Shaders.ScreenShaderData))]

// ReLogic
[assembly: TypeForwardedTo(typeof(ReLogic.Content.AssetRequestMode))]
[assembly: TypeForwardedTo(typeof(ReLogic.Content.Asset<>))]
[assembly: TypeForwardedTo(typeof(ReLogic.Graphics.DynamicSpriteFont))]
[assembly: TypeForwardedTo(typeof(ReLogic.Graphics.DynamicSpriteFontExtensionMethods))]

// Microsoft.Xna.Framework (兼容 FNA 依赖)
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Vector2))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Vector3))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Vector4))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Matrix))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Color))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Rectangle))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Point))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.GameTime))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Input.Keys))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Input.Buttons))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.GraphicsDevice))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.Texture2D))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.SpriteEffects))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.BlendState))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.SamplerState))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.DepthStencilState))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.RasterizerState))]
[assembly: TypeForwardedTo(typeof(Microsoft.Xna.Framework.Graphics.Effect))]
