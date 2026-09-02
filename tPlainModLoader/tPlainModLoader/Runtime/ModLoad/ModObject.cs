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

        public List<PatchMain> inheritance_patchMain = new List<PatchMain>();
        public List<PatchPlayer> inheritance_patchPlayer = new List<PatchPlayer>();
        public List<PatchNPC> inheritance_patchNPC = new List<PatchNPC>();
        public List<PatchItem> inheritance_patchItem = new List<PatchItem>();
        public List<PatchProjectile> inheritance_patchProjectile = new List<PatchProjectile>();
        public List<PatchTileLightScanner> inheritance_patchTileLightScanner = new List<PatchTileLightScanner>();
        public List<PatchRemadeChatMonitor> inheritance_patchRemadeChatMonitor = new List<PatchRemadeChatMonitor>();
        public List<PatchWorldFile> inheritance_patchWorldFile = new List<PatchWorldFile>();
        public List<PatchNetMessage> inheritance_patchNetMessage = new List<PatchNetMessage>();
        public List<PatchMessageBuffer> inheritance_patchMessageBuffer = new List<PatchMessageBuffer>();
        public List<PatchChest> inheritance_patchChest = new List<PatchChest>();
        public List<PatchRemoteClient> inheritance_patchRemoteClient = new List<PatchRemoteClient>();
        public List<PatchWorldGen> inheritance_patchWorldGen = new List<PatchWorldGen>();

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
