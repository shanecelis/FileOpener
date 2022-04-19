/* Original code[1] Copyright (c) 2018 Juan Karlo[2]
   Modified code[3] Copyright (c) 2022 Shane Celis[4]
   Licensed under the MIT License[5]

   This comment generated by code-cite[6].

   [1]: https://gist.github.com/accidentalrebel/69ac38f729e72c170a8d091b4daaec52
   [2]: https://github.com/accidentalrebel
   [3]: https://github.com/shanecelis/FileOpener.git
   [4]: http://twitter.com/shanecelis
   [5]: https://opensource.org/licenses/MIT
   [6]: https://github.com/shanecelis/code-cite
*/
//#define DEBUG_FILE_OPENER
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace SeawispHunter.FileOpener {

class FileOpenerSettings : ScriptableObject {
  public const string FileOpenerSettingsPath = "Assets/Settings/FileOpenerSettings.asset";

  [SerializeField]
  public bool enabled = true;

  [System.Serializable]
  public class Opener {
    [Tooltip("Name is purely descriptive (like a comment)")]
    public string name;
    public bool enabled = false;
    [Header("Semi-colon separated list of extensions this opener will accept")]
    public string fileExtensions = "cs;txt";
    [Header("Absolute path to the executable")]
    public string executablePath;
    [Header("Substitutions available: {filePath}")]
    public string argumentFormat0 = "\"{filePath}\"";
    [Header("Substitutions available: {filePath} and {line}")]
    public string argumentFormat1 = "\"{filePath}\"";
    [Header("Substitutions available: {filePath}, {line}, and {column}")]
    public string argumentFormat2 = "\"{filePath}\"";
    [System.NonSerialized]
    private HashSet<string> _fileExtensionsSet;
    public HashSet<string> fileExtensionsSet {
      get {
        if (_fileExtensionsSet == null)
          _fileExtensionsSet = new HashSet<string>(fileExtensions.ToLower().Split(';'));
        return _fileExtensionsSet;
      }
    }
    public void ClearCache() {
      _fileExtensionsSet = null;
    }
  }


  public void ClearCache() {
    foreach (var opener in openers)
      opener.ClearCache();
  }

  [Header("The first to match a file's extension executes.")]
  [SerializeField]
  public List<Opener> openers = new List<Opener> {
    new Opener { name = "Emacs",
                 executablePath = "/usr/local/bin/emacsclient",
                 argumentFormat0 = "-n \"{filePath}\"",
                 argumentFormat1 = "-n +{line} \"{filePath}\"",
                 argumentFormat2 = "-n +{line}:{column} \"{filePath}\"",
                 fileExtensions = "cs;txt;js;javascript;json;html;shader;template",
    },
    new Opener { name = "vim",
                 executablePath = "/usr/local/bin/mvim",
                 argumentFormat0 = "\"{filePath}\"",
                 argumentFormat1 = "+{line} \"{filePath}\"",
                 argumentFormat2 = "-c \"call cursor({line},{column})\" \"{filePath}\"",
                 fileExtensions = "cs;txt;js;javascript;json;html;shader;template",
    },
  };

  /* TODO: Consider adding advanced settings for controlling the StartInfo of the process. */
  // class AdvancedSettings { }

  internal static FileOpenerSettings settings = null;

  [InitializeOnEnterPlayMode]
  static void ResetSingleton() {
    settings = null;
  }

  void Reset() {
    ResetSingleton();
  }

  internal static FileOpenerSettings GetOrCreateSettings() {
    if (settings == null) {
      settings = AssetDatabase.LoadAssetAtPath<FileOpenerSettings>(FileOpenerSettingsPath);
      if (settings == null) {
          settings = ScriptableObject.CreateInstance<FileOpenerSettings>();
          var dirName = Path.GetDirectoryName(FileOpenerSettingsPath);
          if (! AssetDatabase.IsValidFolder(dirName))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(dirName), Path.GetFileName(dirName));
          AssetDatabase.CreateAsset(settings, FileOpenerSettingsPath);
          AssetDatabase.SaveAssets();
      }
    }
    return settings;
  }

  internal static SerializedObject GetSerializedSettings() {
      return new SerializedObject(GetOrCreateSettings());
  }
}

