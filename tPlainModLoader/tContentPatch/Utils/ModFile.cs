using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using tContentPatch.ModLoad;

namespace tContentPatch.Utils
{
    /// <summary/>
    public static class ModFile
    {
        /// <summary>
        /// 获取模组专属用户配置目录 (Documents/My Games/Terraria/tPlainModLoader/Config/<ModKey>)
        /// </summary>
        private static string GetModConfigDirectory(ModObject mo)
        {
            string key = mo?.config?.key;
            if (string.IsNullOrEmpty(key) && mo?.assembly != null)
            {
                key = mo.assembly.GetName().Name;
            }
            if (string.IsNullOrEmpty(key)) key = "UnknownMod";

            string baseConfigDir = ContentPatch.ConfigDirectory;
            if (string.IsNullOrEmpty(baseConfigDir))
            {
                string baseSavePath = Terraria.Main.SavePath;
                if (string.IsNullOrEmpty(baseSavePath))
                {
                    baseSavePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My Games", "Terraria");
                }
                baseConfigDir = Path.Combine(baseSavePath, InfoList.Directorys.UserDataRoot, InfoList.Directorys.Config);
            }

            string modConfigDir = Path.Combine(baseConfigDir, key);
            if (!Directory.Exists(modConfigDir))
            {
                Directory.CreateDirectory(modConfigDir);
            }
            return modConfigDir;
        }

        /// <summary>
        /// 在该模组的用户配置文件夹下保存文件, 目录不存在则创建
        /// </summary>
        /// <param name="file">文件相对位置</param>
        /// <param name="save"></param>
        /// <param name="mo">为<see langword="null"/>时从已加载的模组中匹配调用该方法的模组的路径</param>
        public static bool SaveFileTry(string file, Func<string, bool> save, ModObject mo = null)
        {
            try
            {
                if (file == null) return false;
                if (save == null) return false;

                if (mo == null)
                {
                    MethodBase method = new StackTrace().GetFrame(1).GetMethod();
                    Assembly assembly = method.ReflectedType.Assembly;
                    mo = LoaderControl.GetModObjects()?.FirstOrDefault(i => i.assembly == assembly);
                    if (mo == null) return false;
                }

                string modConfigDir = GetModConfigDirectory(mo);
                string targetFile = Path.Combine(modConfigDir, file);
                string path = Path.GetDirectoryName(targetFile);

                if (Directory.Exists(path) == false)
                {
                    Directory.CreateDirectory(path);
                    if (Directory.Exists(path) == false) return false;
                }

                return save(targetFile);
            }
            catch { return false; }
        }

        /// <summary>
        /// 在该模组的用户配置文件夹下读取文件 (若新路径不存在则平滑尝试从旧路径读取并迁移)
        /// </summary>
        /// <param name="file">文件相对位置</param>
        /// <param name="read"></param>
        /// <param name="mo">为<see langword="null"/>时从已加载的模组中匹配调用该方法的模组的路径</param>
        public static bool ReadFileTry(string file, Func<string, bool> read, ModObject mo = null)
        {
            try
            {
                if (file == null) return false;
                if (read == null) return false;

                if (mo == null)
                {
                    MethodBase method = new StackTrace().GetFrame(1).GetMethod();
                    Assembly assembly = method.ReflectedType.Assembly;
                    mo = LoaderControl.GetModObjects()?.FirstOrDefault(i => i.assembly == assembly);
                    if (mo == null) return false;
                }

                string modConfigDir = GetModConfigDirectory(mo);
                string targetFile = Path.Combine(modConfigDir, file);

                if (File.Exists(targetFile))
                {
                    return read(targetFile);
                }

                // 若新路径不存在，尝试从旧版模组部署目录平滑迁移
                if (!string.IsNullOrEmpty(mo.modPath))
                {
                    string legacyFile = Path.Combine(mo.modPath, file);
                    if (File.Exists(legacyFile))
                    {
                        bool success = read(legacyFile);
                        if (success)
                        {
                            try
                            {
                                string targetDir = Path.GetDirectoryName(targetFile);
                                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                                File.Copy(legacyFile, targetFile, true);
                            }
                            catch { }
                        }
                        return success;
                    }
                }

                return false;
            }
            catch { return false; }
        }
    }
}
