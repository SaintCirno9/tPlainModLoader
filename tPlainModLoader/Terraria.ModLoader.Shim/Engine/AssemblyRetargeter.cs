using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace Terraria.ModLoader.Engine
{
    /// <summary>
    /// tModLoader 外部模组程序集目标框架与签名内存重定向器 (Mono.Cecil)
    /// </summary>
    public static class AssemblyRetargeter
    {
        private static readonly byte[] MscorlibPKT = new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };
        private static readonly byte[] XnaPKT = new byte[] { 0x84, 0x2c, 0xf8, 0xbe, 0x1d, 0xe5, 0x05, 0x53 };

        public static byte[] Retarget(byte[] rawBytes, string modName) => RetargetAssembly(rawBytes, modName);

        public static byte[] RetargetAssembly(byte[] rawBytes, string modName)
        {
            if (rawBytes == null || rawBytes.Length == 0) return rawBytes;

            try
            {
                using (var ms = new MemoryStream(rawBytes))
                using (var asmDef = AssemblyDefinition.ReadAssembly(ms, new ReaderParameters { ReadSymbols = false }))
                {
                    bool modified = false;
                    var module = asmDef.MainModule;

                    // 1. 准备标准 .NET Framework 4.7.2 / XNA 4.0 引用
                    var mscorlibRef = GetOrCreateAssemblyRef(module, "mscorlib", new Version(4, 0, 0, 0), MscorlibPKT);
                    var systemCoreRef = GetOrCreateAssemblyRef(module, "System.Core", new Version(4, 0, 0, 0), MscorlibPKT);
                    var xnaCoreRef = GetOrCreateAssemblyRef(module, "Microsoft.Xna.Framework", new Version(4, 0, 0, 0), XnaPKT);
                    var xnaGraphicsRef = GetOrCreateAssemblyRef(module, "Microsoft.Xna.Framework.Graphics", new Version(4, 0, 0, 0), XnaPKT);
                    var xnaGameRef = GetOrCreateAssemblyRef(module, "Microsoft.Xna.Framework.Game", new Version(4, 0, 0, 0), XnaPKT);
                    var xnaXactRef = GetOrCreateAssemblyRef(module, "Microsoft.Xna.Framework.Xact", new Version(4, 0, 0, 0), XnaPKT);
                    var tmlRef = GetOrCreateAssemblyRef(module, "Terraria.ModLoader", new Version(0, 0, 0, 0), null);

                    // 2. 重写 AssemblyReferences
                    for (int i = module.AssemblyReferences.Count - 1; i >= 0; i--)
                    {
                        var anr = module.AssemblyReferences[i];
                        string name = anr.Name;

                        if (name.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("System.Collections", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("System.IO", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("System.Threading", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase) ||
                            name.Equals("netstandard", StringComparison.OrdinalIgnoreCase))
                        {
                            anr.Name = "mscorlib";
                            anr.Version = new Version(4, 0, 0, 0);
                            anr.PublicKeyToken = MscorlibPKT;
                            modified = true;
                        }
                        else if (name.Equals("System.Linq", StringComparison.OrdinalIgnoreCase) ||
                                 name.Equals("System.Linq.Expressions", StringComparison.OrdinalIgnoreCase))
                        {
                            anr.Name = "System.Core";
                            anr.Version = new Version(4, 0, 0, 0);
                            anr.PublicKeyToken = MscorlibPKT;
                            modified = true;
                        }
                        else if (name.Equals("FNA", StringComparison.OrdinalIgnoreCase))
                        {
                            anr.Name = "Microsoft.Xna.Framework";
                            anr.Version = new Version(4, 0, 0, 0);
                            anr.PublicKeyToken = XnaPKT;
                            modified = true;
                        }
                        else if (name.Equals("tModLoader", StringComparison.OrdinalIgnoreCase) ||
                                 name.Equals("TerrariaHooks", StringComparison.OrdinalIgnoreCase))
                        {
                            anr.Name = "Terraria.ModLoader";
                            anr.Version = new Version(0, 0, 0, 0);
                            anr.PublicKeyToken = null;
                            modified = true;
                        }
                    }

                    // 3. 精确重定向 TypeReferences 到具体程序集（解决 FNA 分割为 XNA.Graphics / XNA.Game 等的签名匹配）
                    foreach (var typeRef in module.GetTypeReferences())
                    {
                        string ns = typeRef.Namespace ?? string.Empty;

                        if (ns.StartsWith("Microsoft.Xna.Framework.Graphics"))
                        {
                            typeRef.Scope = xnaGraphicsRef;
                            modified = true;
                        }
                        else if (ns.StartsWith("Microsoft.Xna.Framework.Audio") || ns.StartsWith("Microsoft.Xna.Framework.Media"))
                        {
                            typeRef.Scope = xnaXactRef;
                            modified = true;
                        }
                        else if (ns.StartsWith("Microsoft.Xna.Framework.Game") || ns.Equals("Microsoft.Xna.Framework.GamerServices"))
                        {
                            typeRef.Scope = xnaGameRef;
                            modified = true;
                        }
                        else if (ns.StartsWith("Microsoft.Xna.Framework"))
                        {
                            typeRef.Scope = xnaCoreRef;
                            modified = true;
                        }
                        else if (ns.Equals("System.Linq") || ns.StartsWith("System.Linq."))
                        {
                            typeRef.Scope = systemCoreRef;
                            modified = true;
                        }
                        else if (ns.StartsWith("System.") || ns.Equals("System"))
                        {
                            typeRef.Scope = mscorlibRef;
                            modified = true;
                        }
                        else if (ns.StartsWith("Terraria.ModLoader"))
                        {
                            typeRef.Scope = tmlRef;
                            modified = true;
                        }
                    }

                    // 4. 准备扩展方法 MethodReference
                    MethodInfo getModPlayerMethodInfo = typeof(ModPlayerExtensions).GetMethod(nameof(ModPlayerExtensions.GetModPlayer));
                    MethodReference getModPlayerRef = getModPlayerMethodInfo != null ? module.ImportReference(getModPlayerMethodInfo) : null;

                    MethodInfo getSourceMiscMethodInfo = typeof(ModPlayerExtensions).GetMethod(nameof(ModPlayerExtensions.GetSource_Misc));
                    MethodReference getSourceMiscRef = getSourceMiscMethodInfo != null ? module.ImportReference(getSourceMiscMethodInfo) : null;

                    // 5. 扫描所有类型与方法中的 IL 指令，重写扩展方法调用
                    foreach (var type in module.Types)
                    {
                        RetargetTypeMethods(type, getModPlayerRef, getSourceMiscRef, ref modified);
                    }

                    if (modified)
                    {
                        using (var outMs = new MemoryStream())
                        {
                            asmDef.Write(outMs);
                            TModShimEngine.Log($"[AssemblyRetargeter] 模组 [{modName}] 成功完成 IL 元数据框架与 XNA/FNA 签名精确重定向 (net8/FNA/GetModPlayer -> net472/XNA.Graphics/Shim)");
                            return outMs.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TModShimEngine.Log($"[AssemblyRetargeter] 模组 [{modName}] IL 重定向异常: {ex.Message}\n{ex.StackTrace}");
            }

            return rawBytes;
        }

        private static AssemblyNameReference GetOrCreateAssemblyRef(ModuleDefinition module, string name, Version version, byte[] pkt)
        {
            var existing = module.AssemblyReferences.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Version = version;
                existing.PublicKeyToken = pkt;
                return existing;
            }

            var anr = new AssemblyNameReference(name, version)
            {
                PublicKeyToken = pkt
            };
            module.AssemblyReferences.Add(anr);
            return anr;
        }

        private static void RetargetTypeMethods(TypeDefinition type, MethodReference getModPlayerRef, MethodReference getSourceMiscRef, ref bool modified)
        {
            if (type == null) return;

            foreach (var method in type.Methods)
            {
                if (!method.HasBody) continue;

                var instructions = method.Body.Instructions;
                for (int i = 0; i < instructions.Count; i++)
                {
                    var inst = instructions[i];
                    if (inst.OpCode != OpCodes.Callvirt && inst.OpCode != OpCodes.Call) continue;

                    // 检查 Player.GetModPlayer<T>()
                    if (inst.Operand is GenericInstanceMethod gim)
                    {
                        if (gim.ElementMethod.DeclaringType.FullName == "Terraria.Player" &&
                            gim.ElementMethod.Name == "GetModPlayer" &&
                            getModPlayerRef != null)
                        {
                            var genericCall = new GenericInstanceMethod(getModPlayerRef);
                            foreach (var arg in gim.GenericArguments)
                            {
                                genericCall.GenericArguments.Add(arg);
                            }

                            inst.OpCode = OpCodes.Call;
                            inst.Operand = genericCall;
                            modified = true;
                        }
                    }
                    else if (inst.Operand is MethodReference mr)
                    {
                        if (mr.DeclaringType.FullName == "Terraria.Entity" &&
                            mr.Name == "GetSource_Misc" &&
                            getSourceMiscRef != null)
                        {
                            inst.OpCode = OpCodes.Call;
                            inst.Operand = getSourceMiscRef;
                            modified = true;
                        }
                    }
                }
            }

            // 递归处理嵌套类型
            if (type.HasNestedTypes)
            {
                foreach (var nested in type.NestedTypes)
                {
                    RetargetTypeMethods(nested, getModPlayerRef, getSourceMiscRef, ref modified);
                }
            }
        }
    }
}
