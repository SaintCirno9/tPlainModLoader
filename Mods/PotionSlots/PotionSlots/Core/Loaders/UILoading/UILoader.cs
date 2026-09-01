using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using TPML.Content;
using Terraria.UI;

namespace PotionSlots.Core.Loaders.UILoading
{
    internal class UILoader : ModSystem
    {
        public static List<UserInterface> UserInterfaces = new List<UserInterface>();
        public static List<SmartUIState> UIStates = new List<SmartUIState>();

        public override void Load()
        {
            if (Main.dedServ) return;

            UserInterfaces = new List<UserInterface>();
            UIStates = new List<SmartUIState>();

            Type[] types = Mod.Code.GetTypes();
            foreach (Type type in types)
            {
                if (!type.IsAbstract && type.IsSubclassOf(typeof(SmartUIState)))
                {
                    SmartUIState smartUIState = (SmartUIState)Activator.CreateInstance(type);
                    UserInterface val = new UserInterface();
                    smartUIState.UserInterface = val;
                    val.SetState(smartUIState);
                    smartUIState.Activate();
                    smartUIState.Recalculate();
                    UIStates.Add(smartUIState);
                    UserInterfaces.Add(val);
                }
            }
        }

        public override void Unload()
        {
            if (UIStates != null)
            {
                foreach (var n in UIStates) n.Unload();
            }
            UserInterfaces = null;
            UIStates = null;
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.ingameOptionsWindow || Main.InGameUI.IsVisible) return;
            if (UserInterfaces == null) return;

            foreach (UserInterface userInterface in UserInterfaces)
            {
                if (userInterface?.CurrentState is SmartUIState smartState && smartState.Visible)
                {
                    userInterface.Update(gameTime);
                }
            }
        }

        public static T GetUIState<T>() where T : SmartUIState
        {
            return UIStates?.FirstOrDefault(n => n is T) as T;
        }

        public static void ReloadState<T>() where T : SmartUIState
        {
            if (UIStates == null || UserInterfaces == null) return;
            int index = UIStates.IndexOf(GetUIState<T>());
            if (index >= 0)
            {
                UIStates[index] = (T)Activator.CreateInstance(typeof(T));
                UserInterfaces[index] = new UserInterface();
                UIStates[index].UserInterface = UserInterfaces[index];
                UserInterfaces[index].SetState(UIStates[index]);
                UIStates[index].Activate();
                UIStates[index].Recalculate();
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (UIStates == null) return;
            for (int i = 0; i < UIStates.Count; i++)
            {
                SmartUIState smartUIState = UIStates[i];
                if (smartUIState == null) continue;

                string layerName = "BrickAndMortar: " + smartUIState.GetType().Name;
                if (layers.Any(l => l.Name == layerName))
                    continue;

                int index = smartUIState.InsertionIndex(layers);
                if (index < 0)
                {
                    index = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text", StringComparison.OrdinalIgnoreCase));
                    if (index < 0) index = layers.Count;
                }

                layers.Insert(index, new LegacyGameInterfaceLayer(layerName, delegate
                {
                    if (smartUIState.Visible)
                    {
                        if (smartUIState.UserInterface != null)
                        {
                            smartUIState.UserInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        else
                        {
                            smartUIState.Draw(Main.spriteBatch);
                        }
                    }
                    return true;
                }, smartUIState.Scale));
            }
        }
    }
}
