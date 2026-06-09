using System.Collections.Generic;
using System.IO;
using Awen.UI;
using Luban.SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace Awen.Editor {
  [CustomEditor(typeof(LocalizedTMP))]
  public class LocalizedTMPEditor : UnityEditor.Editor {
    static readonly string _dataPath = Path.Combine(
        Application.dataPath, "StreamingAssets", "Tables", "localization_tblocalization.json");

    static List<string> _keys;
    static Dictionary<string, string> _previews;  // key → ZhCN
    static long _lastWriteTime;

    public override void OnInspectorGUI() {
      serializedObject.Update();

      var keyProp = serializedObject.FindProperty("_key");

      // Key 字段 + 选择按钮
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.PropertyField(keyProp, new GUIContent("Key"));
      if (GUILayout.Button("▼", GUILayout.Width(26))) {
        ShowKeyMenu(keyProp);
      }
      EditorGUILayout.EndHorizontal();

      // ZhCN 预览（只读）
      var preview = GetPreview(keyProp.stringValue);
      if (preview != null) {
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.LabelField("Preview", preview);
        EditorGUI.EndDisabledGroup();
      }

      serializedObject.ApplyModifiedProperties();
    }

    void ShowKeyMenu(SerializedProperty keyProp) {
      RefreshCacheIfNeeded();
      if (_keys == null || _keys.Count == 0) {
        EditorUtility.DisplayDialog(
            "LocalizedTMP", "未找到本地化数据，请先运行 Tools → Generate Luban Tables。", "OK");
        return;
      }

      var menu = new GenericMenu();
      foreach (var key in _keys) {
        var menuPath = key.Replace("_", "/");
        var captured = key;
        menu.AddItem(
            new GUIContent(menuPath),
            keyProp.stringValue == key,
            () => {
              keyProp.stringValue = captured;
              serializedObject.ApplyModifiedProperties();
              EditorUtility.SetDirty(target);
            });
      }
      menu.ShowAsContext();
    }

    static void RefreshCacheIfNeeded() {
      if (!File.Exists(_dataPath)) {
        _keys = null;
        _previews = null;
        return;
      }

      var writeTime = File.GetLastWriteTime(_dataPath).Ticks;
      if (_keys != null && writeTime == _lastWriteTime) {
        return;
      }

      _lastWriteTime = writeTime;
      _keys = new List<string>();
      _previews = new Dictionary<string, string>();

      var node = JSON.Parse(File.ReadAllText(_dataPath));
      foreach (JSONNode entry in node.Children) {
        var id = entry["id"].Value;
        if (string.IsNullOrEmpty(id)) {
          continue;
        }
        _keys.Add(id);
        _previews[id] = entry["ZhCN"].Value;
      }
    }

    static string GetPreview(string key) {
      if (string.IsNullOrEmpty(key)) {
        return null;
      }
      RefreshCacheIfNeeded();
      if (_previews == null) {
        return "(数据未加载)";
      }
      return _previews.TryGetValue(key, out var text) ? text : $"(key 不存在: {key})";
    }
  }
}
