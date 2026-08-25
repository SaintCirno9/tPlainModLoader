using Terraria;
using Terraria.IO;
using Terraria.Localization;

namespace tContentPatch
{
    /// <summary/>
    public abstract class PatchPlayer
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public virtual void Initialize() { }
        /// <summary>
        /// <see cref="Player.Update(int)"/>前调用
        /// </summary>
        public virtual void UpdatePrefix(Player This, int playerI) { }
        /// <summary>
        /// <see cref="Player.Update(int)"/>后调用
        /// </summary>
        public virtual void UpdatePostfix(Player This, int playerI) { }
        /// <summary>
        public virtual void UpdateEquipsPrefix(Player This, int playerI) { }
        /// <summary>
        public virtual void UpdateEquipsPostfix(Player This, int playerI) { }
        /// <summary/>
        public virtual void UpdateArmorSetsPostfix(Player This, int playerI) { }
        /// <summary>
        /// 保存玩家数据前, 单人和客户端有效
        /// </summary>
        public virtual void SavePlayerPrefix(PlayerFileData playerFile, bool skipMapSave) { }
        /// <summary>
        /// 保存玩家数据后, 单人和客户端有效
        /// </summary>
        public virtual void SavePlayerPostfix(PlayerFileData playerFile, bool skipMapSave) { }
        /// <summary>
        /// 读取玩家数据后, 单人和客户端有效
        /// </summary>
        public virtual void LoadPlayerPostfix(PlayerFileData playerFile) { }
        /// <summary>
        /// 激活玩家为当前控制角色后调用
        /// </summary>
        public virtual void SetAsActivePostfix(PlayerFileData playerFile) { }
        /// <summary>
        /// 能否掉落墓碑
        /// </summary>
        public virtual bool CanDropTombstone(Player This, long coinsOwned, NetworkText deathText, int hitDirection) => true;
    }
}
