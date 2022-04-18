using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

class FileOpenerSettings : ScriptableObject {
    public const string FileOpenerSettingsPath = "Assets/Settings/FileOpenerSettings.asset";

    [SerializeField]
    public bool enabled = true;

    [System.Serializable]
    public class Opener {
      [Header("Name is purely descriptive (like a comment).")]
      public string name;
      public bool enabled = false;
      [Header("Absolute path to the executable.")]
      public string executablePath;
      [Header("Substitutions available: {filePath}, {line}, and {column}")]
      public string argumentFormat = "{filePath}";
      [Header("The following extensions will be sent to this program.")]
      public string fileExtensions = "cs;txt";
      [System.NonSerialized]
      private string[] _fileExtensionsArray;// = new [] { ".cs", ".txt", ".js", ".javascript", ".json", ".html", ".shader", ".template" };
      public string[] fileExtensionsArray {
        get {
          if (_fileExtensionsArray == null)
            _fileExtensionsArray = fileExtensions.ToLower().Split(';');
          return _fileExtensionsArray;
        }
      }
    }

    [Header("The first enabled opener that matches an opened file's extension, will be executed.")]
    [SerializeField]
    public List<Opener> openers = new List<Opener> {
              new Opener { name = "Emacs",
                           executablePath = "/usr/local/bin/emacsclient",
                           argumentFormat = "-n +{line}:{column} {filePath}",
                           fileExtensions = "cs;txt;js;javascript;json;html;shader;template",
              },
              new Opener { name = "vim",
                           executablePath = "/usr/local/bin/mvim",
                           argumentFormat = "-c \"call cursor({line},{column})\" {filePath}",
                           fileExtensions = "cs;txt;js;javascript;json;html;shader;template",
              },
    };

    internal static FileOpenerSettings settings = null;

    [InitializeOnEnterPlayMode]
    void Clear() {
      settings = null;
    }

    internal static FileOpenerSettings GetOrCreateSettings() {
      if (settings == null) {
        settings = AssetDatabase.LoadAssetAtPath<FileOpenerSettings>(FileOpenerSettingsPath);
        if (settings == null) {
            settings = ScriptableObject.CreateInstance<FileOpenerSettings>();
            settings.openers = new List<Opener> {
              new Opener { name = "Emacs",
                
                           executablePath = "/usr/local/bin/emacsclient",
                           // argumentFormat = "-n {{#line}}+line
              },
              
            };
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
                var settings = FileOpenerSettings.GetSerializedSettings();
                EditorGUILayout.PropertyField(settings.FindProperty("enabled"), new GUIContent("Enabled"));
                EditorGUILayout.PropertyField(settings.FindProperty("openers"), new GUIContent("Openers"), true);
                // EditorGUILayout.PropertyField(settings.FindProperty("m_SomeString"), new GUIContent("My String"));
                settings.ApplyModifiedProperties();
            },

            // Populate the search keywords to enable smart search filtering and label highlighting:
            keywords = new HashSet<string>(new[] { "File", "Open", "Emacs", "vim" })
        };
        return provider;
    }

  [OnOpenAsset]
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
        opener.fileExtensionsArray.Contains(selectedFileExt)) {

      string projectPath = Path.GetDirectoryName(UnityEngine.Application.dataPath);
      string absoluteFilePath = Path.Combine(projectPath, selectedFilePath);
      string arguments = opener.argumentFormat;
      if (line < 0)
        line = 1;
      if (column < 0)
        column = 1;
      arguments = arguments.Replace("{line}", line.ToString());
      arguments = arguments.Replace("{column}", column.ToString());
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
      return true;
    }
    }

    // Debug.LogWarning("Letting Unity handle opening of script instead of Emacs.");
    return false;
  }
}
