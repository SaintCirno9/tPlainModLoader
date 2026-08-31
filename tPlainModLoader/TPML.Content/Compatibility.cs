// ====================================================================
// TPML.Content 现代化向后兼容桥接层
// 为旧式或过渡代码提供 Terraria.ModLoader 命名空间下的类型映射
// ====================================================================
using System;
using System.Collections.Generic;

namespace Terraria.ModLoader
{
    public abstract class Mod : TPML.Content.Mod { }
    public abstract class ModItem : TPML.Content.ModItem { }
    public abstract class ModSystem : TPML.Content.ModSystem { }
    public abstract class ModPlayer : TPML.Content.ModPlayer { }
    public abstract class GlobalItem : TPML.Content.GlobalItem { }
    public abstract class ModType : TPML.Content.ModType { }
    public interface ILoadable : TPML.Content.ILoadable { }
    public class TooltipLine : TPML.Content.UI.TooltipLine
    {
        public TooltipLine(TPML.Content.Mod mod, string name, string text) : base(mod, name, text) { }
    }
    public class EntitySource_Misc : TPML.Content.EntitySource_Misc
    {
        public EntitySource_Misc(string context) : base(context) { }
    }

    public static class ModPlayerExtensions
    {
        public static T GetModPlayer<T>(this Terraria.Player player) where T : TPML.Content.ModPlayer
            => TPML.Content.ModPlayerExtensions.GetModPlayer<T>(player);

        public static bool TryGetModPlayer<T>(this Terraria.Player player, out T result) where T : TPML.Content.ModPlayer
            => TPML.Content.ModPlayerExtensions.TryGetModPlayer(player, out result);

        public static Terraria.DataStructures.IEntitySource GetSource_Misc(this Terraria.Player player, string context)
            => TPML.Content.ModPlayerExtensions.GetSource_Misc(player, context);

        public static bool HasBuff(this Terraria.Player player, int type)
            => TPML.Content.PlayerExtensions.HasBuff(player, type);

        public static bool CanAfford(this Terraria.Player player, long price, int customCurrency = -1)
            => TPML.Content.PlayerExtensions.CanAfford(player, price, customCurrency);
    }

    public static class ItemExtensions
    {
        public static void CloneDefaults(this Terraria.Item item, int typeToClone)
            => TPML.Content.ItemExtensions.CloneDefaults(item, typeToClone);

        public static TPML.Content.ModItem GetModItem(this Terraria.Item item)
            => TPML.Content.ItemExtensions.GetModItem(item);

        public static T GetModItem<T>(this Terraria.Item item) where T : TPML.Content.ModItem
            => TPML.Content.ItemExtensions.GetModItem<T>(item);

        public static bool IsNotSameTypePrefixAndStack(this Terraria.Item item, Terraria.Item compareItem)
            => TPML.Content.ItemExtensions.IsNotSameTypePrefixAndStack(item, compareItem);
    }

    public static class NPCExtensions
    {
        public static bool HasBuff(this Terraria.NPC npc, int type)
            => TPML.Content.NPCExtensions.HasBuff(npc, type);
    }

    public static class ModContent
    {
        public static IReadOnlyCollection<TPML.Content.Mod> Mods => TPML.Content.ModContent.Mods;
        public static void RegisterMod(TPML.Content.Mod mod) => TPML.Content.ModContent.RegisterMod(mod);
        public static void RegisterContent(TPML.Content.ILoadable content) => TPML.Content.ModContent.RegisterContent(content);
        public static void RegisterItemType(Type type, int id) => TPML.Content.ModContent.RegisterItemType(type, id);
        public static void Clear() => TPML.Content.ModContent.Clear();
        public static T GetInstance<T>() where T : class => TPML.Content.ModContent.GetInstance<T>();
        public static bool TryFind<T>(string fullName, out T value) where T : class => TPML.Content.ModContent.TryFind<T>(fullName, out value);
        public static T Find<T>(string fullName) where T : class => TPML.Content.ModContent.Find<T>(fullName);
        public static int ItemType<T>() where T : TPML.Content.ModItem => TPML.Content.ModContent.ItemType<T>();
        public static IEnumerable<T> GetContent<T>() where T : class => TPML.Content.ModContent.GetContent<T>();
        public static bool TryGetMod(string name, out TPML.Content.Mod mod) => TPML.Content.ModContent.TryGetMod(name, out mod);
        public static TPML.Content.Mod GetMod(string name) => TPML.Content.ModContent.GetMod(name);
        public static ReLogic.Content.Asset<T> Request<T>(string path, ReLogic.Content.AssetRequestMode mode = ReLogic.Content.AssetRequestMode.ImmediateLoad) where T : class =>
            TPML.Content.ModContent.Request<T>(path, mode);
    }

    public static class MonoModHooks
    {
        public static void Add(System.Reflection.MethodBase target, Delegate hookDelegate)
            => TPML.Content.Engine.MonoModHooks.Add(target, hookDelegate);

        public static void Modify(System.Reflection.MethodBase target, MonoMod.Cil.ILContext.Manipulator callback)
            => TPML.Content.Engine.MonoModHooks.Modify(target, callback);

