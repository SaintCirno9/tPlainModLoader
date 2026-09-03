using CommandHelp;
using System.Collections.Generic;
using TPML;

namespace WandsTool
{
    public class Command : Mod
    {
        public override List<CommandObject> GetCommands()
        {
            List<CommandObject> cos = new List<CommandObject>();

            CommandObject root = new CommandObject(nameof(WandsTool));
            root.SubCommand.Add(TPML.Command.Utils.GetCO_OutputCOList(root.SubCommand));

            // enable [true|false]
            CommandMethod enable = new CommandMethod("enable", 1);
            enable.SubCommand.Add(new CommandTrue());
            enable.SubCommand.Add(new CommandFalse());
            enable.Runing += v =>
            {
                if (v == null || v.Length < 1) return;
                if (v[0] is bool e)
                {
                    Content.GameMain.SetWandEnabled(e);
                }
            };
            root.SubCommand.Add(enable);

            // replace [true|false]
            CommandMethod replace = new CommandMethod("replace", 1);
            replace.SubCommand.Add(new CommandTrue());
            replace.SubCommand.Add(new CommandFalse());
            replace.Runing += v =>
            {
                if (v == null || v.Length < 1) return;
                if (v[0] is bool r)
                {
                    Content.GameMain.Wand_ReplaceExisting = r;
                }
            };
            root.SubCommand.Add(replace);

            // collectDrops [true|false]
            CommandMethod collect = new CommandMethod("collectDrops", 1);
            collect.SubCommand.Add(new CommandTrue());
            collect.SubCommand.Add(new CommandFalse());
            collect.Runing += v =>
            {
                if (v == null || v.Length < 1) return;
                if (v[0] is bool c)
                {
                    Content.GameMain.Wand_CollectDrops = c;
                }
            };
            root.SubCommand.Add(collect);

            // infiniteLiquid [true|false]
            CommandMethod infLiquid = new CommandMethod("infiniteLiquid", 1);
            infLiquid.SubCommand.Add(new CommandTrue());
            infLiquid.SubCommand.Add(new CommandFalse());
            infLiquid.Runing += v =>
            {
                if (v == null || v.Length < 1) return;
                if (v[0] is bool il)
                {
                    Content.GameMain.Wand_InfiniteLiquid = il;
                }
            };
            root.SubCommand.Add(infLiquid);

            if (ContentPatch.NoPublic == false)
            {
                CommandMethod updateCount = new CommandMethod("updateCount", 1);
                updateCount.SubCommand.Add(new CommandInt());
                updateCount.Runing += v =>
                {
                    if (v == null || v.Length < 1) return;
                    if (v[0] is int uc)
                    {
                        Content.GameMain.Wand_UpdateCount = uc;
                    }
                };
                root.SubCommand.Add(updateCount);

                CommandMethod batchSize = new CommandMethod("batchSize", 1);
                batchSize.SubCommand.Add(new CommandInt());
                batchSize.Runing += v =>
                {
                    if (v == null || v.Length < 1) return;
                    if (v[0] is int bs && bs > 0)
                    {
                        Content.GameMain.Wand_BatchSize = bs;
                    }
                };
                root.SubCommand.Add(batchSize);
            }
            
            cos.Add(root);
            return cos;
        }
    }
}
