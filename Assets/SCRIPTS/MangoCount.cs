using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using System.IO;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;


public class MangoCount : MonoBehaviour
{

    public TextMeshProUGUI mangoUIText;
    public TextMeshProUGUI appleUIText;
    

    void Start()
    {
        Time.timeScale = 0f;
        mangoscript.mangoText = mangoUIText;
        mangoscript.appleText = appleUIText;
        
    }
    public void restart() // call this from your Restart button OnClick()
{
    // save any runtime data
    try
    {
        PlayerPrefs.Save();
    }
    catch (Exception e)
    {
        Debug.LogWarning("Failed saving PlayerPrefs before restart: " + e.Message);
    }

#if UNITY_EDITOR
    // In editor: stop play mode
    UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE_WIN
    try
    {
        string dataPath = Application.dataPath; // e.g. "C:/.../MyGame.exe_Data"
        string exePath = GetExePathFromDataPath(dataPath);

        if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            };
            Process.Start(startInfo);
            Debug.Log("[restart] Relaunch started: " + exePath);
        }
        else
        {
            Debug.LogWarning("[restart] Relaunch failed — exe not found at: " + exePath);
        }
    }
    catch (Exception ex)
    {
        Debug.LogError("[restart] Relaunch exception: " + ex);
    }
    finally
    {
        // Quit the current process (will close the app)
        Application.Quit();
    }
#else
    // Other platforms: do safe soft-restart (reload first scene)
    Debug.Log("[restart] Platform does not support exe relaunch. Reloading initial scene instead.");
    try
    {
        // use build index 0 or replace with your initial scene name
        UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    catch (Exception e)
    {
        Debug.LogError("[restart] Soft reload failed: " + e.Message);
    }
#endif

    // Local helper (C# local function) to infer EXE path from Application.dataPath
    string GetExePathFromDataPath(string dataPath)
    {
        if (string.IsNullOrEmpty(dataPath)) return string.Empty;

        // Typical Windows build: "<...>/MyGame.exe_Data"
        if (dataPath.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
        {
            string basePath = dataPath.Substring(0, dataPath.Length - "_Data".Length);
            return basePath + ".exe";
        }

        // fallback: try parent folder + foldername.exe
        try
        {
            var parent = Directory.GetParent(dataPath);
            if (parent != null)
            {
                string guess = Path.Combine(parent.FullName, Path.GetFileName(parent.FullName) + ".exe");
                return guess;
            }
        }
        catch (Exception) { /* ignore */ }

        return string.Empty;
    }
}
    public void quit9() // for quiting the game 
    {
        Application.Quit();
    }
    

}

