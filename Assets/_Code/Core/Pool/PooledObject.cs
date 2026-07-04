using UnityEngine;

namespace GamePrototype.Core {
  /// <summary>
  /// 由对象池自动挂到每个池化实例上的标记组件。
  /// 持有一个指向所属 <see cref="Pool"/> 的回引，使归还可以 O(1) 完成，
  /// 并负责把生命周期回调转发给实例上所有的 <see cref="IPoolable"/>。
  /// 不要手动添加 —— 由 <see cref="Pool"/> 在实例化时负责挂载。
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class PooledObject : MonoBehaviour {
    private IPoolable[] _poolables;
    private bool _cached;

    /// <summary>所属对象池。由 <see cref="Pool"/> 在创建时写入。</summary>
    public Pool Pool { get; internal set; }

    /// <summary>当前是否处于激活（已取出）状态。用于防止重复归还。</summary>
    public bool IsActive { get; internal set; }

    /// <summary>便捷方法：把自己归还到所属对象池。</summary>
    public void Release() {
      if (Pool != null) {
        Pool.Release(gameObject);
      }
    }

    internal void InvokeSpawn() {
      CacheIfNeeded();
      for (int i = 0; i < _poolables.Length; i++) {
        _poolables[i].OnSpawn();
      }
    }

    internal void InvokeDespawn() {
      CacheIfNeeded();
      for (int i = 0; i < _poolables.Length; i++) {
        _poolables[i].OnDespawn();
      }
    }

    private void CacheIfNeeded() {
      if (_cached) {
        return;
      }
      _poolables = GetComponentsInChildren<IPoolable>(true);
      _cached = true;
    }
  }
}
