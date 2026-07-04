using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GamePrototype.Core {
  /// <summary>
  /// 全局对象池管理器。统一管理所有 <see cref="Pool"/>，并维护层级结构：
  /// <code>
  /// Pool (根)
  /// ├── Goblin      ← 每个预制体一个类别容器
  /// │   ├── Goblin
  /// │   └── Goblin
  /// └── Explosion
  ///     └── Explosion
  /// </code>
  /// 把它放在常驻场景里即可（继承自 <see cref="MonoSingleton{T}"/>，不使用 DontDestroyOnLoad）。
  /// </summary>
  public sealed class PoolManager : MonoSingleton<PoolManager> {
    private const string _rootName = "Pool";

    // 预制体 -> 对应的池。
    private readonly Dictionary<GameObject, Pool> _pools = new();
    private Transform _root;

    protected override void OnAwake() {
      var rootGo = new GameObject(_rootName);
      rootGo.transform.SetParent(transform, false);
      _root = rootGo.transform;
    }

    // ── 池的获取 / 预热 ────────────────────────────────────────

    /// <summary>获取（或创建）指定预制体的对象池。</summary>
    /// <param name="prefab">预制体。</param>
    /// <param name="prewarm">仅在首次创建时生效：预热数量。</param>
    /// <param name="maxSize">仅在首次创建时生效：缓存上限，0 表示不限。</param>
    public Pool GetOrCreatePool(GameObject prefab, int prewarm = 0, int maxSize = 0) {
      if (prefab == null) {
        Debug.LogError("[PoolManager] prefab 为 null。");
        return null;
      }

      if (!_pools.TryGetValue(prefab, out var pool)) {
        pool = new Pool(prefab, _root, prewarm, maxSize);
        _pools[prefab] = pool;
      }
      return pool;
    }

    /// <summary>预热：提前实例化若干个实例放入池中，避免运行时实例化卡顿。</summary>
    public void Prewarm(GameObject prefab, int count, int maxSize = 0) =>
        GetOrCreatePool(prefab, 0, maxSize)?.Prewarm(count);

    // ── 取出 ───────────────────────────────────────────────────

    /// <summary>取出一个实例（位置/旋转默认为零值，挂在该类别容器下）。</summary>
    public GameObject Get(GameObject prefab) =>
        GetOrCreatePool(prefab)?.Get(Vector3.zero, Quaternion.identity);

    /// <summary>在指定位置/旋转处取出一个实例。</summary>
    public GameObject Get(
        GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null) =>
        GetOrCreatePool(prefab)?.Get(position, rotation, parent);

    /// <summary>取出并返回实例上的指定组件。</summary>
    public T Get<T>(GameObject prefab) where T : Component {
      var go = Get(prefab);
      return go != null ? go.GetComponent<T>() : null;
    }

    /// <summary>在指定位置/旋转处取出并返回实例上的指定组件。</summary>
    public T Get<T>(GameObject prefab, Vector3 position, Quaternion rotation,
        Transform parent = null) where T : Component {
      var go = Get(prefab, position, rotation, parent);
      return go != null ? go.GetComponent<T>() : null;
    }

    // ── 归还 ───────────────────────────────────────────────────

    /// <summary>归还一个实例到它所属的池。若不是池化对象则直接销毁。</summary>
    public void Release(GameObject instance) {
      if (instance == null) {
        return;
      }

      var po = instance.GetComponent<PooledObject>();
      if (po == null || po.Pool == null) {
        Debug.LogWarning($"[PoolManager] '{instance.name}' 非池化对象，已直接销毁。", instance);
        Destroy(instance);
        return;
      }
      po.Pool.Release(instance);
    }

    /// <summary>延迟 <paramref name="delay"/> 秒后归还实例（常用于特效自动回收）。</summary>
    public void Release(GameObject instance, float delay) {
      if (instance == null) {
        return;
      }
      if (delay <= 0f) {
        Release(instance);
        return;
      }
      ReleaseAfterAsync(instance, delay).Forget();
    }

    private async UniTaskVoid ReleaseAfterAsync(GameObject instance, float delay) {
      // 绑定到管理器生命周期：场景卸载/管理器销毁时自动取消，避免操作已销毁对象。
      await UniTask.Delay(
          TimeSpan.FromSeconds(delay), cancellationToken: this.GetCancellationTokenOnDestroy());
      if (instance != null) {
        Release(instance);
      }
    }

    // ── 清理 ───────────────────────────────────────────────────

    /// <summary>清理指定预制体池中的未激活实例。</summary>
    public void ClearInactive(GameObject prefab) {
      if (prefab != null && _pools.TryGetValue(prefab, out var pool)) {
        pool.ClearInactive();
      }
    }

    /// <summary>清理所有池的未激活实例。</summary>
    public void ClearAllInactive() {
      foreach (var pool in _pools.Values) {
        pool.ClearInactive();
      }
    }
  }
}