        public static void RequestNativeAccess() => TPML.Content.Engine.MonoModHooks.RequestNativeAccess();
        public static void DumpILHooks() => TPML.Content.Engine.MonoModHooks.DumpILHooks();
        public static void DumpOnHooks() => TPML.Content.Engine.MonoModHooks.DumpOnHooks();
        public static void DumpIL(TPML.Content.Mod mod, MonoMod.Cil.ILContext il) => TPML.Content.Engine.MonoModHooks.DumpIL(mod, il);
    }

    public static class ItemLoader
    {
        public const int ModItemOffset = TPML.Content.ItemLoader.ModItemOffset;
        public static int ItemCount => TPML.Content.ItemLoader.ItemCount;
        public static int NextItemID => TPML.Content.ItemLoader.NextItemID;
        public static IReadOnlyCollection<TPML.Content.ModItem> Items => TPML.Content.ItemLoader.Items;
        public static int Register(TPML.Content.ModItem item) => TPML.Content.ItemLoader.Register(item);
        public static void ReloadTextures() => TPML.Content.ItemLoader.ReloadTextures();
        public static void LoadItemTexture(TPML.Content.ModItem item) => TPML.Content.ItemLoader.LoadItemTexture(item);
        public static TPML.Content.ModItem GetItem(int type) => TPML.Content.ItemLoader.GetItem(type);
        public static void SetDisplayName(int type, string name) => TPML.Content.ItemLoader.SetDisplayName(type, name);
        public static string GetDisplayName(int type) => TPML.Content.ItemLoader.GetDisplayName(type);
        public static void SetTooltip(int type, string tooltip) => TPML.Content.ItemLoader.SetTooltip(type, tooltip);
        public static string GetTooltip(int type) => TPML.Content.ItemLoader.GetTooltip(type);
        public static void SetDefaults(Terraria.Item item) => TPML.Content.ItemLoader.SetDefaults(item);
        public static void Clear() => TPML.Content.ItemLoader.Clear();
    }

    public static class RecipeLoader
    {
        public static TPML.Content.ModRecipe CreateRecipe(int resultType, int amount = 1) => TPML.Content.RecipeLoader.CreateRecipe(resultType, amount);
        public static TPML.Content.ModRecipe CreateRecipe(TPML.Content.ModItem item, int amount = 1) => TPML.Content.RecipeLoader.CreateRecipe(item, amount);
        public static void SetupRecipes() => TPML.Content.RecipeLoader.SetupRecipes();
        public static void Clear() => TPML.Content.RecipeLoader.Clear();
    }

    public static class ModLoader
    {
        public static string Version => TPML.Content.ModLoader.Version;
        public static Action<string> LogCallback
        {
            get => TPML.Content.ModLoader.LogCallback;
            set => TPML.Content.ModLoader.LogCallback = value;
        }
        public static void Log(string message) => TPML.Content.ModLoader.Log(message);
        public static bool TryGetMod(string name, out TPML.Content.Mod mod) => TPML.Content.ModLoader.TryGetMod(name, out mod);
        public static TPML.Content.Mod GetMod(string name) => TPML.Content.ModLoader.GetMod(name);
    }
}

namespace Terraria.ModLoader.Engine
{
    public static class ContentHookDispatcher
    {
        public static void Initialize(string harmonyId = "TPML.Content.HookDispatcher") => TPML.Content.Engine.ContentHookDispatcher.Initialize(harmonyId);
        public static void RegisterHookInstances(IEnumerable<TPML.Content.ILoadable> contents) => TPML.Content.Engine.ContentHookDispatcher.RegisterHookInstances(contents);
        public static void Clear() => TPML.Content.Engine.ContentHookDispatcher.Clear();
    }

    [Obsolete("请改用 TPML.Content.Engine.ContentHookDispatcher")]
    public static class TModHookDispatcher
    {
        public static void Initialize(string harmonyId = "TPML.Content.HookDispatcher") => TPML.Content.Engine.ContentHookDispatcher.Initialize(harmonyId);
        public static void RegisterHookInstances(IEnumerable<TPML.Content.ILoadable> contents) => TPML.Content.Engine.ContentHookDispatcher.RegisterHookInstances(contents);
        public static void Clear() => TPML.Content.Engine.ContentHookDispatcher.Clear();
    }
}

namespace Terraria.ModLoader.IO
{
    public static class SidecarSaveManager
    {
        public static string SaveDirectory => TPML.Content.IO.SidecarSaveManager.SaveDirectory;
    }
}

namespace Terraria.ModLoader.Fusion
{
    public interface IFusionItemSource : TPML.Content.Fusion.IFusionItemSource { }
    public static class InventoryFusionManager
    {
        public static void RegisterSource(TPML.Content.Fusion.IFusionItemSource source) => TPML.Content.Fusion.InventoryFusionManager.RegisterSource(source);
        public static void UnregisterSource(string id) => TPML.Content.Fusion.InventoryFusionManager.UnregisterSource(id);
        public static void Clear() => TPML.Content.Fusion.InventoryFusionManager.Clear();
        public static bool HasItem(Terraria.Player player, int type) => TPML.Content.Fusion.InventoryFusionManager.HasItem(player, type);
        public static int CountItem(Terraria.Player player, int type, int stopCountingAt = 0) => TPML.Content.Fusion.InventoryFusionManager.CountItem(player, type, stopCountingAt);
        public static bool ConsumeItem(Terraria.Player player, int type, bool reverseOrder = false) => TPML.Content.Fusion.InventoryFusionManager.ConsumeItem(player, type, reverseOrder);
    }
}
