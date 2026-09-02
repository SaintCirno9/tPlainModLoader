using System;
using Terraria;
using Terraria.IO;
using Terraria.Localization;
using TPML.Content;

namespace tContentPatch
{
    /// <summary>
    /// 玩家生命周期兼容基类（建议直接继承 <see cref="TPML.Content.ModPlayer"/>）。
    /// 作者: SaintCirno9
    /// </summary>
    public abstract class PatchPlayer : TPML.Content.ModPlayer
    {
        /// <summary>
        /// <see cref="Mod.Loaded"/>后调用
        /// </summary>
        public override void Initialize() { }
        /// <summary>
        /// <see cref="Player.Update(int)"/>前调用
        /// </summary>
        public override void UpdatePrefix(Player This, int playerI) { }
        /// <summary>
        /// <see cref="Player.Update(int)"/>后调用
        /// </summary>
        public override void UpdatePostfix(Player This, int playerI) { }
        /// <summary>
        public override void UpdateEquipsPrefix(Player This, int playerI) { }
        /// <summary>
        public override void UpdateEquipsPostfix(Player This, int playerI) { }
        /// <summary/>
        public override void UpdateArmorSetsPostfix(Player This, int playerI) { }
        /// <summary>
        /// 保存玩家数据前, 单人和客户端有效
        /// </summary>
        public override void SavePlayerPrefix(PlayerFileData playerFile, bool skipMapSave) { }
        /// <summary>
        /// 保存玩家数据后, 单人和客户端有效
        /// </summary>
        public override void SavePlayerPostfix(PlayerFileData playerFile, bool skipMapSave) { }
        /// <summary>
        /// 读取玩家数据后, 单人和客户端有效
        /// </summary>
        public override void LoadPlayerPostfix(PlayerFileData playerFile) { }
        /// <summary>
        /// 激活玩家为当前控制角色后调用
        /// </summary>
        public override void SetAsActivePostfix(PlayerFileData playerFile) { }
        /// <summary>
        /// 能否掉落墓碑
        /// </summary>
        public override bool CanDropTombstone(Player This, long coinsOwned, NetworkText deathText, int hitDirection) => true;
    }
}

