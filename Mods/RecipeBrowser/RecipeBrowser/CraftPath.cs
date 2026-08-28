using System;
using System.Collections.Generic;
using System.Linq;
using RecipeBrowser.Common;
using RecipeBrowser.TagHandlers;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TPML.Content.Fusion;

namespace RecipeBrowser
{
    /// <summary>
    /// 配方合成树（CraftPath）—— 与原版 v0.12 对齐的库存感知路径构造
    /// 恢复五分支决策（JourneyDuplicate / HaveItem / HaveItems / Unfulfilled）与消耗回退体系
    /// 作者: SaintCirno9
    /// </summary>
    public class CraftPath
    {
        public abstract class CraftPathNode
        {
            public int multiplier = 1;
            public int ChildNumber;
            public CraftPathNode parent;
            public CraftPath craftPath;
            public CraftPathNode[] children;

            public CraftPathNode(int childNumber, CraftPathNode parent, CraftPath craftPath)
            {
                ChildNumber = childNumber;
                this.parent = parent;
                this.craftPath = craftPath;
            }

            public abstract CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent);
            public abstract string ToUITextString();

            public virtual IEnumerable<CraftPathNode> GetAllChildrenPreOrder()
            {
                yield return this;
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        if (child != null)
                        {
                            foreach (var item in child.GetAllChildrenPreOrder())
                            {
                                yield return item;
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// 消耗资源：递归向下扣除 haveItems（HaveItemNode/HaveItemsNode 生效）
            /// </summary>
            public virtual void ConsumeResources(CraftPath path)
            {
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        child?.ConsumeResources(path);
                    }
                }
            }

            /// <summary>
            /// 回退资源：递归向上恢复 haveItems
            /// </summary>
            public virtual void UnConsumeResources(CraftPath path)
            {
                if (children != null)
                {
                    foreach (var child in children)
                    {
                        child?.UnConsumeResources(path);
                    }
                }
            }

            /// <summary>
            /// 配方环检测：向上遍历祖先，仅移除祖先配方的"产物"类型，防止 产物→原料 闭环
            /// （原版语义：绝不误删可行原料路径）
            /// </summary>
            public void CheckParentsForRecipeLoopViaIngredients(HashSet<int> viableIngredients)
            {
                CraftPathNode p = parent;
                while (p != null)
                {
                    if (p is RecipeNode rn)
                    {
                        viableIngredients.Remove(rn.recipe.createItem.type);
                    }
                    p = p.parent;
                }
            }
        }

        public class RecipeNode : CraftPathNode
        {
            public Recipe recipe;

