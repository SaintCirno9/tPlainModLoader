using System;
using System.Collections.Generic;

namespace TPML.ModLoad
{
    internal partial class LoadInstance : IModLoader
    {
        public List<ModObject> Load()
        {
            try
            {
                if (state == LoadState.Loading) throw new Exception("加载中不可再次加载");
                if (state == LoadState.Unloading) throw new Exception("卸载中不可加载");

                state = LoadState.Loading;

                progressV = 0;
                progressMax = 0;
                stateText = string.Empty;

                List<ModObject> modList = loadAssembly.Load();
                CheckLoadCancel();

                CreateModInstance(modList);
                CheckLoadCancel();

                return modList;
            }
            finally
            {
                state = LoadState.Loaded;
            }
        }

        public void Unload()
        {
            if (state == LoadState.Loading) throw new Exception("加载中不可卸载");
            if (state == LoadState.Canceling) throw new Exception("取消加载中不可卸载");
            if (state == LoadState.Unloading) return;

            state = LoadState.Unloading;

            loadAssembly.Unload();

            state = LoadState.None;
        }

        public void CancelLoad()
        {
            if (state == LoadState.Canceling) return;
            if (state == LoadState.Unloading) return;

            isCancel = true;
            while (state == LoadState.Loading)
            {
                System.Threading.Thread.Sleep(10);
            }

            isCancel = false;
            state = LoadState.None;
        }

        public string GetTip() => loadAssembly.IsLoading() ? loadAssembly.GetTip() : stateText;

        public bool IsLoading() => state == LoadState.Loading;

        public void ProgressBar(out int val, out int max)
        {
            if (loadAssembly.IsLoading())
            {
                loadAssembly.ProgressBar(out int subVal, out int subMax);
                val = subMax > 0 ? (int)(40.0 * Math.Min(subVal, subMax) / subMax) : 0;
            }
            else
            {
                val = 40 + (progressMax > 0 ? (int)(60.0 * Math.Min(progressV, progressMax) / progressMax) : 0);
            }
            max = 100;
        }
    }
}
