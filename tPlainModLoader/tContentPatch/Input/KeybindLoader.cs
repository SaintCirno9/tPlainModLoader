using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using tContentPatch.ModLoad;
using Terraria.GameInput;
using TPML.Core.Logging;

namespace tContentPatch.Input
{
    /// <summary>
    /// 模组快捷键注册中心与生命周期管理器
    /// </summary>
    public static class KeybindLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("KeybindLoader");
        private static readonly Dictionary<string, ModKeybind> _keybinds = new Dictionary<string, ModKeybind>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 已注册的所有模组快捷键集合
        /// </summary>
        public static IEnumerable<ModKeybind> Keybinds => _keybinds.Values;

        /// <summary>
        /// 注册一个模组快捷键
        /// </summary>
        /// <param name="mod">模组实例、ModObject 或模组内任意类型对象</param>
        /// <param name="name">快捷键内部唯一标识（如 "PickColor"）</param>
        /// <param name="defaultBinding">默认按键（如 "C", "Mouse3", "LeftControl" 等）</param>
        /// <param name="displayName">在设置界面中显示的名称（如 "吸取物块颜色"）</param>
        public static ModKeybind RegisterKeybind(object mod, string name, string defaultBinding, string displayName = null)
        {
            string modName = ResolveModName(mod);
            return RegisterKeybind(modName, name, defaultBinding, displayName);
        }

        /// <summary>
        /// 注册一个模组快捷键
        /// </summary>
        /// <param name="modName">模组名称</param>
        /// <param name="name">快捷键内部唯一标识（如 "PickColor"）</param>
        /// <param name="defaultBinding">默认按键（如 "C", "Mouse3", "LeftControl" 等）</param>
        /// <param name="displayName">在设置界面中显示的名称（如 "吸取物块颜色"）</param>
        public static ModKeybind RegisterKeybind(string modName, string name, string defaultBinding, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(modName)) throw new ArgumentNullException(nameof(modName));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var keybind = new ModKeybind(modName.Trim(), name.Trim(), defaultBinding ?? "", displayName);
            _keybinds[keybind.FullName] = keybind;

            Logger.Info($"已注册模组快捷键: [{keybind.FullName}] (默认按键: {keybind.DefaultBinding}, 显示名称: {keybind.DisplayName})");

            // 如果此时游戏已处于运行阶段，立即执行增量同步
            SyncKeybindWithPlayerInput(keybind);

