using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GamePrototype.Editor {
  public static class LubanGenerator {
    [MenuItem("Tools/GamePrototype/生成 Luban 配表数据")]
    public static void Generate() {
      var batPath = Path.GetFullPath(
          Path.Combine(Application.dataPath, "..", "Tools", "gen_client.bat"));

      EditorUtility.DisplayProgressBar("Luban", "Generating tables...", 0.5f);

      try {
        var process = new Process {
          StartInfo = new ProcessStartInfo {
            FileName = "cmd.exe",
            Arguments = $"/c \"{batPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
          }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0) {
          UnityEngine.Debug.Log($"[Luban] Generation succeeded.\n{output}");
          AssetDatabase.Refresh();
        } else {
          UnityEngine.Debug.LogError($"[Luban] Generation failed.\n{output}\n{error}");
        }
      } finally {
        EditorUtility.ClearProgressBar();
      }
    }
  }
}
