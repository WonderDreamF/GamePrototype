using cfg.Localization;
using UnityEngine;

namespace GamePrototype.Core {
  public static class LocalizationManager {
    private const string _prefsKey = "Language";

    private static Language _currentLanguage;
    private static TbLocalization _tb;

    public static Language CurrentLanguage => _currentLanguage;

    public static void Initialize(TbLocalization tb) {
      _tb = tb;
      _currentLanguage = (Language)PlayerPrefs.GetInt(_prefsKey, (int)Language.ZhCN);
    }

    public static void SetLanguage(Language language) {
      if (_currentLanguage == language) {
        return;
      }
      _currentLanguage = language;
      PlayerPrefs.SetInt(_prefsKey, (int)language);
      EventBus.Emit(GameEventType.LanguageChanged, new LanguageChangedData { Language = language });
    }

    public static string Get(string key) {
      if (_tb == null) {
        Debug.LogError("[Localization] Not initialized.");
        return $"[{key}]";
      }

      var data = _tb.GetOrDefault(key);
      if (data == null) {
        return $"[{key}]";
      }

      return _currentLanguage switch {
        Language.ZhCN => data.ZhCN,
        Language.EnUS => data.EnUS,
        Language.JaJP => data.JaJP,
        _ => $"[{key}]",
      };
    }

    public static string Get(string key, params object[] args) {
      var text = Get(key);
      if (args != null && args.Length > 0) {
        text = string.Format(text, args);
      }
      return text;
    }
  }
}
