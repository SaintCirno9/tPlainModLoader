using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace tPlainModLoaderInjector
{
    public static class ProcessUtils
    {
        public static List<Process> GetProcessPID(string[] name)
        {
            if (name == null || name.Length < 1) return null;

            Process[] processes = Process.GetProcesses();

            List<Process> ids = processes.Where(i => name.Contains(i.ProcessName) && i.Id != -1).ToList();

            return ids.Count > 0 ? ids : null;
        }

        public static int SwitchPID(string[] names)
        {
            string nameString = null;
            foreach (string i in names)
            {
                if (nameString == null) nameString = $"[{i}]";
                else nameString = $"{nameString},[{i}]";
            }

            List<Process> ids = null;
            int index = 0;

            while (true)
            {
                Console.Clear();

                if (ids == null) ids = GetProcessPID(names);
                if (ids?.Count > 0 != true)
                {
                    Console.WriteLine($"找不到符合条件的程序:{nameString}");
                    Console.ReadLine();
                    continue;
                }

                if (index < 0) index = 0;
                else if (index >= ids.Count) index = ids.Count - 1;

                //

                Console.WriteLine("输入要附加到第几个,什么都不输入以确认. 输入\"u\"更新列表\n");
                Console.WriteLine($"当前选择: {GetPrintProcess(ids, index)}");

                for (int i = 0; i < ids.Count; ++i)
                {
                    Console.WriteLine(GetPrintProcess(ids, i));
                }

                //

                string s = Console.ReadLine();
                if (s == null) continue;
                if (s == "u")
                {
                    ids = null;
                    continue;
                }
                if (s == string.Empty) break;

                if (int.TryParse(s, out int temp)) index = temp;
            }

            Console.Clear();

            return ids[index].Id;
        }

        private static string GetPrintProcess(List<Process> ids, int i)
        {
            string s = $"{i}:名称[{ids[i].ProcessName}],".PadRight(24);
            s += $"pid[{ids[i].Id}],".PadRight(12);
            s += $"size[{ids[i].WorkingSet64 / 1024}]K";

            return s;
        }
    }
}
