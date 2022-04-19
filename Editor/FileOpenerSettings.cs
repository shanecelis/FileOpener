//#define DEBUG_FILE_OPENER
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

class FileOpenerSettings : ScriptableObject {
    public const string FileOpenerSettingsPath = "Assets/Settings/FileOpenerSettings.asset";

    [SerializeField]
    public bool enabled = true;

    [System.Serializable]
    public class Opener {
      [Header("Name is purely descriptive (like a comment)")]
      public string name;
      public bool enabled = false;
      [Header("Absolute path to the executable")]
      public string executablePath;
      [Header("Substitutions available: {filePath}")]
      public string argumentFormat0 = "\"{filePath}\"";
      [Header("Substitutions available: {filePath} and {line}")]
      public string argumentFormat1 = "\"{filePath}\"";
      [Header("Substitutions available: {filePath}, {line}, and {column}")]
      public string argumentFormat2 = "\"{filePath}\"";
      [Header("Semi-colon separated list of extensions this opener will accept")]
      public string fileExtensions = "cs;txt";
      [System.NonSerialized]
      private HashSet<string> _fileExtensionsSet;// = new [] { ".cs", ".txt", ".js", ".javascript", ".json", ".html", ".shader", ".template" };
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

    [Header("The first enabled opener that matches an opened file's extension, will be executed.")]
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

static class FileOpenerSettingsIMGUIRegister {
    [SettingsProvider]
    public static SettingsProvider CreateFileOpenerSettingsProvider() {
        // First parameter is the path in the Settings window.
        // Second parameter is the scope of this setting: it only appears in the Project Settings window.
        var provider = new SettingsProvider("Project/File Opener", SettingsScope.Project) {
            // By default the last token of the path is used as display name if no label is provided.
            label = "File Opener",
            // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
            guiHandler = (searchContext) => {

              EditorGUILayout.LabelField("A file opener for Unity3d's editor, ensures Emacs and vim will go to the right line and column, if that information is available.", EditorStyles.wordWrappedLabel);
                var settings = FileOpenerSettings.GetSerializedSettings();
                EditorGUILayout.PropertyField(settings.FindProperty("enabled"), new GUIContent("Enabled"));
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
