using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class Level8StageManager : MonoBehaviour
{
    #region ===== Stage Setup =====
    [Header("Stage Setup")]
    private Level80StageManager textt;
    public int stageNumber = 1;
    public float timer = 60f;
    private bool isTimerRunning = false;
    public bool isGameOver = false;

    private float score = 0f;

    [SerializeField] private List<int> stageSequence = new List<int>();
    private int currentSequenceIndex = 0;
    #endregion

    #region ===== UI =====
    [Header("UI References")]
    public GameObject instructionPanel;
    public TextMeshProUGUI instructionText;
    public GameObject startPrompt;
    public GameObject hand;
    public GameObject hand02;

    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    public Button helpButton;
    public GameObject helpPanel;
    public TextMeshProUGUI helpInstructionText;

    [Header("Help Instructions Per Stage (index by stageNumber)")]
    [TextArea] public string[] helpTexts = new string[12];

    [Header("Optional secondary instruction label")]
    public TextMeshProUGUI instruction02Text;
    public TextMeshProUGUI instruction03Text;

    private readonly string[] stageInstructions = new string[]
    {
        "",
        "Pluck only mangoes",
        "Pluck only apples",
        "Pluck only sunflowers",
        "Pluck only roses"
    };
    #endregion

    #region ===== Panels (HUD counters) =====
    [Header("Panels")]
    public GameObject panel_mango;
    public GameObject panel_apple;
    public GameObject panel_sunflower;
    public GameObject panel_rose;
    #endregion

    #region ===== Variants (enabled per stage set) =====
    [Header("Variant Objects (used by stages 1–4)")]
    public GameObject[] yellowMangoes;
    public GameObject[] redRoses;
    public GameObject[] regularApples;
    public GameObject[] regularSunflowers;
    #endregion

    #region ===== Limits (index by stageNumber 0..4; 0 unused) =====
    public static int[] mangoLimits     = { 0, 7, 0, 0, 0 };
    public static int[] appleLimits     = { 0, 0, 7, 0, 0 };
    public static int[] sunflowerLimits = { 0, 0, 0, 6, 0 };
    public static int[] roseLimits = { 0, 0, 0, 0, 6 };
    #endregion

    #region ===== Camera / Hand receiver =====
    public Camera cam04;
    public Transform pointer;
    private Vector3 initialHandPos;
    private Quaternion initialHandRot;
    #endregion

    #region ===== Bloom Pulse / Blink Cue =====
    [Header("Bloom Pulse")]
    public PostProcessVolume VOLUME;
    private Bloom bloom;
    public bool enableBlinking = true;
    public float blinkSpeed = 15f;
    private float minIntensity = 1f;
    private float maxIntensity = 25f;
    private bool bloomActiveThisAttempt = false;
    #endregion

    #region ===== Players / Env (handoff to next level) =====
    public GameObject level_2assest;
    public GameObject level_3assest;
    public GameObject player_5;
    public GameObject player_4;
    public GameObject player_3;
    public GameObject lightt;
    #endregion

    #region ===== Performance Tracking (1..4) =====
    public static float pf1, pf2, pf3, pf4;
    public static float Pff;

    [Header("Performance Settings")]
    public float[] stageMaxScores = { 0f, 7f, 7f, 6f, 6f };

    public GameObject panel_failedlevel;
    public TextMeshProUGUI failedLevelText;
    public Button failedRestartButton;
    public float passThreshold = 70f;

    private List<int> selectedStages = new List<int>();
    #endregion

    #region ===== Attempts / Cues =====
    public enum CueTier { None = 0, OneCue = 1, TwoCues = 2 }

    [SerializeField] private CueTier currentCueTier = CueTier.None;
    public int attemptNumber = 1;
    [SerializeField] private int attemptIndex = 0;
    private bool lastAttemptPassed = false;
    #endregion

    #region ===== CSV Logging fields =====
    public int levelId = 3; // mapped to Level 3 per your mapping
    private string playerName = "";

    private float scoreAtT1 = 0f, scoreAtT2 = 0f, scoreAtT3 = 0f;
    private bool t1Sent = false, t2Sent = false, t3Sent = false;
    private DateTime stageStartTime, stageStopTime, attemptStartTime, attemptStopTime;
    #endregion

    #region ===== Materials =====
    [Header("Materials")]
    public Material mangoMaterial;
    public Material appleMaterial;
    public List<Renderer> mangoRenderers = new List<Renderer>();
    public List<Renderer> appleRenderers = new List<Renderer>();
     public AudioSource sfxSource;
public AudioClip pluckSFX;

    #endregion

    private void OnEnable()
    {
        if (instruction02Text != null && stageNumber >= 1 && stageNumber < stageInstructions.Length)
            instruction02Text.text = stageInstructions[stageNumber];
        instructionPanel.SetActive(true);
    }

    private void Awake()
    {
        if (hand != null) DontDestroyOnLoad(hand);

        stageNumber = Mathf.Clamp(stageNumber, 1, 4);

        List<int> pool = new List<int>() { 1, 2, 3, 4 };
        selectedStages.Clear();
        for (int i = 0; i < 3; i++)
        {
            int pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            selectedStages.Add(pick);
            pool.Remove(pick);
        }

        stageSequence = new List<int>(selectedStages);
        currentSequenceIndex = 0;
        stageNumber = stageSequence[0];

        Debug.Log($"[L8] Randomized 3-stage attempt: {string.Join(" → ", stageSequence)}");
    }

    private void Start()
    {
         playerName = PlayerDataLogger.CurrentPlayerID ?? "Player";
        if (instruction02Text != null && stageNumber >= 1 && stageNumber < stageInstructions.Length)
      
            instruction02Text.text = stageInstructions[stageNumber];
        instructionPanel.SetActive(true);

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
                Debug.LogWarning("[L8] Bloom not found in volume profile.");
            }
            else
            {
                bloom.enabled.Override(true);
                bloom.intensity.Override(minIntensity);
                VOLUME.enabled = false;
                VOLUME.weight = 0f;
            }
        }

        currentCueTier = CueTier.None;
        attemptIndex = 0;
        attemptNumber = 1;
        ApplyCuePermissionsForAttempt();
        attemptStartTime = DateTime.Now;
        ResetAllObjects();
scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        t1Sent = t2Sent = t3Sent = false;
        
    }

    private void Update()
    {
        if (bloom != null)
        {
            if (bloomActiveThisAttempt && timer < 20f && timer >= 0f && enableBlinking && bloom != null && bloom.enabled.value)
            {
                EnsureVolumeEnabled();
                float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
                float computed = Mathf.Lerp(minIntensity, maxIntensity, t);
                bloom.intensity.Override(computed);
            }
            else
            {
                bloom.intensity.Override(minIntensity);
                if (VOLUME != null)
                {
                    VOLUME.enabled = false;
                    VOLUME.weight = 0f;
                }
            }
        }

        if (!isTimerRunning || isGameOver) return;

        timer -= Time.deltaTime;
        int curSecond = Mathf.Max(0, Mathf.FloorToInt(timer));
        if (timerText) timerText.text = $"Time: {curSecond}";
        if (scoreText) scoreText.text = $"Score: {score:F1}";

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

    #region ===== Public UI Hooks =====
    public void OnStartStage()
    {
        if (stageSequence != null && stageSequence.Count > 0)
            stageNumber = stageSequence[Mathf.Clamp(currentSequenceIndex, 0, stageSequence.Count - 1)];

        Level8Object[] allObjects = FindObjectsOfType<Level8Object>(true);
        foreach (Level8Object obj in allObjects)
        {
            if (obj.countText != null) obj.countText.text = "0";
        }

        var receiver = FindObjectOfType<HandPositionReceiver>();
        if (receiver != null) receiver.RefreshMapping(cam04);

        UpdateLayersForStage();

        if (instructionPanel) instructionPanel.SetActive(false);
        StartCoroutine(BeginStage());
    }

    public void OnNextStageClick()
    {
        if (pointer != null)
        {
            pointer.transform.localPosition = initialHandPos;
            pointer.transform.localRotation = initialHandRot;
        }

        if (currentSequenceIndex >= stageSequence.Count - 1)
        {
            ComputeFinalPerformance4();
            lastAttemptPassed = (Pff > passThreshold);
            Debug.Log($"[L8] Final Pff={Pff:F1} tier={currentCueTier} lastPassed={lastAttemptPassed}");

            if (currentCueTier == CueTier.None)
            {
                if (lastAttemptPassed)
                {
                    level_2assest.SetActive(false);
                    level_3assest.SetActive(true);
                    if (lightt != null) lightt.transform.rotation = Quaternion.Euler(101.8f, -271.3f, 0f);
                    if (player_4) player_4.SetActive(false);
                    if (player_5) player_5.SetActive(true);
                    hand02.SetActive(true);

                    var l80 = GetComponent<Level80StageManager>();
                    if (l80 != null) l80.enabled = true;
                    this.enabled = false;
                }
                else
                {
                    if (gameOverPanel) gameOverPanel.SetActive(false);
                    if (panel_failedlevel != null)
                    {
                        panel_failedlevel.SetActive(true);
                        if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again(Glow Cue).\nTotalScore = {Pff:F1}%";
                    }
                }
            }
            else if (currentCueTier == CueTier.OneCue)
            {
                if (lastAttemptPassed)
                {
                    attemptNumber++;
                    StartClosedLoopRun(CueTier.None, "passed_with_one_cue_validation");
                }
                else
                {
                    if (gameOverPanel) gameOverPanel.SetActive(false);
                    if (panel_failedlevel != null)
                    {
                        panel_failedlevel.SetActive(true);
                        if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again(Glow +Arrow Cue).\nTotalScore = {Pff:F1}%";
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
                    if (gameOverPanel) gameOverPanel.SetActive(false);
                    if (panel_failedlevel != null)
                    {
                        panel_failedlevel.SetActive(true);
                        if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again.\nTotalScore = {Pff:F1}%";
                    }
                }
            }

            return;
        }

        currentSequenceIndex = Mathf.Clamp(currentSequenceIndex + 1, 0, Mathf.Max(0, stageSequence.Count - 1));
        stageNumber = stageSequence[currentSequenceIndex];

        ResetCounts();
        isGameOver = false;
        isTimerRunning = false;
        timer = 60f;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (timerText) timerText.text = "Time: 60";

        if (instruction02Text != null && stageNumber >= 1 && stageNumber < stageInstructions.Length)
            instruction02Text.text = stageInstructions[stageNumber];

        if (instructionText) instructionText.text = $"Level 3:Stage {stageNumber}";
        if (instructionPanel) instructionPanel.SetActive(true);

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

        Level8Object[] all = FindObjectsOfType<Level8Object>(true);
        foreach (var o in all)
        {
            if (o != null)
            {
                o.gameObject.SetActive(true);
                o.ResetVisuals();
            }
        }

        StartClosedLoopRun(next, "manual_restart_from_failed_panel");
    }
    #endregion

    #region ===== Internal Flow =====
    IEnumerator BeginStage()
    {
         t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        Time.timeScale = 0f;
        if (startPrompt) startPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        if (startPrompt) startPrompt.SetActive(false);
        Time.timeScale = 1f;

        stageStartTime = DateTime.Now;


        isGameOver = false;
        isTimerRunning = true;
        timer = 60f;

        ResetCounts();
        ResetAllObjects();
        SetVariantObjectsForCurrentStage();
        EnableRelevantPanels();

        if (hand) hand.SetActive(true);
        if (instructionText) instructionText.text = $"Level 3:Stage {stageNumber}";
        if (instructionText) instructionText.enabled = true;
        if (timerText) timerText.gameObject.SetActive(true);
        if (scoreText) scoreText.gameObject.SetActive(true);

        ApplyCuePermissionsForAttempt();
    }

    private void TriggerGameOver()
    {
        float stageScoreMade = 0f;

        switch (stageNumber)
        {
            case 1: stageScoreMade = Level8Object.mangoCount; break;
            case 2: stageScoreMade = Level8Object.appleCount; break;
            case 3: stageScoreMade = Level8Object.sunflowerCount; break;
            case 4: stageScoreMade = Level8Object.roseCount; break;
            default:
                Debug.LogWarning($"[L8] Invalid stageNumber: {stageNumber}. Skipping GameOver.");
                return;
        }

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
        stageStopTime = DateTime.Now;
        LocalCSVLogger.Instance.SaveStageTimestamps(
            pid,
            levelId,
            csvStageAppearance,
            GetCueTypeLabel(),
            scoreAtT1,
            scoreAtT2,
            scoreAtT3,
            Mathf.RoundToInt(stageMaxScores[stageNumber]),
            stageStartTime,
            stageStopTime
        );
    }
    catch (Exception) { }
}


        if (currentSequenceIndex >= stageSequence.Count - 1)
{
    ComputeFinalPerformance4();
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
        catch (Exception) { }
    }
}


        isGameOver = true;
        isTimerRunning = false;

        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (timer <= 0) gameOverText.text = $"Score: {score:F1}     Time : 60";
        else gameOverText.text = $"Score: {score:F1}     Time : {Mathf.Max(0, 60f - Mathf.FloorToInt(timer))}";
    }
    #endregion

    #region ===== Cues / Attempts =====
    private void ApplyCuePermissionsForAttempt()
    {
        bool allowBlink = (currentCueTier >= CueTier.OneCue);
        bool allowArrow = (currentCueTier >= CueTier.TwoCues);

        bloomActiveThisAttempt = allowBlink;

        var objs = FindObjectsOfType<Level8Object>(true);
        foreach (var o in objs)
        {
            if (o != null) o.SetCuePermissions(allowBlink, allowArrow);
        }

        attemptIndex = (int)currentCueTier;
        if (allowBlink) EnsureVolumeEnabled();
    }
    #endregion

    #region ===== Soft-restart (closed-loop run) =====
    private void StartClosedLoopRun(CueTier nextTier, string reason)
    {
        Debug.Log($"[L8] StartClosedLoopRun attempt#{attemptNumber} tier={nextTier} reason={reason}");

        if (bloom != null) bloom.intensity.Override(minIntensity);
        if (VOLUME != null) { VOLUME.enabled = false; VOLUME.weight = 0f; }

        currentCueTier = nextTier;
        attemptIndex = (int)nextTier;

        ResetPerformance10();
        ResetCounts();

        currentSequenceIndex = 0;
        stageNumber = (stageSequence != null && stageSequence.Count > 0) ? stageSequence[currentSequenceIndex] : Mathf.Clamp(stageNumber, 1, 4);

        timer = 60f;
        isGameOver = false;
        isTimerRunning = false;

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (panel_failedlevel) panel_failedlevel.SetActive(false);

        if (instruction02Text != null) instruction02Text.text = stageInstructions[stageNumber];
        if (instructionText) instructionText.text = $"Level 3:Stage {stageNumber}";
        if (instructionPanel) instructionPanel.SetActive(true);

        ResetAllObjects();
        SetVariantObjectsForCurrentStage();
        ApplyCuePermissionsForAttempt();

        if (pointer != null)
        {
            pointer.transform.localPosition = initialHandPos;
            pointer.transform.localRotation = initialHandRot;
        }

        attemptStartTime = DateTime.Now;
     
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
    }
    #endregion

    #region ===== Help / Panels / Variants =====
    public void OnHelpClick()
    {
        bool isNowActive = !helpPanel.activeSelf;
        helpPanel.SetActive(isNowActive);
        if (isNowActive && stageNumber >= 1 && stageNumber < helpTexts.Length)
            helpInstructionText.text = helpTexts[stageNumber];
    }

    private void SetInitialUIState()
    {
        if (helpPanel) helpPanel.SetActive(false);
        if (gameOverPanel) gameOverPanel.SetActive(false);

        if (instructionText) instructionText.text = $"Level 3: Stage {stageNumber}";
        EnableRelevantPanels();
    }

    private void EnableRelevantPanels()
    {
        if (panel_mango) panel_mango.SetActive(true);
        if (panel_apple) panel_apple.SetActive(true);
        if (panel_sunflower) panel_sunflower.SetActive(true);
        if (panel_rose) panel_rose.SetActive(true);
    }

    private void SetVariantObjectsForCurrentStage()
    {
        ToggleObjectArray(yellowMangoes, false);
        ToggleObjectArray(redRoses, false);
        ToggleObjectArray(regularApples, false);
        ToggleObjectArray(regularSunflowers, false);

        ToggleObjectArray(yellowMangoes, true);
        ToggleObjectArray(redRoses, true);
        ToggleObjectArray(regularApples, true);
        ToggleObjectArray(regularSunflowers, true);
    }

    private void ToggleObjectArray(GameObject[] objects, bool state)
    {
        if (objects == null) return;
        foreach (GameObject go in objects)
            if (go != null) go.SetActive(state);
    }
    #endregion

    #region ===== Layers (glow layer) =====
    public void UpdateLayersForStage()
    {
        var allObjects = FindObjectsOfType<Level8Object>(true);
        foreach (var obj in allObjects)
        {
            bool isRelevant = IsRelevantObjectForLayer(obj);
            int targetLayer = isRelevant ? LayerMask.NameToLayer("RelevantGlow") : LayerMask.NameToLayer("Fruit");
            SetLayerRecursively(obj.transform, targetLayer);
        }
    }

    private void SetLayerRecursively(Transform objTransform, int targetLayer)
    {
        objTransform.gameObject.layer = targetLayer;
        foreach (Transform child in objTransform)
        {
            SetLayerRecursively(child, targetLayer);
        }
    }

    private bool IsRelevantObjectForLayer(Level8Object obj)
    {
        int sNum = Mathf.Clamp(stageNumber, 1, 4);

        int sunflowerLimit = sunflowerLimits[sNum];
        int roseLimit = roseLimits[sNum];

        switch (sNum)
        {
            case 1:
            case 2:
                return false;
            case 3:
                return (obj.objectType == Level8Object.ObjectType.Sunflower && Level8Object.sunflowerCount < sunflowerLimit);
            case 4:
                return (obj.objectType == Level8Object.ObjectType.Rose && Level8Object.roseCount < roseLimit);
            default:
                return false;
        }
    }
    #endregion

    #region ===== Resets =====
    public void ResetCounts()
    {
        Level8Object.mangoCount = 0;
        Level8Object.appleCount = 0;
        Level8Object.sunflowerCount = 0;
        Level8Object.roseCount = 0;

        score = 0f;
        if (scoreText != null) scoreText.text = $"Score: {score:F1}";
    }

    public void ResetAllObjects()
    {
        Level8Object[] allObjects = FindObjectsOfType<Level8Object>(true);
        foreach (Level8Object obj in allObjects)
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(true);
                obj.ResetVisuals();
            }
        }
    }
    #endregion

    #region ===== Pickup / Rules =====
    public void AddScore(float delta)
    {
        score += delta;
        if (scoreText != null) scoreText.text = $"Score: {score:F1}";
    }

    public void HandlePickup(Level8Object obj)
    {
        if (isGameOver || !isTimerRunning) return;

        int sNum = Mathf.Clamp(stageNumber, 1, 4);

        float delta = -0.5f;

        int mangoLimit     = mangoLimits[sNum];
        int appleLimit     = appleLimits[sNum];
        int sunflowerLimit = sunflowerLimits[sNum];
        int roseLimit      = roseLimits[sNum];

        switch (sNum)
        {
            case 1:
                if (obj.objectType == Level8Object.ObjectType.Mango)
                {
                    Level8Object.mangoCount++;
                    obj.UpdatePanelUI(Level8Object.mangoCount, mangoLimit);
                    delta = Level8Object.mangoCount <= mangoLimit ? 1f : -0.5f;
                }
                else
                {
                    IncrementWrongCounter(obj);
                    delta = -0.5f;
                }
                break;
            case 2:
                if (obj.objectType == Level8Object.ObjectType.Apple)
                {
                    Level8Object.appleCount++;
                    obj.UpdatePanelUI(Level8Object.appleCount, appleLimit);
                    delta = Level8Object.appleCount <= appleLimit ? 1f : -0.5f;
                }
                else
                {
                    IncrementWrongCounter(obj);
                    delta = -0.5f;
                }
                break;
            case 3:
                if (obj.objectType == Level8Object.ObjectType.Sunflower)
                {
                    Level8Object.sunflowerCount++;
                    obj.UpdatePanelUI(Level8Object.sunflowerCount, sunflowerLimit);
                    delta = Level8Object.sunflowerCount <= sunflowerLimit ? 1f : -0.5f;
                }
                else
                {
                    IncrementWrongCounter(obj);
                    delta = -0.5f;
                }
                break;
            case 4:
                if (obj.objectType == Level8Object.ObjectType.Rose)
                {
                    Level8Object.roseCount++;
                    obj.UpdatePanelUI(Level8Object.roseCount, roseLimit);
                    delta = Level8Object.roseCount <= roseLimit ? 1f : -0.5f;
                }
                else
                {
                    IncrementWrongCounter(obj);
                    delta = -0.5f;
                }
                break;
        }

        AddScore(delta);
        obj.ShowFloatingText(delta);
    }

    private void IncrementWrongCounter(Level8Object obj)
    {
        switch (obj.objectType)
        {
            case Level8Object.ObjectType.Mango:
                Level8Object.mangoCount++;
                obj.UpdatePanelUI(Level8Object.mangoCount, 0);
                break;
            case Level8Object.ObjectType.Apple:
                Level8Object.appleCount++;
                obj.UpdatePanelUI(Level8Object.appleCount, 0);
                break;
            case Level8Object.ObjectType.Sunflower:
                Level8Object.sunflowerCount++;
                obj.UpdatePanelUI(Level8Object.sunflowerCount, 0);
                break;
            case Level8Object.ObjectType.Rose:
                Level8Object.roseCount++;
                obj.UpdatePanelUI(Level8Object.roseCount, 0);
                break;
        }
    }

    public bool ShouldShowOutline(Level8Object obj)
    {
        if (timer >= 20f) return false;
        return IsRelevantObject(obj);
    }

    public bool IsRelevantObject(Level8Object obj)
    {
        int sNum = Mathf.Clamp(stageNumber, 1, 4);

        int mangoLimit     = mangoLimits[sNum];
        int appleLimit     = appleLimits[sNum];
        int sunflowerLimit = sunflowerLimits[sNum];
        int roseLimit = roseLimits[sNum];

        switch (sNum)
        {
            case 1:
                return obj.objectType == Level8Object.ObjectType.Mango && Level8Object.mangoCount < mangoLimit;
            case 2:
                return obj.objectType == Level8Object.ObjectType.Apple && Level8Object.appleCount < appleLimit;
            case 3:
                return obj.objectType == Level8Object.ObjectType.Sunflower && Level8Object.sunflowerCount < sunflowerLimit;
            case 4:
                return obj.objectType == Level8Object.ObjectType.Rose && Level8Object.roseCount < roseLimit;
            default:
                return false;
        }
    }
    #endregion

    #region ===== Completion =====
    private bool CheckStageCompletion()
    {
        switch (stageNumber)
        {
            case 1: return Level8Object.mangoCount >= 7;
            case 2: return Level8Object.appleCount >= 7;
            case 3: return Level8Object.sunflowerCount >= 6;
            case 4: return Level8Object.roseCount >= 6;
            default: return false;
        }
    }
    #endregion

    #region ===== Performance =====
    private void RecordStagePerformance(int stageIndex, float currentScore)
    {
        if (stageIndex < 1 || stageIndex > 4) return;

        float maxScore = Mathf.Clamp(stageMaxScores[stageIndex], 1f, 1000f);
        float pct = Mathf.Clamp((currentScore / maxScore) * 100f, 0f, 100f);

        Debug.Log($"[Perf L8] Stage {stageIndex} -> {pct:F1}% (score={currentScore}, max={maxScore})");

        if (!selectedStages.Contains(stageIndex)) return;

        if (pf1 == 0f) pf1 = pct;
        else if (pf2 == 0f) pf2 = pct;
        else if (pf3 == 0f) pf3 = pct;
    }

    private void ComputeFinalPerformance4()
    {
        if (selectedStages.Count != 3)
        {
            Debug.LogWarning($"[Perf L8] Expected 3 selected stages, got {selectedStages.Count}");
            return;
        }

        Pff = (0.2f * pf1) + (0.3f * pf2) + (0.5f * pf3);
        Pff = Mathf.Clamp(Pff, 0f, 100f);
        Debug.Log($"[Perf L8] Weighted Final Performance (Pff) = {Pff:F1}%");
    }

    public static void ResetPerformance10()
    {
        pf1 = pf2 = pf3 = pf4 = 0f;
        Pff = 0f;
    }
    #endregion

    #region ===== Helpers =====
    private void BuildOrShuffleStageSequence()
    {
        if (stageSequence == null) stageSequence = new List<int>();
        bool validInspectorSequence = (stageSequence.Count == 4);
        if (validInspectorSequence)
        {
            var set = new HashSet<int>(stageSequence);
            validInspectorSequence = set.SetEquals(new HashSet<int> { 1, 2, 3, 4 });
        }

        if (!validInspectorSequence)
        {
            stageSequence = new List<int> { 1, 2, 3, 4 };
            ShuffleList(stageSequence);
        }
        currentSequenceIndex = 0;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private void EnsureVolumeEnabled()
    {
        if (VOLUME == null) return;
        if (!VOLUME.enabled) VOLUME.enabled = true;
        if (VOLUME.weight < 0.99f) VOLUME.weight = 1f;
    }

    public void ResetAllGroupMaterialsForNextAttempt()
    {
        ResetGroup(mangoRenderers, mangoMaterial);
        ResetGroup(appleRenderers, appleMaterial);
    }

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
    #endregion

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
