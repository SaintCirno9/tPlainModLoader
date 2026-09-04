using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace OptimizeAndTool.Utils
{
    /// <summary>
    /// 标注在配置数据类 (如 SettingUI_player.Data) 字段上，声明其对应的底层目标成员。
    /// Author: SaintCirno9
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigBindAttribute : Attribute
    {
        /// <summary>
        /// 包含目标静态 GetSetReset 实例的类型
        /// </summary>
        public Type TargetType { get; }

        /// <summary>
        /// 目标静态成员名称（字段或属性）。若为 null，则依次尝试推断：与字段同名、"Enable"、"Enabled"。
        /// </summary>
        public string MemberName { get; }

        public ConfigBindAttribute(Type targetType, string memberName = null)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
            MemberName = memberName;
        }
    }

    /// <summary>
    /// 单个配置项绑定条目接口
    /// Author: SaintCirno9
    /// </summary>
    public interface IConfigBindingEntry<TData>
    {
        string Name { get; }
        void LoadFrom(TData data);
        void SaveTo(TData data);
        void Reset();
        void RegisterAutoSave(Action onDirty);
    }

    /// <summary>
    /// 强类型绑定的具体条目实现，通过启动时预编译委托实现零反射、零装箱、运行时直连
    /// Author: SaintCirno9
    /// </summary>
    internal class BoundProperty<TData, TValue> : IConfigBindingEntry<TData>
    {
        private readonly string _name;
        private readonly Func<TData, TValue> _getter;
        private readonly Action<TData, TValue> _setter;
        private readonly GetSetReset<TValue> _target;
        private bool _autoSaveHooked = false;

        public BoundProperty(
            string name,
            Func<TData, TValue> getter,
            Action<TData, TValue> setter,
            GetSetReset<TValue> target)
        {
            _name = name;
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public string Name => _name;

        public void LoadFrom(TData data)
        {
            if (data != null && _target != null)
            {
                _target.val = _getter(data);
            }
        }

        public void SaveTo(TData data)
        {
            if (data != null && _target != null)
            {
                _setter(data, _target.val);
            }
        }

        public void Reset()
        {
            _target?.Reset();
        }

        public void RegisterAutoSave(Action onDirty)
        {
            if (_target != null && !_autoSaveHooked)
            {
                _autoSaveHooked = true;
                _target.OnValUpdate += _ => onDirty?.Invoke();
            }
        }
    }

    /// <summary>
    /// 轻量配置绑定器，提供配置数据类与游戏逻辑底层 GetSetReset 实例的双向自动绑定。
    /// 解决配置项在 Data、Load、OnValUpdate、GetSaveData、SetDefault 散弹式多处维护的核心痛点。
    /// Author: SaintCirno9
    /// </summary>
    public class ConfigBinder<TData> where TData : class
    {
        private readonly List<IConfigBindingEntry<TData>> _entries = new List<IConfigBindingEntry<TData>>();
        private readonly HashSet<string> _boundNames = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyList<IConfigBindingEntry<TData>> Entries => _entries;

        /// <summary>
        /// 自动通过反射扫描 TData 上的 [ConfigBind] 特性建立绑定。
        /// 仅在初始化阶段调用一次并编译缓存委托，后续纯委托调用零运行时开销。
        /// </summary>
        public ConfigBinder<TData> AutoBindFromAttributes()
        {
            var fields = typeof(TData).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<ConfigBindAttribute>();
                if (attr == null) continue;

                BindFieldWithAttribute(field, attr);
            }
            return this;
        }

        /// <summary>
        /// 手动通过表达式绑定一个字段到指定的 GetSetReset 实例
        /// </summary>
        public ConfigBinder<TData> Bind<TValue>(
            Expression<Func<TData, TValue>> fieldSelector,
            GetSetReset<TValue> target)
        {
            if (fieldSelector == null) throw new ArgumentNullException(nameof(fieldSelector));
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (!(fieldSelector.Body is MemberExpression memberExpr) ||
                !(memberExpr.Member is FieldInfo fieldInfo))
            {
                throw new ArgumentException("表达式必须是字段访问，例如 d => d.SomeField", nameof(fieldSelector));
            }

            BindFieldCore(fieldInfo, target);
            return this;
        }

        /// <summary>
        /// 核心绑定逻辑，生成预编译强类型 getter 和 setter 委托
        /// </summary>
        private void BindFieldCore<TValue>(FieldInfo fieldInfo, GetSetReset<TValue> target)
        {
            if (!_boundNames.Add(fieldInfo.Name))
            {
                throw new InvalidOperationException($"字段 {fieldInfo.Name} 已经被绑定过，严禁重复绑定！");
            }

            // 编译 getter: data => data.Field
            var paramData = Expression.Parameter(typeof(TData), "data");
            var fieldAccess = Expression.Field(paramData, fieldInfo);
            var getter = Expression.Lambda<Func<TData, TValue>>(fieldAccess, paramData).Compile();

            // 编译 setter: (data, val) => { data.Field = val; }
            var paramVal = Expression.Parameter(typeof(TValue), "val");
            var assign = Expression.Assign(fieldAccess, paramVal);
            var body = Expression.Block(typeof(void), assign);
            var setter = Expression.Lambda<Action<TData, TValue>>(body, paramData, paramVal).Compile();

            _entries.Add(new BoundProperty<TData, TValue>(fieldInfo.Name, getter, setter, target));
        }

        private static readonly MethodInfo BindFieldCoreMethod = typeof(ConfigBinder<TData>)
            .GetMethod(nameof(BindFieldCore), BindingFlags.NonPublic | BindingFlags.Instance);

        private void BindFieldWithAttribute(FieldInfo field, ConfigBindAttribute attr)
        {
            string memberName = attr.MemberName;
            MemberInfo targetMember = null;
            var targetType = attr.TargetType;

            if (!string.IsNullOrEmpty(memberName))
            {
                targetMember = FindStaticMember(targetType, memberName);
            }
            else
            {
                // 优先尝试与字段同名
                targetMember = FindStaticMember(targetType, field.Name)
                            ?? FindStaticMember(targetType, "Enable")
                            ?? FindStaticMember(targetType, "Enabled");
            }

            if (targetMember == null)
            {
                throw new InvalidOperationException($"无法在类型 {targetType.FullName} 及其基类中找到匹配配置项 {field.Name} 的静态成员 (指定: {memberName ?? "<自动推断>"})");
            }

            object targetInstance = (targetMember is FieldInfo f) ? f.GetValue(null) : ((PropertyInfo)targetMember).GetValue(null);
            if (targetInstance == null)
            {
                throw new InvalidOperationException($"目标成员 {targetType.FullName}.{targetMember.Name} 实例为 null，无法完成绑定！");
            }

            var expectedType = typeof(GetSetReset<>).MakeGenericType(field.FieldType);
            if (!expectedType.IsInstanceOfType(targetInstance))
            {
                throw new InvalidOperationException($"目标成员 {targetType.FullName}.{targetMember.Name} 的类型为 {targetInstance.GetType().FullName}，与数据字段 {field.Name} 的类型 GetSetReset<{field.FieldType.Name}> 不匹配！");
            }

            // 通过泛型方法注入
            var genericMethod = BindFieldCoreMethod.MakeGenericMethod(field.FieldType);
            genericMethod.Invoke(this, new object[] { field, targetInstance });
        }

        /// <summary>
        /// 沿继承链自底向上查找静态成员（包含 internal / private / protected）
        /// </summary>
        private static MemberInfo FindStaticMember(Type type, string memberName)
        {
            var current = type;
            while (current != null && current != typeof(object))
            {
                var member = (MemberInfo)current.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
                          ?? current.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                if (member != null) return member;
                current = current.BaseType;
            }
            return null;
        }

        /// <summary>
        /// 批量将 Data 的值加载赋给所有绑定的 GetSetReset 实例
        /// </summary>
        public void BindAll(TData data)
        {
            if (data == null) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].LoadFrom(data);
            }
        }

        /// <summary>
        /// 批量从各个 GetSetReset 实例读取当前值，写回 Data 实例
        /// </summary>
        public void ExportToData(TData data)
        {
            if (data == null) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].SaveTo(data);
            }
        }

        /// <summary>
        /// 批量调用所有绑定的 GetSetReset 实例的 Reset 方法重置为默认值
        /// </summary>
        public void ResetDefaults()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].Reset();
            }
        }

        /// <summary>
        /// 批量为所有绑定的 GetSetReset 挂接自动保存事件（内置防重复注册机制）
        /// </summary>
        public void RegisterAutoSave(Action onDirty)
        {
            if (onDirty == null) return;
            for (int i = 0; i < _entries.Count; i++)
            {
                _entries[i].RegisterAutoSave(onDirty);
            }
        }
    }
}
