using System;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace RecipeBrowser
{
    /// <summary>
    /// RecipeBrowser 客户端配置数据
    /// 作者: SaintCirno9
    /// </summary>
    public class RecipeBrowserClientConfig
    {
        public static RecipeBrowserClientConfig Instance { get; set; } = new RecipeBrowserClientConfig();

        public bool ShowRecipeModFilter { get; set; } = true;
        public bool ShowItemModFilter { get; set; } = true;
        public bool ShowNPCModFilter { get; set; } = true;
        public bool ItemChecklistOnlyId { get; set; } = false;
        public bool RecipeChecklistOnlyId { get; set; } = false;
        public bool ShowAllNPCs { get; set; } = false;
        public bool AutomaticallyHideWhenItemSlotClicked { get; set; } = false;
        public bool SaveLastSelectedRecipe { get; set; } = false;
        public bool OnlyShowFavoritedWhileInInventory { get; set; } = false;
        public bool EnableProfiler { get; set; } = false;

        public Vector2 RecipeBrowserPosition { get; set; } = new Vector2(400, 400);
        public Vector2 RecipeBrowserSize { get; set; } = new Vector2(475, 350);
        public Vector2 FavoritedRecipePanelPosition { get; set; } = new Vector2(20, 200);

        private static string ConfigPath
        {
            get
            {
                string baseSavePath = Terraria.Main.SavePath;
                if (string.IsNullOrEmpty(baseSavePath))
                {
                    baseSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
                }
                return Path.Combine(baseSavePath, "tPlainModLoader", "Config", "RecipeBrowser", "config.json");
            }
        }

        public static void LoadConfig() => Load();
        public static void SaveConfig() => Save();

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Instance = JsonConvert.DeserializeObject<RecipeBrowserClientConfig>(json) ?? new RecipeBrowserClientConfig();
                    return;
                }
            }
            catch { }

            Instance = new RecipeBrowserClientConfig();
            Save();
        }

        public static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonConvert.SerializeObject(Instance, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}
