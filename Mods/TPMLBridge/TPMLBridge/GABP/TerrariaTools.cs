using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TPMLBridge.GABP.Tools;

namespace TPMLBridge.GABP
{
    /// <summary>
    /// GABP 自动化工具统一路由与调度总线
    /// 作者: SaintCirno9
    /// </summary>
    public static class TerrariaTools
    {
        public static int PendingHoldUseFrames = 0;
        public static bool PendingHoldAlt = false;

        public static List<GABPToolDescriptor> GetDescriptors()
        {
            var list = new List<GABPToolDescriptor>();
            list.AddRange(LifecycleTools.GetDescriptors());
            list.AddRange(PlayerInventoryTools.GetDescriptors());
            list.AddRange(CreativeInventoryTools.GetDescriptors());
            list.AddRange(InstavatorTools.GetDescriptors());
            list.AddRange(ItemContainerTools.GetDescriptors());
            list.AddRange(AccessoryBagTools.GetDescriptors());
            list.AddRange(SidecarTools.GetDescriptors());
            list.AddRange(ScreenCaptureTools.GetDescriptors());
            list.AddRange(RecipeBrowserTools.GetDescriptors());
            return list;
        }

        public static async Task<object> CallToolAsync(string name, JObject args)
        {
            // 1. 生命周期、世界、指令与存档保护工具
            var result = await LifecycleTools.HandleAsync(name, args);
            if (result != null) return result;

            // 2. 玩家实体、背包、快捷栏与输入模拟工具
            result = await PlayerInventoryTools.HandleAsync(name, args);
            if (result != null) return result;

            // 3. 创造模式物品浏览器 UI 工具
            result = await CreativeInventoryTools.HandleAsync(name, args);
            if (result != null) return result;

            // 4. Instavator 直通车与矿道物理扫描工具
            result = await InstavatorTools.HandleAsync(name, args);
            if (result != null) return result;

            // 5. 药水袋与旗帜盒收纳容器工具
            result = await ItemContainerTools.HandleAsync(name, args);
            if (result != null) return result;

            // 6. 随身饰品袋独立实体与属性挂载工具
            result = await AccessoryBagTools.HandleAsync(name, args);
            if (result != null) return result;

            // 7. Sidecar 模组物品全域持久化工具
            result = await SidecarTools.HandleAsync(name, args);
            if (result != null) return result;

            // 8. 游戏内截图与 UI 捕获工具
            result = await ScreenCaptureTools.HandleAsync(name, args);
            if (result != null) return result;

            // 9. RecipeBrowser 合成表与物品图鉴工具
            result = await RecipeBrowserTools.HandleAsync(name, args);
            if (result != null) return result;

            throw new KeyNotFoundException($"未知的工具名称: {name}");
        }
    }
}
