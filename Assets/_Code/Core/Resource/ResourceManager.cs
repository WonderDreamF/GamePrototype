using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace Awen.Core {
  /// <summary>
  /// 资源管理器：项目所有资源（预制体、Sprite、AudioClip、ScriptableObject…）统一通过
  /// YooAsset 按「地址（location）」加载，<b>不再往 Inspector 拖拽引用</b>。
  ///
  /// 缓存策略：每个地址加载一次后,句柄常驻 <see cref="_assetCache"/>,再次请求直接返回缓存,
  /// 不重复加载、不被自动卸载 —— 即「读取到一遍就保存在脚本中」。需要主动释放时调用
  /// <see cref="Release"/> / <see cref="ReleaseAll"/>。
  ///
  /// 运行模式：编辑器下 EditorSimulateMode（免打包）,打包后 OfflinePlayMode（资源随包内置）。
  /// 异步统一用 UniTask。放在常驻场景里（继承 <see cref="MonoSingleton{T}"/>,不使用 DontDestroyOnLoad）。
  /// </summary>
  public sealed class ResourceManager : MonoSingleton<ResourceManager> {
    /// <summary>资源包名称。打包时 YooAsset 的包名需与此一致。</summary>
    public const string PackageName = "DefaultPackage";

    // 地址 -> 已加载并常驻的资源句柄。
    private readonly Dictionary<string, AssetHandle> _assetCache = new();

    private ResourcePackage _package;
    private bool _initialized;
    private bool _initStarted;
    private UniTask _initTask;

    /// <summary>是否已完成初始化。</summary>
    public bool Initialized => _initialized;

    // ── 初始化 ─────────────────────────────────────────────────

    /// <summary>
    /// 初始化 YooAsset。可重复 await：内部只真正执行一次（用 Preserve 支持多次等待）。
    /// 建议在 Boot 场景启动时 await 一次。
    /// </summary>
    public UniTask InitializeAsync() {
      if (_initialized) {
        return UniTask.CompletedTask;
      }
      if (!_initStarted) {
        _initStarted = true;
        _initTask = InitializeInternalAsync().Preserve();
      }
      return _initTask;
    }

    private async UniTask InitializeInternalAsync() {
      YooAssets.Initialize();
      _package = YooAssets.CreatePackage(PackageName);
      YooAssets.SetDefaultPackage(_package);

      InitializeParameters initParams;
#if UNITY_EDITOR
      // 编辑器模拟模式：无需打包，直接读取工程内资源。
      var simulateResult = EditorSimulateModeHelper.SimulateBuild(PackageName);
      var editorFileSystem =
          FileSystemParameters.CreateDefaultEditorFileSystemParameters(
              simulateResult.PackageRootDirectory);
      initParams = new EditorSimulateModeParameters { EditorFileSystemParameters = editorFileSystem };
#else
      // 单机离线模式：读取随包内置（StreamingAssets）的资源。
      var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
      initParams = new OfflinePlayModeParameters { BuildinFileSystemParameters = buildinFileSystem };
#endif

      var initOp = _package.InitializeAsync(initParams);
      await initOp.Task;
      if (initOp.Status != EOperationStatus.Succeed) {
        Debug.LogError($"[ResourceManager] 初始化失败: {initOp.Error}");
        return;
      }

      var versionOp = _package.RequestPackageVersionAsync();
      await versionOp.Task;
      if (versionOp.Status != EOperationStatus.Succeed) {
        Debug.LogError($"[ResourceManager] 请求资源版本失败: {versionOp.Error}");
        return;
      }

      var manifestOp = _package.UpdatePackageManifestAsync(versionOp.PackageVersion);
      await manifestOp.Task;
      if (manifestOp.Status != EOperationStatus.Succeed) {
        Debug.LogError($"[ResourceManager] 更新资源清单失败: {manifestOp.Error}");
        return;
      }

      _initialized = true;
      Debug.Log($"[ResourceManager] 初始化完成 (version: {versionOp.PackageVersion}).");
    }

    // ── 资源加载（加载一次即缓存，调用方无需释放） ──────────────

    /// <summary>按地址异步加载资源。已缓存则直接返回。</summary>
    public async UniTask<T> LoadAsync<T>(string location) where T : Object {
      if (string.IsNullOrEmpty(location)) {
        return null;
      }
      await InitializeAsync();
      if (!_initialized) {
        return null;
      }

      if (_assetCache.TryGetValue(location, out var cached)) {
        return cached.AssetObject as T;
      }

      var handle = _package.LoadAssetAsync<T>(location);
      await handle.Task;
      if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null) {
        Debug.LogError($"[ResourceManager] 加载失败: '{location}' ({handle.LastError})");
        handle.Release();
        return null;
      }

      _assetCache[location] = handle;  // 句柄常驻，资源不会被卸载。
      return handle.AssetObject as T;
    }

    /// <summary>按地址同步加载资源（会阻塞，慎用于大资源）。已缓存则直接返回。</summary>
    public T Load<T>(string location) where T : Object {
      if (string.IsNullOrEmpty(location)) {
        return null;
      }
      if (!_initialized) {
        Debug.LogError(
            $"[ResourceManager] 尚未初始化，无法同步加载 '{location}'。请先 await InitializeAsync()。");
        return null;
      }

      if (_assetCache.TryGetValue(location, out var cached)) {
        return cached.AssetObject as T;
      }

      var handle = _package.LoadAssetSync<T>(location);
      if (handle.Status != EOperationStatus.Succeed || handle.AssetObject == null) {
        Debug.LogError($"[ResourceManager] 同步加载失败: '{location}' ({handle.LastError})");
        handle.Release();
        return null;
      }

      _assetCache[location] = handle;
      return handle.AssetObject as T;
    }

    // ── 与对象池联动 ───────────────────────────────────────────

    /// <summary>
    /// 按地址加载预制体（缓存），再通过 <see cref="PoolManager"/> 取出一个实例。
    /// 频繁生成的对象推荐用这个：资源由本管理器缓存,实例由对象池复用。
    /// </summary>
    public async UniTask<GameObject> InstantiateAsync(
        string location, Vector3 position, Quaternion rotation, Transform parent = null) {
      var prefab = await LoadAsync<GameObject>(location);
      if (prefab == null) {
        return null;
      }
      return PoolManager.Instance.Get(prefab, position, rotation, parent);
    }

    /// <summary>按地址加载预制体并取出实例，返回其上的指定组件。</summary>
    public async UniTask<T> InstantiateAsync<T>(
        string location, Vector3 position, Quaternion rotation, Transform parent = null)
        where T : Component {
      var go = await InstantiateAsync(location, position, rotation, parent);
      return go != null ? go.GetComponent<T>() : null;
    }

    // ── 释放 ───────────────────────────────────────────────────

    /// <summary>主动释放某个缓存资源（仅当确定不再使用时）。</summary>
    public void Release(string location) {
      if (_assetCache.TryGetValue(location, out var handle)) {
        handle.Release();
        _assetCache.Remove(location);
      }
    }

    /// <summary>释放全部缓存资源，并卸载无用资源包。</summary>
    public void ReleaseAll() {
      foreach (var handle in _assetCache.Values) {
        handle.Release();
      }
      _assetCache.Clear();
      _package?.UnloadUnusedAssetsAsync();
    }

    protected override void OnDestroyed() {
      ReleaseAll();
      YooAssets.Destroy();
    }
  }
}
