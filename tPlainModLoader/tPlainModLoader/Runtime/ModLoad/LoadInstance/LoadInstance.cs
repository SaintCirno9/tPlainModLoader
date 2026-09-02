using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPML.Content;

namespace tContentPatch.ModLoad
{
    internal partial class LoadInstance
    {
        private readonly IModLoader loadAssembly = null;

        private bool isCancel = false;
        private LoadState state = LoadState.None;

        private string stateText = string.Empty;
        private int progressV = 0;
        private int progressMax = 0;

        public LoadInstance(IModLoader loadAssembly)
        {
            if (loadAssembly == null) throw new ArgumentNullException(nameof(loadAssembly));
            this.loadAssembly = loadAssembly;
        }

        private void CreateModInstance(List<ModObject> mos)
        {
            progressV = 0;
            progressMax = mos.Count;
            stateText = "创建模组实例";

            foreach (ModObject mo in mos)
            {
                try
                {
                    if (mo.assembly == null) continue;

                    stateText = $"创建模组实例:{mo.info?.name ?? mo.config.key}";

                    CheckLoadCancel();

                    mo.inheritance_mod = Utils.CreateInstance<Mod>(mo.assembly);
                    ContentHost.RegisterFromAssembly(mo.assembly);

                    CheckLoadCancel();
                    mo.inheritance_setting = Utils.CreateInstance<ModSetting>(mo.assembly);

                    CheckLoadCancel();
                    mo.inheritance_netPacket = Utils.CreateInstance<ModNetPacket>(mo.assembly);
                }
                catch (Exception ex)
                {
                    throw new Exception($"创建模组实例失败:{stateText}", ex);
                }
                finally
                {
                    ++progressV;
                }
            }
        }

        private void CheckLoadCancel()
        {
            if (isCancel) throw new TaskCanceledException();
        }
    }
}
