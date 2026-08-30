using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Terraria;
using Terraria.ID;
using TPML.Content;
using TPML.Content.IO;

namespace TPMLBridge.GABP.Tools
{
    /// <summary>
    /// PotionSlots 自动化测试工具集
    /// 作者: SaintCirno9
    /// </summary>
    public static class PotionSlotTools
    {
        public static List<GABPToolDescriptor> GetDescriptors()
        {
            return new List<GABPToolDescriptor>
            {
                new GABPToolDescriptor
                {
                    Name = "tpml/test_potion_slots",
                    Description = "执行 PotionSlots 完整功能回归测试（槽位存储、QuickHeal 钩子、QuickMana 钩子、自动拾取与 Sidecar 持久化）。",
                    Tags = new List<string> { "testing", "potion_slots" },
                    InputSchema = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            };
        }

        public static async Task<object> HandleAsync(string name, JObject args)
        {
            switch (name)
            {
                case "tpml/test_potion_slots":
                case "tpml_test_potion_slots":
                    return await MainThreadQueue.EnqueueAsync(() => TestPotionSlots());

                default:
                    return null;
            }
        }

        private static object TestPotionSlots()
        {
            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                return new { success = false, message = "当前不在世界中，无法测试 PotionSlots。" };
            }

            var player = Main.LocalPlayer;
            var mod = ModLoader.GetMod("PotionSlots");
            if (mod == null)
            {
                return new { success = false, message = "PotionSlots 模组未加载！" };
            }

            var potionPlayerType = mod.GetType().Assembly.GetType("PotionSlots.Core.PotionStoragePlayer");
            if (potionPlayerType == null)
            {
                return new { success = false, message = "未找到 PotionStoragePlayer 类型！" };
            }

            var getModPlayerMethod = typeof(ModPlayerExtensions).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .First(m => m.Name == "GetModPlayer" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
                .MakeGenericMethod(potionPlayerType);
            var psp = getModPlayerMethod.Invoke(null, new object[] { player });
            if (psp == null)
            {
                return new { success = false, message = "无法获取当前玩家的 PotionStoragePlayer 实例！" };
            }

            var lifeField = potionPlayerType.GetField("lifeSlot");
            var manaField = potionPlayerType.GetField("manaSlot");
            var wormField = potionPlayerType.GetField("wormholeSlot");

            var testSteps = new List<object>();

            // Step 1: 槽位初始化与直接赋值
            var healPotion = new Item();
            healPotion.SetDefaults(ItemID.LesserHealingPotion);
            healPotion.stack = 10;
            lifeField.SetValue(psp, healPotion);

            var manaPotion = new Item();
            manaPotion.SetDefaults(ItemID.LesserManaPotion);
            manaPotion.stack = 15;
            manaField.SetValue(psp, manaPotion);

            var wormPotion = new Item();
            wormPotion.SetDefaults(ItemID.WormholePotion);
            wormPotion.stack = 5;
            wormField.SetValue(psp, wormPotion);

            testSteps.Add(new { step = "1. 槽位赋值", status = "PASS", details = "成功设置 lifeSlot(10), manaSlot(15), wormholeSlot(5)" });

            // Step 2: 测试 QuickHeal 钩子拦截
            player.statLife = 20; // 扣血
            player.potionDelay = 0; // 清除冷却
            int lifeBefore = player.statLife;
            player.QuickHeal();
            var curLifeSlot = (Item)lifeField.GetValue(psp);
            int lifeAfter = player.statLife;
            bool healSuccess = lifeAfter > lifeBefore && curLifeSlot.stack == 9;
            testSteps.Add(new { step = "2. QuickHeal 钩子消耗与治疗", status = healSuccess ? "PASS" : "FAIL", details = $"血量: {lifeBefore} -> {lifeAfter}, 槽位剩余: {curLifeSlot.stack}" });

            // Step 3: 测试 QuickMana 钩子拦截
            player.statMana = 0; // 扣魔
            int manaBefore = player.statMana;
            player.QuickMana();
            var curManaSlot = (Item)manaField.GetValue(psp);
            int manaAfter = player.statMana;
            bool manaSuccess = manaAfter > manaBefore && curManaSlot.stack == 14;
            testSteps.Add(new { step = "3. QuickMana 钩子消耗与回蓝", status = manaSuccess ? "PASS" : "FAIL", details = $"魔力: {manaBefore} -> {manaAfter}, 槽位剩余: {curManaSlot.stack}" });

            // Step 4: 测试 OnPickup 自动填充
            var pickupItem = new Item();
            pickupItem.SetDefaults(ItemID.LesserHealingPotion);
            pickupItem.stack = 3;
            var onPickupMethod = potionPlayerType.GetMethod("OnPickup");
            bool pickupResult = (bool)onPickupMethod.Invoke(psp, new object[] { pickupItem });
            curLifeSlot = (Item)lifeField.GetValue(psp);
            bool pickupSuccess = curLifeSlot.stack == 12 && pickupItem.stack == 0;
            testSteps.Add(new { step = "4. OnPickup 自动拾取合并", status = pickupSuccess ? "PASS" : "FAIL", details = $"槽位堆叠: 9 -> {curLifeSlot.stack}, 拾取物品剩余: {pickupItem.stack}" });

            // Step 5: 测试 TagCompound 序列化与反序列化
            var tag = new TagCompound();
            var saveMethod = potionPlayerType.GetMethod("SaveData");
            saveMethod.Invoke(psp, new object[] { tag });

            // 重置槽位
            lifeField.SetValue(psp, new Item());
            manaField.SetValue(psp, new Item());
            wormField.SetValue(psp, new Item());

            // 恢复
            var loadMethod = potionPlayerType.GetMethod("LoadData");
            loadMethod.Invoke(psp, new object[] { tag });

            var resLife = (Item)lifeField.GetValue(psp);
            var resMana = (Item)manaField.GetValue(psp);
            var resWorm = (Item)wormField.GetValue(psp);

            bool tagSuccess = resLife.type == ItemID.LesserHealingPotion && resLife.stack == 12 &&
                              resMana.type == ItemID.LesserManaPotion && resMana.stack == 14 &&
                              resWorm.type == ItemID.WormholePotion && resWorm.stack == 5;

            testSteps.Add(new { step = "5. TagCompound 存档序列化/反序列化", status = tagSuccess ? "PASS" : "FAIL", details = $"恢复结果: Life={resLife.type}({resLife.stack}), Mana={resMana.type}({resMana.stack}), Wormhole={resWorm.type}({resWorm.stack})" });

            // 清理
            lifeField.SetValue(psp, new Item());
            manaField.SetValue(psp, new Item());
            wormField.SetValue(psp, new Item());
            player.statLife = player.statLifeMax2;
            player.statMana = player.statManaMax2;
            player.potionDelay = 0;

            bool allPassed = healSuccess && manaSuccess && pickupSuccess && tagSuccess;

            return new
            {
                success = allPassed,
                message = allPassed ? "PotionSlots 全部 5 项自动化回归测试通过！" : "PotionSlots 自动化测试存在失败项！",
                steps = testSteps
            };
        }
    }
}
