using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GamePrototype.Core {
  /// <summary>
  /// 游戏启动流程。挂在 Boot 场景里 —— Boot 是常驻场景（持有各管理器、相机、EventSystem），
  /// 后续场景以 Additive 方式加载在其之上，Boot 不卸载。
  ///
  /// 流程：加载配置表 → 初始化 YooAsset → 附加加载主菜单。
  /// </summary>
  public class GameBootstrap : MonoBehaviour {
    private const string _mainMenuScene = "MainMenu";

    private async UniTaskVoid Start() {
      try {
        // 1) 配置表（Luban） + 本地化
        ConfigManager.Load();

        // 2) 资源系统（YooAsset：编辑器 EditorSimulate / 打包 Offline）
        await ResourceManager.Instance.InitializeAsync();
        if (!ResourceManager.Instance.Initialized) {
          Debug.LogError("[Bootstrap] 资源系统初始化失败，启动中止。");
          return;
        }

        // 3) 进入主菜单（Additive，Boot 作为常驻场景保留）
        await SceneManager.LoadSceneAsync(_mainMenuScene, LoadSceneMode.Additive).ToUniTask();

        Debug.Log("[Bootstrap] 启动完成，已进入主菜单。");
      } catch (Exception e) {
        Debug.LogError($"[Bootstrap] 启动异常：{e}");
      }
    }
  }
}
