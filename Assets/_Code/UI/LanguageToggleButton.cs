using GamePrototype.Core;
using cfg.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GamePrototype.UI {
  /// <summary>
  /// 语言切换按钮：点击在「中文 / English」之间切换（调用 <see cref="LocalizationManager.SetLanguage"/>）。
  /// 按钮文案显示当前语言，并随语言变化（含外部触发）自动刷新。
  /// 切换后所有 <see cref="LocalizedTMP"/> 会通过 LanguageChanged 事件自动更新。
  /// </summary>
  [RequireComponent(typeof(Button))]
  public class LanguageToggleButton : MonoBehaviour {
    [SerializeField]
    [FormerlySerializedAs("label")]
    private TextMeshProUGUI _label;

    private Button _button;

    private void Awake() {
      _button = GetComponent<Button>();
      _button.onClick.AddListener(Toggle);
    }

    private void OnEnable() {
      EventBus.Subscribe<LanguageChangedData>(GameEventType.LanguageChanged, OnLanguageChanged);
      RefreshLabel();
    }

    private void OnDisable() {
      EventBus.Unsubscribe<LanguageChangedData>(GameEventType.LanguageChanged, OnLanguageChanged);
    }

    private void Toggle() {
      var next = LocalizationManager.CurrentLanguage == Language.ZhCN
          ? Language.EnUS
          : Language.ZhCN;
      LocalizationManager.SetLanguage(next);
    }

    private void OnLanguageChanged(LanguageChangedData _) => RefreshLabel();

    private void RefreshLabel() {
      if (_label != null) {
        _label.text = LocalizationManager.CurrentLanguage == Language.ZhCN ? "中文" : "English";
      }
    }
  }
}
