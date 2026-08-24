using CommandHelp;
using OptimizeAndTool.Utils;
using OptimizeAndTool.Utils.quickBuild;
using System.Collections.Generic;
using tContentPatch;
using Terraria;
using Terraria.UI;

namespace OptimizeAndTool.Content.Cheat.HeldItemModify
{
    internal class ValSet : PatchPlayer
    {
        public static GetSetReset<bool> useTime = new GetSetReset<bool>();
        public static GetSetReset<int> useTime_val = new GetSetReset<int>();
        public static GetSetReset<bool> useAnimation = new GetSetReset<bool>();
        public static GetSetReset<int> useAnimation_val = new GetSetReset<int>();
        public static GetSetReset<bool> shootSpeed = new GetSetReset<bool>();
        public static GetSetReset<float> shootSpeed_val = new GetSetReset<float>();
        public static GetSetReset<bool> shoot = new GetSetReset<bool>();
        public static GetSetReset<int> shoot_val = new GetSetReset<int>();
        public static GetSetReset<bool> tileBoost = new GetSetReset<bool>();
        public static GetSetReset<int> tileBoost_val = new GetSetReset<int>();

        private static Item _lastModifiedItem = null;
        private static bool _lastUseTime = false;
        private static bool _lastUseAnimation = false;
        private static bool _lastShootSpeed = false;
        private static bool _lastShoot = false;
        private static bool _lastTileBoost = false;

        public override void Initialize()
        {
            _lastModifiedItem = null;
            _lastUseTime = false;
            _lastUseAnimation = false;
            _lastShootSpeed = false;
            _lastShoot = false;
            _lastTileBoost = false;
        }

        public override void SavePlayerPrefix(Terraria.IO.PlayerFileData playerFile, bool skipMapSave)
        {
            if (_lastModifiedItem != null)
            {
                ResetItemProperties(_lastModifiedItem);
                _lastModifiedItem = null;
            }
        }

        public static void ResetItemProperties(Item item)
        {
            if (item == null || item.type <= Terraria.ID.ItemID.None) return;
            int prefix = item.prefix;
            int stack = item.stack;
            bool favorited = item.favorited;

            item.SetDefaults(item.type);
            item.stack = stack;
            item.favorited = favorited;
            if (prefix > 0)
            {
                item.Prefix(prefix);
            }
        }

        public override void UpdatePrefix(Player This, int playerI)
        {
            if (This != Main.LocalPlayer) return;

            Item item = This.HeldItem;
            bool anyActive = useTime.val || useAnimation.val || shootSpeed.val || shoot.val || tileBoost.val;

            // 若之前修改过物品，但在切换手持物品或全部关闭时还原原物品
            if (_lastModifiedItem != null && (!anyActive || _lastModifiedItem != item))
            {
                ResetItemProperties(_lastModifiedItem);
                _lastModifiedItem = null;
            }

            if (!anyActive || item == null || item.type <= Terraria.ID.ItemID.None)
            {
                _lastUseTime = false;
                _lastUseAnimation = false;
                _lastShootSpeed = false;
                _lastShoot = false;
                _lastTileBoost = false;
                return;
            }

            // 若某个开关单独从开启变为关闭，先还原一次物品以恢复被关闭属性的原生值
            bool switchTurnedOff = (_lastUseTime && !useTime.val) ||
                                   (_lastUseAnimation && !useAnimation.val) ||
                                   (_lastShootSpeed && !shootSpeed.val) ||
                                   (_lastShoot && !shoot.val) ||
                                   (_lastTileBoost && !tileBoost.val);

            if (switchTurnedOff)
            {
                ResetItemProperties(item);
            }

            _lastModifiedItem = item;
            _lastUseTime = useTime.val;
            _lastUseAnimation = useAnimation.val;
            _lastShootSpeed = shootSpeed.val;
            _lastShoot = shoot.val;
            _lastTileBoost = tileBoost.val;

            if (useTime.val)
            {
                item.useTime = useTime_val.val;
            }
            if (useAnimation.val)
            {
                item.useAnimation = useAnimation_val.val;
            }
            if (shootSpeed.val)
            {
                item.shootSpeed = shootSpeed_val.val;
            }
            if (shoot.val)
            {
                item.shoot = shoot_val.val;
            }
            if (tileBoost.val)
            {
                item.tileBoost = tileBoost_val.val;
            }
        }

        public static List<CommandObject> GetCO()
        {
            List<CommandObject> cos = new List<CommandObject>
            {
                CommandBuild.get1("useTime", useTime, useTime_val, new CommandInt()),
                CommandBuild.get1("useAnimation", useAnimation, useAnimation_val, new CommandInt()),
                CommandBuild.get1("shootSpeed", shootSpeed, shootSpeed_val, new CommandFloat()),
                CommandBuild.get1("shoot", shoot, shoot_val, new CommandInt()),
                CommandBuild.get1("tileBoost", tileBoost, tileBoost_val, new CommandInt()),
            };

            return cos;
        }

        public static List<UIElement> GetUI()
        {
            List<UIElement> uis = new List<UIElement>
            {
                UIBuild.get1(useTime, useTime_val, int.Parse, "物品使用后需要经过几帧的时间才能再次使用<int>", null, "物品使用帧数"),
                UIBuild.get1(useAnimation, useAnimation_val, int.Parse, "物品使用后动画会播放几帧<int>", null, "物品动画帧数"),
                UIBuild.get1(shootSpeed, shootSpeed_val, float.Parse, "<float>", null, "射弹速度"),
                UIBuild.get1(shoot, shoot_val, int.Parse, "<int>", null, "射弹id"),
                UIBuild.get1(tileBoost, tileBoost_val, int.Parse, "<int>", null, "物品使用距离"),
            };

            return uis;
        }
    }
}
