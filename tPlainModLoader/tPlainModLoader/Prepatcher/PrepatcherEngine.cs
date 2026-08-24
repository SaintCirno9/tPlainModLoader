using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using tContentPatch.Prepatcher;
using tContentPatch.Utils;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodImplAttributes = Mono.Cecil.MethodImplAttributes;

namespace tPlainModLoader.Prepatcher
{
    /// <summary>
    /// tPlainModLoader Prepatcher 核心调度与 IL 预修补引擎。<para/>
    /// 负责在游戏启动前执行：<br/>
    /// 1. 扫描各已启用 Mod 中的 <see cref="PrepatcherFieldAttribute"/> 特性；<br/>
    /// 2. 向原版目标类型（如 Player / NPC / Item）动态注入原生字段（零开销 Free Fields）；<br/>
    /// 3. 将 Mod 扩展访问器重写为单条原生 IL 访问指令（ldflda / ldfld）；<br/>
    /// 4. 扫描并调度 <see cref="IPrepatcher"/> 与 <see cref="FreePatchAttribute"/> 早期 Cecil 预补丁。
    /// </summary>
    public static class PrepatcherEngine
    {
        public static void Process(AssemblyDefinition terrariaAssembly, string gameDir, string hostDir)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine("[Prepatcher] 正在启动 Prepatcher 预修补引擎...");
            Log.Add("[Prepatcher] 启动 Prepatcher 引擎");

            List<string> modDlls = ModScanner.ScanActiveModDlls(gameDir, hostDir);
            Console.WriteLine($"[Prepatcher] 发现 {modDlls.Count} 个活跃模组程序集");
            Log.Add($"[Prepatcher] 发现 {modDlls.Count} 个模组程序集");

            var resolver = new CustomModAssemblyResolver(gameDir, hostDir, modDlls);
            var readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = false
            };

            int injectedFieldCount = 0;
            int patchedMethodCount = 0;
            int executedEarlyPatchCount = 0;

            // 维护原版类型的快速查表字典
            Dictionary<string, TypeDefinition> terrariaTypeMap = new Dictionary<string, TypeDefinition>(StringComparer.Ordinal);
            CollectTypesRecursive(terrariaAssembly.MainModule.Types, terrariaTypeMap);

            List<(string DllPath, AssemblyDefinition ModAsm, bool Modified)> loadedMods = new List<(string, AssemblyDefinition, bool)>();

