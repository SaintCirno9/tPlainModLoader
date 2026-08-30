using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using MonoMod.RuntimeDetour;

namespace tContentPatch.Patch
{
    /// <summary>
    /// IAddPatch 动态 prefix/postfix 绑定器：把 (MethodBase 目标, MethodInfo prefix/postfix) 编译成
    /// MonoMod On 风格 detour 委托（M2 迁移）。参数绑定遵循 Harmony 惯例：
    /// - "__instance"：实例方法目标时绑定实例参数；
    /// - "__result"：ref 绑定方法返回值；
    /// - 其余按参数名匹配目标方法参数；
    /// - prefix 返回 bool 且为 false 时跳过原方法。
    ///
    /// M2 实测（重要）：wrapper 必须由 C# 编译器（csc）编译成真实磁盘程序集 —— MonoMod Hook 生成
    /// orig 委托代理时只对"磁盘程序集方法"能正确 ImportReference 参数类型（Terraria 为
    /// Assembly.Load(byte[]) 内存程序集，Cecil 对动态/Cecil 生成程序集的 detour 参数表解析失败，
    /// 抛 "值不能为 null(parameter)"）。C# 编译器生成的方法与 ContentHookDispatcher 的 lambda
    /// 同源（磁盘程序集），实测可用。
    /// </summary>
    internal static class HookBinder
    {
        private static int _typeSeq = 0;
        private static readonly object _lock = new object();

        public static Delegate Build(MethodBase target, MethodInfo prefix, MethodInfo postfix)
        {
            bool isInstance = !target.IsStatic;
            Type retType = GetReturnType(target);
            ParameterInfo[] tps = target.GetParameters();
            int seq = System.Threading.Interlocked.Increment(ref _typeSeq);

            // orig 委托类型：void -> Action<T..>；有返回值 -> Func<T.., R>；实例方法含 self
            Type origType = MakeOrigDelegateType(target, retType);

            // 生成 C# 源并编译为磁盘程序集
            string source = GenerateSource(seq, target, prefix, postfix, origType, isInstance, retType, tps);
            string dllPath = CompileSource(seq, source, prefix, postfix);

            Assembly loaded = Assembly.LoadFrom(dllPath);
            Type hookType = loaded.GetType("TPMLHook_" + seq + ".Hook_" + seq);
            MethodInfo impl = loaded.GetType("TPMLHook_" + seq).GetMethod("InvokeImpl", BindingFlags.Public | BindingFlags.Static);
            return impl.CreateDelegate(hookType);
        }

        /// <summary>生成 C# wrapper 源（编译器生成磁盘程序集，代理生成兼容）</summary>
        private static string GenerateSource(int seq, MethodBase target, MethodInfo prefix, MethodInfo postfix,
            Type origType, bool isInstance, Type retType, ParameterInfo[] tps)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("");
            sb.AppendLine("namespace TPMLHook_" + seq);
            sb.AppendLine("{");
            sb.AppendLine("    public delegate " + CSharpType(retType) + " Hook_" + seq + "(" + CSharpType(origType) + " orig, "
                + (isInstance ? CSharpType(target.DeclaringType) + " self, " : "") + JoinParams(tps) + ");");
            sb.AppendLine("");
            sb.AppendLine("    public static class Host");
            sb.AppendLine("    {");
            sb.Append("        public static " + CSharpType(retType) + " InvokeImpl(" + CSharpType(origType) + " orig, "
                + (isInstance ? CSharpType(target.DeclaringType) + " self, " : "") + JoinParams(tps) + ")");
            sb.AppendLine("");
            sb.AppendLine("        {");

            bool hasReturn = retType != typeof(void);
            if (hasReturn)
            {
                sb.AppendLine("            " + CSharpType(retType) + " __result = default(" + CSharpType(retType) + ");");
            }

            // prefix 绑定调用
            if (prefix != null)
            {
                EmitBoundCall(sb, prefix, isInstance, tps, "prefix");
                if (prefix.ReturnType == typeof(bool))
                {
                    sb.AppendLine("            if (!prefixResult) { " + (hasReturn ? "return __result;" : "return;") + " }");
                }
            }

            // orig 调用
            sb.Append("            orig(" + (isInstance ? "self" : ""));
            for (int i = 0; i < tps.Length; i++) sb.Append((i > 0 || isInstance ? ", " : "") + "p" + i);
            if (!isInstance && tps.Length == 0) { }
            else if (!isInstance) { }
            sb.AppendLine(");");

            // postfix 绑定调用
            if (postfix != null)
            {
                EmitBoundCall(sb, postfix, isInstance, tps, "postfix");
            }

