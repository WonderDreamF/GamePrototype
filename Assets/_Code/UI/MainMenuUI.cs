using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GamePrototype.UI {
  /// <summary>
  /// 主菜单逻辑。按钮引用通过 Inspector 绑定（同场景内组件引用，零运行时查找）。
  /// </summary>
  public class MainMenuUI : MonoBehaviour {
    [SerializeField]
    [FormerlySerializedAs("startButton")]
    private Button _startButton;
    [SerializeField]
    [FormerlySerializedAs("quitButton")]
    private Button _quitButton;
    [SerializeField]
    [FormerlySerializedAs("settingsButton")]
    private Button _settingsButton;
    [SerializeField]
    [FormerlySerializedAs("settingsCloseButton")]
    private Button _settingsCloseButton;
    [SerializeField]
    [FormerlySerializedAs("settingsPanel")]
    private GameObject _settingsPanel;

    private void Awake() {
      if (_startButton != null) {
        _startButton.onClick.AddListener(OnStartClicked);
      }
      if (_quitButton != null) {
        _quitButton.onClick.AddListener(OnQuitClicked);
      }
      if (_settingsButton != null) {
        _settingsButton.onClick.AddListener(OpenSettings);
      }
      if (_settingsCloseButton != null) {
        _settingsCloseButton.onClick.AddListener(CloseSettings);
      }
      if (_settingsPanel != null) {
        _settingsPanel.SetActive(false);
      }
    }

    private void OpenSettings() {
      if (_settingsPanel != null) {
        _settingsPanel.SetActive(true);
      }
    }

    private void CloseSettings() {
      if (_settingsPanel != null) {
        _settingsPanel.SetActive(false);
      }
    }

    private void OnStartClicked() {
      // TODO: 加载第一章场景（GameRes/Scenes/Chapters/... 经 YooAsset 的 LoadSceneAsync）
      Debug.Log("[MainMenu] 开始游戏（章节场景待接入）。");
    }

    private void OnQuitClicked() {
      Debug.Log("[MainMenu] 退出游戏。");
#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
    }
  }
}
