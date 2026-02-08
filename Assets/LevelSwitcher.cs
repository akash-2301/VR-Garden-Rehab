using UnityEngine;
using UnityEngine.UI;

public class LevelManagerUI : MonoBehaviour
{
    [Header("Level Setup")]
    public GameObject[] players;          // Player1 to Player6
    public GameObject[] levelAssets;      // Level1_Assets to Level6_Assets
    public GameObject levelPanel;         // The panel with level buttons

    [Header("Single Object Holding All Level Scripts")]
    public GameObject levelManagerObject; // The GameObject with all LevelXStageManager scripts

    void Start()
    {
        if (levelPanel != null)
            levelPanel.SetActive(false);

   
    }

    // Called when Option button is clicked
    public void ToggleLevelPanel()
    {
        if (levelPanel != null)
            levelPanel.SetActive(!levelPanel.activeSelf);
    }

    // Called when a level button is clicked — Pass index (0–5)
    public void SelectLevel(int levelIndex)
    {
        for (int i = 0; i < players.Length; i++)
        {
            bool isActive = (i == levelIndex);

            players[i].SetActive(isActive);
            levelAssets[i].SetActive(isActive);
        }

        EnableSelectedLevelScript(levelIndex);

        if (levelPanel != null)
            levelPanel.SetActive(false);

        Debug.Log($"✅ Level {levelIndex + 1} selected and correct StageManager enabled.");
    }

    // Disable all Level scripts
    private void DisableAllLevelManagers()
    {
        if (levelManagerObject == null) return;

        var l1 = levelManagerObject.GetComponent<Level1StageManager>();
        var l3 = levelManagerObject.GetComponent<Level3StageManager>();
        var l8 = levelManagerObject.GetComponent<Level8StageManager>();
        var l80 = levelManagerObject.GetComponent<Level80StageManager>();
        var l5 = levelManagerObject.GetComponent<Level5StageManager>();
        var l6 = levelManagerObject.GetComponent<Level6StageManager>();

        if (l1) l1.enabled = false;
        if (l3) l3.enabled = false;
        if (l8) l8.enabled = false;
        if (l80) l80.enabled = false;
        if (l5) l5.enabled = false;
        if (l6) l6.enabled = false;
    }

    // Enable only the correct script for the selected level
    private void EnableSelectedLevelScript(int levelIndex)
    {
        DisableAllLevelManagers();

        if (levelManagerObject == null) return;

        switch (levelIndex)
        {
            case 0: // Level 1
                var l1 = levelManagerObject.GetComponent<Level1StageManager>();
                if (l1) l1.enabled = true;
                break;

            case 1: // Level 3
                var l3 = levelManagerObject.GetComponent<Level3StageManager>();
                if (l3) l3.enabled = true;
                break;

            case 2: // Level 8
                var l8 = levelManagerObject.GetComponent<Level8StageManager>();
                if (l8) l8.enabled = true;
                break;

            case 3: // Level 80
                var l80 = levelManagerObject.GetComponent<Level80StageManager>();
                if (l80) l80.enabled = true;
                break;

            case 4: // Level 5
                var l5 = levelManagerObject.GetComponent<Level5StageManager>();
                if (l5) l5.enabled = true;
                break;

            case 5: // Level 6
                var l6 = levelManagerObject.GetComponent<Level6StageManager>();
                if (l6) l6.enabled = true;
                break;

            default:
                Debug.LogWarning("⚠️ No stage manager defined for index " + levelIndex);
                break;
        }
    }
}