            public RecipeNode(Recipe recipe, int multiplier, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.recipe = recipe;
                this.multiplier = multiplier;
                int reqCount = recipe.requiredItem.Count(x => x != null && !x.IsAir);
                children = new CraftPathNode[reqCount];

                int childIdx = 0;
                for (int i = 0; i < recipe.requiredItem.Length; i++)
                {
                    Item req = recipe.requiredItem[i];
                    if (req == null || req.IsAir) continue;

                    int need = req.stack * multiplier;
                    int slot = childIdx;
                    bool handled = false;

                    // 1. RecipeGroup 组原料（iconic 匹配）：遍历组内全部合法物品做五分支决策
                    foreach (int acceptedGroup in recipe.acceptedGroups)
                    {
                        if (!RecipeGroup.recipeGroups.TryGetValue(acceptedGroup, out var rg)) continue;
                        if (rg == null || rg.ValidItems == null) continue;
                        if (!rg.ValidItems.Contains(req.type)) continue;

                        bool fullyCovered = false;   // 已完全满足（叶子节点）
                        bool partiallyOwned = false; // 部分拥有（可分摊）
                        foreach (int validItem in rg.ValidItems)
                        {
                            if (Main.GameMode == 3 && RecipePath.ItemFullyResearched(validItem))
                            {
                                children[slot] = new JourneyDuplicateItemNode(validItem, need, slot, this, craftPath);
                                fullyCovered = true;
                                break;
                            }
                            if (craftPath.haveItems.TryGetValue(validItem, out var have) && have >= need)
                            {
                                children[slot] = new HaveItemNode(validItem, need, slot, this, craftPath);
                                fullyCovered = true;
                                break;
                            }
                            if (craftPath.haveItems.ContainsKey(validItem))
                            {
                                partiallyOwned = true;
                            }
                        }

                        if (!fullyCovered && partiallyOwned)
                        {
                            // 部分拥有：HaveItemsNode 按 Math.Min 分摊消耗，差额挂 UnfulfilledNode
                            List<Tuple<int, int>> list = new List<Tuple<int, int>>();
                            int remaining = need;
                            foreach (int validItem in rg.ValidItems)
                            {
                                if (remaining > 0 && craftPath.haveItems.TryGetValue(validItem, out var have2))
                                {
                                    int take = Math.Min(remaining, have2);
                                    list.Add(new Tuple<int, int>(validItem, take));
                                    remaining -= take;
                                }
                            }
                            children[slot] = new HaveItemsNode(rg, list, slot, this, craftPath);
                            if (remaining > 0)
                            {
                                children[slot].children = new CraftPathNode[1];
                                children[slot].children[0] = new UnfulfilledNode(rg, new HashSet<int>(rg.ValidItems), remaining, 0, children[slot], craftPath);
                            }
                        }
                        else if (!fullyCovered)
                        {
                            children[slot] = new UnfulfilledNode(rg, new HashSet<int>(rg.ValidItems), need, slot, this, craftPath);
                        }
                        handled = true;
                        break;
                    }

                    if (handled)
                    {
                        childIdx++;
                        continue;
                    }

                    // 2. 普通原料：五分支决策
                    if (Main.GameMode == 3 && RecipePath.ItemFullyResearched(req.type))
                    {
                        children[slot] = new JourneyDuplicateItemNode(req.type, need, slot, this, craftPath);
                    }
                    else if (craftPath.haveItems.TryGetValue(req.type, out var haveFull) && haveFull >= need)
                    {
                        children[slot] = new HaveItemNode(req.type, need, slot, this, craftPath);
                    }
                    else if (craftPath.haveItems.TryGetValue(req.type, out var havePart))
                    {
                        children[slot] = new HaveItemNode(req.type, havePart, slot, this, craftPath);
                        children[slot].children = new CraftPathNode[1];
                        children[slot].children[0] = new UnfulfilledNode(new HashSet<int> { req.type }, need - havePart, 0, children[slot], craftPath);
                    }
                    else
                    {
                        children[slot] = new UnfulfilledNode(new HashSet<int> { req.type }, need, slot, this, craftPath);
                    }
                    childIdx++;
                }
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                // 浅拷贝保留本节点已完成的五分支决策快照（不得用当前 haveItems 重新决策，
                // 否则克隆出的路径会把"已有"误标为"缺失"）
                RecipeNode node = (RecipeNode)MemberwiseClone();
                node.parent = parent;
                node.craftPath = craftPath;
                node.children = null;
                if (children != null)
                {
                    node.children = new CraftPathNode[children.Length];
                    for (int i = 0; i < children.Length; i++)
                    {
                        if (children[i] != null)
                        {
                            node.children[i] = children[i].Clone(craftPath, node);
                        }
                    }
                }
                return node;
            }

            public override string ToUITextString()
            {
                return $"Recipe: {recipe.createItem.Name} x{recipe.createItem.stack * multiplier}";
            }
        }

        public class JourneyDuplicateItemNode : CraftPathNode
        {
            public int itemid;
            public int stack;

            public JourneyDuplicateItemNode(int itemid, int stack, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.itemid = itemid;
                this.stack = stack;
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new JourneyDuplicateItemNode(itemid, stack, ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                return $"{RBLanguage.GetText("CraftUI", "Duplicate")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack, null, true)}";
            }
        }

        public class UnfulfilledNode : CraftPathNode
        {
            public RecipeGroup recipeGroup;
            public HashSet<int> item;
            public int stack;

            public UnfulfilledNode(RecipeGroup recipeGroup, HashSet<int> item, int stack, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.recipeGroup = recipeGroup;
                this.item = item;
                this.stack = stack;
            }