            return keybind;
        }

        /// <summary>
        /// 根据 Trigger 全名查找对应的快捷键
        /// </summary>
        public static bool TryGetKeybind(string fullName, out ModKeybind keybind)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                keybind = null;
                return false;
            }
            return _keybinds.TryGetValue(fullName, out keybind);
        }

        /// <summary>
        /// 将所有已注册的模组快捷键完整同步注入原版 PlayerInput 系统
        /// </summary>
        public static void SyncWithPlayerInput()
        {
            foreach (var keybind in _keybinds.Values)
            {
                SyncKeybindWithPlayerInput(keybind);
            }

            // 尝试从磁盘 input profiles.json 恢复玩家历史保存的自定义按键
            TryRestoreSavedKeybindsFromDisk();
        }

        /// <summary>
        /// 从原版 input profiles.json 配置文件中恢复玩家之前保存过的自定义模组快捷键
        /// </summary>
        private static void TryRestoreSavedKeybindsFromDisk()
        {
            try
            {
                if (string.IsNullOrEmpty(Terraria.Main.SavePath)) return;
                string jsonPath = System.IO.Path.Combine(Terraria.Main.SavePath, "input profiles.json");
                if (!System.IO.File.Exists(jsonPath)) return;

                string json = System.IO.File.ReadAllText(jsonPath);
                if (string.IsNullOrWhiteSpace(json)) return;

                var root = Newtonsoft.Json.Linq.JObject.Parse(json);
                if (root == null || PlayerInput.Profiles == null) return;

                // 原版 input profiles.json 中的模式键名映射
                var modeMap = new Dictionary<string, InputMode>
                {
                    { "Mouse And Keyboard", InputMode.Keyboard },
                    { "Gamepad", InputMode.XBoxGamepad },
                    { "Mouse And Keyboard UI", InputMode.KeyboardUI },
                    { "Gamepad UI", InputMode.XBoxGamepadUI }
                };

                HashSet<string> restoredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var profileProp in root.Properties())
                {
                    string profileName = profileProp.Name;
                    if (profileName == "Selected Profile") continue;
                    if (!PlayerInput.Profiles.TryGetValue(profileName, out var profile) || profile?.InputModes == null) continue;

                    if (profileProp.Value is Newtonsoft.Json.Linq.JObject profileObj)
                    {
                        foreach (var kvp in modeMap)
                        {
                            string jsonModeKey = kvp.Key;
                            InputMode inputMode = kvp.Value;

                            if (!profile.InputModes.TryGetValue(inputMode, out var config) || config?.KeyStatus == null) continue;

                            if (profileObj[jsonModeKey] is Newtonsoft.Json.Linq.JObject modeObj)
                            {
                                foreach (var keybind in _keybinds.Values)
                                {
                                    if (modeObj[keybind.FullName] is Newtonsoft.Json.Linq.JArray keyArr)
                                    {
                                        var loadedKeys = keyArr.Select(t => t.ToString()).ToList();
                                        config.KeyStatus[keybind.FullName] = loadedKeys;
                                        restoredKeys.Add(keybind.FullName);
                                    }
                                }
                            }
                        }
                    }
                }

                if (restoredKeys.Count > 0)
                {
                    Logger.Info($"成功从磁盘恢复 {restoredKeys.Count} 个模组快捷键配置 ({string.Join(", ", restoredKeys)})");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"从磁盘加载历史按键配置异常 (已忽略): {ex.Message}");
            }
        }

        /// <summary>
        /// 将单个快捷键同步到原版 PlayerInput.KnownTriggers 与所有 Profile
        /// </summary>
        private static void SyncKeybindWithPlayerInput(ModKeybind keybind)
        {
            if (PlayerInput.KnownTriggers == null) return;

            // 1. 注册到 KnownTriggers
            if (!PlayerInput.KnownTriggers.Contains(keybind.FullName))
            {
                PlayerInput.KnownTriggers.Add(keybind.FullName);
            }

            // 2. 注入所有活跃 Profiles（包含 Custom 等用户配置）
            if (PlayerInput.Profiles != null)
            {
                foreach (var profile in PlayerInput.Profiles.Values)
                {
                    InjectKeybindToProfile(profile, keybind, isOriginal: false);
                }
            }

            // 3. 注入 OriginalProfiles（作为重置按键的参考源）
            if (PlayerInput.OriginalProfiles != null)
            {
                foreach (var profile in PlayerInput.OriginalProfiles.Values)
                {
                    InjectKeybindToProfile(profile, keybind, isOriginal: true);
                }
            }

            // 4. 关键：向 PlayerInput.Triggers 的 4 个 TriggersSet (Current, Old, JustPressed, JustReleased) 补齐 Key
            if (PlayerInput.Triggers != null)
            {
                EnsureTriggerKey(PlayerInput.Triggers.Current, keybind.FullName);
                EnsureTriggerKey(PlayerInput.Triggers.Old, keybind.FullName);
                EnsureTriggerKey(PlayerInput.Triggers.JustPressed, keybind.FullName);
                EnsureTriggerKey(PlayerInput.Triggers.JustReleased, keybind.FullName);
            }
        }

        private static void EnsureTriggerKey(TriggersSet set, string key)
        {
            if (set?.KeyStatus != null && !set.KeyStatus.ContainsKey(key))
            {
                set.KeyStatus[key] = false;
            }
        }

        private static void InjectKeybindToProfile(PlayerInputProfile profile, ModKeybind keybind, bool isOriginal)
        {
            if (profile?.InputModes == null) return;

            foreach (var kvp in profile.InputModes)
            {
                InputMode mode = kvp.Key;
                KeyConfiguration config = kvp.Value;
                if (config?.KeyStatus == null) continue;

                if (!config.KeyStatus.ContainsKey(keybind.FullName))
                {
                    var list = new List<string>();
                    // 默认仅对键盘输入模式注入默认按键
                    if (mode == InputMode.Keyboard || mode == InputMode.KeyboardUI)
                    {
                        if (!string.IsNullOrEmpty(keybind.DefaultBinding))
                        {
                            list.Add(keybind.DefaultBinding);
                        }
                    }
                    config.KeyStatus[keybind.FullName] = list;
                }
            }
        }

        /// <summary>
        /// 解析模组对象获取模组名称
        /// </summary>
        private static string ResolveModName(object mod)
        {
            if (mod == null) return "UnknownMod";
            if (mod is string str && !string.IsNullOrWhiteSpace(str)) return str;
            if (mod is ModObject mo)
            {
                if (!string.IsNullOrWhiteSpace(mo.info?.name)) return mo.info.name;
                if (mo.assembly != null) return mo.assembly.GetName().Name;
            }

            Type type = mod.GetType();
            return type.Namespace?.Split('.').FirstOrDefault() ?? type.Assembly.GetName().Name;
        }

        /// <summary>
        /// 模组卸载时清空注册表
        /// </summary>
        public static void Unload()
        {
            _keybinds.Clear();
        }
    }
}
