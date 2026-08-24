using System;
using System.Runtime.CompilerServices;

namespace tContentPatch.Prepatcher
{
    /// <summary>
    /// Prepatcher 编译期占位存根类。<para/>
    /// 在模组编写声明式字段访问扩展方法时作为占位方法体使用。<para/>
    /// 在游戏启动时，Prepatcher 引擎会将调用此存根的方法体清空并重写为原生高效 IL 指令。
    /// </summary>
    public static class Prepatcher
    {
        /// <summary>
        /// 编译期占位存根：返回类型 <typeparamref name="T"/> 的引用。<para/>
        /// 注意：该方法不应在运行时被直接执行。如果抛出异常，说明 Prepatcher 引擎未在启动阶段成功修补该方法。
        /// </summary>
        /// <typeparam name="T">字段数据类型</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ref T UnsafeRef<T>()
        {
            throw new NotImplementedException("Prepatcher 存根方法被直接调用。请检查 Prepatcher 是否在启动时成功对该扩展方法进行了预修补。");
        }

        /// <summary>
        /// 编译期占位存根：返回类型 <typeparamref name="T"/> 的值。<para/>
        /// 注意：该方法不应在运行时被直接执行。如果抛出异常，说明 Prepatcher 引擎未在启动阶段成功修补该方法。
        /// </summary>
        /// <typeparam name="T">字段数据类型</typeparam>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T UnsafeVal<T>()
        {
            throw new NotImplementedException("Prepatcher 存根方法被直接调用。请检查 Prepatcher 是否在启动时成功对该扩展方法进行了预修补。");
        }
    }
}
