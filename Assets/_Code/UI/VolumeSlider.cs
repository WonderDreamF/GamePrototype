using Awen.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Awen.UI {
  /// <summary>
  /// 绑定到一个 Slider，控制指定 <see cref="AudioChannel"/> 的音量。
  /// 拖动实时调用 <see cref="AudioManager.SetVolume"/>；面板打开(OnEnable)时用
  /// <see cref="AudioManager.GetVolume"/> 回填当前值。可选地显示百分比。
  /// </summary>
  [RequireComponent(typeof(Slider))]
  public class VolumeSlider : MonoBehaviour {
    [SerializeField]
    [FormerlySerializedAs("channel")]
    private AudioChannel _channel;
    [SerializeField]
    [FormerlySerializedAs("valueLabel")]
    private TextMeshProUGUI _valueLabel;

    private Slider _slider;

    private void Awake() {
      _slider = GetComponent<Slider>();
      _slider.minValue = 0f;
      _slider.maxValue = 1f;
      _slider.onValueChanged.AddListener(OnChanged);
    }

    private void OnEnable() {
      if (!AudioManager.HasInstance) {
        return;
      }
      float v = AudioManager.Instance.GetVolume(_channel);
      _slider.SetValueWithoutNotify(v);
      UpdateLabel(v);
    }

    private void OnChanged(float v) {
      if (AudioManager.HasInstance) {
        AudioManager.Instance.SetVolume(_channel, v);
      }
      UpdateLabel(v);
    }

    private void UpdateLabel(float v) {
      if (_valueLabel != null) {
        _valueLabel.text = Mathf.RoundToInt(v * 100f) + "%";
      }
    }
  }
}