static class FileOpenerSettingsProvider {

  [SettingsProvider]
  public static SettingsProvider CreateFileOpenerSettingsProvider() {
    var provider = new SettingsProvider("Project/File Opener", SettingsScope.Project) {
      label = "File Opener",
      guiHandler = (searchContext) => {

        // EditorGUILayout.LabelField("A file opener for Unity3d's editor");
        EditorGUILayout.LabelField("Made to ensure Emacs and vim will go to the right line and column if that information is available.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();
        var settings = FileOpenerSettings.GetSerializedSettings();
        var enabled = settings.FindProperty("enabled");
        enabled.boolValue = EditorGUILayout.ToggleLeft("Enabled", enabled.boolValue);
        // EditorGUILayout.PropertyField(settings.FindProperty("enabled"), new GUIContent("Enabled"));
        EditorGUILayout.PropertyField(settings.FindProperty("openers"), new GUIContent("Openers"), true);
        // EditorGUILayout.PropertyField(settings.FindProperty("m_SomeString"), new GUIContent("My String"));
        // Might be nice to have a reset button.
        EditorGUILayout.HelpBox($"These settings are stored in \"{FileOpenerSettings.FileOpenerSettingsPath}\". Resetting that asset will restore the original settings.", MessageType.Info);

        if (settings.ApplyModifiedProperties()) {
          var _settings = (FileOpenerSettings) settings.targetObject;
          _settings.ClearCache();
        }
      },
      // Populate the search keywords to enable smart search filtering and label highlighting:
      keywords = new HashSet<string>(new[] { "File", "Open", "Emacs", "vim" })
    };
    return provider;
  }
  private static string[] argumentFormats = new string[3];

  [OnOpenAsset(0)]
  public static bool OnOpenedAsset(int instanceID, int line, int column) {
    var settings = FileOpenerSettings.GetOrCreateSettings();
    if (settings != null && ! settings.enabled)
      return false;
    UnityEngine.Object selected = EditorUtility.InstanceIDToObject(instanceID);
    string selectedFilePath = AssetDatabase.GetAssetPath(selected);
    string selectedFileExt = (Path.GetExtension(selectedFilePath) ?? "")
      .ToLower()
      .TrimStart('.');

    foreach (var opener in settings.openers) {
      if (opener.enabled &&
        opener.fileExtensionsSet.Contains(selectedFileExt)) {

      string projectPath = Path.GetDirectoryName(UnityEngine.Application.dataPath);
      string absoluteFilePath = Path.Combine(projectPath, selectedFilePath);
      string arguments = opener.argumentFormat0;
      if (line >= 0) {
        // There is a line argument.
        if (column < 0) {
        // There is no column argument.
          arguments = opener.argumentFormat1;
        } else {
          arguments = opener.argumentFormat2;
          arguments = arguments.Replace("{column}", column.ToString());
        }
        arguments = arguments.Replace("{line}", line.ToString());
      }
      arguments = arguments.Replace("{filePath}", absoluteFilePath);

      var proc = new System.Diagnostics.Process {
        StartInfo = {
          FileName = opener.executablePath,
          Arguments = arguments,
          UseShellExecute = false,
          WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
          CreateNoWindow = true,
          RedirectStandardOutput = true,
        }
      };
      proc.Start();

#if DEBUG_FILE_OPENER
      Debug.Log($"FileOpener: file \"{selectedFilePath}\" opened with {opener.name}.");
#endif
      return true;
    }
    }
#if DEBUG_FILE_OPENER
      Debug.Log($"FileOpener: no opener found for file \"{selectedFilePath}\"; use default opener.");
#endif
    return false;
  }
}

}
