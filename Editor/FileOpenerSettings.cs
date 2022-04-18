using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

class FileOpenerSettings : ScriptableObject {
    public const string FileOpenerSettingsPath = "ProjectSettings/FileOpenerSettings.asset";

    [SerializeField]
    public bool enabled = true;

    [System.Serializable]
    public class Opener {
      [SerializeField]
      public string executablePath = "/usr/local/Cellar/emacs-mac/emacs-28.1-mac-9.0/bin/emacsclient";
      [SerializeField]
      public string[] fileExtensions = new [] { ".cs", ".txt", ".js", ".javascript", ".json", ".html", ".shader", ".template" };
      public string executableFormat = "{executablePath} -n +{line} {filePath}";
    }

    [SerializeField]
    public List<Opener> openers;

    internal static FileOpenerSettings GetOrCreateSettings() {
        var settings = AssetDatabase.LoadAssetAtPath<FileOpenerSettings>(FileOpenerSettingsPath);
        if (settings == null) {
            settings = ScriptableObject.CreateInstance<FileOpenerSettings>();
            settings.openers = new List<Opener> {
              new Opener { }
            };
            AssetDatabase.CreateAsset(settings, FileOpenerSettingsPath);
            AssetDatabase.SaveAssets();
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
                EditorGUILayout.PropertyField(settings.FindProperty("openers"), new GUIContent("Openers"));
                // EditorGUILayout.PropertyField(settings.FindProperty("m_SomeString"), new GUIContent("My String"));
                settings.ApplyModifiedProperties();
            },

            // Populate the search keywords to enable smart search filtering and label highlighting:
            keywords = new HashSet<string>(new[] { "Number", "Some String" })
        };
        return provider;
    }
}