            public UnfulfilledNode(HashSet<int> item, int stack, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : this(null, item, stack, childNumber, parent, craftPath)
            {
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new UnfulfilledNode(recipeGroup, new HashSet<int>(item), stack, ChildNumber, parent, craftPath);
            }

            /// <summary>
            /// 带 stack 一致性校验的环检测（原版语义）：产物类型相同且 stack 不同视为异常
            /// </summary>
            public bool CheckParentsForRecipeLoop(Recipe recipe)
            {
                CraftPathNode p = parent;
                while (p != null)
                {
                    if (p is RecipeNode rn)
                    {
                        if (rn.recipe.createItem.type == recipe.createItem.type && rn.recipe.createItem.stack != recipe.createItem.stack)
                        {
                            throw new Exception("Found a stack size problem craft path!");
                        }
                        if (rn.recipe.createItem.type == recipe.createItem.type)
                        {
                            return true;
                        }
                    }
                    p = p.parent;
                }
                return false;
            }

            public override string ToUITextString()
            {
                if (recipeGroup != null)
                {
                    int iconic = recipeGroup.ValidItems.FirstOrDefault();
                    return $"{RBLanguage.GetText("CraftUI", "Missing")}: {ItemHoverFixTagHandler.GenerateTag(iconic, stack, recipeGroup.GetText())}";
                }
                if (item != null && item.Count > 0)
                {
                    int first = item.First();
                    return $"{RBLanguage.GetText("CraftUI", "Missing")}: {ItemHoverFixTagHandler.GenerateTag(first, stack)}";
                }
                return $"{RBLanguage.GetText("CraftUI", "Missing")}: ?";
            }
        }

        public class HaveItemNode : CraftPathNode
        {
            public int itemid;
            public int stack;

            public HaveItemNode(int itemid, int stack, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.itemid = itemid;
                this.stack = stack;
            }

            public override void ConsumeResources(CraftPath path)
            {
                path.haveItems.Adjust(itemid, -stack);
                base.ConsumeResources(path);
            }

            public override void UnConsumeResources(CraftPath path)
            {
                path.haveItems.Adjust(itemid, stack);
                base.UnConsumeResources(path);
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new HaveItemNode(itemid, stack, ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                return $"{RBLanguage.GetText("CraftUI", "Have")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack, null, true)}";
            }
        }

        public class HaveItemsNode : CraftPathNode
        {
            public RecipeGroup recipeGroup;
            public List<Tuple<int, int>> listOfItems;

            public HaveItemsNode(RecipeGroup recipeGroup, List<Tuple<int, int>> listOfItems, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.recipeGroup = recipeGroup;
                this.listOfItems = listOfItems;
            }

            public override void ConsumeResources(CraftPath path)
            {
                if (listOfItems != null)
                {
                    foreach (var tuple in listOfItems)
                    {
                        path.haveItems.Adjust(tuple.Item1, -tuple.Item2);
                    }
                }
                base.ConsumeResources(path);
            }

            public override void UnConsumeResources(CraftPath path)
            {
                if (listOfItems != null)
                {
                    foreach (var tuple in listOfItems)
                    {
                        path.haveItems.Adjust(tuple.Item1, tuple.Item2);
                    }
                }
                base.UnConsumeResources(path);
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new HaveItemsNode(recipeGroup, new List<Tuple<int, int>>(listOfItems), ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                int totalStack = listOfItems?.Sum(x => x.Item2) ?? 0;
                string itemsTag = listOfItems != null ? string.Concat(listOfItems.Select(x => ItemHoverFixTagHandler.GenerateTag(x.Item1, x.Item2, null, true))) : "";
                if (recipeGroup != null)
                {
                    int iconic = recipeGroup.ValidItems.FirstOrDefault();
                    return $"{RBLanguage.GetText("CraftUI", "Have")}: {ItemHoverFixTagHandler.GenerateTag(iconic, totalStack, recipeGroup.GetText(), true)} ({itemsTag})";
                }
                return $"{RBLanguage.GetText("CraftUI", "Have")}: {itemsTag}";
            }
        }

        public class BuyItemNode : CraftPathNode
        {
            public int itemid;
            public int stack;
            public int price;
            public int npcType;

            public int TotalPrice => price * stack;

            public BuyItemNode(int itemid, int stack, int price, int npcType, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.itemid = itemid;
                this.stack = stack;
                this.price = price;
                this.npcType = npcType;
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new BuyItemNode(itemid, stack, price, npcType, ChildNumber, parent, craftPath);
            }

            public static string GetTotalCostAsTags(int price)
            {
                int platinum = price / 1000000;
                int gold = (price % 1000000) / 10000;
                int silver = (price % 10000) / 100;
                int copper = price % 100;

                string res = "";
                if (platinum > 0) res += ItemHoverFixTagHandler.GenerateTag(ItemID.PlatinumCoin, platinum);
                if (gold > 0) res += ItemHoverFixTagHandler.GenerateTag(ItemID.GoldCoin, gold);
                if (silver > 0) res += ItemHoverFixTagHandler.GenerateTag(ItemID.SilverCoin, silver);
                if (copper > 0) res += ItemHoverFixTagHandler.GenerateTag(ItemID.CopperCoin, copper);
                return res;
            }

