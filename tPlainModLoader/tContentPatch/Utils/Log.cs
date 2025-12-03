using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace tContentPatch.Utils
{
    /// <summary/>
    public static class Log
    {
        /// <summary/>
        public static string path { get; private set; } = null;
        private static List<string> logs = new List<string>();

        /// <summary/>
        public static void Add(string s)
        {
            DateTime time = DateTime.Now;

            string ss = $"[{time.Hour}:{time.Minute}:{time.Millisecond}]:{s}\n";
            logs.Add(ss);
        }

        /// <summary/>
        public static void SaveTry()
        {
            try
            {
                string file = path;
                string s = "";
                for (int i = 0; i < logs.Count; ++i) s += logs[i];

                if (Directory.Exists(Path.GetDirectoryName(file)) == false) file = Path.GetFileName(file);
                File.WriteAllText(file, s, Encoding.UTF8);
            }
            catch { }
        }

        /// <summary/>
        public static void SetPath(string filePath)
        {
            if (filePath == null) return;
            if (Directory.Exists(Path.GetDirectoryName(filePath)) == false) return;

            Log.path = filePath;
        }
    }
}
