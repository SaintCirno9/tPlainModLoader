using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using HarmonyLib;
using ReLogic.Localization.IME;
using ReLogic.OS;
using Terraria;
using Terraria.GameInput;
using TPML.Core.Logging;

namespace tContentPatch.Content
{
    /// <summary>
    /// Windows 输入法运行态诊断。仅记录状态，不修改或拦截输入消息。
    /// </summary>
    internal static class ImeDiagnostics
    {
        private const int PollIntervalMilliseconds = 250;
        private const int ActiveHeartbeatMilliseconds = 2000;

        private static readonly ILogger Logger = LogManager.GetLogger("ImeDiagnostics");
        private static readonly object SyncLock = new object();

        private static string _lastStateKey;
        private static int _nextPollTick = Environment.TickCount;
        private static int _nextHeartbeatTick = Environment.TickCount;
        private static bool _loggedHandleImeBefore;
        private static bool _loggedHandleImeAfter;

        internal static void LogInstalled()
        {
            try
            {
                Logger.Info("[IME-DIAG] 诊断补丁已启用：仅记录状态和窗口消息，不改变输入行为");
                LogState("install", true);
            }
            catch (Exception ex)
            {
                LogFailure("install", ex);
            }
        }

        internal static void Poll()
        {
            try
            {
                if (Main.dedServ) return;

                int now = Environment.TickCount;
                lock (SyncLock)
                {
                    if (unchecked(now - _nextPollTick) < 0) return;
                    _nextPollTick = unchecked(now + PollIntervalMilliseconds);
                }

                StateSnapshot snapshot = CaptureState();
                bool forceHeartbeat = false;

                if (snapshot.WritingText)
                {
                    lock (SyncLock)
                    {
                        if (unchecked(now - _nextHeartbeatTick) >= 0)
                        {
                            _nextHeartbeatTick = unchecked(now + ActiveHeartbeatMilliseconds);
                            forceHeartbeat = true;
                        }
                    }
                }

                LogState("poll", forceHeartbeat, snapshot);
            }
            catch (Exception ex)
            {
                LogFailure("poll", ex);
            }
        }

        internal static void LogHandleIme(string stage)
        {
            try
            {
                bool force = false;
                lock (SyncLock)
                {
                    if (stage == "before" && !_loggedHandleImeBefore)
                    {
                        _loggedHandleImeBefore = true;
                        force = true;
                    }
                    else if (stage == "after" && !_loggedHandleImeAfter)
                    {
                        _loggedHandleImeAfter = true;
                        force = true;
                    }
                }

                LogState("Main.HandleIME/" + stage, force);
            }
            catch (Exception ex)
            {
                LogFailure("Main.HandleIME/" + stage, ex);
            }
        }

        internal static void LogPlatformImeCall(string method, string stage, PlatformIme instance)
        {
            try
            {
                string instanceType = instance?.GetType().FullName ?? "<null>";
                string enabled = SafeRead(() => instance?.IsEnabled.ToString() ?? "<null>");
                Logger.Info($"[IME-DIAG] PlatformIme.{method}/{stage}: instance={instanceType}, managedEnabled={enabled}");
                LogState("PlatformIme." + method + "/" + stage, true);
            }
            catch (Exception ex)
            {
                LogFailure("PlatformIme." + method + "/" + stage, ex);
            }
        }

        internal static MessageTrace CaptureMessage(ref Message message)
        {
            try
            {
                bool shouldLog = ShouldLogMessage(message);
                if (!shouldLog) return default(MessageTrace);

                return new MessageTrace
                {
                    ShouldLog = true,
                    Name = GetMessageName(message.Msg),
                    MessageId = message.Msg,
                    WindowHandle = message.HWnd,
                    WParam = message.WParam,
                    LParam = message.LParam
                };
            }
            catch (Exception ex)
            {
                LogFailure("WindowsIme.PreFilterMessage/capture", ex);
                return default(MessageTrace);
            }
        }

