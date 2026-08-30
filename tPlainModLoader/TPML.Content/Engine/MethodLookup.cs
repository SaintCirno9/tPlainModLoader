using System;
using System.Reflection;

namespace TPML.Content.Engine
{
    /// <summary>
    /// 方法查找工具（M2 实测结论）：在本运行时环境下，<c>Type.GetMethod(name, Type[])</c>
    /// 对从 <c>Assembly.Load(byte[])</c> 加载的游戏程序集（Terraria 经 Publicize/Prepatcher 处理后）
    /// 返回 null（实测：类型引用相等仍匹配失败），而 <c>GetMethods(flags)</c> 可正常枚举。
    /// 统一改用 GetMethods 按 名字 + 参数类型（含 ref 语义）精确匹配。
    /// </summary>
    public static class MethodLookup
    {
        /// <summary>查找实例方法（含非 public）</summary>
        public static MethodInfo Instance(Type type, string name, params Type[] types)
        {
            return Find(type, name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, types);
        }

        /// <summary>查找静态方法（含非 public）</summary>
        public static MethodInfo Static(Type type, string name, params Type[] types)
        {
            return Find(type, name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, types);
        }

        /// <summary>按名字与参数类型（byref 敏感）精确匹配</summary>
        public static MethodInfo Find(Type type, string name, BindingFlags flags, params Type[] types)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            foreach (var m in type.GetMethods(flags))
            {
                if (m.Name != name) continue;
                var ps = m.GetParameters();
                if (ps.Length != types.Length) continue;
                bool ok = true;
                for (int i = 0; i < types.Length; i++)
                {
                    if (ps[i].ParameterType != types[i]) { ok = false; break; }
                }
                if (ok) return m;
            }
            return null;
        }
    }
}
