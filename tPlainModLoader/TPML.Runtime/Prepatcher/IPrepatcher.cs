using System;
using Mono.Cecil;

namespace tContentPatch.Prepatcher
{
    /// <summary>
    /// 模组早期 Cecil 预补丁接口。<para/>
    /// 实现该接口的类将在游戏程序集（Terraria.exe）被 CLR 加载前执行，允许直接使用 Mono.Cecil 进行底层类型改写、常量扩容、接口注入或 IL 织入。
    /// </summary>
    public interface IPrepatcher
    {
        /// <summary>
        /// 执行早期 Cecil 预补丁。
        /// </summary>
        /// <param name="terrariaAssembly">原版 Terraria 程序集定义</param>
        void EarlyPatch(AssemblyDefinition terrariaAssembly);
    }

    /// <summary>
    /// 标记用于早期预修补的静态方法。<para/>
    /// 方法签名支持：<br/>
    /// <c>public static void MyPatch(AssemblyDefinition asm)</c> 或 <br/>
    /// <c>public static void MyPatch(ModuleDefinition module)</c>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class FreePatchAttribute : Attribute
    {
    }
}
