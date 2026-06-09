using Awen.Core;
using TMPro;
using UnityEngine;

namespace Awen.UI {
  [RequireComponent(typeof(TMP_Text))]
  public class LocalizedTMP : MonoBehaviour {
    [SerializeField]
    private string _key;

    private TMP_Text _text;

    private void Awake() => _text = GetComponent<TMP_Text>();

    private void OnEnable() {
      EventBus.Subscribe<LanguageChangedData>(GameEventType.LanguageChanged, OnLanguageChanged);
      Refresh();
    }

    private void OnDisable() {
      EventBus.Unsubscribe<LanguageChangedData>(GameEventType.LanguageChanged, OnLanguageChanged);
    }

    private void OnLanguageChanged(LanguageChangedData _) => Refresh();

    public void Refresh() => _text.text = LocalizationManager.Get(_key);
  }
}