        internal static void LogMessage(MessageTrace trace, ref Message message, bool handled)
        {
            if (!trace.ShouldLog) return;

            try
            {
                StateSnapshot snapshot = CaptureState();
                Logger.Info(
                    $"[IME-DIAG] WindowsIme.PreFilterMessage: msg={trace.Name}(0x{trace.MessageId:X4}), " +
                    $"hwnd={FormatPointer(trace.WindowHandle)}, wParam={FormatPointer(trace.WParam)}, " +
                    $"lParamBefore={FormatPointer(trace.LParam)}, lParamAfter={FormatPointer(message.LParam)}, " +
                    $"handled={handled}; {snapshot.Details}");
                RememberState(snapshot);
            }
            catch (Exception ex)
            {
                LogFailure("WindowsIme.PreFilterMessage/log", ex);
            }
        }

        private static void LogState(string source, bool force)
        {
            LogState(source, force, CaptureState());
        }

        private static void LogState(string source, bool force, StateSnapshot snapshot)
        {
            bool changed;
            lock (SyncLock)
            {
                changed = !string.Equals(_lastStateKey, snapshot.Key, StringComparison.Ordinal);
                if (!force && !changed) return;
                _lastStateKey = snapshot.Key;
            }

            string reason = changed ? "changed" : "heartbeat";
            Logger.Info($"[IME-DIAG] state/{source}/{reason}: {snapshot.Details}");
        }

        private static void RememberState(StateSnapshot snapshot)
        {
            lock (SyncLock)
            {
                _lastStateKey = snapshot.Key;
            }
        }

        private static StateSnapshot CaptureState()
        {
            bool writingText = SafeReadBool(() => PlayerInput.WritingText);
            bool imeToggle = SafeReadBool(() => Main.instance != null && Main.instance._imeToggle);

            IImeService service = null;
            string serviceError = null;
            try
            {
                service = Platform.Get<IImeService>();
            }
            catch (Exception ex)
            {
                serviceError = ErrorName(ex);
            }

            string serviceType = service?.GetType().FullName ?? (serviceError == null ? "<null>" : "<error:" + serviceError + ">");
            string managedEnabled = SafeRead(() => service?.IsEnabled.ToString() ?? "<null>");
            string composition = SafeRead(() => EscapeText(service?.CompositionString));
            string candidateVisible = SafeRead(() => service?.IsCandidateListVisible.ToString() ?? "<null>");
            string nativeEnabled = SafeRead(() => ImeUi_IsEnabled().ToString());

            IntPtr gameWindow = IntPtr.Zero;
            string gameWindowError = null;
            try
            {
                if (Main.instance != null && Main.instance.Window != null)
                {
                    gameWindow = Main.instance.Window.Handle;
                }
            }
            catch (Exception ex)
            {
                gameWindowError = ErrorName(ex);
            }

            IntPtr foregroundWindow = SafeReadPointer(GetForegroundWindow);
            uint processId = 0;
            uint windowThreadId = 0;
            string keyboardLayout = "<n/a>";
            if (gameWindow != IntPtr.Zero)
            {
                try
                {
                    windowThreadId = GetWindowThreadProcessId(gameWindow, out processId);
                    keyboardLayout = FormatPointer(GetKeyboardLayout(windowThreadId));
                }
                catch (Exception ex)
                {
                    keyboardLayout = "<error:" + ErrorName(ex) + ">";
                }
            }

            string immContext;
            string immOpen;
            ReadImmState(gameWindow, out immContext, out immOpen);

            int managedThreadId = Thread.CurrentThread.ManagedThreadId;
            uint nativeThreadId = SafeReadUInt(GetCurrentThreadId);
            string apartmentState = SafeRead(() => Thread.CurrentThread.GetApartmentState().ToString());
            int keyCount = SafeReadInt(() => Main.keyCount);

            string gameWindowText = gameWindowError == null
                ? FormatPointer(gameWindow)
                : "<error:" + gameWindowError + ">";

            string key =
                writingText + "|" + imeToggle + "|" + serviceType + "|" + managedEnabled + "|" +
                composition + "|" + candidateVisible + "|" + nativeEnabled + "|" + gameWindowText + "|" +
                FormatPointer(foregroundWindow) + "|" + windowThreadId + "|" + keyboardLayout + "|" +
                immContext + "|" + immOpen + "|" + managedThreadId + "|" + nativeThreadId + "|" + apartmentState;

            string details =
                $"writing={writingText}, toggle={imeToggle}, service={serviceType}, managedEnabled={managedEnabled}, " +
                $"nativeEnabled={nativeEnabled}, composition=\"{composition}\", candidatesVisible={candidateVisible}, " +
                $"gameHwnd={gameWindowText}, foregroundHwnd={FormatPointer(foregroundWindow)}, " +
                $"windowThread={windowThreadId}, windowProcess={processId}, keyboardLayout={keyboardLayout}, " +
                $"immContext={immContext}, immOpen={immOpen}, keyCount={keyCount}, " +
                $"managedThread={managedThreadId}, nativeThread={nativeThreadId}, apartment={apartmentState}";

            return new StateSnapshot
            {
                WritingText = writingText,
                Key = key,
                Details = details
            };
        }