            public override string ToUITextString()
            {
                return $"{RBLanguage.GetText("CraftUI", "Purchase")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack)} {RBLanguage.GetText("CraftUI", "From")} [npc:{npcType}] {RBLanguage.GetText("CraftUI", "For")} {GetTotalCostAsTags(TotalPrice)}";
            }
        }

        public class LootItemNode : CraftPathNode
        {
            public int itemid;
            public int stack;

            public LootItemNode(int itemid, int stack, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.itemid = itemid;
                this.stack = stack;
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new LootItemNode(itemid, stack, ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                string npcs = "";
                if (LootCache.instance?.lootInfos != null && LootCache.instance.lootInfos.TryGetValue(itemid, out var list))
                {
                    // 仅列出已解锁图鉴的 NPC（对齐原版）
                    npcs = string.Concat(list.Where(RecipePath.NPCUnlocked).Select(NPCTagHandler.GenerateTag));
                }
                return $"{RBLanguage.GetText("CraftUI", "Farm")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack)} {RBLanguage.GetText("CraftUI", "From")} {npcs}";
            }
        }

        public class MineItemNode : CraftPathNode
        {
            public int itemid;
            public int stack;

            public MineItemNode(int itemid, int stack, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.itemid = itemid;
                this.stack = stack;
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new MineItemNode(itemid, stack, ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                return $"{RBLanguage.GetText("CraftUI", "Mine")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack)} {RBLanguage.GetText("CraftUI", "FromTheWorld")}";
            }
        }

        public class BugNetItemNode : CraftPathNode
        {
            public int itemid;
            public int stack;
            public int npcType;

            public BugNetItemNode(int itemid, int stack, int npcType, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.itemid = itemid;
                this.stack = stack;
                this.npcType = npcType;
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new BugNetItemNode(itemid, stack, npcType, ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                return $"{RBLanguage.GetText("CraftUI", "BugNet")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack)} {RBLanguage.GetText("CraftUI", "ByCapturing")} {NPCTagHandler.GenerateTag(npcType)}";
            }
        }

        public CraftPathNode root;
        public Dictionary<int, int> haveItems;

        public CraftPath(Recipe recipe, Dictionary<int, int> haveItems)
        {
            this.haveItems = new Dictionary<int, int>(haveItems);
            root = new RecipeNode(recipe, 1, -1, null, this);
            ConsumeResources(root);
        }

        public CraftPath Clone()
        {
            CraftPath path = new CraftPath();
            path.haveItems = new Dictionary<int, int>(haveItems);
            path.root = root.Clone(path, null);
            return path;
        }

        private CraftPath() { }

        public UnfulfilledNode GetCurrent()
        {
            return root.GetAllChildrenPreOrder().OfType<UnfulfilledNode>().FirstOrDefault();
        }

        /// <summary>
        /// 将 UnfulfilledNode 替换为展开的 RecipeNode，并消耗其"已有"资源
        /// </summary>
        public RecipeNode Push(UnfulfilledNode current, Recipe recipe, int multiplier)
        {
            RecipeNode recipeNode = new RecipeNode(recipe, multiplier, current.ChildNumber, current.parent, this);
            current.parent.children[current.ChildNumber] = recipeNode;
            current.parent = null;
            current.ChildNumber = -1;
            ConsumeResources(recipeNode);
            return recipeNode;
        }

        /// <summary>
        /// 将 RecipeNode 摘除、UnfulfilledNode 放回原位，并回退资源
        /// </summary>
        public void Pop(UnfulfilledNode current, CraftPathNode recipeNode)
        {
            current.parent = recipeNode.parent;
            current.ChildNumber = recipeNode.ChildNumber;
            if (current.parent != null && current.ChildNumber >= 0)
            {
                current.parent.children[current.ChildNumber] = current;
            }
            recipeNode.parent = null;
            recipeNode.ChildNumber = -1;
            UnConsumeResources(recipeNode);
        }

        public void Push(UnfulfilledNode current, CraftPathNode newNode)
        {
            current.parent.children[current.ChildNumber] = newNode;
            current.parent = null;
            current.ChildNumber = -1;
            ConsumeResources(newNode);
        }

        private void ConsumeResources(CraftPathNode node)
        {
            node.ConsumeResources(this);
        }

        private void UnConsumeResources(CraftPathNode node)
        {
            node.UnConsumeResources(this);
        }
    }
}
