using CommandHelp;
using System;
using System.Collections.Generic;
using Terraria;
using TPML.Core.Diagnostics;

namespace tContentPatch.Command
{
    public class ProfileRootCommand : CommandObject
    {
        public ProfileRootCommand() : base("profile")
        {
            TipText = "性能剖析诊断指令。用法: profile <秒数> | profile start [秒数] | profile stop | profile report | profile status";
        }

        public override object Run(ref int index, List<CommandObject> commandList)
        {
            // 如果仅输入了 profile 没有后续子指令，打印当前状态与帮助
            if (index == commandList.Count - 1)
            {
                if (PerformanceProfiler.IsEnabled)
                {
                    string dur = PerformanceProfiler.TargetDurationSeconds > 0 ? $" / {PerformanceProfiler.TargetDurationSeconds:0.##}s" : "";
                    ProfileCommand.Notify($"[Profiler] 运行中 (已采样 {PerformanceProfiler.ElapsedSeconds:0.##}s{dur}, 采样帧数: {PerformanceProfiler.TotalFrames})。使用 'profile stop' 结束采样。");
                }
                else
                {
                    ProfileCommand.Notify("[Profiler] 未运行。用法: 'profile <秒数>' (如 'profile 10' 采样10秒) | 'profile stop' | 'profile report'");
                }
                return null;
            }

            return base.Run(ref index, commandList);
        }
    }

    /// <summary>
    /// 支持直接 'profile <秒数>' 格式的数值子指令 (例如 profile 10)
    /// </summary>
    public class CommandProfileDuration : CommandFloat
    {
        public override CommandObject Parse(string command)
        {
            if (float.TryParse(command, out _))
            {
                return base.Parse(command);
            }
            return null; // 非数字则跳过，让后续子指令匹配
        }

        public override object Run(ref int index, List<CommandObject> commandList)
        {
            object val = base.Run(ref index, commandList);
            float duration = val != null ? Convert.ToSingle(val) : 10f;
            if (duration <= 0f) duration = 10f;
            PerformanceProfiler.Start(duration);
            ProfileCommand.Notify($"[Profiler] 已启动性能采样，计划采样时长: {duration:0.##} 秒 (倒计时结束后自动停止并生成报告)");
            return duration;
        }
    }

    /// <summary>
    /// 性能剖析器游戏内/控制台指令分发器
    /// </summary>
    public static class ProfileCommand
    {
        private static bool _eventsHooked = false;

        public static void Initialize()
        {
            if (_eventsHooked) return;
            _eventsHooked = true;

            PerformanceProfiler.OnReportGenerated += report =>
            {
                Notify("[Profiler] 性能采样已完成，完整结构化报告已输出至控制台与日志！");
            };
        }

        public static CommandObject CreateCommand()
        {
            Initialize();

            ProfileRootCommand profile = new ProfileRootCommand();

            // 1. profile start <float> / profile start
            CommandMethod profile_start_val = new CommandMethod("start", 1);
            profile_start_val.SubCommand.Add(new CommandFloat());
            profile_start_val.Runing += args =>
            {
                float duration = Convert.ToSingle(args[0]);
                PerformanceProfiler.Start(duration);
                Notify($"[Profiler] 已启动性能采样，计划采样时长: {duration:0.##} 秒 (倒计时结束后自动停止并生成报告)");
            };
            profile.SubCommand.Add(profile_start_val);

            CommandMethod profile_start_inf = new CommandMethod("start", 0);
            profile_start_inf.Runing += args =>
            {
                PerformanceProfiler.Start(0f);
                Notify("[Profiler] 已启动持续性能采样模式。使用 'profile stop' 结束采样并生成报告。");
            };
            profile.SubCommand.Add(profile_start_inf);

            // 2. profile stop
            CommandMethod profile_stop = new CommandMethod("stop");
            profile_stop.Runing += args =>
            {
                if (!PerformanceProfiler.IsEnabled)
                {
                    Notify("[Profiler] 当前未处于采样状态。");
                    return;
                }
                PerformanceProfiler.StopAndReport();
            };
            profile.SubCommand.Add(profile_stop);

            // 3. profile report
            CommandMethod profile_report = new CommandMethod("report");
            profile_report.Runing += args =>
            {
                string report = PerformanceProfiler.GenerateReport();
                ContentPatch.PrintTry(report);
                Notify("[Profiler] 已在控制台与日志输出当前性能快照。");
            };
            profile.SubCommand.Add(profile_report);

            // 4. profile status
            CommandMethod profile_status = new CommandMethod("status");
            profile_status.Runing += args =>
            {
                if (PerformanceProfiler.IsEnabled)
                {
                    string dur = PerformanceProfiler.TargetDurationSeconds > 0 ? $" / {PerformanceProfiler.TargetDurationSeconds:0.##}s" : "";
                    Notify($"[Profiler] 状态: 运行中 (已采样 {PerformanceProfiler.ElapsedSeconds:0.##}s{dur}, 采样帧数: {PerformanceProfiler.TotalFrames})");
                }
                else
                {
                    Notify("[Profiler] 状态: 未运行。使用 'profile 10' 或 'profile start' 启动性能采样。");
                }
            };
            profile.SubCommand.Add(profile_status);

            // 5. profile <float> (直接跟秒数，如 profile 10)
            profile.SubCommand.Add(new CommandProfileDuration());

            profile.SubCommand.Add(Utils.GetCO_OutputCOList(profile.SubCommand));
            return profile;
        }

        public static void Notify(string msg)
        {
            ContentPatch.PrintTry(msg);
            try
            {
                if (Main.netMode == 0 || Main.netMode == 1)
                {
                    Main.NewText(msg, 255, 220, 100);
                }
            }
            catch { }
        }
    }
}
