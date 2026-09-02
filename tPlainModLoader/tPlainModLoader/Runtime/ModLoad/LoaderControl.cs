using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TPML.Utils;
using TPML.Core.Logging;

namespace TPML.ModLoad
{
    internal class LoaderControl
    {
        private static readonly ILogger Logger = LogManager.GetLogger("LoaderControl");

        public static Action<IModLoader> OnModLoad_Start = null;
        public static Action OnModLoad_Ok = null;
        public static Action<IModLoader> OnModLoad_Cancel = null;
        public static Action OnModLoad_Canceled = null;
        public static Action<Exception> OnModLoad_Exception = null;
        public static Action<Exception> OnModUnload_Exception = null;
        public static bool CanLoad => ContentPatch.Initialized;

        private static List<ModObject> loadedMod = null;
        private static IModLoader modLoader = null;
        private static int _loadInFlight;

        internal static void SetModLoader(IModLoader modLoader, Intercept intercept)
        {
            intercept.OnLoaded += mos => loadedMod = mos;

            Intercept ml = new Intercept(modLoader);
            ml.OnLoadException += ex => loadedMod = null;

            LoaderControl.modLoader = ml;
        }

        /// <summary>
        /// 获取加载的模组, 加载失败时为null, 需要修改内容建议使用返回复制内容的<see cref="ContentPatch.GetModObjects"/>
        /// </summary>
        internal static List<ModObject> GetModObjects()
        {
            return loadedMod?.ToList();
        }

        /// <summary>
        /// 已加载过再调用会先卸载
        /// </summary>
        internal static void Load()
        {
            if (CanLoad == false)
            {
                Logger.Warn("当前不可加载模组");
                ContentPatch.PrintTry("当前不可加载模组");
                return;
            }

            if (System.Threading.Interlocked.CompareExchange(ref _loadInFlight, 1, 0) != 0)
            {
                Logger.Warn("模组正在加载中，已忽略重复请求");
                ContentPatch.PrintTry("模组正在加载中");
                return;
            }

            Logger.Info("开始加载模组...");

            ConsoleUtils.Clear();
            ContentPatch.PrintTry("加载模组");

            OnModLoad_Start?.Invoke(modLoader);

            Task.Run(() =>
            {
                try
                {
                    if (loadedMod != null)
                        if (Unload() == false) return;
                    loadedMod = null;

                    _ = modLoader.Load();

                    Logger.Info("加载模组成功");
                    ContentPatch.PrintTry("加载完成");
                    OnModLoad_Ok?.Invoke();//成功
                }
                catch (TaskCanceledException)
                {
                    Logger.Warn("加载模组取消");
                    ContentPatch.PrintTry("加载取消");

                    OnModLoad_Cancel?.Invoke(modLoader);//取消

                    if (Unload() == false) return;

                    OnModLoad_Canceled?.Invoke();
                }
                catch (Exception ex)
                {
                    Logger.Error($"加载模组失败: {ex.Message}", ex);
                    ContentPatch.PrintTry("加载失败");

                    OnModLoad_Exception?.Invoke(ex);//失败

                    if (Unload() == false) return;
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref _loadInFlight, 0);
                }
            });
        }

        internal static void CancelLoad()
        {
            Logger.Warn("取消加载模组");
            ConsoleUtils.Clear();
            ContentPatch.PrintTry("取消加载");

            modLoader.CancelLoad();
        }

        private static bool Unload()
        {
            try
            {
                Logger.Info("正在卸载模组...");
                ContentPatch.PrintTry("卸载");

                loadedMod = null;
                modLoader.Unload();
                TPML.Content.KeybindLoader.Unload();

                Logger.Info("卸载模组完成");
                ContentPatch.PrintTry("卸载完成");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"卸载失败: {ex.Message}", ex);
                ContentPatch.PrintTry("卸载失败");

                OnModUnload_Exception?.Invoke(ex);

                return false;
            }
        }
    }
}
