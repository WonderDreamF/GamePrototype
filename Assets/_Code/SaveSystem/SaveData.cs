using System;
using System.Collections.Generic;

namespace GamePrototype.SaveSystem {
  [Serializable]
  public class SaveData {
    public int Version = 1;
    public MetaData Meta = new();
    public PlayerData Player = new();
    public ChapterData Chapter = new();
    public WorldState World = new();
    public PuzzleState Puzzle = new();
    public AbilityState Ability = new();
    public LoreState Lore = new();
  }

  [Serializable]
  public class MetaData {
    public string SaveId;
    public string SceneName;
    public string ChapterName;
    public float PlayTime;   // seconds
    public string SaveTime;  // ISO 8601
    public float PlayerX;
    public float PlayerY;
  }

  [Serializable]
  public class PlayerData {
    public float PosX;
    public float PosY;
    public string CurrentForm;  // 当前形态
  }

  [Serializable]
  public class ChapterData {
    public int ChapterIndex;
    public string ChapterId;
    public string CurrentAreaId;
    public bool ChapterCompleted;
    public List<string> VisitedAreas = new();
  }

  [Serializable]
  public class WorldState {
    public List<WorldFlag> Flags = new();
  }

  [Serializable]
  public class WorldFlag {
    public string Key;
    public bool Value;
  }

  [Serializable]
  public class PuzzleState {
    public List<PuzzleRecord> SolvedPuzzles = new();
  }

  [Serializable]
  public class PuzzleRecord {
    public string PuzzleId;
    public bool Solved;
    public int SolveCount;
  }

  [Serializable]
  public class AbilityState {
    public List<string> UnlockedAbilities = new();
  }

  [Serializable]
  public class LoreState {
    public List<string> CollectedLore = new();
  }
}
