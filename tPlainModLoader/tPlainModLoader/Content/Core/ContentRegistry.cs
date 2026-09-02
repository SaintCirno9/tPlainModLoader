using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TPML.Content.Core
{
    /// <summary>
    /// TPML 泛型内容注册表，统一管理内容 ID 映射、名称索引与本地化显示名称
    /// 作者: SaintCirno9
    /// </summary>
    public class ContentRegistry<T> where T : ModType
    {
        private readonly ConcurrentDictionary<int, T> _byType;
        private readonly ConcurrentDictionary<string, int> _byName;
        private readonly ConcurrentDictionary<int, string> _displayNames;
        private int _nextId;

        public int Offset { get; }
        public int NextId => _nextId;
        public int Count => _byType.Count;
        public ICollection<T> Values => _byType.Values;
        public IEnumerable<KeyValuePair<int, T>> Entries => _byType;

        public ContentRegistry(int offset)
        {
            Offset = offset;
            _nextId = offset;
            _byType = new ConcurrentDictionary<int, T>();
            _byName = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _displayNames = new ConcurrentDictionary<int, string>();
        }

        public int ReserveNextId() => Interlocked.Increment(ref _nextId) - 1;

        public void ResetNextId() => _nextId = Offset;

        public void Register(T item, int type)
        {
            if (item == null) return;
            _byType[type] = item;
            if (!string.IsNullOrEmpty(item.FullName))
            {
                _byName[item.FullName] = type;
            }
            if (!string.IsNullOrEmpty(item.Name))
            {
                _byName[item.Name] = type;
            }
        }

        public T Get(int type)
        {
            return _byType.TryGetValue(type, out var item) ? item : null;
        }

        public bool TryGet(int type, out T item) => _byType.TryGetValue(type, out item);

        public int GetType(string modName, string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            if (!string.IsNullOrEmpty(modName) && _byName.TryGetValue($"{modName}/{name}", out int type))
            {
                return type;
            }
            if (_byName.TryGetValue(name, out int fallbackType))
            {
                return fallbackType;
            }
            return 0;
        }

        public int GetType(string fullName)
        {
            if (string.IsNullOrEmpty(fullName)) return 0;
            if (_byName.TryGetValue(fullName, out int type)) return type;
            int idx = fullName.IndexOf('/');
            if (idx >= 0 && idx < fullName.Length - 1)
            {
                string shortName = fullName.Substring(idx + 1);
                if (_byName.TryGetValue(shortName, out int shortType)) return shortType;
            }
            return 0;
        }

        public string GetDisplayName(int type)
        {
            return _displayNames.TryGetValue(type, out var name) ? name : string.Empty;
        }

        public void SetDisplayName(int type, string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                _displayNames[type] = name;
            }
        }

        public void Clear()
        {
            _byType.Clear();
            _byName.Clear();
            _displayNames.Clear();
            _nextId = Offset;
        }
    }
}
