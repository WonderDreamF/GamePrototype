using UnityEngine;

namespace Awen.Core {
  /// <summary>
  /// MonoBehaviour 单例基类。<b>不使用</b> DontDestroyOnLoad —— 约定把这些管理器
  /// 放在一个不会被卸载的常驻场景里，由场景本身保证其生命周期。
  ///
  /// 派生类请重写 <see cref="OnAwake"/>（而不是自己写 Awake），以免覆盖单例初始化逻辑：
  /// <code>
  /// public class FooManager : MonoSingleton&lt;FooManager&gt; {
  ///   protected override void OnAwake() { /* 初始化 */ }
  /// }
  /// </code>
  /// </summary>
  public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T> {
    private static T _instance;

    /// <summary>
    /// 全局单例。常驻场景加载时由 <c>Awake</c> 注册，访问时直接返回缓存字段，<b>零查找开销</b>。
    /// 仅当访问早于 Awake 注册（脚本执行顺序问题）才做一次性兜底查找并告警 —— 正常运行不会触发。
    /// </summary>
    public static T Instance {
      get {
        if (_instance == null) {
          _instance = FindAnyObjectByType<T>();
          if (_instance != null) {
            Debug.LogWarning($"[{typeof(T).Name}] 在 Awake 注册前被访问，已做一次性兜底查找。" +
                "建议调整脚本执行顺序，或确保在常驻场景初始化后再访问。");
          } else {
            Debug.LogError($"[{typeof(T).Name}] 实例不存在。请确认它已放入常驻场景。");
          }
        }
        return _instance;
      }
    }

    /// <summary>是否已存在实例（不触发查找）。</summary>
    public static bool HasInstance => _instance != null;

    /// <summary>派生类的初始化入口，等价于 Awake（单例已就绪后调用）。</summary>
    protected virtual void OnAwake() { }

    /// <summary>派生类的销毁入口，等价于 OnDestroy。</summary>
    protected virtual void OnDestroyed() { }

    private void Awake() {
      if (_instance != null && _instance != this) {
        Debug.LogWarning($"[{typeof(T).Name}] 场景中存在多个实例，销毁多余的 '{name}'。", this);
        Destroy(gameObject);
        return;
      }
      _instance = (T)this;
      OnAwake();
    }

    private void OnDestroy() {
      if (_instance == this) {
        _instance = null;
      }
      OnDestroyed();
    }
  }
}