            // 1. 处理 [PrepatcherField] 自由字段注入与 IL 访问器重写
            foreach (string dllPath in modDlls)
            {
                try
                {
                    AssemblyDefinition modAsm = AssemblyDefinition.ReadAssembly(dllPath, readerParams);
                    bool modified = false;

                    List<MethodDefinition> accessorMethods = FindPrepatcherFieldAccessors(modAsm.MainModule.Types);
                    foreach (MethodDefinition accessor in accessorMethods)
                    {
                        try
                        {
                            if (ProcessFieldAccessor(accessor, terrariaAssembly, terrariaTypeMap, modAsm))
                            {
                                injectedFieldCount++;
                                patchedMethodCount++;
                                modified = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            string err = $"[Prepatcher] 处理字段访问器 {accessor.DeclaringType.FullName}.{accessor.Name} 失败: {ex.Message}";
                            Console.WriteLine(err);
                            Log.Add(err);
                        }
                    }

                    if (modified)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            modAsm.Write(ms);
                            byte[] patchedBytes = ms.ToArray();
                            PrepatcherStorage.RegisterPatchedBytes(dllPath, patchedBytes);
                        }
                    }

                    loadedMods.Add((dllPath, modAsm, modified));
                }
                catch (Exception ex)
                {
                    string err = $"[Prepatcher] 读取模组程序集 {Path.GetFileName(dllPath)} 失败: {ex.Message}";
                    Console.WriteLine(err);
                    Log.Add(err);
                }
            }

            // 2. 处理 IPrepatcher 与 [FreePatch] 早期 Cecil 预补丁（使用 Cecil 静态元数据进行高效先验检测）
            foreach (var modItem in loadedMods)
            {
                AssemblyDefinition modAsm = modItem.ModAsm;
                var prepatcherTypes = FindPrepatcherImplementations(modAsm.MainModule.Types);
                var freePatchMethods = FindFreePatchMethods(modAsm.MainModule.Types);

                if (prepatcherTypes.Count == 0 && freePatchMethods.Count == 0)
                {
                    // 绝大多数模组未声明早期 Cecil 补丁，直接纯静态跳过，避免无效反射与依赖加载开销
                    continue;
                }

                try
                {
                    byte[] asmBytes;
                    if (!PrepatcherStorage.TryGetPatchedBytes(modItem.DllPath, out asmBytes))
                    {
                        asmBytes = File.ReadAllBytes(modItem.DllPath);
                    }

                    Assembly modReflectionAsm = Assembly.Load(asmBytes);

                    // 执行已声明的 IPrepatcher 实现类
                    foreach (TypeDefinition typeDef in prepatcherTypes)
                    {
                        try
                        {
                            Type type = modReflectionAsm.GetType(typeDef.FullName, false);
                            if (type != null && typeof(IPrepatcher).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                            {
                                IPrepatcher instance = (IPrepatcher)Activator.CreateInstance(type);
                                instance.EarlyPatch(terrariaAssembly);
                                executedEarlyPatchCount++;
                                Console.WriteLine($"[Prepatcher] 成功执行早期补丁: {type.FullName}.EarlyPatch");
                                Log.Add($"[Prepatcher] 执行早期补丁: {type.FullName}.EarlyPatch");
                            }
                        }
                        catch (Exception ex)
                        {
                            string err = $"[Prepatcher] 执行 IPrepatcher {typeDef.FullName} 异常: {ex.Message}";
                            Console.WriteLine(err);
                            Log.Add(err);
                        }
                    }

                    // 执行已声明的 [FreePatch] 静态方法
                    foreach (MethodDefinition methodDef in freePatchMethods)
                    {
                        try
                        {
                            Type type = modReflectionAsm.GetType(methodDef.DeclaringType.FullName, false);
                            MethodInfo method = type?.GetMethod(methodDef.Name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            if (method != null)
                            {
                                ParameterInfo[] pars = method.GetParameters();
                                if (pars.Length == 1 && pars[0].ParameterType == typeof(AssemblyDefinition))
                                {
                                    method.Invoke(null, new object[] { terrariaAssembly });
                                    executedEarlyPatchCount++;
                                    Console.WriteLine($"[Prepatcher] 成功执行 FreePatch: {type.FullName}.{method.Name}");
                                    Log.Add($"[Prepatcher] 成功执行 FreePatch: {type.FullName}.{method.Name}");
                                }
                                else if (pars.Length == 1 && pars[0].ParameterType == typeof(ModuleDefinition))
                                {
                                    method.Invoke(null, new object[] { terrariaAssembly.MainModule });
                                    executedEarlyPatchCount++;
                                    Console.WriteLine($"[Prepatcher] 成功执行 FreePatch: {type.FullName}.{method.Name}");
                                    Log.Add($"[Prepatcher] 成功执行 FreePatch: {type.FullName}.{method.Name}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string err = $"[Prepatcher] 执行 FreePatch {methodDef.DeclaringType.FullName}.{methodDef.Name} 异常: {ex.Message}";
                            Console.WriteLine(err);
                            Log.Add(err);
                        }
                    }
                }
                catch (Exception ex)
                {
                    string err = $"[Prepatcher] 处理早期补丁程序集异常 {Path.GetFileName(modItem.DllPath)}: {ex.Message}";
                    Console.WriteLine(err);
                    Log.Add(err);
                }
            }

            // 3. 织入核心窗口早期黑化补丁（彻底杜绝启动白屏闪烁）
            InjectGameWindowDarkener(terrariaAssembly);

            sw.Stop();
            string finishMsg = $"[Prepatcher] 预修补完成 (注入字段: {injectedFieldCount}, 重写访问器: {patchedMethodCount}, 早期补丁: {executedEarlyPatchCount}, 耗时: {sw.ElapsedMilliseconds}ms)";
            Console.WriteLine(finishMsg);
            Log.Add(finishMsg);
        }

        private static void InjectGameWindowDarkener(AssemblyDefinition terrariaAssembly)
        {
            try
            {
                TypeDefinition mainType = terrariaAssembly.MainModule.Types.FirstOrDefault(t => t.FullName == "Terraria.Main");
                if (mainType == null) return;

                MethodDefinition ctor = mainType.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0);
                if (ctor == null || ctor.Body == null) return;

                Type darkenerType = typeof(GameWindowDarkener);
                MethodInfo applyMethod = darkenerType.GetMethod("ApplyFromGame", BindingFlags.Static | BindingFlags.Public);
                if (applyMethod == null) return;

                MethodReference applyMethodRef = terrariaAssembly.MainModule.ImportReference(applyMethod);

                var il = ctor.Body.GetILProcessor();
                var retInstructions = ctor.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToList();
                foreach (var ret in retInstructions)
                {
                    var ldarg = il.Create(OpCodes.Ldarg_0);
                    var call = il.Create(OpCodes.Call, applyMethodRef);
                    il.InsertBefore(ret, ldarg);
                    il.InsertBefore(ret, call);
                }

                Console.WriteLine("[Prepatcher] 成功向 Terraria.Main..ctor 织入 GameWindowDarkener 早期黑化拦截");
                Log.Add("[Prepatcher] 成功向 Terraria.Main..ctor 织入 GameWindowDarkener 早期黑化拦截");
            }
            catch (Exception ex)
            {
                string err = $"[Prepatcher] 织入 GameWindowDarkener 失败: {ex.Message}";
                Console.WriteLine(err);
                Log.Add(err);
            }
        }

        private static bool ProcessFieldAccessor(
            MethodDefinition accessor,
            AssemblyDefinition terrariaAssembly,
            Dictionary<string, TypeDefinition> terrariaTypeMap,
            AssemblyDefinition modAsm)
        {
            if (!accessor.IsStatic || accessor.Parameters.Count < 1)
            {
                Console.WriteLine($"[Prepatcher] 跳过无效的 PrepatcherField 方法 {accessor.Name}: 必须为静态方法且至少包含一个宿主参数");
                return false;
            }

            // 1. 获取目标类型
            TypeReference targetParamType = accessor.Parameters[0].ParameterType;
            string targetTypeName = targetParamType.FullName;

            TypeDefinition targetTypeDef;
            if (!terrariaTypeMap.TryGetValue(targetTypeName, out targetTypeDef))
            {
                Console.WriteLine($"[Prepatcher] 未在原版程序集中找到目标宿主类型: {targetTypeName}");
                return false;
            }

            // 2. 获取字段类型与返回方式
            TypeReference returnType = accessor.ReturnType;
            bool isRef = returnType.IsByReference;
            TypeReference fieldType = isRef ? ((ByReferenceType)returnType).ElementType : returnType;

            // 3. 计算字段名称
            string customFieldName = GetCustomFieldName(accessor);
            string fieldName = !string.IsNullOrEmpty(customFieldName)
                ? customFieldName
                : $"_pp_{modAsm.Name.Name}_{accessor.Name}_{accessor.MetadataToken.RID}";

            // 检查目标类型是否已存在同名字段
            FieldDefinition existingField = targetTypeDef.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (existingField == null)
            {
                // 将字段类型导入原版模块
                TypeReference importedFieldType = targetTypeDef.Module.ImportReference(fieldType);
                FieldDefinition newField = new FieldDefinition(
                    fieldName,
                    FieldAttributes.Public,
                    importedFieldType
                );
                targetTypeDef.Fields.Add(newField);
                existingField = newField;
            }

            // 4. 重写 Mod 扩展方法体 IL 为原生直接访问
            accessor.ImplAttributes &= ~MethodImplAttributes.InternalCall;
            accessor.Body.Instructions.Clear();
            accessor.Body.Variables.Clear();
            accessor.Body.ExceptionHandlers.Clear();

            var il = accessor.Body.GetILProcessor();

            // 构建在 Mod 模块内对原版目标类型及新字段的 FieldReference
            TypeReference importedTargetType = accessor.Module.ImportReference(targetTypeDef);
            TypeReference importedFieldTypeInMod = accessor.Module.ImportReference(existingField.FieldType);
            FieldReference fieldRef = new FieldReference(fieldName, importedFieldTypeInMod, importedTargetType);

            il.Emit(OpCodes.Ldarg_0);
            if (isRef)
            {
                il.Emit(OpCodes.Ldflda, fieldRef);
            }
            else
            {
                il.Emit(OpCodes.Ldfld, fieldRef);
            }
            il.Emit(OpCodes.Ret);

            return true;
        }

        private static string GetCustomFieldName(MethodDefinition method)
        {
            foreach (var ca in method.CustomAttributes)
            {
                if (ca.AttributeType.FullName == "tContentPatch.Prepatcher.PrepatcherFieldAttribute" ||
                    ca.AttributeType.Name == "PrepatcherFieldAttribute")
                {
                    if (ca.ConstructorArguments.Count > 0)
                    {
                        var arg = ca.ConstructorArguments[0];
                        if (arg.Value is string s && !string.IsNullOrEmpty(s))
                            return s;
                    }
                    if (ca.HasProperties)
                    {
                        var prop = ca.Properties.FirstOrDefault(p => p.Name == "FieldName");
                        if (prop.Argument.Value is string ps && !string.IsNullOrEmpty(ps))
                            return ps;
                    }
                }
            }
            return null;
        }

        private static List<MethodDefinition> FindPrepatcherFieldAccessors(IEnumerable<TypeDefinition> types)
        {
            List<MethodDefinition> result = new List<MethodDefinition>();
            foreach (TypeDefinition type in types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method.CustomAttributes.Any(ca =>
                        ca.AttributeType.FullName == "tContentPatch.Prepatcher.PrepatcherFieldAttribute" ||
                        ca.AttributeType.Name == "PrepatcherFieldAttribute"))
                    {
                        result.Add(method);
                    }
                }

                if (type.HasNestedTypes)
                {
                    result.AddRange(FindPrepatcherFieldAccessors(type.NestedTypes));
                }
            }
            return result;
        }

        private static List<TypeDefinition> FindPrepatcherImplementations(IEnumerable<TypeDefinition> types)
        {
            List<TypeDefinition> result = new List<TypeDefinition>();
            foreach (TypeDefinition type in types)
            {
                if (!type.IsAbstract && !type.IsInterface)
                {
                    if (type.Interfaces.Any(i =>
                        i.InterfaceType.FullName == "tContentPatch.Prepatcher.IPrepatcher" ||
                        i.InterfaceType.Name == "IPrepatcher"))
                    {
                        result.Add(type);
                    }
                }

                if (type.HasNestedTypes)
                {
                    result.AddRange(FindPrepatcherImplementations(type.NestedTypes));
                }
            }
            return result;
        }

        private static List<MethodDefinition> FindFreePatchMethods(IEnumerable<TypeDefinition> types)
        {
            List<MethodDefinition> result = new List<MethodDefinition>();
            foreach (TypeDefinition type in types)
            {
                foreach (MethodDefinition method in type.Methods)
                {
                    if (method.IsStatic && method.CustomAttributes.Any(ca =>
                        ca.AttributeType.FullName == "tContentPatch.Prepatcher.FreePatchAttribute" ||
                        ca.AttributeType.Name == "FreePatchAttribute"))
                    {
                        result.Add(method);
                    }
                }

                if (type.HasNestedTypes)
                {
                    result.AddRange(FindFreePatchMethods(type.NestedTypes));
                }
            }
            return result;
        }

        private static void CollectTypesRecursive(IEnumerable<TypeDefinition> types, Dictionary<string, TypeDefinition> map)
        {
            foreach (TypeDefinition type in types)
            {
                map[type.FullName] = type;
                if (type.HasNestedTypes)
                {
                    CollectTypesRecursive(type.NestedTypes, map);
                }
            }
        }

        private class CustomModAssemblyResolver : BaseAssemblyResolver
        {
            private readonly DefaultAssemblyResolver _defaultResolver = new DefaultAssemblyResolver();
            private readonly HashSet<string> _searchDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public CustomModAssemblyResolver(string gameDir, string hostDir, IEnumerable<string> modDlls)
            {
                if (!string.IsNullOrEmpty(gameDir) && Directory.Exists(gameDir))
                {
                    _defaultResolver.AddSearchDirectory(gameDir);
                    _searchDirs.Add(gameDir);
                }
                if (!string.IsNullOrEmpty(hostDir) && Directory.Exists(hostDir))
                {
                    _defaultResolver.AddSearchDirectory(hostDir);
                    _searchDirs.Add(hostDir);
                }
                _searchDirs.Add(Directory.GetCurrentDirectory());

                foreach (string dll in modDlls)
                {
                    string dir = Path.GetDirectoryName(dll);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && _searchDirs.Add(dir))
                    {
                        _defaultResolver.AddSearchDirectory(dir);
                    }
                }
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                try
                {
                    return _defaultResolver.Resolve(name, parameters);
                }
                catch { }

                foreach (string dir in _searchDirs)
                {
                    string candidate = Path.Combine(dir, name.Name + ".dll");
                    if (File.Exists(candidate))
                    {
                        return AssemblyDefinition.ReadAssembly(candidate, parameters);
                    }
                }

                Assembly loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name.Name);
                if (loaded != null && !string.IsNullOrEmpty(loaded.Location) && File.Exists(loaded.Location))
                {
                    return AssemblyDefinition.ReadAssembly(loaded.Location, parameters);
                }

                throw new AssemblyResolutionException(name);
            }
        }
    }
}
