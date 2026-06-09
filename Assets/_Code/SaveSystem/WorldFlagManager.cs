using System;
using System.Collections.Generic;

namespace Awen.SaveSystem {
  /// <summary>
  /// 运行时 Flag 访问层。读写都经过这里，Save 时由 SaveManager 统一序列化。
  /// </summary>
  public static class WorldFlagManager {
    private static readonly Dictionary<string, bool> _flags = new();

    /// <summary>Flag 变化时触发。参数：(key, newValue)</summary>
    public static event Action<string, bool> OnFlagChanged;

    public static void Initialize(WorldState state) {
      _flags.Clear();
      foreach (var flag in state.Flags) {
        _flags[flag.Key] = flag.Value;
      }
    }

    public static void Set(string key, bool value) {
      _flags[key] = value;
      OnFlagChanged?.Invoke(key, value);
    }

    public static bool Get(string key) => _flags.TryGetValue(key, out var v) && v;

    public static void Toggle(string key) => Set(key, !Get(key));

    public static List<WorldFlag> Serialize() {
      var list = new List<WorldFlag>(_flags.Count);
      foreach (var kv in _flags) {
        list.Add(new WorldFlag { Key = kv.Key, Value = kv.Value });
      }
      return list;
    }
  }
}
