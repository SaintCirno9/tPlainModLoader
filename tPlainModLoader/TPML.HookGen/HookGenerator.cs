using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MonoMod.Utils;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using MethodImplAttributes = Mono.Cecil.MethodImplAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace TPML.HookGen
{
    /// <summary>
    /// TPML 核心 Hook 生成器（基于 Cecil 扫描原版 Terraria 并全量生成强类型 On_/IL_ 事件门面）
    /// </summary>
    public class HookGenerator
    {
        private static readonly Dictionary<string, string> TypeNameMap = new Dictionary<string, string>()
        {
            { "System.String", "string" },
            { "System.Object", "object" },
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Int16", "short" },
            { "System.Int32", "int" },
            { "System.Int64", "long" },
            { "System.SByte", "sbyte" },
            { "System.Single", "float" },
            { "System.UInt16", "ushort" },
            { "System.UInt32", "uint" },
            { "System.UInt64", "ulong" },
            { "System.Void", "void" }
        };

        public ModuleDefinition InputModule { get; }
        public ModuleDefinition OutputModule { get; }

        public bool HookPrivate { get; set; } = true;
        public bool HookOrig { get; set; } = false;

        private readonly TypeReference t_MulticastDelegate;
        private readonly TypeReference t_IAsyncResult;
        private readonly TypeReference t_AsyncCallback;
        private readonly TypeReference t_MethodBase;
        private readonly TypeReference t_RuntimeMethodHandle;
        private readonly TypeReference t_EditorBrowsableState;

        private readonly MethodReference m_GetMethodFromHandle;
        private readonly MethodReference m_Add;
        private readonly MethodReference m_Remove;
        private readonly MethodReference m_Modify;
        private readonly MethodReference m_Unmodify;

        private readonly TypeReference t_ILManipulator;
        private readonly MethodReference m_EditorBrowsableAttribute_ctor;
        private readonly bool _modifyIsGeneric;

        public int GeneratedTypeCount { get; private set; }
        public int GeneratedMethodCount { get; private set; }

        public HookGenerator(ModuleDefinition inputModule, string outputAssemblyName)
        {
            InputModule = inputModule ?? throw new ArgumentNullException(nameof(inputModule));

            OutputModule = ModuleDefinition.CreateModule(outputAssemblyName, new ModuleParameters
            {
                Architecture = inputModule.Architecture,
                AssemblyResolver = inputModule.AssemblyResolver,
                Kind = ModuleKind.Dll,
                Runtime = inputModule.Runtime
            });

            // 导入核心基础类型引用
            t_MulticastDelegate = OutputModule.ImportReference(typeof(MulticastDelegate));
            t_IAsyncResult = OutputModule.ImportReference(typeof(IAsyncResult));
            t_AsyncCallback = OutputModule.ImportReference(typeof(AsyncCallback));
            t_MethodBase = OutputModule.ImportReference(typeof(System.Reflection.MethodBase));
            t_RuntimeMethodHandle = OutputModule.ImportReference(typeof(RuntimeMethodHandle));
            t_EditorBrowsableState = OutputModule.ImportReference(typeof(EditorBrowsableState));

            m_GetMethodFromHandle = OutputModule.ImportReference(
                typeof(System.Reflection.MethodBase).GetMethod(nameof(System.Reflection.MethodBase.GetMethodFromHandle),
                    new[] { typeof(RuntimeMethodHandle) })
            );

            m_EditorBrowsableAttribute_ctor = OutputModule.ImportReference(
                typeof(EditorBrowsableAttribute).GetConstructor(new[] { typeof(EditorBrowsableState) })
            );

            // 导入 MonoMod HookEndpointManager 核心方法
            Type hemType = typeof(MonoMod.RuntimeDetour.HookGen.HookEndpointManager);
            System.Reflection.MethodInfo addMethodInfo = hemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Add" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
            System.Reflection.MethodInfo removeMethodInfo = hemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Remove" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
            System.Reflection.MethodInfo modifyMethodInfo = hemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Modify" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
                ?? hemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).FirstOrDefault(m => m.Name == "Modify" && m.GetParameters().Length == 2);
            System.Reflection.MethodInfo unmodifyMethodInfo = hemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Unmodify" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2)
                ?? hemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).FirstOrDefault(m => m.Name == "Unmodify" && m.GetParameters().Length == 2);

            m_Add = OutputModule.ImportReference(addMethodInfo);
            m_Remove = OutputModule.ImportReference(removeMethodInfo);
            m_Modify = OutputModule.ImportReference(modifyMethodInfo);
            m_Unmodify = OutputModule.ImportReference(unmodifyMethodInfo);
            _modifyIsGeneric = modifyMethodInfo != null && modifyMethodInfo.IsGenericMethodDefinition;

            // 导入 ILContext.Manipulator
            t_ILManipulator = OutputModule.ImportReference(typeof(MonoMod.Cil.ILContext.Manipulator));
        }

        public void Generate()
        {
            var typesToProcess = InputModule.Types.ToList();
            foreach (var type in typesToProcess)
            {
                if (type.IsNested) continue;
                if (!type.FullName.StartsWith("Terraria", StringComparison.Ordinal)) continue;
                if (type.FullName.StartsWith("Terraria.ModLoader", StringComparison.Ordinal)) continue;

                GenerateFor(type, out var hookType, out var hookILType);
                if (hookType != null)
                {
                    AdjustNamespaceStyle(hookType);
                    OutputModule.Types.Add(hookType);
                    GeneratedTypeCount++;
                }

                if (hookILType != null)
                {
                    AdjustNamespaceStyle(hookILType);
                    OutputModule.Types.Add(hookILType);
                }
            }
        }

        /// <summary>
        /// 将命名空间样式从 On.Namespace.Type 调整为 Namespace.On_Type（100% 对齐 tModLoader 官方 HookGenTask 规范）
        /// 彻底杜绝 using On.Terraria 与 using Terraria 产生的命名冲突，允许开发者直接写 On_Player.xxx
        /// </summary>
        private static void AdjustNamespaceStyle(TypeDefinition type)
        {
            if (string.IsNullOrEmpty(type.Namespace))
                return;

            type.Name = type.Namespace.Substring(0, 2) + "_" + type.Name;
            type.Namespace = type.Namespace.Substring(Math.Min(3, type.Namespace.Length));
        }

        public void GenerateFor(TypeDefinition type, out TypeDefinition hookType, out TypeDefinition hookILType)
        {
            hookType = hookILType = null;

            if (type.HasGenericParameters ||
                type.IsRuntimeSpecialName ||
                type.Name.StartsWith("<", StringComparison.Ordinal) ||
                type.Name.Contains("$") ||
                type.Name.StartsWith("__", StringComparison.Ordinal))
                return;

            if (!HookPrivate && type.IsNotPublic)
                return;

            string targetNamespace = type.Namespace;
            string hookNs = "On" + (string.IsNullOrEmpty(targetNamespace) ? "" : ("." + targetNamespace));
            string hookILNs = "IL" + (string.IsNullOrEmpty(targetNamespace) ? "" : ("." + targetNamespace));

            hookType = new TypeDefinition(
                type.IsNested ? null : hookNs,
                type.Name,
                (type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public) | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class,
                OutputModule.TypeSystem.Object
            );

            hookILType = new TypeDefinition(
                type.IsNested ? null : hookILNs,
                type.Name,
                (type.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public) | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class,
                OutputModule.TypeSystem.Object
            );

            bool hasValidMembers = false;

            foreach (var method in type.Methods)
            {
                try
                {
                    if (GenerateForMethod(hookType, hookILType, method))
                    {
                        hasValidMembers = true;
                        GeneratedMethodCount++;
                    }
                }
                catch
                {
                    // 跳过极个别不可解析或冲突方法
                }
            }

            foreach (var nested in type.NestedTypes)
            {
                GenerateFor(nested, out var nestedHookType, out var nestedHookILType);
                if (nestedHookType != null && nestedHookILType != null)
                {
                    hasValidMembers = true;
                    hookType.NestedTypes.Add(nestedHookType);
                    hookILType.NestedTypes.Add(nestedHookILType);
                }
            }

            if (!hasValidMembers)
            {
                hookType = null;
                hookILType = null;
            }
        }

        private bool GenerateForMethod(TypeDefinition hookType, TypeDefinition hookILType, MethodDefinition method)
        {
            if (method.HasGenericParameters ||
                method.IsAbstract ||
                (method.IsSpecialName && !method.IsConstructor))
                return false;

            if (!HookOrig && method.Name.StartsWith("orig_", StringComparison.Ordinal))
                return false;

            if (!HookPrivate && method.IsPrivate)
                return false;

            if (method.Name.StartsWith("<", StringComparison.Ordinal))
                return false;

            string name = GetFriendlyMethodName(method);
            bool needSuffix = method.Parameters.Count > 0;

            var overloads = method.DeclaringType.Methods.Where(m =>
                !m.HasGenericParameters &&
                GetFriendlyMethodName(m) == name &&
                m != method
            ).ToList();

            if (overloads.Count == 0)
            {
                needSuffix = false;
            }

            if (needSuffix)
            {
                var sb = new StringBuilder();
                foreach (var param in method.Parameters)
                {
                    string typeName;
                    if (!TypeNameMap.TryGetValue(param.ParameterType.FullName, out typeName))
                        typeName = GetFriendlyTypeName(param.ParameterType);

                    sb.Append('_');
                    sb.Append(typeName.Replace(".", "").Replace("`", "").Replace("[]", "Array").Replace("&", "Ref"));
                }
                name += sb.ToString();
            }

            // 避免名称冲突
            if (hookType.Events.Any(e => e.Name == name))
            {
                int index = 1;
                string newName = name + "_" + index;
                while (hookType.Events.Any(e => e.Name == newName))
                {
                    index++;
                    newName = name + "_" + index;
                }
                name = newName;
            }

            var delOrig = GenerateDelegateFor(method, "orig_" + name);
            delOrig.CustomAttributes.Add(GenerateEditorBrowsable(EditorBrowsableState.Never));
            hookType.NestedTypes.Add(delOrig);

            var delHook = GenerateDelegateFor(method, "hook_" + name, delOrig);
            delHook.CustomAttributes.Add(GenerateEditorBrowsable(EditorBrowsableState.Never));
            hookType.NestedTypes.Add(delHook);

            var methodRef = OutputModule.ImportReference(method);

            #region On Hook Event

            var addHook = new MethodDefinition(
                "add_" + name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                OutputModule.TypeSystem.Void
            );
            addHook.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, delHook));
            addHook.Body = new MethodBody(addHook);
            var il = addHook.Body.GetILProcessor();
            il.Emit(OpCodes.Ldtoken, methodRef);
            il.Emit(OpCodes.Call, m_GetMethodFromHandle);
            il.Emit(OpCodes.Ldarg_0);
            var addEndpoint = new GenericInstanceMethod(m_Add);
            addEndpoint.GenericArguments.Add(delHook);
            il.Emit(OpCodes.Call, addEndpoint);
            il.Emit(OpCodes.Ret);
            hookType.Methods.Add(addHook);

            var removeHook = new MethodDefinition(
                "remove_" + name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                OutputModule.TypeSystem.Void
            );
            removeHook.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, delHook));
            removeHook.Body = new MethodBody(removeHook);
            il = removeHook.Body.GetILProcessor();
            il.Emit(OpCodes.Ldtoken, methodRef);
            il.Emit(OpCodes.Call, m_GetMethodFromHandle);
            il.Emit(OpCodes.Ldarg_0);
            var removeEndpoint = new GenericInstanceMethod(m_Remove);
            removeEndpoint.GenericArguments.Add(delHook);
            il.Emit(OpCodes.Call, removeEndpoint);
            il.Emit(OpCodes.Ret);
            hookType.Methods.Add(removeHook);

            var evHook = new EventDefinition(name, EventAttributes.None, delHook)
            {
                AddMethod = addHook,
                RemoveMethod = removeHook
            };
            hookType.Events.Add(evHook);

            #endregion

            #region IL Hook Event

            var addIL = new MethodDefinition(
                "add_" + name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                OutputModule.TypeSystem.Void
            );
            addIL.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, t_ILManipulator));
            addIL.Body = new MethodBody(addIL);
            il = addIL.Body.GetILProcessor();
            il.Emit(OpCodes.Ldtoken, methodRef);
            il.Emit(OpCodes.Call, m_GetMethodFromHandle);
            il.Emit(OpCodes.Ldarg_0);
            if (_modifyIsGeneric)
            {
                var modifyEndpoint = new GenericInstanceMethod(m_Modify);
                modifyEndpoint.GenericArguments.Add(delHook);
                il.Emit(OpCodes.Call, modifyEndpoint);
            }
            else
            {
                il.Emit(OpCodes.Call, m_Modify);
            }
            il.Emit(OpCodes.Ret);
            hookILType.Methods.Add(addIL);

            var removeIL = new MethodDefinition(
                "remove_" + name,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Static,
                OutputModule.TypeSystem.Void
            );
            removeIL.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, t_ILManipulator));
            removeIL.Body = new MethodBody(removeIL);
            il = removeIL.Body.GetILProcessor();
            il.Emit(OpCodes.Ldtoken, methodRef);
            il.Emit(OpCodes.Call, m_GetMethodFromHandle);
            il.Emit(OpCodes.Ldarg_0);
            if (_modifyIsGeneric)
            {
                var unmodifyEndpoint = new GenericInstanceMethod(m_Unmodify);
                unmodifyEndpoint.GenericArguments.Add(delHook);
                il.Emit(OpCodes.Call, unmodifyEndpoint);
            }
            else
            {
                il.Emit(OpCodes.Call, m_Unmodify);
            }
            il.Emit(OpCodes.Ret);
            hookILType.Methods.Add(removeIL);

            var evIL = new EventDefinition(name, EventAttributes.None, t_ILManipulator)
            {
                AddMethod = addIL,
                RemoveMethod = removeIL
            };
            hookILType.Events.Add(evIL);

            #endregion

            return true;
        }

        private TypeDefinition GenerateDelegateFor(MethodDefinition method, string delegateName, TypeDefinition origDelegate = null)
        {
            var del = new TypeDefinition(
                null, delegateName,
                TypeAttributes.NestedPublic | TypeAttributes.Sealed | TypeAttributes.Class,
                t_MulticastDelegate
            );

            // .ctor(object, IntPtr)
            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.ReuseSlot,
                OutputModule.TypeSystem.Void
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };
            ctor.Parameters.Add(new ParameterDefinition("object", ParameterAttributes.None, OutputModule.TypeSystem.Object));
            ctor.Parameters.Add(new ParameterDefinition("method", ParameterAttributes.None, OutputModule.TypeSystem.IntPtr));
            ctor.Body = new MethodBody(ctor);
            del.Methods.Add(ctor);

            // Invoke(...)
            var invoke = new MethodDefinition(
                "Invoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                ImportVisible(method.ReturnType)
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };

            if (origDelegate != null)
            {
                invoke.Parameters.Add(new ParameterDefinition("orig", ParameterAttributes.None, origDelegate));
            }

            if (!method.IsStatic)
            {
                TypeReference selfType = ImportVisible(method.DeclaringType);
                if (method.DeclaringType.IsValueType)
                    selfType = new ByReferenceType(selfType);
                invoke.Parameters.Add(new ParameterDefinition("self", ParameterAttributes.None, selfType));
            }

            foreach (var param in method.Parameters)
            {
                invoke.Parameters.Add(new ParameterDefinition(
                    param.Name,
                    param.Attributes & ~ParameterAttributes.Optional & ~ParameterAttributes.HasDefault,
                    ImportVisible(param.ParameterType)
                ));
            }

            invoke.Body = new MethodBody(invoke);
            del.Methods.Add(invoke);

            // BeginInvoke
            var invokeBegin = new MethodDefinition(
                "BeginInvoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                t_IAsyncResult
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };
            foreach (var param in invoke.Parameters)
            {
                invokeBegin.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
            }
            invokeBegin.Parameters.Add(new ParameterDefinition("callback", ParameterAttributes.None, t_AsyncCallback));
            invokeBegin.Parameters.Add(new ParameterDefinition("object", ParameterAttributes.None, OutputModule.TypeSystem.Object));
            invokeBegin.Body = new MethodBody(invokeBegin);
            del.Methods.Add(invokeBegin);

            // EndInvoke
            var invokeEnd = new MethodDefinition(
                "EndInvoke",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                ImportVisible(method.ReturnType)
            )
            {
                ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed,
                HasThis = true
            };
            foreach (var param in invoke.Parameters)
            {
                if (param.ParameterType.IsByReference)
                    invokeEnd.Parameters.Add(new ParameterDefinition(param.Name, param.Attributes, param.ParameterType));
            }
            invokeEnd.Parameters.Add(new ParameterDefinition("result", ParameterAttributes.None, t_IAsyncResult));
            invokeEnd.Body = new MethodBody(invokeEnd);
            del.Methods.Add(invokeEnd);

            return del;
        }

        private static string GetFriendlyMethodName(MethodDefinition method)
        {
            string name = method.Name;
            if (name.StartsWith(".", StringComparison.Ordinal))
                name = name.Substring(1);
            return name.Replace('.', '_');
        }

        private static string GetFriendlyTypeName(TypeReference type)
        {
            if (type is TypeSpecification spec)
            {
                string baseName = GetFriendlyTypeName(spec.ElementType);
                if (type.IsByReference) return "ref_" + baseName;
                if (type.IsArray) return baseName + "Array";
                if (type.IsPointer) return "ptr_" + baseName;
                return baseName;
            }
            return type.Name;
        }

        private TypeReference ImportVisible(TypeReference typeRef)
        {
            if (typeRef == null) return OutputModule.TypeSystem.Object;
            try
            {
                return OutputModule.ImportReference(typeRef);
            }
            catch
            {
                return OutputModule.TypeSystem.Object;
            }
        }

        private CustomAttribute GenerateEditorBrowsable(EditorBrowsableState state)
        {
            var attrib = new CustomAttribute(m_EditorBrowsableAttribute_ctor);
            attrib.ConstructorArguments.Add(new CustomAttributeArgument(t_EditorBrowsableState, (int)state));
            return attrib;
        }
    }
}