        private static void ReadImmState(IntPtr windowHandle, out string contextState, out string openState)
        {
            contextState = "<none>";
            openState = "<n/a>";
            if (windowHandle == IntPtr.Zero) return;

            IntPtr context = IntPtr.Zero;
            try
            {
                context = ImmGetContext(windowHandle);
                if (context == IntPtr.Zero) return;

                contextState = FormatPointer(context);
                openState = ImmGetOpenStatus(context).ToString();
            }
            catch (Exception ex)
            {
                contextState = "<error:" + ErrorName(ex) + ">";
                openState = "<error>";
            }
            finally
            {
                if (context != IntPtr.Zero)
                {
                    try
                    {
                        ImmReleaseContext(windowHandle, context);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static bool ShouldLogMessage(Message message)
        {
            switch (message.Msg)
            {
                case 0x0007: // WM_SETFOCUS
                case 0x0008: // WM_KILLFOCUS
                case 0x0051: // WM_INPUTLANGCHANGE
                case 0x010D: // WM_IME_STARTCOMPOSITION
                case 0x010E: // WM_IME_ENDCOMPOSITION
                case 0x010F: // WM_IME_COMPOSITION
                case 0x0281: // WM_IME_SETCONTEXT
                case 0x0282: // WM_IME_NOTIFY
                case 0x0283: // WM_IME_CONTROL
                case 0x0288: // WM_IME_REQUEST
                case 0x0290: // WM_IME_KEYDOWN
                case 0x0291: // WM_IME_KEYUP
                    return true;
                case 0x0100: // WM_KEYDOWN
                    return message.WParam.ToInt64() == 229 || SafeReadBool(() => PlayerInput.WritingText);
                case 0x0102: // WM_CHAR
                    return SafeReadBool(() => PlayerInput.WritingText);
                default:
                    return false;
            }
        }

        private static string GetMessageName(int messageId)
        {
            switch (messageId)
            {
                case 0x0007: return "WM_SETFOCUS";
                case 0x0008: return "WM_KILLFOCUS";
                case 0x0051: return "WM_INPUTLANGCHANGE";
                case 0x0100: return "WM_KEYDOWN";
                case 0x0102: return "WM_CHAR";
                case 0x010D: return "WM_IME_STARTCOMPOSITION";
                case 0x010E: return "WM_IME_ENDCOMPOSITION";
                case 0x010F: return "WM_IME_COMPOSITION";
                case 0x0281: return "WM_IME_SETCONTEXT";
                case 0x0282: return "WM_IME_NOTIFY";
                case 0x0283: return "WM_IME_CONTROL";
                case 0x0288: return "WM_IME_REQUEST";
                case 0x0290: return "WM_IME_KEYDOWN";
                case 0x0291: return "WM_IME_KEYUP";
                default: return "UNKNOWN";
            }
        }

        private static string EscapeText(string value)
        {
            if (value == null) return "<null>";

            string escaped = value
                .Replace("\\", "\\\\")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");

            return escaped.Length <= 80 ? escaped : escaped.Substring(0, 80) + "...";
        }

        private static string FormatPointer(IntPtr value)
        {
            return "0x" + unchecked((ulong)value.ToInt64()).ToString("X");
        }

        private static string SafeRead(Func<string> read)
        {
            try
            {
                return read();
            }
            catch (Exception ex)
            {
                return "<error:" + ErrorName(ex) + ">";
            }
        }

        private static bool SafeReadBool(Func<bool> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return false;
            }
        }

        private static int SafeReadInt(Func<int> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return -1;
            }
        }

        private static uint SafeReadUInt(Func<uint> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return 0;
            }
        }

        private static IntPtr SafeReadPointer(Func<IntPtr> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return IntPtr.Zero;
            }
        }

