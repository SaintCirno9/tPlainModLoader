using System;
using System.IO;

namespace BatchSapwn
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "生成配置.txt");

                SpawnConfig config = SpawnConfig.TryLoad(path);
                if (config == null) throw new Exception("配置为null");
                config.Check();

                Console.WriteLine($"从:{config.form}");
                Console.WriteLine($"到:{config.to}");
                for (int i = 0; i < config.mods.Count; ++i)
                {
                    Console.WriteLine($"{i}:{config.mods[i]}");
                }

                Console.WriteLine("准备开始生成");
                Console.ReadLine();
                Console.Clear();
                Spawn(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("ok");
            Console.ReadLine();
        }

        private static void Spawn(SpawnConfig config)
        {
            config.For((form, to, name) =>
            {
                form = Path.Combine(form, name);
                to = Path.Combine(to, name);

                Directory.CreateDirectory(to);

                CopyFile(form, to, "info.json");
                CopyFile(form, to, "loadConfig.json");
                CopyFile(form, to, "ico.png");
                CopyFile(form, to, $"{name}.dll");
            });
        }

        private static void CopyFile(string form, string to, string file)
        {
            form = Path.Combine(form, file);
            to = Path.Combine(to, file);

            if (File.Exists(form) == false)
            {
                Console.WriteLine($"文件不存在, 跳过[{form}]");
                return;
            }
            File.Copy(form, to);
        }
    }
}