            if (hasReturn) sb.AppendLine("            return __result;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>按名绑定并发出 prefix/postfix 调用（C# 源）</summary>
        private static void EmitBoundCall(StringBuilder sb, MethodInfo method, bool isInstance, ParameterInfo[] tps, string kind)
        {
            string fullName = method.DeclaringType.FullName.Replace('+', '.') + "." + method.Name;
            var args = new List<string>();
            bool hasResult = false;

            foreach (var pp in method.GetParameters())
            {
                string name = pp.Name;
                bool byRef = pp.ParameterType.IsByRef;
                if (name == "__instance")
                {
                    args.Add((byRef ? "ref " : "") + "self");
                }
                else if (name == "__result")
                {
                    args.Add((byRef ? "ref " : "") + "__result");
                    hasResult = true;
                }
                else
                {
                    int idx = FindParamIndex(tps, name);
                    if (idx < 0) throw new NotSupportedException("[HookBinder] 参数 " + name + " 在目标方法中不存在");
                    args.Add((byRef ? "ref " : "") + "p" + idx);
                }
            }

            string call = "            " + (method.ReturnType == typeof(bool) ? "bool prefixResult = " : "") + fullName + "(" + string.Join(", ", args) + ");";
            sb.AppendLine(call);
            if (method.ReturnType == typeof(bool) && !hasResult)
            {
                // prefix 的 bool 返回值用于跳过判断；postfix 的 bool 忽略
                if (kind == "prefix") sb.AppendLine("            // prefix 返回值用于跳过判断");
            }
        }

        /// <summary>调用 csc 编译生成的源文件为磁盘 dll（M2 实测必需：磁盘程序集才兼容 MonoMod 代理生成）</summary>
        private static string CompileSource(int seq, string source, MethodInfo prefix, MethodInfo postfix)
        {
            lock (_lock)
            {
                string tmpDir = Path.Combine(Path.GetTempPath(), "tpml-hookbinder");
                Directory.CreateDirectory(tmpDir);
                string csPath = Path.Combine(tmpDir, "Hook_" + seq + ".cs");
                string dllPath = Path.Combine(tmpDir, "Hook_" + seq + ".dll");
                File.WriteAllText(csPath, source, Encoding.UTF8);

                var args = new List<string>();
                args.Add("/nologo");
                args.Add("/target:library");
                args.Add("/platform:x86");
                args.Add("/optimize+");
                foreach (var r in CollectReferences(prefix, postfix))
                {
                    args.Add("/r:\"" + r + "\"");
                }
                args.Add("/out:\"" + dllPath + "\"");
                args.Add("\"" + csPath + "\"");

                string csc = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "Microsoft.NET", "Framework", "v4.0.30319", "csc.exe");
                var psi = new ProcessStartInfo(csc, string.Join(" ", args))
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using (var proc = Process.Start(psi))
                {
                    string stdout = proc.StandardOutput.ReadToEnd();
                    string stderr = proc.StandardError.ReadToEnd();
                    proc.WaitForExit(30000);
                    if (proc.ExitCode != 0)
                    {
                        throw new Exception("[HookBinder] csc 编译失败\n命令行: " + psi.FileName + " " + psi.Arguments + "\n" + stdout + "\n" + stderr + "\n源文件: " + csPath);
                    }
                }
                return dllPath;
            }
        }

        /// <summary>收集 csc 引用：launcher 目录托管 dll + 游戏 Terraria.exe + mod 程序集</summary>
        private static List<string> CollectReferences(MethodInfo prefix, MethodInfo postfix)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (Directory.Exists(baseDir))
            {
                foreach (var f in Directory.GetFiles(baseDir, "*.dll"))
                {
                    if (IsManagedAssembly(f)) set.Add(f);
                }
            }
            string gameDir = Path.GetDirectoryName(baseDir);
            if (gameDir != null)
            {
                // Terraria.exe 直接加入（进程内已被 Assembly.Load(byte[]) 加载时 GetAssemblyName 会抛
                // FileLoadException 被 IsManagedAssembly 吞掉，导致 csc 缺 Terraria 引用 → CS0234）
                string terraria = Path.Combine(gameDir, "Terraria.exe");
                if (File.Exists(terraria)) set.Add(terraria);
                foreach (var f in Directory.GetFiles(gameDir, "*.dll"))
                {
                    if (IsManagedAssembly(f)) set.Add(f);
                }
            }
            if (prefix != null && prefix.DeclaringType != null && !string.IsNullOrEmpty(prefix.DeclaringType.Assembly.Location))
                set.Add(prefix.DeclaringType.Assembly.Location);
            if (postfix != null && postfix.DeclaringType != null && !string.IsNullOrEmpty(postfix.DeclaringType.Assembly.Location))
                set.Add(postfix.DeclaringType.Assembly.Location);
            return new List<string>(set);
        }

