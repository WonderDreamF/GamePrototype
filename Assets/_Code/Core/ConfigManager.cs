using System.IO;
using cfg;
using Luban.SimpleJSON;
using UnityEngine;

namespace GamePrototype.Core {
  /// <summary>
  /// Luban 配置表入口。从 StreamingAssets/Tables 读取 json 构建 <see cref="Tables"/>，
  /// 并初始化 <see cref="LocalizationManager"/>。在 Boot 启动流程中调用一次。
  ///
  /// 注意：用同步 File 读取，适用于 PC/编辑器（StreamingAssets 是真实目录）。
  /// 若将来要出 Android/WebGL，需改用 UnityWebRequest 异步读取后再构建 Tables。
  /// </summary>
  public static class ConfigManager {
    /// <summary>全局配置表。Load 之后可用。</summary>
    public static Tables Tables { get; private set; }

    public static void Load() {
      string dir = Path.Combine(Application.streamingAssetsPath, "Tables");
      Tables = new Tables(name => {
        string file = Path.Combine(dir, name + ".json");
        return JSON.Parse(File.ReadAllText(file));
      });
      LocalizationManager.Initialize(Tables.TbLocalization);
      Debug.Log("[ConfigManager] 配置表加载完成。");
    }
  }
}
