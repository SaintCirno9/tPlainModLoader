using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using TPML.Content;

namespace TPML.ModLoad
{
    internal partial class ModLoader
    {
        private void Initialize_Mod(List<ModObject> mods)
        {
            progressV = 0;
            progressMax = 1;
            stateText = "初始化模组";

            Func<ModObject, Exception, string> exMess = (m, ex) => $"初始化模组[{m.info?.name ?? m.config.key}]失败:{ex.Message}";
            Func<ModObject, Exception, string> exMess2 = (m, ex) => $"添加模组[{m.info?.name ?? m.config.key}]的补丁失败:{ex.Message}";

            Action<ModObject>[] action = new Action<ModObject>[] {
                mo =>
                {
                    stateText = $"初始化模组:{mo.info?.name ?? mo.config.key}";

                    Utils.ForHelp(mo.inheritance_mod, item => item.Load(), ex => exMess(mo, ex));
                },
                mo =>
                {
                    stateText = $"初始化模组设置:{mo.info?.name ?? mo.config.key}";

                    Utils.ForHelp(mo.inheritance_setting, item => LoadModSet(mo, item), ex => exMess(mo, ex));
                },
                mo =>
                {
                    stateText = $"初始化模组:{mo.info?.name ?? mo.config.key}";

                    Utils.ForHelp(mo.inheritance_mod, item => item.Loaded(), ex => exMess(mo, ex));
                },
                mo =>
                {
                    stateText = $"初始化模组:{mo.info?.name ?? mo.config.key}";

                    Utils.ForHelp(mo.inheritance_netPacket, item => item.Initialize(), ex => exMess(mo, ex));
                },
                mo =>
                {
                    string text = $"注册网络包:{mo.info?.name ?? mo.config.key}";
                    stateText = text;

                    try
                    {
                        TPML.Network.ModNetworkPacket.Register(mo.inheritance_netPacket);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"失败:{text}", ex);
                    }
                },
                mo =>
                {
                    stateText = $"添加模组补丁:{mo.info?.name ?? mo.config.key}";

                    Utils.ForHelp(mo.inheritance_mod, item => item.AddPatch(modPatch), ex => exMess2(mo, ex));
                }
            };

            progressMax = mods.Count * action.Length;

            foreach (Action<ModObject> i in action)
            {
                CheckLoadCancel();

                foreach (ModObject mo in mods)
                {
                    i(mo);
                    ++progressV;
                }
            }

            // 所有模组的 Load/Loaded 完成后，统一构建并注入 TPML.Content 配方
            ContentHost.CompleteLoading();
        }

        private void Initialize_SetupDrawInterfaceLayers()
        {
            progressV = 0;
            progressMax = 1;
            stateText = "初始化UI";

            Main.instance._needToSetupDrawInterfaceLayers = true;

            progressV = 1;
        }
    }
}
