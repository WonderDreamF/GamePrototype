using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Awen.SaveSystem {
  /// <summary>
  /// 存档管理器。支持 3 个存档槽位 + 自动存档。
  /// 放在常驻场景里，不使用 DontDestroyOnLoad。
  /// 注意：本系统在独立程序集 Awen.SaveSystem 中（便于单元测试），无法引用位于
  /// Assembly-CSharp 的 <c>MonoSingleton</c>，因此这里保留自包含的单例实现。
  /// 用 OnSaveCompleted / OnSaveLoaded 事件对接外部系统（如 EventBus）。
  /// </summary>
  public class SaveManager : MonoBehaviour {
    private const string _slotFileName = "save_slot_{0}.json";
    private const string _autoSaveFileName = "save_auto.json";

    private static readonly JsonSerializerSettings _jsonSettings = new() {
      Formatting = Formatting.Indented,
      NullValueHandling = NullValueHandling.Ignore,
      MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    private SaveData _current;
    private float _sessionStartTime;

    public static SaveManager Instance { get; private set; }

    public SaveData Current => _current;
    public bool HasActiveGame => _current != null;

    /// <summary>存档写入完成。参数：slot（-1 = 自动存档）</summary>
    public event Action<int> OnSaveCompleted;

    /// <summary>存档读取完成。参数：slot（-1 = 自动存档）</summary>
    public event Action<int> OnSaveLoaded;

    private string SaveDir => Application.persistentDataPath;

    private void Awake() {
      if (Instance != null && Instance != this) {
        Destroy(gameObject);
        return;
      }
      Instance = this;
    }

    private void OnDestroy() {
      if (Instance == this) {
        Instance = null;
      }
    }

    // ── 游戏流程 ──────────────────────────────────────────────

    public void StartNewGame() {
      _current = new SaveData();
      WorldFlagManager.Initialize(_current.World);
      _sessionStartTime = Time.unscaledTime;
    }

    public bool LoadSlot(int slot) {
      var data = ReadFile(SlotPath(slot));
      if (data == null) {
        return false;
      }

      _current = data;
      WorldFlagManager.Initialize(_current.World);
      _sessionStartTime = Time.unscaledTime;
      OnSaveLoaded?.Invoke(slot);
      return true;
    }

    // ── 保存 ──────────────────────────────────────────────────

    public void SaveToSlot(int slot) => Save(SlotPath(slot), slot);

    public void AutoSave() => Save(AutoSavePath(), -1);

    private void Save(string path, int slot) {
      if (_current == null) {
        return;
      }

      FlushRuntimeState();
      WriteFile(path, _current);
      OnSaveCompleted?.Invoke(slot);
    }

    // ── 槽位查询 ──────────────────────────────────────────────

    public bool HasSave(int slot) => File.Exists(SlotPath(slot));

    public MetaData GetSlotMeta(int slot) {
      var data = ReadFile(SlotPath(slot));
      return data?.Meta;
    }

    public void DeleteSlot(int slot) {
      string path = SlotPath(slot);
      if (File.Exists(path)) {
        File.Delete(path);
      }
    }

    // ── 运行时状态写回 ────────────────────────────────────────

    private void FlushRuntimeState() {
      float elapsed = Time.unscaledTime - _sessionStartTime;
      _current.Meta.PlayTime += elapsed;
      _sessionStartTime = Time.unscaledTime;
      _current.Meta.SaveTime = DateTime.UtcNow.ToString("o");
      _current.World.Flags = WorldFlagManager.Serialize();
    }

    // ── 文件 I/O ──────────────────────────────────────────────

    private string SlotPath(int slot) => Path.Combine(SaveDir, string.Format(_slotFileName, slot));
    private string AutoSavePath() => Path.Combine(SaveDir, _autoSaveFileName);

    private void WriteFile(string path, SaveData data) {
      try {
        File.WriteAllText(path, JsonConvert.SerializeObject(data, _jsonSettings));
      } catch (Exception e) {
        Debug.LogError($"[SaveManager] Write failed: {e.Message}");
      }
    }

    private SaveData ReadFile(string path) {
      if (!File.Exists(path)) {
        return null;
      }
      try {
        return JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(path), _jsonSettings);
      } catch (Exception e) {
        Debug.LogError($"[SaveManager] Read failed: {e.Message}");
        return null;
      }
    }
  }
}
