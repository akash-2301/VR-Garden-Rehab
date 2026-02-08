using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class Level3StageManager : MonoBehaviour
{
    [Header("Panels & UI")]
    public GameObject panel_mangocount;
    public GameObject panel_applecount;
    public GameObject panel_SUNFLOWERcount;
    public GameObject panel_ROSEcount;

    public GameObject[] stageGroups;
    public string[] stageLabels;
    public string[] stageLabels2;

    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI mangoCountText;
    public TextMeshProUGUI appleCountText;
    public TextMeshProUGUI SUNFLOWERCountText;
    public TextMeshProUGUI ROSECountText;

    public TextMeshProUGUI timertext;
    
    public GameObject panel_instruction;
    public GameObject panel_gameover;
    public GameObject startPrompt;
    public GameObject hand;
    public timer_02 levelTimer;
    public GameObject canvas;
    public TextMeshProUGUI score_gameover;
    public UIFixer uiFixer;
    public Camera cam01;

    [Header("Players/Assets")]
    public GameObject player_3;
    public GameObject player_4;
    public GameObject level3_assest;

    [Header("Failed Level")]
    public GameObject panel_failedlevel;
    public TextMeshProUGUI failedLevelText;
    public Button failedRestartButton;

    [Header("Stage Instructions")]
    public string[][] stageInstructions = new string[][]
    {
        new string[] {"Collect mangoes only, Avoid apples.\n+1 for mango \n -0.5 for apple."},
        new string[] {"Collect apples only, Avoid mangoes.\n +1 for apple \n -0.5 for mango"},
        new string[] {"Collect sunflowers only, Avoid roses.\n +1 for sunflower \n -0.5 for rose"},
        new string[] {"Collect roses only, Avoid sunflowers.\n +1 for rose \n -0.5 for sunflower"},
    };

    [Header("Timing")]
    public float[] stageTimes = { 60f, 60f, 60f, 60f };

    public int currentStage = 0;
    public float timeRemaining = 0f;
    private bool gameOverShown = false;
    public bool isGameOver = false;
    private bool stageCompleted = false;

    private Vector3 initialHandPosition;
    private Quaternion initialHandRotation;

    [Header("PostProcess Blink")]
    public PostProcessVolume VOLUME;
    private Bloom bloom;
    private ColorGrading colorGrading;
    public bool enableBlinking = true;
    public float blinkSpeed = 15f;
    private float minIntensity = 1f;
    private float maxIntensity = 25f;
    public GameObject lightt;

    [Header("CSV Logging")]
    public int levelId = 2; // mapped to Level 2 per your mapping
    private string playerName = "";

    private int[] playOrder = new int[3];
    private float scoreAtT1 = 0f, scoreAtT2 = 0f, scoreAtT3 = 0f;
    private bool t1Sent = false, t2Sent = false, t3Sent = false;
    private DateTime stageStartTime, stageStopTime, attemptStartTime, attemptStopTime;

    public static float pf1, pf2, pf3, pf4;
    public static float Pff;
    public int[] stageMaxScores = { 8, 8, 8, 8 };
    public float passThreshold = 70f;

    public enum CueTier { None = 0, OneCue = 1, TwoCues = 2 }
    public CueTier currentCueTier = CueTier.None;
    public int attemptNumber = 1;
    private bool lastAttemptPassed = false;
    private bool bloomActiveThisAttempt = false;

    [Header("Materials")]
    public Material mangoMaterial;
    public Material appleMaterial;
    public List<Renderer> mangoRenderers = new List<Renderer>();
    public List<Renderer> appleRenderers = new List<Renderer>();
 public AudioSource sfxSource;
public AudioClip pluckSFX;

    void Start()
    {
        playerName = PlayerDataLogger.CurrentPlayerID ?? "Player";

        ResetAllGroupMaterialsForNextAttempt();

        initialHandPosition = hand.transform.localPosition;
        initialHandRotation = hand.transform.localRotation;

        if (VOLUME != null && VOLUME.profile != null)
        {
            VOLUME.profile.TryGetSettings(out bloom);
            VOLUME.profile.TryGetSettings(out colorGrading);
            if (bloom != null) bloom.intensity.Override(minIntensity);
            if (colorGrading != null) colorGrading.enabled.Override(true);
        }

        foreach (var g in stageGroups) if (g != null) g.SetActive(false);
        if (stageGroups.Length > 0) stageGroups[0].SetActive(true);

        Level3Object.counterText = counterText;
        Level3Object.mangoCountText = mangoCountText;
        Level3Object.appleCountText = appleCountText;
        Level3Object.SUNFLOWERCountText = SUNFLOWERCountText;
        Level3Object.ROSECountText = ROSECountText;

        Level3Object.ResetCounts();

        playOrder[0] = 0;
        playOrder[1] = 1;
        playOrder[2] = (UnityEngine.Random.value < 0.5f) ? 2 : 3;

        attemptStartTime = DateTime.Now;

        attemptNumber = 1;
        currentCueTier = CueTier.None;
        ApplyCuePermissionsForAttempt();

scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        t1Sent = t2Sent = t3Sent = false;
        ShowInstructionPanel();
    }

    void Update()
    {
        if (levelTimer == null || !levelTimer.timeCounting || gameOverShown) return;
        timeRemaining = levelTimer.timeRemaining;

        int activeStage = GetActiveStageIndex();
        float tr = levelTimer.timeRemaining;
int secLeft = Mathf.FloorToInt(tr);

if (!t1Sent && (secLeft == 20 || tr < 20f))
{
    scoreAtT1 = Level3Object.score;
    t1Sent = true;
}
if (!t2Sent && (secLeft == 10 || tr < 10f))
{
    scoreAtT2 = Level3Object.score;
    t2Sent = true;
}
if (!t3Sent && (secLeft <= 0 || tr <= 0f))
{
    scoreAtT3 = Level3Object.score;
    t3Sent = true;
}


        if (enableBlinking && currentCueTier >= CueTier.OneCue && bloom != null)
        {
            EnsureVolumeEnabled();
            if (levelTimer.timeRemaining < 20f && levelTimer.timeRemaining >= 0f)
            {
                float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
                float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
                bloom.intensity.Override(intensity);
            }
            else bloom.intensity.Override(minIntensity);
        }

        int targetCount = Mathf.RoundToInt(stageMaxScores[activeStage]);
        
        if (!isGameOver && (Level3Object.GetCurrentStageCount(activeStage) >= targetCount || levelTimer.timeRemaining <= 0f))
        {
       
            stageCompleted = true;
            TheGameover();
        }
    }

    private void EnsureVolumeEnabled()
    {
        if (VOLUME == null) return;
        if (!VOLUME.enabled) VOLUME.enabled = true;
        if (VOLUME.weight < 0.99f) VOLUME.weight = 1f;
    }

    public int GetActiveStageIndex()
    {
        int idx = Mathf.Clamp(currentStage, 0, playOrder.Length - 1);
        return playOrder[idx];
    }

    public void TheGameover()
    {
        if (isGameOver) return;
        isGameOver = true;
        gameOverShown = true;

      int activeStage = GetActiveStageIndex();
RecordStagePerformance(activeStage, Level3Object.score);

// ensure snapshots exist
if (!t1Sent) scoreAtT1 = Level3Object.score;
if (!t2Sent) scoreAtT2 = Level3Object.score;
if (!t3Sent) scoreAtT3 = Level3Object.score;

string pid = !string.IsNullOrEmpty(playerName) ? playerName : PlayerDataLogger.CurrentPlayerID ?? "Player";
int csvStageAppearance = currentStage + 1;

if (LocalCSVLogger.Instance != null)
{
    try
    {
        stageStopTime = DateTime.Now;
        LocalCSVLogger.Instance.SaveStageTimestamps(
            pid,
            levelId,
            csvStageAppearance,
            GetCueTypeLabel(),
            scoreAtT1,
            scoreAtT2,
            scoreAtT3,
            stageMaxScores[GetActiveStageIndex()],
            stageStartTime,
            stageStopTime
        );
    }
    catch { }
}


     int lastStageIndex = playOrder.Length - 1;
if (currentStage == lastStageIndex)
{
    ComputeFinalPerformance();
    attemptStopTime = DateTime.Now;

    if (LocalCSVLogger.Instance != null)
    {
        try
        {
            LocalCSVLogger.Instance.SaveAttemptPFF(
                pid,
                levelId,
                attemptNumber,
                GetCueTypeLabel(),
                Pff,
                attemptStartTime,
                attemptStopTime
            );
        }
        catch { }
    }
}


        if (levelTimer != null) levelTimer.timeCounting = false;
        if (hand != null) hand.SetActive(false);

        score_gameover.gameObject.SetActive(true);
        panel_gameover.SetActive(true);

        int timeUsed = Mathf.FloorToInt(stageTimes[activeStage] - timeRemaining);
        int minutes = timeUsed / 60;
        int seconds = timeUsed % 60;
        score_gameover.text = $"Score: {Level3Object.score:F1}   Time : {minutes:00}:{seconds:00}";

        Level3Object.ResetCounts();
    }

    public void OnInstructionNextClick()
    {
        timertext.enabled = true;
        HandPositionReceiver receiver = FindObjectOfType<HandPositionReceiver>();
        if (receiver != null) receiver.RefreshMapping(cam01);

        int activeStage = GetActiveStageIndex();
        if (activeStage >= 0 && activeStage < stageInstructions.Length)
        {
            instructionText.text = stageInstructions[activeStage][0];
        }

        panel_instruction.SetActive(false);
        StartCoroutine(BeginStage());
    }

    IEnumerator BeginStage()
    {
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
       

        levelTimer.timeRemaining = stageTimes[GetActiveStageIndex()];
        levelTimer.timeCounting = false;

        Time.timeScale = 0f;
        startPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        startPrompt.SetActive(false);
        Time.timeScale = 1f;
        stageStartTime = DateTime.Now;
        counterText.enabled = true;
        counterText.text = $"Score : {Level3Object.score:F1}";
        hand.SetActive(true);

        gameOverShown = false;
        isGameOver = false;
        stageCompleted = false;

        levelTimer.timeCounting = true;
        UpdateLayersForStage();
        ApplyCuePermissionsForAttempt();
    }

    public void NextStage()
    {
        panel_gameover.SetActive(false);
        panel_failedlevel.SetActive(false);
        hand.SetActive(false);
        counterText.enabled = false;
        timertext.enabled = false;

        int lastStageIndex = playOrder.Length - 1;

        if (currentStage == lastStageIndex)
        {
            ComputeFinalPerformance();
            lastAttemptPassed = (Pff > passThreshold);
            Debug.Log($"Pff={Pff:F1} threshold={passThreshold} tier={currentCueTier}");

            if (currentCueTier == CueTier.None)
            {
                if (lastAttemptPassed)
                {
                    player_3.SetActive(false);
                    player_4.SetActive(true);
                    level3_assest.SetActive(true);
                }
                else
                {
                    panel_failedlevel.SetActive(true);
                    failedLevelText.text = $"Level failed..\nTry again(Glow Cue).\nTotalScore = {Pff:F1}%";
                }
            }
            else if (currentCueTier == CueTier.OneCue)
            {
                if (lastAttemptPassed)
                    StartClosedLoopRun(CueTier.None, "passed_with_one_cue_validation");
                else
                {
                    panel_failedlevel.SetActive(true);
                    failedLevelText.text = $"Level failed..\nTry again(Glow +Arrow Cue).\nTotalScore = {Pff:F1}%";
                }
            }
            else
            {
                if (lastAttemptPassed)
                    StartClosedLoopRun(CueTier.OneCue, "passed_with_two_cues_stepdown");
                else
                {
                    panel_failedlevel.SetActive(true);
                    failedLevelText.text = $"Level failed..\nTry again..\nTotalScore = {Pff:F1}%";
                }
            }
            return;
        }

        if (stageGroups.Length > 0) stageGroups[GetActiveStageIndex()].SetActive(false);
        currentStage++;
        if (stageGroups.Length > 0) stageGroups[GetActiveStageIndex()].SetActive(true);
        ShowInstructionPanel();
    }

    public void ShowInstructionPanel()
    {
        int activeStage = GetActiveStageIndex();


for (int i = 0; i < stageGroups.Length; i++)
{
    if (stageGroups[i] != null)
        stageGroups[i].SetActive(i == activeStage);
}

        counterText.enabled = true;
        counterText.text = "Score : 0";
        timertext.text = "Time : 00:00";

        if (currentStage < 2)
        {
            panel_instruction.SetActive(true);
        }

        int activeStageIndex = GetActiveStageIndex();

        if (activeStageIndex >= 0 && activeStageIndex < stageInstructions.Length)
        {
            instructionText.text = stageInstructions[activeStageIndex][0];
        }
        else
        {
            instructionText.text = "Collect the relevant objects for this stage.";
        }


    }

    public void ResetAllGroupMaterialsForNextAttempt()
    {
        ResetGroup(mangoRenderers, mangoMaterial);
        ResetGroup(appleRenderers, appleMaterial);
    }
// resetting for next attempt
    private void ResetGroup(List<Renderer> renderers, Material mat)
    {
        if (renderers == null || mat == null) return;
        foreach (var r in renderers)
        {
            if (r == null) continue;
            var shared = r.sharedMaterials;
            for (int i = 0; i < shared.Length; i++) shared[i] = mat;
            r.sharedMaterials = shared;
            var outline = r.GetComponent<Outline>();
            if (outline) outline.enabled = false;
        }
    }

    private void ApplyCuePermissionsForAttempt()
    {
        bool allowBlink = (currentCueTier >= CueTier.OneCue);
        bool allowArrow = (currentCueTier >= CueTier.TwoCues);
        bloomActiveThisAttempt = allowBlink;

        Level3Object[] objs = FindObjectsOfType<Level3Object>(true);
        foreach (var obj in objs)
            obj.SetCuePermissions(allowBlink, allowArrow);
    }

    private void SetLayerRecursively(Transform objTransform, int targetLayer)
    {
        objTransform.gameObject.layer = targetLayer;
        foreach (Transform child in objTransform)
        {
            SetLayerRecursively(child, targetLayer);
        }
    }

    private void RecordStagePerformance(int stageIndex, float rawScore)
    {
        float denom = (stageIndex >= 0 && stageIndex < stageMaxScores.Length) ? stageMaxScores[stageIndex] : 8f;
        float pct = Mathf.Clamp((rawScore / denom) * 100f, 0f, 100f);

        int playPos = -1;
        for (int i = 0; i < playOrder.Length; i++)
        {
            if (playOrder[i] == stageIndex) { playPos = i; break; }
        }

        if (playPos == 0) pf1 = pct;
        else if (playPos == 1) pf2 = pct;
        else if (playPos == 2) pf3 = pct;
    }

    private void ComputeFinalPerformance()
    {
        Pff = (0.2f * pf1) + (0.3f * pf2) + (0.5f * pf3);
        Debug.Log($"PFs: {pf1}, {pf2}, {pf3} → PFF = {Pff:F1}%");
    }

    public void UpdateLayersForStage()
    {
        int active = GetActiveStageIndex();
        Level3Object[] objs = FindObjectsOfType<Level3Object>(true);
        foreach (var obj in objs)
        {
            bool isRelevant = IsRelevantObject(obj, active);
            int targetLayer = isRelevant ? LayerMask.NameToLayer("RelevantGlow") : LayerMask.NameToLayer("Fruit");
            SetLayerRecursively(obj.transform, targetLayer);
        }
    }

    private bool IsRelevantObject(Level3Object obj, int stage)
    {
        switch (stage)
        {
            case 0: return obj.objectType == Level3Object.Level3ObjectType.Mango;
            case 1: return obj.objectType == Level3Object.Level3ObjectType.Apple;
            case 2: return obj.objectType == Level3Object.Level3ObjectType.Sunflower;
            case 3: return obj.objectType == Level3Object.Level3ObjectType.Rose;
            default: return false;
        }
    }

    public void OnFailedRestartClick()
    {
        panel_failedlevel.SetActive(false);

        CueTier next = currentCueTier;
        if (currentCueTier == CueTier.None) next = CueTier.OneCue;
        else if (currentCueTier == CueTier.OneCue) next = CueTier.TwoCues;

        attemptNumber++;

        foreach (Level3Object obj in FindObjectsOfType<Level3Object>(true))
        {
            obj.gameObject.SetActive(true);
            obj.ResetForRetry();
        }

        StartClosedLoopRun(next, "manual_restart_from_failed_panel");
    }
//Attempt next
    private void StartClosedLoopRun(CueTier nextTier, string reason)
    {
        Debug.Log($"ClosedLoop → Attempt #{attemptNumber} starting with {nextTier} ({reason})");

        if (uiFixer != null) uiFixer.RestoreOriginalUI();

        if (bloom != null) bloom.intensity.Override(minIntensity);
        ResetAllGroupMaterialsForNextAttempt();
        Level3Object.ResetCounts();

        pf1 = pf2 = pf3 = pf4 = 0f;
        Pff = 0f;

        currentCueTier = nextTier;
        ApplyCuePermissionsForAttempt();

        foreach (var g in stageGroups) if (g != null) g.SetActive(false);
        currentStage = 0;
        if (stageGroups.Length > 0) stageGroups[GetActiveStageIndex()].SetActive(true);

       

        hand.transform.localPosition = initialHandPosition;
        hand.transform.localRotation = initialHandRotation;
        hand.SetActive(false);

        Level3Object[] allObjects = FindObjectsOfType<Level3Object>(true);
        foreach (var obj in allObjects)
        {
            obj.gameObject.SetActive(true);
            obj.ResetForRetry();
        }

        if (levelTimer != null)
        {
            levelTimer.timeCounting = false;
            levelTimer.timeRemaining = stageTimes[GetActiveStageIndex()];
        }

        ApplyCuePermissionsForAttempt();
t1Sent = t2Sent = t3Sent = false;
scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
attemptStartTime = DateTime.Now;

        FindObjectOfType<MyController>()?.ResetCameraAndPlayer();
        FindObjectOfType<MyController>()?.ReplayIntroAnimation();
        FindObjectOfType<MidpointAnimatorHelper>()?.PlayFromMarkedMidpoint();

        ShowInstructionPanel();
    }
// related with csv file
    private string GetCueTypeLabel()
    {
        switch (currentCueTier)
        {
            case CueTier.None: return "NO CUE";
            case CueTier.OneCue: return "glow/blink";
            case CueTier.TwoCues: return "glow/blink +arrow";
            default: return "NO CUE";
        }
    }
    public void UpdateObjectCountUI()
{
    if (counterText != null)
    {
        string safeText = "Score : " + Level3Object.score.ToString("0.0");
        counterText.SetText(safeText, true);
    }

    if (mangoCountText != null)
        mangoCountText.SetText(Level3Object.mangoCount.ToString(), true);
    if (appleCountText != null)
        appleCountText.SetText(Level3Object.appleCount.ToString(), true);
    if (SUNFLOWERCountText != null)
        SUNFLOWERCountText.SetText(Level3Object.sunflowerCount.ToString(), true);
    if (ROSECountText != null)
        ROSECountText.SetText(Level3Object.roseCount.ToString(), true);
}

}
