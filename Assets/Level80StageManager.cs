using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class Level80StageManager : MonoBehaviour
{
    [Header("Stage Setup")]
    public int stageNumber = 5; 
    public float timer = 60f;
    private bool isTimerRunning = false;
    public bool isGameOver = false;

    [Header("UI References")]
    public GameObject instructionPanel;
    public GameObject startPrompt;
    public GameObject hand;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    public Button helpButton;
    public GameObject helpPanel;
    public TextMeshProUGUI helpInstructionText;

    [Header("Help Instructions Per Stage (index by stageNumber)")]
    [TextArea] public string[] helpTexts = new string[12];

    [Header("Panels")]
    public GameObject panel_mango;
    public GameObject panel_apple;
    public GameObject panel_sunflower;
    public GameObject panel_rose;

    public Color defaultMangoTextColor = Color.yellow;
    public Color defaultRoseTextColor  = new Color(1f, 0.4f, 0.7f, 1f);

    [Header("Special Stage 10 Objects")]
    public GameObject[] extraObjectsToEnableInStage10;
    public GameObject[] objectsToDisableInStage10;

    private float score = 0f;

    [Header("Stage 9-10 Variant Objects")]
    public GameObject[] yellowMangoes;
    public GameObject[] greenMangoes;
    public GameObject[] redRoses;
    public GameObject[] blueRoses;
    public GameObject[] regularApples;
    public GameObject[] regularSunflowers;
    public GameObject[] NONGREENMANGOS10;
    public GameObject[] NONBLUEROSES10;

    public Camera cam04;

    private List<int> fullStageSequence = null;
    private List<int> selectedStages = new List<int>();
    private int currentSequenceIndex = 0;

    // limits indexed by (sNum - 5)
    public static int[] mangoLimits     = { 4, 0, 0, 2, 5, 3, 3 };
    public static int[] appleLimits     = { 5, 0, 5, 4, 3, 4, 4 };
    public static int[] sunflowerLimits = { 0, 5, 0, 3, 2, 4, 4 };
    public static int[] roseLimits      = { 0, 4, 4, 0, 4, 3, 3 };
    

    private Vector3 initialHandPos;
    private Quaternion initialHandRot;

    public TextMeshProUGUI instruction02Text;

    public readonly string[] stageInstructions = new string[]
    {
        "Pluck 4 mangoes and 5 apples",
        "Pluck 5 sunflowers and 4 roses",
        "Pluck 5 apples and 4 roses",
        "Pluck 2 mangoes, 4 apples, and 3 sunflowers",
        "Pluck 5 mangoes, 3 apples, 2 sunflowers, 4 roses",
        "Pluck 3 green mangoes, 4 apples, 3 blue roses, 4 sunflowers",
    };

    public Transform pointer;

    [Header("Bloom Pulse")]
    public PostProcessVolume VOLUME;
    private Bloom bloom;
    public bool enableBlinking = true;
    public float blinkSpeed = 15f;
    private float minIntensity = 1f;
    private float maxIntensity = 25f;

    [Header("Players/Env")]
    public GameObject level_2assest;
    public GameObject player_5;
    public GameObject player_4;
    public GameObject lightt;

    // PERFORMANCE TRACKING
    public static float pf1, pf2, pf3, pf4, pf5, pf6, pf7, pf8, pf9_, pf10;
    public static float Pff;

    [Header("Performance Settings")]
    public GameObject panel_failedlevel;
    public TextMeshProUGUI failedLevelText;
    public float passThreshold = 70f;

    public enum CueTier { None = 0, OneCue = 1, TwoCues = 2 }
    [SerializeField] private CueTier currentCueTier = CueTier.None;
    [SerializeField] private int attemptIndex = 0;
    public int attemptNumber = 1;
    private bool bloomActiveThisAttempt = false;
    private bool lastAttemptPassed = false;

    [Header("CSV Logging")]
    // map Level80StageManager to overall level id 4 (per your mapping)
    public int levelId = 4;
    private string playerName = ""; // prefer PlayerDataLogger.CurrentPlayerID fallback
    private DateTime attemptStartTime;
    private DateTime attemptStopTime;
    private DateTime stageStartTime;
    private DateTime stageStopTime;
    private bool t1Sent = false, t2Sent = false, t3Sent = false;
    private float scoreAtT1 = 0f, scoreAtT2 = 0f, scoreAtT3 = 0f;
     public AudioSource sfxSource;
public AudioClip pluckSFX;


    private void Awake()
    {
        if (hand != null) DontDestroyOnLoad(hand);
        BuildOrShuffleFullSequence();
        if (fullStageSequence != null && fullStageSequence.Count > 0)
        {
            currentSequenceIndex = 0;
            stageNumber = fullStageSequence[currentSequenceIndex];
        }
        Debug.Log($"[L80 Awake] Full sequence (debug): {string.Join(", ", fullStageSequence ?? new List<int>())}");
    }

    private void Start()
    {
         playerName = PlayerDataLogger.CurrentPlayerID ?? "Player";
        GenerateSelectedStagesForAttempt();
        if (instruction02Text != null && stageNumber - 5 >= 0 && stageNumber - 5 < stageInstructions.Length)
            instruction02Text.text = stageInstructions[stageNumber - 5];
        instructionPanel?.SetActive(true);

        hand?.SetActive(true);
        SetInitialUIState();
        ResetPerformance10();
        if (pointer != null)
        {
            initialHandPos = pointer.transform.localPosition;
            initialHandRot = pointer.transform.localRotation;
        }

        if (VOLUME != null && VOLUME.profile != null)
        {
            if (!VOLUME.profile.TryGetSettings(out bloom))
            {
                bloom = null;
            }
            else
            {
                bloom.enabled.Override(true);
                bloom.intensity.Override(minIntensity);
                VOLUME.enabled = false;
                VOLUME.weight = 0f;
            }
        }
        attemptIndex = 0;
        currentCueTier = CueTier.None;
        attemptNumber = 1;
        ApplyCuePermissionsForAttempt();
        ResetAllObjects();
        attemptStartTime = DateTime.Now;

        if (selectedStages.Count > 0)
        {
            currentSequenceIndex = 0;
            stageNumber = selectedStages[0];
        }
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        t1Sent = t2Sent = t3Sent = false;
        if (instruction02Text != null && stageNumber - 5 >= 0 && stageNumber - 5 < stageInstructions.Length)
            instruction02Text.text = stageInstructions[stageNumber - 5];
    }

    private void Update()
    {
        if (bloom != null && VOLUME != null)
        {
            bool canBlink = bloomActiveThisAttempt && timer < 20f && timer >= 0f && enableBlinking;
            if (canBlink)
            {
                VOLUME.enabled = true;
                VOLUME.weight = 1f;
                bloom.active = true;
                float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
                float computed = Mathf.Lerp(minIntensity, maxIntensity, t);
                bloom.intensity.Override(computed);
            }
            else
            {
                bloom.intensity.Override(minIntensity);
                VOLUME.enabled = false;
                VOLUME.weight = 0f;
            }
        }

        if (!isTimerRunning || isGameOver) return;

        timer -= Time.deltaTime;
        int curSecond = Mathf.Max(0, Mathf.FloorToInt(timer));
        timerText?.SetText($"Time: {curSecond}");
        scoreText?.SetText($"Score: {score:F1}");

        float tr = timer;
int secLeft = Mathf.FloorToInt(tr);

if (!t1Sent && (secLeft == 20 || tr < 20f))
{
    scoreAtT1 = score;
    t1Sent = true;
}
if (!t2Sent && (secLeft == 10 || tr < 10f))
{
    scoreAtT2 = score;
    t2Sent = true;
}
if (!t3Sent && (secLeft <= 0 || tr <= 0f))
{
    scoreAtT3 = score;
    t3Sent = true;
}


        if (CheckStageCompletion() || timer <= 0f)
        {
            TriggerGameOver();
        }
    }

    private void EnsureVolumeEnabled()
    {
        if (VOLUME == null) return;
        if (!VOLUME.enabled) VOLUME.enabled = true;
        if (VOLUME.weight < 0.99f) VOLUME.weight = 1f;
    }

    public void OnStartStage()
    {
        if (selectedStages != null && selectedStages.Count > 0)
            stageNumber = selectedStages[Mathf.Clamp(currentSequenceIndex, 0, selectedStages.Count - 1)];
        else if (fullStageSequence != null && fullStageSequence.Count > 0)
            stageNumber = fullStageSequence[Mathf.Clamp(currentSequenceIndex, 0, fullStageSequence.Count - 1)];

        var receiver = FindObjectOfType<HandPositionReceiver>();
        receiver?.RefreshMapping(cam04);

        UpdateLayersForStage();
        instructionPanel?.SetActive(false);
        StartCoroutine(BeginStage());
    }

    public void OnNextStageClick()
    {
        if (stageNumber == 10 && lightt != null)
            lightt.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        if (selectedStages != null && currentSequenceIndex >= (selectedStages.Count - 1))
        {
            ComputeFinalPerformance10();
            lastAttemptPassed = (Pff > passThreshold);
            Debug.Log($"[L80] Final Pff={Pff:F1} tier={currentCueTier} lastPassed={lastAttemptPassed}");

            if (currentCueTier == CueTier.None)
            {
                if (lastAttemptPassed)
                {
                    player_4?.SetActive(false);
                    player_5?.SetActive(true);
                    level_2assest?.SetActive(true);
                    var l5 = GetComponent<Level5StageManager>();
                    if (l5 != null) l5.enabled = true;
                }
                else
                {
                    gameOverPanel?.SetActive(false);
                    if (panel_failedlevel != null)
                    {
                        panel_failedlevel.SetActive(true);
                        failedLevelText.text = $"Level failed..\nTry again(Glow Cue).\nTotalScore = {Pff:F1}%";
                    }
                }
            }
            else if (currentCueTier == CueTier.OneCue)
            {
                if (lastAttemptPassed)
                {
                    attemptNumber++;
                    StartClosedLoopRun(CueTier.None, "passed_with_one_cue_stepdown");
                }
                else
                {
                    gameOverPanel?.SetActive(false);
                    if (panel_failedlevel != null)
                    {
                        panel_failedlevel.SetActive(true);
                        failedLevelText.text = $"Level failed..\nTry again(Glow +Arrow Cue).\nTotalScore = {Pff:F1}%";
                    }
                }
            }
            else
            {
                if (lastAttemptPassed)
                {
                    attemptNumber++;
                    StartClosedLoopRun(CueTier.OneCue, "passed_with_two_cues_stepdown");
                }
                else
                {
                    gameOverPanel?.SetActive(false);
                    if (panel_failedlevel != null)
                    {
                        panel_failedlevel.SetActive(true);
                        failedLevelText.text = $"Level failed..\nTry again.\nTotalScore = {Pff:F1}%";
                    }
                }
            }
            return;
        }

        if (selectedStages != null && selectedStages.Count > 0)
        {
            currentSequenceIndex = Mathf.Clamp(currentSequenceIndex + 1, 0, Mathf.Max(0, selectedStages.Count - 1));
            stageNumber = selectedStages[currentSequenceIndex];
        }
        else
        {
            stageNumber = Mathf.Clamp(stageNumber + 1, 5, 10);
        }

        ResetCounts();
        timer = 60f;
        isTimerRunning = false;
        isGameOver = false;
        gameOverPanel?.SetActive(false);
        timerText?.SetText("Score: 0");

        if (instruction02Text != null && stageNumber - 5 >= 0 && stageNumber - 5 < stageInstructions.Length)
            instruction02Text.text = stageInstructions[stageNumber - 5];

        instructionPanel.SetActive(true);
        EnableRelevantPanels();
        ResetAllObjects();
        ApplyCuePermissionsForAttempt();
        SetVariantObjectsForCurrentStage();
    }

    public void OnFailedRestartClick()
    {
        CueTier next = currentCueTier;
        if (currentCueTier == CueTier.None) next = CueTier.OneCue;
        else if (currentCueTier == CueTier.OneCue) next = CueTier.TwoCues;
        else next = CueTier.TwoCues;

        attemptNumber++;
        StartClosedLoopRun(next, "manual_restart_from_failed_panel");
    }

    IEnumerator BeginStage()
    {
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        
        if (startPrompt != null)
        {
            startPrompt.SetActive(true);
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(2f);
            startPrompt.SetActive(false);
        }

        Time.timeScale = 1f;
        stageStartTime = DateTime.Now;
        isGameOver = false;
        isTimerRunning = true;
        timer = 60f;

        ResetCounts();
        ResetAllObjects();
        SetVariantObjectsForCurrentStage();
        EnableRelevantPanels();

        hand?.SetActive(true);
        timerText?.gameObject.SetActive(true);
        scoreText?.gameObject.SetActive(true);

        ApplyCuePermissionsForAttempt();
        Debug.Log($"[L80] Stage {stageNumber} started. Timer active = {isTimerRunning}");
    }

   private void TriggerGameOver()
{
    stageStopTime = DateTime.Now;

    float stageScoreMade = 0f;
    int stageScoreMax = 0;

    switch (stageNumber)
    {
        case 5:
            stageScoreMade = Level80Object.mangoCount + Level80Object.appleCount;
            break;
        case 6:
            stageScoreMade = Level80Object.sunflowerCount + Level80Object.roseCount;
            break;
        case 7:
            stageScoreMade = Level80Object.appleCount + Level80Object.roseCount;
            break;
        case 8:
            stageScoreMade = Level80Object.sunflowerCount + Level80Object.appleCount + Level80Object.mangoCount;
            break;
        case 9:
            stageScoreMade = Level80Object.sunflowerCount + Level80Object.appleCount + Level80Object.mangoCount + Level80Object.roseCount;
            break;
        case 10:
            stageScoreMade = Level80Object.sunflowerCount + Level80Object.appleCount + Level80Object.mangoCount + Level80Object.roseCount;
            break;
        default:
            Debug.LogWarning($"Invalid stageNumber: {stageNumber}. Skipping GameOver.");
            break;
    }

    // compute stageScoreMax from your limits arrays (indexed by stageNumber - 5)
    int sNum = Mathf.Clamp(stageNumber, 5, 10);
    int idx = sNum - 5;
    if (idx >= 0 && idx < mangoLimits.Length &&
        idx < appleLimits.Length && idx < sunflowerLimits.Length && idx < roseLimits.Length)
    {
        stageScoreMax = mangoLimits[idx] + appleLimits[idx] + sunflowerLimits[idx] + roseLimits[idx];
    }
    else
    {
        // fallback if something's inconsistent
        stageScoreMax = (stageNumber >= 5 && stageNumber <= 8) ? 9 : 14;
    }

    Debug.Log($"[L80:StageEnd] Stage {stageNumber} | ScoreMade={stageScoreMade} / Max={stageScoreMax}");

    RecordStagePerformance(stageNumber, score);

    if (!t1Sent) scoreAtT1 = score;
    if (!t2Sent) scoreAtT2 = score;
    if (!t3Sent) scoreAtT3 = score;

    string pid = !string.IsNullOrEmpty(playerName)
        ? playerName
        : PlayerDataLogger.CurrentPlayerID ?? "Player";

    int csvStageAppearance = currentSequenceIndex + 1;

    if (LocalCSVLogger.Instance != null)
    {
        try
        {
            LocalCSVLogger.Instance.SaveStageTimestamps(
                pid,
                levelId,
                csvStageAppearance,
                GetCueTypeLabel(),
                scoreAtT1,
                scoreAtT2,
                scoreAtT3,
                stageScoreMax,
                stageStartTime,
                stageStopTime
            );
            Debug.Log($"[CSV] Stage {stageNumber} logged (T1/T2/T3) with denom={stageScoreMax}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[L80][CSV] Stage logging failed: " + ex.Message);
        }
    }

    // use selectedStages (not stageSequence) - selectedStages is the list your class uses
    if (selectedStages != null && currentSequenceIndex >= (selectedStages.Count - 1))
    {
        ComputeFinalPerformance10();
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
                Debug.Log($"[CSV] Attempt#{attemptNumber} PFF saved: {Pff:F1}%");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[L80][CSV] Attempt PFF log failed: " + ex.Message);
            }
        }
    }

    isGameOver = true;
    isTimerRunning = false;
    gameOverPanel?.SetActive(true);

    if (timer <= 0) gameOverText.text = $"Score: {score:F1}     Time : 60";
    else gameOverText.text = $"Score: {score:F1}     Time : {Mathf.Max(0, 60f - Mathf.FloorToInt(timer))}";

    Level80Object.ResetCounts();
}


    private void ApplyCuePermissionsForAttempt()
    {
        bool allowBlink = (currentCueTier >= CueTier.OneCue);
        bool allowArrow = (currentCueTier >= CueTier.TwoCues);

        bloomActiveThisAttempt = allowBlink;

        var objs = FindObjectsOfType<Level80Object>(true);
        foreach (var o in objs) o.SetCuePermissions(allowBlink, allowArrow);

        attemptIndex = (int)currentCueTier;
        if (allowBlink)
        {
            EnsureVolumeEnabled();
            if (bloom != null) bloom.intensity.Override(minIntensity);
            Debug.Log("[L80] Blink allowed this attempt — volume primed.");
        }
    }

    private void StartClosedLoopRun(CueTier nextTier, string reason)
    {
        Debug.Log($"[L80] StartClosedLoopRun attempt#{attemptNumber} tier={nextTier} reason={reason}");
        if (bloom != null) bloom.intensity.Override(minIntensity);
        if (VOLUME != null) { VOLUME.enabled = false; VOLUME.weight = 0f; }

        currentCueTier = nextTier;
        attemptIndex = (int)nextTier;

        ResetPerformance10();
        ResetCounts();

        GenerateSelectedStagesForAttempt();
        currentSequenceIndex = 0;
        if (selectedStages.Count > 0) stageNumber = selectedStages[0];

        timer = 60f;
        isGameOver = false;
        isTimerRunning = false;

        gameOverPanel?.SetActive(false);
        panel_failedlevel?.SetActive(false);

        if (instruction02Text != null && stageNumber - 5 >= 0 && stageNumber - 5 < stageInstructions.Length)
            instruction02Text.text = stageInstructions[stageNumber - 5];

        instructionPanel?.SetActive(true);
        ResetAllObjects();
        SetVariantObjectsForCurrentStage();
        ApplyCuePermissionsForAttempt();

         attemptStartTime = DateTime.Now;
     
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
    }

    public void OnHelpClick()
    {
        bool isNowActive = !helpPanel.activeSelf;
        helpPanel.SetActive(isNowActive);
        if (isNowActive && stageNumber >= 5 && stageNumber <= 10)
        {
            int idx = stageNumber - 5;
            if (idx >= 0 && idx < helpTexts.Length) helpInstructionText.text = helpTexts[idx];
        }
    }

    private void SetInitialUIState()
    {
        if (helpPanel) helpPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);
        EnableRelevantPanels();
    }

    private void EnableRelevantPanels()
    {
        bool isStage10 = (stageNumber == 10);

        panel_mango?.SetActive(true);
        panel_apple?.SetActive(true);
        panel_sunflower?.SetActive(true);
        panel_rose?.SetActive(true);

        TextMeshProUGUI mangoText = panel_mango?.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI roseText  = panel_rose?.GetComponentInChildren<TextMeshProUGUI>();
        Image mangoImg = panel_mango?.GetComponent<Image>();
        Image roseImg  = panel_rose?.GetComponent<Image>();

        if (isStage10)
        {
            if (mangoImg) mangoImg.color = new Color(0f, 1f, 0f, 1f);
            if (mangoText) mangoText.color = new Color(0f, 1f, 0f, 1f);
            if (roseImg) roseImg.color = new Color(0f, 0.5f, 1f, 1f);
            if (roseText) roseText.color = new Color(0f, 0.5f, 1f, 1f);
        }
        else
        {
            if (mangoImg) mangoImg.color = Color.white;
            if (roseImg) roseImg.color = Color.white;
            if (mangoText) mangoText.color = defaultMangoTextColor;
            if (roseText) roseText.color = defaultRoseTextColor;
        }
    }

    private void SetVariantObjectsForCurrentStage()
    {
        ToggleObjectArray(greenMangoes, false);
        ToggleObjectArray(blueRoses, false);
        ToggleObjectArray(yellowMangoes, false);
        ToggleObjectArray(redRoses, false);
        ToggleObjectArray(regularApples, false);
        ToggleObjectArray(regularSunflowers, false);
        ToggleObjectArray(NONGREENMANGOS10, false);
        ToggleObjectArray(NONBLUEROSES10, false);

        if (stageNumber == 10)
        {
            ToggleObjectArray(greenMangoes, true);
            ToggleObjectArray(blueRoses, true);
            ToggleObjectArray(NONBLUEROSES10, true);
            ToggleObjectArray(NONGREENMANGOS10, true);
            ToggleObjectArray(regularApples, true);
            ToggleObjectArray(regularSunflowers, true);
            ToggleObjectArray(objectsToDisableInStage10, false);
        }
        else
        {
            ToggleObjectArray(yellowMangoes, true);
            ToggleObjectArray(redRoses, true);
            ToggleObjectArray(regularApples, true);
            ToggleObjectArray(regularSunflowers, true);
            ToggleObjectArray(objectsToDisableInStage10, true);
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject go in allObjects)
        {
            if (go.CompareTag("Collectibles"))
            {
                bool shouldBeActive = (stageNumber == 10);
                go.SetActive(shouldBeActive);
            }
        }

        Debug.Log($"[L80] Stage {stageNumber}: Variants set. Disabled objects: {(objectsToDisableInStage10 != null ? objectsToDisableInStage10.Length : 0)}");
    }

    private void ToggleObjectArray(GameObject[] objects, bool state)
    {
        if (objects == null) return;
        foreach (GameObject go in objects) if (go != null) go.SetActive(state);
    }

    public void UpdateLayersForStage()
    {
        var allObjects = FindObjectsOfType<Level80Object>(true);
        foreach (var obj in allObjects)
        {
            bool isRelevant = IsRelevantObjectForLayer(obj);
            int targetLayer = isRelevant ? LayerMask.NameToLayer("RelevantGlow") : LayerMask.NameToLayer("Fruit");
            obj.gameObject.layer = targetLayer;
            SetLayerRecursively(obj.transform, targetLayer);
        }
    }

    
    private bool IsRelevantObjectForLayer(Level80Object obj)
    {
        int sNum = Mathf.Clamp(stageNumber, 5, 10);
        int idx = sNum - 5;
        int sunflowerLimit = sunflowerLimits[idx];
        int roseLimit = roseLimits[idx];

        switch (sNum)
        {
            case 6:
                return (obj.objectType == Level80Object.ObjectType.Sunflower && Level80Object.sunflowerCount < sunflowerLimit)
                    || (obj.objectType == Level80Object.ObjectType.Rose && Level80Object.roseCount < roseLimit);
            case 7:
                return (obj.objectType == Level80Object.ObjectType.Rose && Level80Object.roseCount < roseLimit);
            case 8:
                return (obj.objectType == Level80Object.ObjectType.Sunflower && Level80Object.sunflowerCount < sunflowerLimit);
            case 9:
            case 10:
                return (obj.objectType == Level80Object.ObjectType.Sunflower && Level80Object.sunflowerCount < sunflowerLimit)
                    || (obj.objectType == Level80Object.ObjectType.Rose && Level80Object.roseCount < roseLimit && Array.IndexOf(blueRoses, obj.gameObject) >= 0);
            default:
                return false;
        }
    }

    public void ResetCounts()
    {
        Level80Object.mangoCount = 0;
        Level80Object.appleCount = 0;
        Level80Object.sunflowerCount = 0;
        Level80Object.roseCount = 0;

        score = 0f;
        if (scoreText != null) scoreText.text = $"Score: {score:F1}";
    }

    public void ResetAllObjects()
    {
        Level80Object[] allObjects = FindObjectsOfType<Level80Object>(true);
        foreach (Level80Object obj in allObjects)
        {
            obj.gameObject.SetActive(true);
            obj.ResetVisuals();
        }
    }

    public void AddScore(float delta)
    {
        score += delta;
        if (scoreText != null) scoreText.text = $"Score: {score:F1}";
    }

    public void HandlePickup(Level80Object obj)
    {
        if (isGameOver || !isTimerRunning) return;

        int sNum = Mathf.Clamp(stageNumber, 5, 10);
        int idx = sNum - 5;
        float delta = -0.5f;

        int mangoLimit     = mangoLimits[idx];
        int appleLimit     = appleLimits[idx];
        int sunflowerLimit = sunflowerLimits[idx];
        int roseLimit      = roseLimits[idx];

        switch (obj.objectType)
        {
            case Level80Object.ObjectType.Mango:
                if (sNum == 10)
                {
                    if (obj.CompareTag("Collectibles"))
                    {
                        Level80Object.mangoCount++;
                        obj.UpdatePanelUI(Level80Object.mangoCount, mangoLimit);
                        delta = Level80Object.mangoCount <= mangoLimit ? 1f : -0.5f;
                    }
                    else delta = -0.5f;
                }
                else
                {
                    Level80Object.mangoCount++;
                    obj.UpdatePanelUI(Level80Object.mangoCount, mangoLimit);
                    delta = Level80Object.mangoCount <= mangoLimit ? 1f : -0.5f;
                }
                break;

            case Level80Object.ObjectType.Rose:
                if (sNum == 10)
                {
                    if (obj.CompareTag("Collectibles"))
                    {
                        Level80Object.roseCount++;
                        obj.UpdatePanelUI(Level80Object.roseCount, roseLimit);
                        delta = Level80Object.roseCount <= roseLimit ? 1f : -0.5f;
                    }
                    else delta = -0.5f;
                }
                else
                {
                    Level80Object.roseCount++;
                    obj.UpdatePanelUI(Level80Object.roseCount, roseLimit);
                    delta = Level80Object.roseCount <= roseLimit ? 1f : -0.5f;
                }
                break;

            case Level80Object.ObjectType.Apple:
                Level80Object.appleCount++;
                obj.UpdatePanelUI(Level80Object.appleCount, appleLimit);
                delta = Level80Object.appleCount <= appleLimit ? 1f : -0.5f;
                break;

            case Level80Object.ObjectType.Sunflower:
                Level80Object.sunflowerCount++;
                obj.UpdatePanelUI(Level80Object.sunflowerCount, sunflowerLimit);
                delta = Level80Object.sunflowerCount <= sunflowerLimit ? 1f : -0.5f;
                break;
        }

        AddScore(delta);
        obj.ShowFloatingText(delta);
        if (obj.objectType == Level80Object.ObjectType.Sunflower || obj.objectType == Level80Object.ObjectType.Rose)
            UpdateGlowForFlowerType(obj.objectType);
    }

    private void UpdateGlowForFlowerType(Level80Object.ObjectType type)
    {
        if (type != Level80Object.ObjectType.Sunflower && type != Level80Object.ObjectType.Rose) return;

        int sNum = Mathf.Clamp(stageNumber, 5, 10);
        int idx  = sNum - 5;
        int sunflowerLimit = sunflowerLimits[idx];
        int roseLimit      = roseLimits[idx];

        Level80Object[] allObjects = FindObjectsOfType<Level80Object>(true);
        foreach (var obj in allObjects)
        {
            if (obj == null) continue;
            if (obj.objectType != type) continue;

            bool stillGlow = type == Level80Object.ObjectType.Sunflower
                ? Level80Object.sunflowerCount < sunflowerLimit
                : Level80Object.roseCount < roseLimit;

            int newLayer = stillGlow ? LayerMask.NameToLayer("RelevantGlow") : LayerMask.NameToLayer("Fruit");
            obj.gameObject.layer = newLayer;
            SetLayerRecursively(obj.transform, newLayer);
        }
    }

    public bool ShouldShowOutline(Level80Object obj)
    {
        if (timer >= 20f) return false;
        int sNum = Mathf.Clamp(stageNumber, 5, 10);
        int idx = sNum - 5;

        int mangoLimit = mangoLimits[idx];
        int appleLimit = appleLimits[idx];
        int sunflowerLimit = sunflowerLimits[idx];
        int roseLimit = roseLimits[idx];

        switch (obj.objectType)
        {
            case Level80Object.ObjectType.Mango:
                if (!AllowedForStage(obj.objectType)) return false;
                if (sNum == 10 && !obj.CompareTag("Collectibles")) return false;
                return Level80Object.mangoCount < mangoLimit;
            case Level80Object.ObjectType.Apple:
                if (!AllowedForStage(obj.objectType)) return false;
                return Level80Object.appleCount < appleLimit;
            case Level80Object.ObjectType.Sunflower:
                if (!AllowedForStage(obj.objectType)) return false;
                return Level80Object.sunflowerCount < sunflowerLimit;
            case Level80Object.ObjectType.Rose:
                if (!AllowedForStage(obj.objectType)) return false;
                if (sNum == 10 && !obj.CompareTag("Collectibles")) return false;
                return Level80Object.roseCount < roseLimit;
            default:
                return false;
        }
    }

    public bool IsRelevantObject(Level80Object obj)
    {
        int sNum = Mathf.Clamp(stageNumber, 5, 10);
        int idx = sNum - 5;
        if (!AllowedForStage(obj.objectType)) return false;

        int mangoLimit = mangoLimits[idx];
        int appleLimit = appleLimits[idx];
        int sunflowerLimit = sunflowerLimits[idx];
        int roseLimit = roseLimits[idx];

        switch (obj.objectType)
        {
            case Level80Object.ObjectType.Mango:
                if (sNum == 10 && !obj.CompareTag("Collectibles")) return false;
                return Level80Object.mangoCount < mangoLimit;
            case Level80Object.ObjectType.Apple:
                return Level80Object.appleCount < appleLimit;
            case Level80Object.ObjectType.Sunflower:
                return Level80Object.sunflowerCount < sunflowerLimit;
            case Level80Object.ObjectType.Rose:
                if (sNum == 10 && !obj.CompareTag("Collectibles")) return false;
                return Level80Object.roseCount < roseLimit;
        }
        return false;
    }

    private bool AllowedForStage(Level80Object.ObjectType t)
    {
        switch (stageNumber)
        {
            case 5: return t == Level80Object.ObjectType.Mango || t == Level80Object.ObjectType.Apple;
            case 6: return t == Level80Object.ObjectType.Sunflower || t == Level80Object.ObjectType.Rose;
            case 7: return t == Level80Object.ObjectType.Apple || t == Level80Object.ObjectType.Rose;
            case 8: return t == Level80Object.ObjectType.Mango || t == Level80Object.ObjectType.Apple || t == Level80Object.ObjectType.Sunflower;
            case 9: return true;
            case 10: return true;
            default: return false;
        }
    }

    private bool CheckStageCompletion()
    {
        switch (stageNumber)
        {
            case 5:  return Level80Object.mangoCount >= 4 && Level80Object.appleCount >= 5;
            case 6:  return Level80Object.sunflowerCount >= 5 && Level80Object.roseCount >= 4;
            case 7:  return Level80Object.appleCount >= 5 && Level80Object.roseCount >= 4;
            case 8:  return Level80Object.mangoCount >= 2 && Level80Object.appleCount >= 4 && Level80Object.sunflowerCount >= 3;
            case 9:  return Level80Object.mangoCount >= 5 && Level80Object.appleCount >= 3 && Level80Object.sunflowerCount >= 2 && Level80Object.roseCount >= 4;
            case 10: return Level80Object.mangoCount >= 3 && Level80Object.appleCount >= 4 && Level80Object.sunflowerCount >= 4 && Level80Object.roseCount >= 3;
        }
        return false;
    }

    private void RecordStagePerformance(int stageIndex, float currentScore)
    {
        float maxScore = 0f;
        if (stageIndex >= 5 && stageIndex <= 8) maxScore = 9f;
        else if (stageIndex >= 9 && stageIndex <= 10) maxScore = 14f;
        if (maxScore <= 0f) { Debug.LogWarning($"[Perf] Stage {stageIndex} has invalid maxScore!"); return; }

        float pct = Mathf.Clamp((currentScore / maxScore) * 100f, 0f, 100f);
        Debug.Log($"[Perf] Stage {stageIndex} -> {pct:F1}% (score={currentScore}, max={maxScore})");

        if (!selectedStages.Contains(stageIndex)) return;
        if (pf1 == 0f) pf1 = pct;
        else if (pf2 == 0f) pf2 = pct;
        else pf3 = pct;
    }

    private void ComputeFinalPerformance10()
    {
        if (selectedStages.Count != 3) { Debug.LogWarning($"[Perf] Expected 3 selected stages, got {selectedStages.Count}"); return; }
        Pff = (0.2f * pf1) + (0.3f * pf2) + (0.5f * pf3);
        Pff = Mathf.Clamp(Pff, 0f, 100f);
        Debug.Log($"[Perf] Weighted Final Performance (Pff) = {Pff:F1}%");
    }

    public static void ResetPerformance10()
    {
        pf1 = pf2 = pf3 = pf4 = pf5 = pf6 = pf7 = pf8 = pf9_ = pf10 = 0f;
        Pff = 0f;
    }

    private void BuildOrShuffleFullSequence()
    {
        fullStageSequence = new List<int> { 5, 6, 7, 8, 9, 10 };
        for (int i = fullStageSequence.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = fullStageSequence[i];
            fullStageSequence[i] = fullStageSequence[j];
            fullStageSequence[j] = tmp;
        }
        currentSequenceIndex = 0;
        Debug.Log($"[L80] Full stage sequence (debug): {string.Join(" -> ", fullStageSequence)}");
    }

    private void GenerateSelectedStagesForAttempt()
    {
        selectedStages.Clear();
        List<int> pool = new List<int> { 5, 6, 7, 8, 9, 10 };
        for (int i = 0; i < 3; i++)
        {
            int pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            selectedStages.Add(pick);
            pool.Remove(pick);
        }
        currentSequenceIndex = 0;
        if (selectedStages.Count > 0) stageNumber = selectedStages[0];
    }

    private void SetLayerRecursively(Transform objTransform, int targetLayer)
{
    objTransform.gameObject.layer = targetLayer;
    foreach (Transform child in objTransform)
    {
        SetLayerRecursively(child, targetLayer);
    }
}


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
}