        /// <summary>csc 只接受托管程序集；native dll（如 ReLogic.Native.dll）会报 CS0009</summary>
        private static bool IsManagedAssembly(string path)
        {
            try
            {
                AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>orig 委托类型：void -> Action&lt;T..&gt;；有返回值 -> Func&lt;T.., R&gt;；实例方法首参 self</summary>
        private static Type MakeOrigDelegateType(MethodBase target, Type retType)
        {
            bool isInstance = !target.IsStatic;
            ParameterInfo[] tps = target.GetParameters();
            int total = tps.Length + (isInstance ? 1 : 0);
            if (total > 8)
                throw new NotSupportedException("[HookBinder] 目标方法参数超过 8 个，本机 CLR 的泛型 Action/Func 无法表达（M2 限制）");
            var paramTypes = new Type[total];
            int idx = 0;
            if (isInstance) paramTypes[idx++] = target.DeclaringType;
            for (int i = 0; i < tps.Length; i++)
            {
                if (tps[i].ParameterType.IsByRef)
                    throw new NotSupportedException("[HookBinder] 目标方法含 ref/out 参数，泛型 orig 委托无法表达（M2 限制）: " + tps[i].Name);
                paramTypes[idx++] = tps[i].ParameterType;
            }

            if (retType == typeof(void))
            {
                return ActionDefs[total].MakeGenericType(paramTypes);
            }
            Type[] typeArgs = new Type[paramTypes.Length + 1];
            Array.Copy(paramTypes, typeArgs, paramTypes.Length);
            typeArgs[paramTypes.Length] = retType;
            return FuncDefs[total].MakeGenericType(typeArgs);
        }

        // Action`0..`8 / Func`0..`8 泛型定义缓存（本机 CLR mscorlib 仅含 Action`1..8）
        private static readonly Type[] ActionDefs = BuildActionDefs();
        private static readonly Type[] FuncDefs = BuildFuncDefs();

        private static Type[] BuildActionDefs()
        {
            var arr = new Type[9];
            arr[0] = typeof(Action);
            FillGenericDefs(arr, typeof(Action).Assembly, "Action`");
            return arr;
        }

        private static Type[] BuildFuncDefs()
        {
            var arr = new Type[9];
            arr[0] = typeof(Func<>); // Func<TResult>
            FillGenericDefs(arr, typeof(Func<>).Assembly, "Func`");
            return arr;
        }

        private static void FillGenericDefs(Type[] arr, Assembly asm, string prefix)
        {
            foreach (var t in asm.GetTypes())
            {
                if (!t.IsGenericTypeDefinition) continue;
                if (t.Namespace != "System") continue;
                if (!t.Name.StartsWith(prefix)) continue;
                int arity;
                if (!int.TryParse(t.Name.Substring(prefix.Length), out arity)) continue;
                if (arity >= 1 && arity <= 8) arr[arity] = t;
            }
            for (int i = 1; i <= 8; i++)
            {
                if (arr[i] == null) throw new NotSupportedException("[HookBinder] 无法定位 " + prefix + i + "（asm=" + asm.FullName + "）");
            }
        }

        private static int FindParamIndex(ParameterInfo[] tps, string name)
        {
            for (int i = 0; i < tps.Length; i++)
            {
                if (tps[i].Name == name) return i;
            }
            return -1;
        }

        private static Type GetReturnType(MethodBase target)
        {
            if (target is MethodInfo mi) return mi.ReturnType;
            if (target is ConstructorInfo) return typeof(void);
            throw new NotSupportedException("[HookBinder] 不支持的目标: " + target);
        }

        private static string CSharpType(Type t)
        {
            if (t == typeof(void)) return "void";
            if (t.IsByRef) return CSharpType(t.GetElementType());
            if (t.IsArray) return CSharpType(t.GetElementType()) + "[]";
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                string name = def.FullName.Substring(0, def.FullName.IndexOf('`')).Replace('+', '.');
                var args = new List<string>();
                foreach (var a in t.GetGenericArguments()) args.Add(CSharpType(a));
                return "global::" + name + "<" + string.Join(", ", args) + ">";
            }
            return "global::" + t.FullName.Replace('+', '.');
        }

        private static string JoinParams(ParameterInfo[] tps)
        {
            var parts = new List<string>();
            for (int i = 0; i < tps.Length; i++)
            {
                parts.Add(CSharpType(tps[i].ParameterType) + " p" + i);
            }
            return string.Join(", ", parts);
        }

        public static IDisposable CreateHook(MethodBase target, MethodInfo prefix, MethodInfo postfix)
        {
            try
            {
                var detour = Build(target, prefix, postfix);
                return new Hook(target, detour);
            }
            catch (Exception ex)
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string gameDir = Path.GetDirectoryName(baseDir);
                string terrariaPath = gameDir == null ? "" : Path.Combine(gameDir, "Terraria.exe");
                string diag = $" baseDir=[{baseDir}] gameDir=[{gameDir}] terrariaExists=[{File.Exists(terrariaPath)}]";
                throw new Exception($"[HookBinder] CreateHook 失败 target={target} prefix={prefix} postfix={postfix}{diag}", ex);
            }
        }
    }
}
