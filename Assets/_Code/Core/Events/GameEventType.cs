namespace Awen.Core {
  /// <summary>
  /// 全局事件类型。命名为 GameEventType 而非 EventType，以避免与 UnityEngine.EventType 冲突。
  /// </summary>
  public enum GameEventType {
    // Scene
    SceneLoaded,
    SceneUnloaded,

    // Localization
    LanguageChanged,

    // Save System
    SaveCompleted,     // 存档写入完成
    SaveLoaded,        // 存档读取完成
    WorldFlagChanged,  // WorldFlag 值发生变化
  }
}
