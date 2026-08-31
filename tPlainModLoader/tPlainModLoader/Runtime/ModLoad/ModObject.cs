using System.Collections.Generic;
using System.Reflection;
using tContentPatch.Utils;

namespace tContentPatch.ModLoad
{
    /// <summary>
    /// 模组对象, inheritance_xxx的可能为<see langword="null"/>
    /// </summary>
    public class ModObject
    {
        /// <exception cref="System.ArgumentNullException"></exception>
        public ModObject(ModConfig config)
        {
            if (config == null) throw new System.ArgumentNullException(nameof(config));

            this.config = config;
        }

        /// <summary>
        /// 模组文件夹
        /// </summary>
        public string modPath = null;
        /// <summary>
        /// 模组程序集
        /// </summary>
        public Assembly assembly = null;
        /// <summary>
        /// 模组加载配置
        /// </summary>
        public ModConfig config = null;
        /// <summary>
        /// 模组信息
        /// </summary>
        public ModInfo info = null;
        /// <summary>
        /// 继承了<see cref="Mod"/>的类
        /// </summary>
        public List<Mod> inheritance_mod = null;
        /// <summary>
        /// 继承了<see cref="ModSetting"/>的类
        /// </summary>
        public List<ModSetting> inheritance_setting = null;
        /// <summary/>
        public List<ModNetPacket> inheritance_netPacket = null;
        /// <summary>
        /// 继承了<see cref="PatchMain"/>的类
        /// </summary>
        public List<PatchMain> inheritance_patchMain = null;
        /// <summary>
        /// 继承了<see cref="PatchPlayer"/>的类
        /// </summary>
        public List<PatchPlayer> inheritance_patchPlayer = null;
        /// <summary/>
        public List<PatchNPC> inheritance_patchNPC = null;
        /// <summary/>
        public List<PatchItem> inheritance_patchItem = null;
        /// <summary/>
        public List<PatchProjectile> inheritance_patchProjectile = null;
        /// <summary/>
        public List<PatchTileLightScanner> inheritance_patchTileLightScanner = null;
        /// <summary/>
        public List<PatchRemadeChatMonitor> inheritance_patchRemadeChatMonitor = null;
        /// <summary/>
        public List<PatchWorldFile> inheritance_patchWorldFile = null;
        /// <summary/>
        public List<PatchNetMessage> inheritance_patchNetMessage = null;
        /// <summary/>
        public List<PatchMessageBuffer> inheritance_patchMessageBuffer = null;
        /// <summary/>
        public List<PatchChest> inheritance_patchChest = null;
        /// <summary/>
        public List<PatchRemoteClient> inheritance_patchRemoteClient = null;
        /// <summary/>
        public List<PatchWorldGen> inheritance_patchWorldGen = null;

        /// <summary>
        /// 复制模组对象的字段, <see cref="config"/>,<see cref="info"/>也为复制对象
        /// </summary>
        /// <param name="mo"></param>
        /// <returns></returns>
        public static ModObject Copy(ModObject mo)
        {
            mo = CopyClass.CopyField(mo, mo.config);
            if (mo.config != null) mo.config = ModConfig.Copy(mo.config);
            if (mo.info != null) mo.info = ModInfo.Copy(mo.info);

            return mo;
        }
    }
}
