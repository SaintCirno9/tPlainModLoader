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

            public void CheckParentsForRecipeLoopViaIngredients(HashSet<int> viableIngredients)
            {
                CraftPathNode p = parent;
                while (p != null)
                {
                    if (p is RecipeNode rn)
                    {
                        foreach (var req in rn.recipe.requiredItem)
                        {
                            if (req != null && !req.IsAir)
                            {
                                viableIngredients.Remove(req.type);
                            }
                        }
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
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                RecipeNode node = new RecipeNode(recipe, multiplier, ChildNumber, parent, craftPath);
                if (children != null)
                {
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
                return $"{RBLanguage.GetText("CraftUI", "Duplicate")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack)}";
            }
        }

        public class UnfulfilledNode : CraftPathNode
        {
            public HashSet<int> item;
            public int stack;

            public UnfulfilledNode(HashSet<int> item, int stack, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.item = item;
                this.stack = stack;
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new UnfulfilledNode(new HashSet<int>(item), stack, ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                return $"Missing: {string.Join("/", item)} x{stack}";
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

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new HaveItemNode(itemid, stack, ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                return $"Have: {ItemHoverFixTagHandler.GenerateTag(itemid, stack, null, true)}";
            }
        }

        public class HaveItemsNode : CraftPathNode
        {
            public List<Tuple<int, int>> listOfItems;

            public HaveItemsNode(List<Tuple<int, int>> listOfItems, int childNumber, CraftPathNode parent, CraftPath craftPath)
                : base(childNumber, parent, craftPath)
            {
                this.listOfItems = listOfItems;
            }

            public override CraftPathNode Clone(CraftPath craftPath, CraftPathNode parent)
            {
                return new HaveItemsNode(new List<Tuple<int, int>>(listOfItems), ChildNumber, parent, craftPath);
            }

            public override string ToUITextString()
            {
                return "Have Items";
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
                return $"{RBLanguage.GetText("CraftUI", "PurchaseFrom")}: {NPCTagHandler.GenerateTag(npcType)} {GetTotalCostAsTags(TotalPrice)}";
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
                    npcs = string.Join(" ", list.Select(NPCTagHandler.GenerateTag));
                }
                return $"{RBLanguage.GetText("CraftUI", "DroppedBy")}: {npcs}";
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
                return $"{RBLanguage.GetText("CraftUI", "Mineable")}: {ItemHoverFixTagHandler.GenerateTag(itemid, stack)}";
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
                return $"{RBLanguage.GetText("CraftUI", "Bugnet")}: {NPCTagHandler.GenerateTag(npcType)}";
            }
        }

        public CraftPathNode root;
        public Dictionary<int, int> haveItems;

        public CraftPath(Recipe recipe, Dictionary<int, int> haveItems)
        {
            this.haveItems = new Dictionary<int, int>(haveItems);
            root = new RecipeNode(recipe, 1, 0, null, this);
            int idx = 0;
            foreach (var req in recipe.requiredItem)
            {
                if (req != null && !req.IsAir)
                {
                    HashSet<int> viable = new HashSet<int> { req.type };
                    foreach (var group in recipe.acceptedGroups)
                    {
                        if (RecipeGroup.recipeGroups.TryGetValue(group, out var rg) && rg.ValidItems.Contains(req.type))
                        {
                            viable.UnionWith(rg.ValidItems);
                        }
                    }
                    root.children[idx] = new UnfulfilledNode(viable, req.stack, idx, root, this);
                    idx++;
                }
            }
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

        public RecipeNode Push(UnfulfilledNode current, Recipe recipe, int multiplier)
        {
            RecipeNode recipeNode = new RecipeNode(recipe, multiplier, current.ChildNumber, current.parent, this);
            current.parent.children[current.ChildNumber] = recipeNode;

            int idx = 0;
            foreach (var req in recipe.requiredItem)
            {
                if (req != null && !req.IsAir)
                {
                    HashSet<int> viable = new HashSet<int> { req.type };
                    foreach (var group in recipe.acceptedGroups)
                    {
                        if (RecipeGroup.recipeGroups.TryGetValue(group, out var rg) && rg.ValidItems.Contains(req.type))
                        {
                            viable.UnionWith(rg.ValidItems);
                        }
                    }
                    recipeNode.children[idx] = new UnfulfilledNode(viable, req.stack * multiplier, idx, recipeNode, this);
                    idx++;
                }
            }
            return recipeNode;
        }

        public void Pop(UnfulfilledNode current, CraftPathNode node)
        {
            current.parent.children[current.ChildNumber] = current;
        }

        public void Push(UnfulfilledNode current, CraftPathNode newNode)
        {
            current.parent.children[current.ChildNumber] = newNode;
        }
    }
}
