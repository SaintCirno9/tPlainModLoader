using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json.Linq;
using tContentPatch.ModLoad;
using tContentPatch.Threading;
using Terraria;
using Terraria.GameInput;
using TPML.Core.Logging;

namespace TPML.Content
{
    /// <summary>
    /// TPML 原生自定义快捷键定义
    /// 作者: SaintCirno9
    /// </summary>
    public class ModKeybind
    {
        private readonly string _modName;

        /// <summary>
        /// 所属模组实例（如果通过 Mod 对象注册）
        /// </summary>
        public Mod Mod { get; internal set; }

        /// <summary>
        /// 所属模组名称
        /// </summary>
        public string ModName => Mod?.Name ?? _modName ?? "UnknownMod";

        /// <summary>
        /// 快捷键内部唯一标识符（如 "PickColor"）
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// 快捷键完整 Trigger 标识（格式为 "ModName/KeybindName"）
        /// </summary>
        public string FullName => !string.IsNullOrEmpty(ModName) ? $"{ModName}/{Name}" : (Mod != null ? $"{Mod.Name}/{Name}" : Name);

        /// <summary>
        /// 默认绑定的按键名称（如 "C", "LeftControl", "Mouse3" 等）
        /// </summary>
        public string DefaultBinding { get; internal set; }

