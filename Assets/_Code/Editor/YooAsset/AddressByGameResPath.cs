using YooAsset.Editor;

namespace GamePrototype.Editor {
  /// <summary>
  /// YooAsset 自定义寻址规则：地址 = 资源相对收集根目录（GameRes）的路径，去掉扩展名。
  /// 例：Assets/GameRes/UI/Icons/heart.png  ->  地址 "UI/Icons/heart"
  /// 这样 <c>ResourceManager.Instance.LoadAsync&lt;Sprite&gt;("UI/Icons/heart")</c> 即可加载。
  /// 路径天然唯一，避免按文件名寻址的重名冲突。
  /// 在收集器里把 AddressRuleName 设为本类名即可（YooAsset 通过反射按类型名发现规则）。
  /// </summary>
  public class AddressByGameResPath : IAddressRule {
    string IAddressRule.GetAssetAddress(AddressRuleData data) {
      string assetPath = data.AssetPath;
      string collectPath = data.CollectPath;

      // 去掉收集根路径前缀（收集器指向某个目录时）。
      if (!string.IsNullOrEmpty(collectPath) && assetPath.StartsWith(collectPath)) {
        assetPath = assetPath.Substring(collectPath.Length);
      }

      assetPath = assetPath.TrimStart('/', '\\');

      // 去掉扩展名。
      int dot = assetPath.LastIndexOf('.');
      if (dot >= 0) {
        assetPath = assetPath.Substring(0, dot);
      }

      return assetPath;
    }
  }
}
