using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace Awen.SaveSystem {
  /// <summary>
  /// EditMode 测试：存档系统核心逻辑。
  /// 在 Window > General > Test Runner > EditMode 中运行。
  /// </summary>
  public class SaveSystemTests {
    private static readonly JsonSerializerSettings _jsonSettings = new() {
      Formatting = Formatting.Indented,
      NullValueHandling = NullValueHandling.Ignore,
      MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    // ── WorldFlagManager ─────────────────────────────────────────

    [SetUp]
    public void SetUp() {
      // 每个测试前重置 Flag 状态
      WorldFlagManager.Initialize(new WorldState());
    }

    [Test]
    public void Flag_SetTrue_GetReturnsTrue() {
      WorldFlagManager.Set("Forest_Shrine_Purified", true);
      Assert.IsTrue(WorldFlagManager.Get("Forest_Shrine_Purified"));
    }

    [Test]
    public void Flag_SetFalse_GetReturnsFalse() {
      WorldFlagManager.Set("Truth_Revealed", true);
      WorldFlagManager.Set("Truth_Revealed", false);
      Assert.IsFalse(WorldFlagManager.Get("Truth_Revealed"));
    }

    [Test]
    public void Flag_UnknownKey_ReturnsFalse() {
      Assert.IsFalse(WorldFlagManager.Get("nonexistent_key"));
    }

    [Test]
    public void Flag_Toggle_FlipsValue() {
      WorldFlagManager.Set("Ruin_Understood", false);
      WorldFlagManager.Toggle("Ruin_Understood");
      Assert.IsTrue(WorldFlagManager.Get("Ruin_Understood"));

      WorldFlagManager.Toggle("Ruin_Understood");
      Assert.IsFalse(WorldFlagManager.Get("Ruin_Understood"));
    }

    [Test]
    public void Flag_Serialize_ContainsAllSetFlags() {
      WorldFlagManager.Set("Flag_A", true);
      WorldFlagManager.Set("Flag_B", false);
      WorldFlagManager.Set("Flag_C", true);

      var list = WorldFlagManager.Serialize();

      Assert.AreEqual(3, list.Count);
    }

    [Test]
    public void Flag_InitializeFromState_RestoresFlags() {
      var state = new WorldState {
        Flags = new List<WorldFlag> {
          new WorldFlag { Key = "Shrine_A", Value = true },
          new WorldFlag { Key = "Shrine_B", Value = false },
        }
      };

      WorldFlagManager.Initialize(state);

      Assert.IsTrue(WorldFlagManager.Get("Shrine_A"));
      Assert.IsFalse(WorldFlagManager.Get("Shrine_B"));
    }

    [Test]
    public void Flag_SerializeAndRestore_RoundTrip() {
      WorldFlagManager.Set("Echo_Read_Unlocked", true);
      WorldFlagManager.Set("Village_Door_Open", true);

      var serialized = WorldFlagManager.Serialize();

      // 模拟存档读取后重新初始化
      WorldFlagManager.Initialize(new WorldState { Flags = serialized });

      Assert.IsTrue(WorldFlagManager.Get("Echo_Read_Unlocked"));
      Assert.IsTrue(WorldFlagManager.Get("Village_Door_Open"));
    }

    // ── SaveData JSON 序列化 ─────────────────────────────────────

    [Test]
    public void SaveData_JsonRoundTrip_PreservesData() {
      var original = new SaveData();
      original.Meta.ChapterName = "Chapter01";
      original.Meta.PlayTime = 123.45f;
      original.Player.CurrentForm = "Wind_Form";
      original.Chapter.ChapterId = "ch_01";
      original.Chapter.VisitedAreas.Add("area_forest");
      original.Ability.UnlockedAbilities.Add("Wind_Breath");
      original.Lore.CollectedLore.Add("LORE_AWEN_ORIGIN_001");
      original.Puzzle.SolvedPuzzles.Add(new PuzzleRecord {
        PuzzleId = "PUZZLE_STONE_RING_01",
        Solved = true,
        SolveCount = 2,
      });

      string json = JsonConvert.SerializeObject(original, _jsonSettings);
      var restored = JsonConvert.DeserializeObject<SaveData>(json, _jsonSettings);

      Assert.AreEqual("Chapter01", restored.Meta.ChapterName);
      Assert.AreEqual(123.45f, restored.Meta.PlayTime, 0.001f);
      Assert.AreEqual("Wind_Form", restored.Player.CurrentForm);
      Assert.AreEqual("ch_01", restored.Chapter.ChapterId);
      Assert.Contains("area_forest", restored.Chapter.VisitedAreas);
      Assert.Contains("Wind_Breath", restored.Ability.UnlockedAbilities);
      Assert.Contains("LORE_AWEN_ORIGIN_001", restored.Lore.CollectedLore);
      Assert.AreEqual(1, restored.Puzzle.SolvedPuzzles.Count);
      Assert.AreEqual("PUZZLE_STONE_RING_01", restored.Puzzle.SolvedPuzzles[0].PuzzleId);
      Assert.AreEqual(2, restored.Puzzle.SolvedPuzzles[0].SolveCount);
    }

    [Test]
    public void SaveData_OldSave_MissingFieldsGetDefaults() {
      // 模拟旧版存档 JSON（缺少新字段）
      const string oldJson = @"{
        ""Version"": 1,
        ""Meta"": { ""ChapterName"": ""Prologue"" }
      }";

      // MissingMemberHandling.Ignore 确保不抛异常
      Assert.DoesNotThrow(() => {
        var data = JsonConvert.DeserializeObject<SaveData>(oldJson, _jsonSettings);
        Assert.IsNotNull(data);
        Assert.AreEqual("Prologue", data.Meta.ChapterName);
        // 缺失的列表字段应为 null（Newtonsoft 行为），不会崩溃
      });
    }

    [Test]
    public void SaveData_NewGame_DefaultsAreValid() {
      var data = new SaveData();

      Assert.AreEqual(1, data.Version);
      Assert.IsNotNull(data.Meta);
      Assert.IsNotNull(data.Player);
      Assert.IsNotNull(data.Chapter);
      Assert.IsNotNull(data.World);
      Assert.IsNotNull(data.Puzzle);
      Assert.IsNotNull(data.Ability);
      Assert.IsNotNull(data.Lore);
      Assert.IsNotNull(data.Chapter.VisitedAreas);
      Assert.IsNotNull(data.Ability.UnlockedAbilities);
      Assert.IsNotNull(data.Lore.CollectedLore);
    }

    // ── 文件 I/O（使用临时目录） ─────────────────────────────────

    [Test]
    public void SaveFile_WriteAndRead_RoundTrip() {
      string tmpPath = Path.Combine(Application.temporaryCachePath, "test_save.json");

      var original = new SaveData();
      original.Meta.SceneName = "Chapter01_Forest";
      original.Meta.PlayTime = 300f;

      File.WriteAllText(tmpPath, JsonConvert.SerializeObject(original, _jsonSettings));
      var restored =
          JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(tmpPath), _jsonSettings);

      Assert.AreEqual("Chapter01_Forest", restored.Meta.SceneName);
      Assert.AreEqual(300f, restored.Meta.PlayTime, 0.001f);

      File.Delete(tmpPath);
    }
  }
}