        /// <summary>
        /// 在设置界面中显示的友好名称（如 "吸取物块颜色"）
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// 当前帧按键是否处于被按住状态（在打字、聊天、编辑告示牌、开箱或输入框聚焦时自动全局静默防误触）
        /// </summary>
        public bool Current
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.Current?.KeyStatus == null) return false;
                return PlayerInput.Triggers.Current.KeyStatus.TryGetValue(FullName, out bool val) && val;
            }
        }

        /// <summary>
        /// 当前帧按键是否刚被按下（边缘单次触发，具备多层冗余保障，在打字输入时自动全局静默防误触）
        /// </summary>
        public bool JustPressed
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.JustPressed?.KeyStatus != null &&
                    PlayerInput.Triggers.JustPressed.KeyStatus.TryGetValue(FullName, out bool val) && val)
                {
                    return true;
                }

                // 冗余容错保障：若原版 TriggersSet 差分字典未及时覆盖，直接由 Current 与 Old 状态计算
                if (PlayerInput.Triggers?.Current?.KeyStatus != null && PlayerInput.Triggers?.Old?.KeyStatus != null)
                {
                    bool cur = PlayerInput.Triggers.Current.KeyStatus.TryGetValue(FullName, out bool c) && c;
                    bool old = PlayerInput.Triggers.Old.KeyStatus.TryGetValue(FullName, out bool o) && o;
                    return cur && !old;
                }

                return false;
            }
        }

        /// <summary>
        /// 当前帧按键是否刚被释放（在打字输入时自动全局静默防误触）
        /// </summary>
        public bool JustReleased
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.JustReleased?.KeyStatus != null &&
                    PlayerInput.Triggers.JustReleased.KeyStatus.TryGetValue(FullName, out bool val) && val)
                {
                    return true;
                }

                if (PlayerInput.Triggers?.Current?.KeyStatus != null && PlayerInput.Triggers?.Old?.KeyStatus != null)
                {
                    bool cur = PlayerInput.Triggers.Current.KeyStatus.TryGetValue(FullName, out bool c) && c;
                    bool old = PlayerInput.Triggers.Old.KeyStatus.TryGetValue(FullName, out bool o) && o;
                    return !cur && old;
                }

                return false;
            }
        }

        /// <summary>
        /// 上一帧按键是否处于被按住状态（在打字输入时自动全局静默防误触）
        /// </summary>
        public bool Old
        {
            get
            {
                if (Main.drawingPlayerChat || Main.editSign || Main.editChest || Main.blockInput || PlayerInput.WritingText) return false;
                if (PlayerInput.Triggers?.Old?.KeyStatus == null) return false;
                return PlayerInput.Triggers.Old.KeyStatus.TryGetValue(FullName, out bool val) && val;
            }
        }

        /// <summary>
        /// 旧版命名兼容别名
        /// </summary>
        public bool RetroOld => Old;

        public ModKeybind(Mod mod, string name, string defaultBinding, string displayName = null)
        {
            Mod = mod;
            _modName = mod?.Name;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DefaultBinding = defaultBinding ?? "";
            DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : name;
        }

        public ModKeybind(string modName, string name, string defaultBinding, string displayName = null)
        {
            _modName = modName ?? throw new ArgumentNullException(nameof(modName));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DefaultBinding = defaultBinding ?? "";
            DisplayName = !string.IsNullOrWhiteSpace(displayName) ? displayName : name;
        }

        /// <summary>
        /// 获取指定输入模式下当前配置绑定的按键名称列表
        /// </summary>
        public List<string> GetAssignedKeys(InputMode mode = InputMode.Keyboard)
        {
            if (PlayerInput.CurrentProfile != null &&
                PlayerInput.CurrentProfile.InputModes.TryGetValue(mode, out var config) &&
                config.KeyStatus.TryGetValue(FullName, out var keys))
            {
                return new List<string>(keys);
            }
            return new List<string>();
        }

        public override string ToString() => FullName;
    }

    /// <summary>
    /// TPML 原生快捷键注册中心、生命周期与输入更新分发中心
    /// 作者: SaintCirno9
    /// </summary>
    public static class KeybindLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("KeybindLoader");
        private static readonly List<ModKeybind> _keybinds = new List<ModKeybind>();
        private static readonly Dictionary<string, ModKeybind> _keybindsByName = new Dictionary<string, ModKeybind>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 已注册的所有模组快捷键集合
        /// </summary>
        public static IReadOnlyList<ModKeybind> Keybinds => _keybinds;

        /// <summary>
        /// 注册一个模组快捷键（通过 Mod 实例）
        /// </summary>
        public static ModKeybind RegisterKeybind(Mod mod, string name, string defaultBinding, string displayName = null)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var keybind = new ModKeybind(mod, name.Trim(), defaultBinding ?? "", displayName);
            return RegisterInternal(keybind);
        }

        /// <summary>
        /// 注册一个模组快捷键（通过模组名称）
        /// </summary>
        public static ModKeybind RegisterKeybind(string modName, string name, string defaultBinding, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(modName)) throw new ArgumentNullException(nameof(modName));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));

            var keybind = new ModKeybind(modName.Trim(), name.Trim(), defaultBinding ?? "", displayName);
            return RegisterInternal(keybind);
        }

        /// <summary>
        /// 注册一个模组快捷键（通过 Keys 枚举）
        /// </summary>
        public static ModKeybind RegisterKeybind(Mod mod, string name, Keys defaultKey, string displayName = null)
        {
            return RegisterKeybind(mod, name, defaultKey.ToString(), displayName);
        }

        /// <summary>
        /// 通用模组快捷键注册重载（支持 Mod 实例、string 或其他模组对象）
        /// </summary>
        public static ModKeybind RegisterKeybind(object mod, string name, string defaultBinding, string displayName = null)
        {
            if (mod is Mod m)
            {
                return RegisterKeybind(m, name, defaultBinding, displayName);
            }
            string modName = ResolveModName(mod);
            return RegisterKeybind(modName, name, defaultBinding, displayName);
        }

        private static ModKeybind RegisterInternal(ModKeybind keybind)
        {
            if (_keybindsByName.TryGetValue(keybind.FullName, out var existing))
            {
                return existing;
            }

            _keybinds.Add(keybind);
            _keybindsByName[keybind.FullName] = keybind;
            if (!_keybindsByName.ContainsKey(keybind.Name))
            {
                _keybindsByName[keybind.Name] = keybind;
            }

            Logger.Info($"已注册模组快捷键: [{keybind.FullName}] (默认按键: {keybind.DefaultBinding}, 显示名称: {keybind.DisplayName})");

            // 如果游戏已处于运行阶段，立即执行增量同步
            SyncKeybindWithPlayerInput(keybind);

            return keybind;
        }

        /// <summary>
        /// 根据 Trigger 全名或局部名称查找对应的快捷键
        /// </summary>
        public static bool TryGetKeybind(string fullName, out ModKeybind keybind)
        {
            if (string.IsNullOrEmpty(fullName))
            {
                keybind = null;
                return false;
            }
            return _keybindsByName.TryGetValue(fullName, out keybind);
        }

        /// <summary>
        /// 根据 Trigger 全名或局部名称查找快捷键实例
        /// </summary>
        public static ModKeybind GetKeybind(string fullName)
        {
            TryGetKeybind(fullName, out var keybind);
            return keybind;
        }

        /// <summary>
        /// 快捷查询快捷键当前按压状态
        /// </summary>
        public static bool GetState(ModKeybind keybind) => keybind?.Current ?? false;

        /// <summary>
        /// 快捷查询快捷键上一帧按压状态
        /// </summary>
        public static bool GetOldState(ModKeybind keybind) => keybind?.Old ?? false;

        /// <summary>
        /// 快捷查询快捷键刚按下状态
        /// </summary>
        public static bool JustPressed(ModKeybind keybind) => keybind?.JustPressed ?? false;

        /// <summary>
        /// 快捷查询快捷键刚释放状态
        /// </summary>
        public static bool JustReleased(ModKeybind keybind) => keybind?.JustReleased ?? false;

        public static bool GetState(string name)
        {
            if (TryGetKeybind(name, out var kb)) return kb.Current;
            return ReadTrigger(name, set => set?.Current);
        }

        public static bool JustPressed(string name)
        {
            if (TryGetKeybind(name, out var kb)) return kb.JustPressed;
            return ReadTrigger(name, set => set?.JustPressed);
        }

        public static bool JustReleased(string name)
        {
            if (TryGetKeybind(name, out var kb)) return kb.JustReleased;
            return ReadTrigger(name, set => set?.JustReleased);
        }

        /// <summary>
        /// 每帧输入更新（由 PlayerInput.UpdateInput 或生命周期派发器调用）
        /// </summary>
        public static void Update()
        {
        }

        public static void ProcessTriggers(TriggersSet triggersSet)
        {
            Update();
        }

        private static bool ReadTrigger(string name, Func<TriggersPack, TriggersSet> selector)
        {
            if (string.IsNullOrEmpty(name) || PlayerInput.Triggers == null) return false;
            TriggersSet set = selector(PlayerInput.Triggers);
            if (set?.KeyStatus == null) return false;
            if (set.KeyStatus.TryGetValue(name, out bool exact) && exact) return true;
            foreach (var kvp in set.KeyStatus)
            {
                if (kvp.Value && (kvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || kvp.Key.EndsWith("/" + name, StringComparison.OrdinalIgnoreCase)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 将所有已注册的模组快捷键完整同步注入原版 PlayerInput 系统
        /// </summary>
        public static void SyncWithPlayerInput()
        {
            foreach (var keybind in _keybinds)
            {
                SyncKeybindWithPlayerInput(keybind);
            }

            // 尝试从磁盘 input profiles.json 恢复玩家历史保存的自定义按键
            TryRestoreSavedKeybindsFromDisk();
        }

        /// <summary>
        /// 将单个快捷键同步到原版 PlayerInput.KnownTriggers 与所有 Profile（须在主线程执行）。
        /// </summary>
        public static void SyncKeybindWithPlayerInput(ModKeybind keybind)
        {
            if (keybind == null || PlayerInput.KnownTriggers == null) return;

            if (!MainThreadDispatcher.IsMainThread)
            {
                MainThreadDispatcher.Enqueue(() => SyncKeybindWithPlayerInput(keybind));
                return;
            }

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
                        if (!string.IsNullOrEmpty(keybind.DefaultBinding) && !keybind.DefaultBinding.Equals("None", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(keybind.DefaultBinding);
                        }
                    }
                    config.KeyStatus[keybind.FullName] = list;
                }
            }
        }

        /// <summary>
        /// 从原版 input profiles.json 配置文件中恢复玩家之前保存过的自定义模组快捷键
        /// </summary>
        private static void TryRestoreSavedKeybindsFromDisk()
        {
            try
            {
                if (string.IsNullOrEmpty(Main.SavePath)) return;
                string jsonPath = Path.Combine(Main.SavePath, "input profiles.json");
                if (!File.Exists(jsonPath)) return;

                string json = File.ReadAllText(jsonPath);
                if (string.IsNullOrWhiteSpace(json)) return;

                var root = JObject.Parse(json);
                if (root == null || PlayerInput.Profiles == null) return;

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

                    if (profileProp.Value is JObject profileObj)
                    {
                        foreach (var kvp in modeMap)
                        {
                            string jsonModeKey = kvp.Key;
                            InputMode inputMode = kvp.Value;

                            if (!profile.InputModes.TryGetValue(inputMode, out var config) || config?.KeyStatus == null) continue;

                            if (profileObj[jsonModeKey] is JObject modeObj)
                            {
                                foreach (var keybind in _keybinds)
                                {
                                    if (modeObj[keybind.FullName] is JArray keyArr)
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
            if (mod is Mod m)
            {
                return m.Name;
            }

            Type type = mod.GetType();
            return type.Namespace?.Split('.').FirstOrDefault() ?? type.Assembly.GetName().Name;
        }

        /// <summary>
        /// 清空快捷键注册表并对称从 PlayerInput 中移除已注入的条目
        /// </summary>
        public static void Clear()
        {
            foreach (var keybind in _keybinds)
            {
                RemoveKeybindFromPlayerInput(keybind);
            }
            _keybinds.Clear();
            _keybindsByName.Clear();
        }

        /// <summary>
        /// 模组卸载清理别名
        /// </summary>
        public static void Unload() => Clear();

        private static void RemoveKeybindFromPlayerInput(ModKeybind keybind)
        {
            if (keybind == null) return;
            string fullName = keybind.FullName;

            try
            {
                PlayerInput.KnownTriggers?.Remove(fullName);
            }
            catch (Exception ex)
            {
                Logger.Warn($"移除 KnownTriggers 失败 [{fullName}]: {ex.Message}");
            }

            RemoveFromProfiles(PlayerInput.Profiles, fullName);
            RemoveFromProfiles(PlayerInput.OriginalProfiles, fullName);

            if (PlayerInput.Triggers != null)
            {
                RemoveTriggerKey(PlayerInput.Triggers.Current, fullName);
                RemoveTriggerKey(PlayerInput.Triggers.Old, fullName);
                RemoveTriggerKey(PlayerInput.Triggers.JustPressed, fullName);
                RemoveTriggerKey(PlayerInput.Triggers.JustReleased, fullName);
            }
        }

        private static void RemoveFromProfiles(Dictionary<string, PlayerInputProfile> profiles, string fullName)
        {
            if (profiles == null) return;
            foreach (var profile in profiles.Values)
            {
                if (profile?.InputModes == null) continue;
                foreach (var config in profile.InputModes.Values)
                {
                    config?.KeyStatus?.Remove(fullName);
                }
            }
        }

        private static void RemoveTriggerKey(TriggersSet set, string key)
        {
            set?.KeyStatus?.Remove(key);
        }
    }
}