        private static string ErrorName(Exception exception)
        {
            return exception.GetType().Name + ":" + exception.Message;
        }

        private static void LogFailure(string source, Exception exception)
        {
            try
            {
                Logger.Warn($"[IME-DIAG] {source} 诊断读取失败: {exception.GetType().Name}: {exception.Message}");
            }
            catch
            {
            }
        }

        [DllImport("ReLogic.Native.dll", EntryPoint = "ImeUi_IsEnabled")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ImeUi_IsEnabled();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint threadId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("imm32.dll")]
        private static extern IntPtr ImmGetContext(IntPtr windowHandle);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmGetOpenStatus(IntPtr inputContext);

        [DllImport("imm32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImmReleaseContext(IntPtr windowHandle, IntPtr inputContext);

        internal struct MessageTrace
        {
            internal bool ShouldLog;
            internal string Name;
            internal int MessageId;
            internal IntPtr WindowHandle;
            internal IntPtr WParam;
            internal IntPtr LParam;
        }

        private struct StateSnapshot
        {
            internal bool WritingText;
            internal string Key;
            internal string Details;
        }
    }

    [HarmonyPatch(typeof(Main), nameof(Main.HandleIME))]
    internal static class ImeDiagnosticsHandleImePatch
    {
        [HarmonyPrefix]
        private static void Prefix()
        {
            ImeDiagnostics.LogHandleIme("before");
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            ImeDiagnostics.LogHandleIme("after");
        }
    }

    [HarmonyPatch(typeof(Main), "Update")]
    internal static class ImeDiagnosticsUpdatePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            ImeDiagnostics.Poll();
        }
    }

    [HarmonyPatch(typeof(PlatformIme), nameof(PlatformIme.Enable))]
    internal static class ImeDiagnosticsEnablePatch
    {
        [HarmonyPrefix]
        private static void Prefix(PlatformIme __instance)
        {
            ImeDiagnostics.LogPlatformImeCall("Enable", "before", __instance);
        }

        [HarmonyPostfix]
        private static void Postfix(PlatformIme __instance)
        {
            ImeDiagnostics.LogPlatformImeCall("Enable", "after", __instance);
        }
    }

    [HarmonyPatch(typeof(PlatformIme), nameof(PlatformIme.Disable))]
    internal static class ImeDiagnosticsDisablePatch
    {
        [HarmonyPrefix]
        private static void Prefix(PlatformIme __instance)
        {
            ImeDiagnostics.LogPlatformImeCall("Disable", "before", __instance);
        }

        [HarmonyPostfix]
        private static void Postfix(PlatformIme __instance)
        {
            ImeDiagnostics.LogPlatformImeCall("Disable", "after", __instance);
        }
    }

    [HarmonyPatch(typeof(WindowsIme), nameof(WindowsIme.PreFilterMessage))]
    internal static class ImeDiagnosticsMessagePatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref Message message, out ImeDiagnostics.MessageTrace __state)
        {
            __state = ImeDiagnostics.CaptureMessage(ref message);
        }

        [HarmonyPostfix]
        private static void Postfix(ref Message message, bool __result, ImeDiagnostics.MessageTrace __state)
        {
            ImeDiagnostics.LogMessage(__state, ref message, __result);
        }
    }
}
