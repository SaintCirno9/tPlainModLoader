using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using TPML.Core.Diagnostics;
using TPML.Core.Logging;

namespace RecipeBrowser.Common
{
    /// <summary>
    /// RecipeBrowser 专用轻量级性能剖析器 (Profiler)，已接入 TPML 统一性能剖析引擎
    /// 作者: SaintCirno9
    /// </summary>
    public static class RBProfiler
    {
        private static readonly ILogger Logger = LogManager.GetLogger("RecipeBrowser.Profiler");
        public static bool Enabled => (RecipeBrowserClientConfig.Instance != null && RecipeBrowserClientConfig.Instance.EnableProfiler) || PerformanceProfiler.IsEnabled;
        public static float LogThresholdMs = 2.0f; // 超过 2ms 的操作打印警告

        internal class Section
        {
            public string Name;
            public Stopwatch Sw = new Stopwatch();
            public List<Section> Children = new List<Section>();
            public Section Parent;
        }

        [ThreadStatic]
        private static Section currentSection;
        [ThreadStatic]
        private static Section rootSection;

        public struct ScopeToken : IDisposable
        {
            private Section section;

            internal ScopeToken(Section section)
            {
                this.section = section;
            }

            public void Dispose()
            {
                if (section != null)
                {
                    section.Sw.Stop();
                    long ticks = section.Sw.ElapsedTicks;
                    if (PerformanceProfiler.IsEnabled)
                    {
                        PerformanceProfiler.Record("RecipeBrowser", section.Name, ticks);
                    }

                    if (section.Parent != null)
                    {
                        currentSection = section.Parent;
                    }
                    else
                    {
                        // 根节点结束，输出报表（仅在独立 RBProfiler 开启且超过阈值时）
                        float totalMs = (float)section.Sw.Elapsed.TotalMilliseconds;
                        if (RecipeBrowserClientConfig.Instance != null && RecipeBrowserClientConfig.Instance.EnableProfiler && totalMs >= LogThresholdMs)
                        {
                            DumpReport(section);
                        }
                        currentSection = null;
                        rootSection = null;
                    }
                }
            }
        }

        public static ScopeToken Step(string name)
        {
            if (!Enabled) return new ScopeToken(null);

            Section newSec = new Section
            {
                Name = name,
                Parent = currentSection
            };

            if (currentSection != null)
            {
                currentSection.Children.Add(newSec);
            }
            else
            {
                rootSection = newSec;
            }

            currentSection = newSec;
            newSec.Sw.Start();
            return new ScopeToken(newSec);
        }

        public static void Log(string message, float ms = -1)
        {
            if (!Enabled) return;
            string formatted = ms >= 0 ? $"[RB-Perf] {message}: {ms:F2}ms" : $"[RB-Perf] {message}";
            Logger.Info(formatted);
            try
            {
                if (Main.LocalPlayer != null && !Main.gameMenu)
                {
                    Color col = ms > 500f ? Color.Red : (ms > 50f ? Color.Orange : Color.Yellow);
                    Main.NewText(formatted, col.R, col.G, col.B);
                }
            }
            catch { }
        }

        private static void DumpReport(Section root)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[RB-Profiler] {root.Name} => Total: {root.Sw.Elapsed.TotalMilliseconds:F2}ms");
            FormatSection(root, sb, "  ");
            string report = sb.ToString();
            Logger.Info(report);
            try
            {
                if (Main.LocalPlayer != null && !Main.gameMenu)
                {
                    float totalMs = (float)root.Sw.Elapsed.TotalMilliseconds;
                    Color col = totalMs > 500f ? Color.Red : (totalMs > 50f ? Color.Orange : Color.LightGreen);
                    Main.NewText($"[RB-Profiler] {root.Name}: {totalMs:F2}ms", col.R, col.G, col.B);
                    foreach (var child in root.Children)
                    {
                        float childMs = (float)child.Sw.Elapsed.TotalMilliseconds;
                        if (childMs > 0.5f)
                        {
                            Main.NewText($"  └─ {child.Name}: {childMs:F2}ms", 220, 220, 180);
                        }
                    }
                }
            }
            catch { }
        }

        private static void FormatSection(Section sec, StringBuilder sb, string indent)
        {
            foreach (var child in sec.Children)
            {
                sb.AppendLine($"{indent}├─ {child.Name}: {child.Sw.Elapsed.TotalMilliseconds:F2}ms");
                FormatSection(child, sb, indent + "│  ");
            }
        }
    }
}
