namespace GamePrototype.Core {
  public struct SceneLoadedData {
    public string SceneName;
  }

  public struct SceneUnloadedData {
    public string SceneName;
  }

  public struct LanguageChangedData {
    public cfg.Localization.Language Language;
  }

  // Slot = -1 表示自动存档
  public struct SaveEventData {
    public int Slot;
  }

  public struct WorldFlagChangedData {
    public string Key;
    public bool Value;
  }
}
