namespace GamePrototype.Core {
  /// <summary>
  /// 可池化对象的生命周期回调。挂在预制体（或其子物体）上的组件实现此接口后，
  /// 会在从对象池取出 / 归还时自动收到通知，用来重置状态、播放/停止特效等。
  /// </summary>
  public interface IPoolable {
    /// <summary>从池中取出、激活之后调用。用于重置状态。</summary>
    void OnSpawn();

    /// <summary>归还到池中、停用之前调用。用于清理状态。</summary>
    void OnDespawn();
  }
}
