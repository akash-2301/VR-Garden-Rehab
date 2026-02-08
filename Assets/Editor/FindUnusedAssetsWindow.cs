// Assets/Editor/FindUnusedAssetsWindow.cs
// Version: With size display + largest-first sorting
// Safe tool for identifying & moving unused assets before build

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FindUnusedAssetsWindow : EditorWindow
{
    
    Vector2 scroll;
    List<AssetInfo> candidates = new List<AssetInfo>();
    bool includeMeta = false;
    bool autoMove = false;

class AssetInfo
    {
        public string path;
        public long sizeBytes;
        public float sizeMB => sizeBytes / (1024f * 1024f);
    }

    [MenuItem("Tools/Find Unused Assets (with Size)")]
    static void OpenWindow() => GetWindow<FindUnusedAssetsWindow>("Find Unused Assets");

    void OnGUI()
    {
        GUILayout.Label("🔍 Find Unused Assets (sorted by size)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        includeMeta = EditorGUILayout.Toggle("Include .meta files in list", includeMeta);
        autoMove = EditorGUILayout.Toggle("Auto move to Assets/_UnusedBackup", autoMove);

        if (GUILayout.Button("🔎 Scan Project (scenes in Build Settings)"))
        {
            ScanProject();
        }

        if (candidates.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Found {candidates.Count} unused assets:", EditorStyles.boldLabel);
            GUILayout.Label("(Largest first)", EditorStyles.miniLabel);

            if (GUILayout.Button("📂 Select _UnusedBackup folder"))
            {
                var path = "Assets/_UnusedBackup";
                if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder("Assets", "_UnusedBackup");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
            }

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(350));
            foreach (var info in candidates)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{info.sizeMB:F2} MB", GUILayout.Width(70));
                EditorGUILayout.LabelField(info.path);
                if (GUILayout.Button("Select", GUILayout.Width(70)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(info.path);
                    if (obj) Selection.activeObject = obj;
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (autoMove && GUILayout.Button("Move to Assets/_UnusedBackup (Safe Move)"))
            {
                MoveCandidatesToBackup();
            }
        }
    }

    void ScanProject()
    {
        candidates.Clear();

        // Step 1 — Collect all assets in the project
        string[] allAssetGuids = AssetDatabase.FindAssets("", new[] { "Assets" });
        HashSet<string> allAssets = new HashSet<string>(allAssetGuids.Select(g => AssetDatabase.GUIDToAssetPath(g)));

        // Step 2 — Find all scenes in build settings
        var buildScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        // Step 3 — Gather dependencies from build scenes
        HashSet<string> used = new HashSet<string>();
        foreach (var scenePath in buildScenes)
        {
            var deps = AssetDatabase.GetDependencies(new[] { scenePath }, true);
            foreach (var d in deps) used.Add(d);
        }

        // Step 4 — Always keep essential folders
        string[] alwaysKeep = { "Assets/StreamingAssets", "Assets/Plugins", "Assets/Editor", "Assets/AddressableAssetsData" };
        foreach (var p in alwaysKeep)
        {
            var found = AssetDatabase.FindAssets("", new[] { p });
            foreach (var g in found)
                used.Add(AssetDatabase.GUIDToAssetPath(g));
        }

        // Step 5 — Keep Resources folder assets
        var resourcesGuids = AssetDatabase.FindAssets("", new[] { "Assets" })
            .Where(g => AssetDatabase.GUIDToAssetPath(g).ToLower().Contains("/resources/"));
        foreach (var g in resourcesGuids)
            used.Add(AssetDatabase.GUIDToAssetPath(g));

        // Step 6 — Find unused candidates
        foreach (var path in allAssets)
        {
            if (!includeMeta && path.EndsWith(".meta")) continue;
            if (path.EndsWith(".cs") || path.EndsWith(".asmdef") || path.EndsWith(".dll")) continue;
            if (path.StartsWith("Assets/Editor") || path.StartsWith("Assets/Plugins") || path.StartsWith("Assets/StreamingAssets"))
                continue;

            if (!used.Contains(path) && !Directory.Exists(path))
            {
                long fileSize = 0;
                try { fileSize = new FileInfo(path).Length; } catch { }
                candidates.Add(new AssetInfo { path = path, sizeBytes = fileSize });
            }
        }

        // Step 7 — Sort by size (largest first)
        candidates = candidates.OrderByDescending(c => c.sizeBytes).ToList();

        Debug.Log($"FindUnusedAssets: found {candidates.Count} unused assets.");
        if (candidates.Count > 0)
        {
            float totalMB = candidates.Sum(c => c.sizeMB);
            Debug.Log($"Total size of unused assets: {totalMB:F2} MB");
        }
    }

    void MoveCandidatesToBackup()
    {
        string backupFolder = "Assets/_UnusedBackup";
        if (!AssetDatabase.IsValidFolder(backupFolder))
        {
            AssetDatabase.CreateFolder("Assets", "_UnusedBackup");
        }

        foreach (var info in candidates)
        {
            string fileName = Path.GetFileName(info.path);
            string dest = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(backupFolder, fileName));
            Debug.Log($"Moving {info.path} → {dest}");
            AssetDatabase.MoveAsset(info.path, dest);
        }
        AssetDatabase.Refresh();
        candidates.Clear();
        Debug.Log("✅ All unused assets moved safely to Assets/_UnusedBackup/");
    }
}
