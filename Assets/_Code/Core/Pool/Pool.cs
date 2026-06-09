using System.Collections.Generic;
using UnityEngine;

namespace Awen.Core {
  /// <summary>
  /// 单个预制体对应的对象池（即层级里的一个「类别」容器）。
  /// 维护一个非激活实例栈和一个激活实例集合，所有实例都挂在自己的容器 Transform 下，
  /// 容器又挂在 <see cref="PoolManager"/> 的「Pool」根节点下。
  /// 一般通过 <see cref="PoolManager"/> 间接使用，无需直接 new。
  /// </summary>
  public sealed class Pool {
    private readonly Stack<GameObject> _inactive = new();
    private readonly HashSet<GameObject> _active = new();
    private readonly int _maxSize;

    /// <param name="prefab">预制体。</param>
    /// <param name="root">Pool 根节点，类别容器会挂在它下面。</param>
    /// <param name="prewarm">预热数量：构造时预先实例化多少个。</param>
    /// <param name="maxSize">缓存上限（未激活实例数）。超出后归还的实例会被销毁。0 表示不限。</param>
    public Pool(GameObject prefab, Transform root, int prewarm = 0, int maxSize = 0) {
      Prefab = prefab;
      _maxSize = maxSize <= 0 ? int.MaxValue : maxSize;

      var containerGo = new GameObject(prefab.name);
      Container = containerGo.transform;
      Container.SetParent(root, false);

      if (prewarm > 0) {
        Prewarm(prewarm);
      }
    }

    /// <summary>该池对应的预制体。</summary>
    public GameObject Prefab { get; }

    /// <summary>该类别的容器节点（层级里 Pool 根下的子物体）。</summary>
    public Transform Container { get; }

    /// <summary>池中可复用（未激活）的实例数量。</summary>
    public int CountInactive => _inactive.Count;

    /// <summary>当前已取出（激活）的实例数量。</summary>
    public int CountActive => _active.Count;

    /// <summary>该池创建过的实例总数（激活 + 未激活）。</summary>
    public int CountAll => CountInactive + CountActive;

    /// <summary>预先实例化 <paramref name="count"/> 个实例并放入池中备用。</summary>
    public void Prewarm(int count) {
      for (int i = 0; i < count; i++) {
        var go = CreateInstance();
        go.SetActive(false);
        _inactive.Push(go);
      }
    }

    /// <summary>从池中取出一个实例并激活。</summary>
    /// <param name="position">世界坐标。</param>
    /// <param name="rotation">世界旋转。</param>
    /// <param name="parent">父节点。为 null 时挂回该类别容器，保持层级整洁。</param>
    public GameObject Get(Vector3 position, Quaternion rotation, Transform parent = null) {
      GameObject go = null;

      // 弹出栈顶，跳过外部销毁导致的空引用。
      while (_inactive.Count > 0) {
        go = _inactive.Pop();
        if (go != null) {
          break;
        }
        go = null;
      }

      if (go == null) {
        go = CreateInstance();
      }

      var t = go.transform;
      t.SetParent(parent != null ? parent : Container, false);
      t.SetPositionAndRotation(position, rotation);
      go.SetActive(true);

      _active.Add(go);

      var po = go.GetComponent<PooledObject>();
      po.IsActive = true;
      po.InvokeSpawn();

      return go;
    }

    /// <summary>把实例归还到池中。</summary>
    public void Release(GameObject go) {
      if (go == null) {
        return;
      }

      var po = go.GetComponent<PooledObject>();
      if (po == null || po.Pool != this) {
        Debug.LogWarning($"[Pool] '{go.name}' 不属于该池，已直接销毁。", go);
        Object.Destroy(go);
        return;
      }

      if (!po.IsActive) {
        return;  // 已经归还过，忽略重复调用。
      }

      _active.Remove(go);
      po.IsActive = false;
      po.InvokeDespawn();

      if (_inactive.Count >= _maxSize) {
        Object.Destroy(go);
        return;
      }

      go.SetActive(false);
      go.transform.SetParent(Container, false);
      _inactive.Push(go);
    }

    /// <summary>销毁池中所有未激活实例（激活中的不动）。</summary>
    public void ClearInactive() {
      while (_inactive.Count > 0) {
        var go = _inactive.Pop();
        if (go != null) {
          Object.Destroy(go);
        }
      }
    }

    private GameObject CreateInstance() {
      var go = Object.Instantiate(Prefab, Container);
      go.name = Prefab.name;  // 去掉 "(Clone)" 后缀，层级更干净。

      var po = go.GetComponent<PooledObject>();
      if (po == null) {
        po = go.AddComponent<PooledObject>();
      }
      po.Pool = this;
      po.IsActive = false;

      return go;
    }
  }
}
