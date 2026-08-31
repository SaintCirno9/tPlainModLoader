using System;
using System.Collections.Generic;
using TPML.Core.Logging;

namespace tContentPatch.ModLoad
{
    internal class Intercept : IModLoader
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Intercept");
        public Action<List<ModObject>> OnLoaded = null;
        public Action<Exception> OnLoadException = null;
        private IModLoader ml = null;
        private List<ModObject> mos = null;

        public Intercept(IModLoader ml)
        {
            this.ml = ml;
        }

        public List<ModObject> Load()
        {
            try
            {
                mos = ml.Load();
            }
            catch (Exception ex)
            {
                Logger.Error($"模组加载异常: {ex.Message}", ex);
                OnLoadException?.Invoke(ex);
                throw;
            }
            OnLoaded?.Invoke(mos);
            return mos;
        }

        public void Unload() => ml.Unload();

        public void CancelLoad() => ml.CancelLoad();

        public string GetTip() => ml.GetTip();

        public bool IsLoading() => ml.IsLoading();

        public void ProgressBar(out int val, out int max) => ml.ProgressBar(out val, out max);
    }
}
