using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using TMPro;
using UnityEngine.UI;

public class Level5StageManager : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject instructionPanel;
    public TextMeshProUGUI instructionPanelText;
    public GameObject startPrompt;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverPanel_score;
    public TextMeshProUGUI stagetext;

    [Header("Debug UI (optional)")]
    public UnityEngine.UI.Text debugPFText;

    [Header("Game Objects")]
    public Transform pointer;
    public GameObject hand;
    [Tooltip("Assign all mango GameObjects (even inactive ones)")]
    public GameObject[] mangoes;
    public GameObject[] apples;
    public GameObject[] sunflowers;
    public GameObject[] roses;
    public GameObject BeeFlight1Controller;
    public GameObject[] permanentlyDisabledObjects;
    public GameObject[] error;

    [Header("Floating Text Prefabs")]
    public GameObject floatingTextPlusPrefab;
    public GameObject floatingTextMinusPrefab;
    public GameObject floatingTextMinusOnePrefab;

    [Header("UI Offset")]
    public Canvas uiCanvas;
    public Vector3 screenOffset = new Vector3(30f, 0f, 0f);
    public Vector3 worldOffset = new Vector3(0f, 0.5f, 0f);

    [Header("Stage Settings")]
    [Range(1, 12)] public int stageNumber = 1;

    [Header("Pickup Counters")]
    public TextMeshProUGUI mangoCountText;
    public TextMeshProUGUI appleCountText;
    public TextMeshProUGUI sunflowerCountText;
    public TextMeshProUGUI roseCountText;

    [Header("Level Assets")]
    public GameObject level5_assest;
    public GameObject level4_assest;
    public GameObject player_5;
    public GameObject player_6;

    [Header("Performance UI")]
    public GameObject panel_failedlevel;
    public TextMeshProUGUI failedLevelText;

    public Button helpButton;
    public GameObject helpPanel;
    public TextMeshProUGUI helpInstructionText;
    [TextArea] public string[] helpTexts = new string[12];

    [Header("Post Processing")]
    public PostProcessVolume VOLUME;
    private Bloom bloom;
    public bool enableBlinking = true;
    public float blinkSpeed = 15f;
    private float minIntensity = 1f;
    private float maxIntensity = 12f;

    [Header("Timing")]
    public float stageDuration = 60f;

    // internal state
    private List<Level5Object> allObjects = new List<Level5Object>();
    private Level5Object.Type currentInstructionType;
    private float timer;
    private float score = 0f;
    private bool isTimerRunning = false;
    public bool isGameOver = false;
    private Vector3 initialHandPos;
    private Quaternion initialHandRot;
    private int mangoCount = 0, appleCount = 0, sunflowerCount = 0, roseCount = 0;
    public Camera cam05;

    private readonly string[] STAGETEXTINSTRUTION = {
        "Stage 0","Stage 1","Stage 2","Stage 3","Stage 4",
        "Stage 5","Stage 6","Stage 7","Stage 8",
        "Stage 9","Stage 10","Stage 11","Stage 12"
    };

    // Attempts & PF
    public enum CueTier { None = 0, OneCue = 1, TwoCues = 2 }
    [SerializeField] private CueTier currentCueTier = CueTier.None;
    [SerializeField] private int attemptIndex = 0;
    public int attemptNumber = 1;
    private bool bloomActiveThisAttempt = false;
    private bool lastAttemptPassed = false;

    // SELECTED STAGES (only these 3 play per attempt)
    private List<int> selectedStages = new List<int>();
    private int currentSelectedIdx = 0;
    
    public static float pf1 = 0f, pf2 = 0f, pf3 = 0f;
    public static float Pff = 0f;
    public float passThreshold = 70f;

    // per-stage PF counters
    private int mangoCorrectNoBee = 0;
    private int appleCorrectNoBee = 0;
    private int sunflowerCorrectNoBee = 0;
    private int roseCorrectNoBee = 0;

    private int mangoInitialNoBee = 0;
    private int appleInitialNoBee = 0;
    private int sunflowerInitialNoBee = 0;
    private int roseInitialNoBee = 0;

    [Header("CSV Logging")]
    public int levelId = 5;
    private string playerName = "Player";
    private DateTime attemptStartTime;
    private DateTime attemptStopTime;
    private DateTime stageStartTime;
    private DateTime stageStopTime;
    private bool t1Sent = false, t2Sent = false, t3Sent = false;
    private float scoreAtT1 = 0f, scoreAtT2 = 0f, scoreAtT3 = 0f;

    private bool bloomGloballyActive = false;
    public AudioSource sfxSource;
    public AudioClip pluckSFX;

    // ---------- NEW: CSV duplicate-prevention bookkeeping ----------
    // ensure we don't write the same stage row multiple times per attempt
    private HashSet<int> savedStagesThisAttempt = new HashSet<int>();
    // guard for attempt-level PFF write (only once per attempt)
    private bool attemptPffSaved = false;
    // ----------------------------------------------------------------

    void Start()
    {
        playerName = PlayerDataLogger.CurrentPlayerID ?? "Player";
        if (VOLUME != null && VOLUME.profile != null && VOLUME.profile.TryGetSettings(out bloom))
        {
            bloom.enabled.Override(true);
            bloom.intensity.Override(minIntensity);
            VOLUME.enabled = false;
            VOLUME.weight = 0f;
        }

        if (pointer != null)
        {
            initialHandPos = pointer.transform.localPosition;
            initialHandRot = pointer.transform.localRotation;
        }

        ComputeInitialRelevantNoBeeCounts();
        if (helpPanel != null) helpPanel.SetActive(false);
        if (helpButton != null)
        {
            helpButton.onClick.RemoveListener(OnHelpClick);
            helpButton.onClick.AddListener(OnHelpClick);
        }

        GenerateSelectedStagesForAttempt();
        currentSelectedIdx = 0;
        if (selectedStages.Count > 0) stageNumber = selectedStages[currentSelectedIdx];
        Debug.Log($"[L5] Selected stages for attempt #{attemptNumber}: {string.Join(", ", selectedStages)}");

        ActivateStageObjects();
        RegisterObjects();
        SetInitialTimerForStage();
        StartCoroutine(FixErrorLater());

        currentCueTier = CueTier.None;
        attemptIndex = 0;
        attemptNumber = 1;
        ApplyCuePermissionsForAttempt();

        SetInstructionPanelText();
        if (stagetext != null) { stagetext.text = STAGETEXTINSTRUTION[stageNumber]; stagetext.enabled = true; }

        attemptStartTime = DateTime.Now;

        // ---------- NEW: initialize CSV bookkeeping for this attempt ----------
        savedStagesThisAttempt.Clear();
        attemptPffSaved = false;
        // --------------------------------------------------------------------
    }

    private void OnDestroy()
    {
        if (helpButton != null)
            helpButton.onClick.RemoveListener(OnHelpClick);
    }

    void Update()
    {
        if (bloomActiveThisAttempt && timer < 20f && timer >= 0f && enableBlinking && bloom != null && bloom.enabled.value)
        {
            if (VOLUME != null) { VOLUME.enabled = true; VOLUME.weight = 1f; }
            float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            bloom.intensity.Override(intensity);
        }
        else if (bloom != null)
        {
            bloom.intensity.Override(minIntensity);
            if (VOLUME != null)
            {
                VOLUME.enabled = false;
                VOLUME.weight = 0f;
            }
        }

        if (isGameOver || !isTimerRunning) return;

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = $"Time: {Mathf.Max(0, Mathf.FloorToInt(timer))}";
        if (scoreText != null) scoreText.text = $"Score: {score:F1}";

        int secLeft = Mathf.FloorToInt(timer);
        if (!t1Sent && secLeft == 20) { scoreAtT1 = score; t1Sent = true; }
        if (!t2Sent && secLeft == 10) { scoreAtT2 = score; t2Sent = true; }
        if (!t3Sent && secLeft == 0) { scoreAtT3 = score; t3Sent = true; }

        if (timer <= 0f || AllCurrentInstructionTypeObjectsHaveBees())
        {
            EndLevel();
        }
    }

    public void OnFirstNextClick()
    {
        helpButton.gameObject.SetActive(true);
        HandPositionReceiver receiver = FindObjectOfType<HandPositionReceiver>();
        if (receiver != null) receiver.RefreshMapping(cam05);
        instructionPanel.SetActive(false);
        StartCoroutine(BeginStage());
    }

    IEnumerator BeginStage()
    {
        ActivateStageObjects();
        RegisterObjects();

        ComputeInitialRelevantNoBeeCounts();

        var beeScript = BeeFlight1Controller?.GetComponent<BeeFlight1>();
        if (beeScript != null)
        {
            beeScript.StopAllCoroutines();
            beeScript.ResetAllBees();
        }

        if (pointer != null)
        {
            pointer.transform.localPosition = initialHandPos;
            pointer.transform.localRotation = initialHandRot;
        }

        Time.timeScale = 0f;
        if (startPrompt != null) startPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(1.8f);
        if (startPrompt != null) startPrompt.SetActive(false);
        Time.timeScale = 1f;

        if (timerText != null) timerText.gameObject.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (hand != null) hand.SetActive(true);

        currentInstructionType = GetInstructionTypeForStage(stageNumber);

        Level5Object.ActiveObjectTypes.Clear();
        foreach (var objType in GetActiveObjectTypesForStage(stageNumber))
            Level5Object.ActiveObjectTypes.Add(objType);

        ResetPerStageCounts();
        ComputeInitialRelevantNoBeeCounts();

        isTimerRunning = true;
        isGameOver = false;
        timer = stageDuration;
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        stageStartTime = DateTime.Now;

        RefreshBeesForStage();

        UpdateLayersForStage();
        ApplyCuePermissionsForAttempt();

        Debug.Log($"[L5] Stage {stageNumber} begin. initialNoBee M:{mangoInitialNoBee} A:{appleInitialNoBee} S:{sunflowerInitialNoBee} R:{roseInitialNoBee}");
    }

    private Level5Object.Type GetInstructionTypeForStage(int stage) => stage switch
    {
        1 => Level5Object.Type.Mango,
        2 => Level5Object.Type.Apple,
        3 => Level5Object.Type.Sunflower,
        4 => Level5Object.Type.Rose,
        5 => Level5Object.Type.Mango,
        6 => Level5Object.Type.Apple,
        7 => Level5Object.Type.Sunflower,
        8 => Level5Object.Type.Rose,
        9 => Level5Object.Type.Mango,
        10 => Level5Object.Type.Apple,
        11 => Level5Object.Type.Sunflower,
        12 => Level5Object.Type.Rose,
        _ => Level5Object.Type.Mango
    };

    private List<Level5Object.Type> GetActiveObjectTypesForStage(int stage)
    {
        if (stage == 1) return new List<Level5Object.Type> { Level5Object.Type.Mango };
        if (stage == 2) return new List<Level5Object.Type> { Level5Object.Type.Apple };
        if (stage == 3) return new List<Level5Object.Type> { Level5Object.Type.Sunflower };
        if (stage == 4) return new List<Level5Object.Type> { Level5Object.Type.Rose };
        if (stage == 5 || stage == 6) return new List<Level5Object.Type> { Level5Object.Type.Mango, Level5Object.Type.Apple };
        if (stage == 7 || stage == 8) return new List<Level5Object.Type> { Level5Object.Type.Sunflower, Level5Object.Type.Rose };
        return new List<Level5Object.Type> { Level5Object.Type.Mango, Level5Object.Type.Apple, Level5Object.Type.Sunflower, Level5Object.Type.Rose };
    }

    private void ActivateStageObjects()
    {
        SetActiveForGroup(apples, false);
        SetActiveForGroup(mangoes, false);
        SetActiveForGroup(sunflowers, false);
        SetActiveForGroup(roses, false);
        DisableTaggedAdditional();

        switch (stageNumber)
        {
            case 1: SetActiveForGroup(mangoes, true); EnableTaggedAdditional("mango"); break;
            case 2: SetActiveForGroup(apples, true); EnableTaggedAdditional("apple"); break;
            case 3: SetActiveForGroup(sunflowers, true); EnableTaggedAdditional("sunflower"); break;
            case 4: SetActiveForGroup(roses, true); EnableTaggedAdditional("rose"); break;
            case 5:
            case 6:
                SetActiveForGroup(mangoes, true);
                SetActiveForGroup(apples, true);
                DisablePermanentlyRemovedObjects();
                break;
            case 7:
            case 8:
                SetActiveForGroup(sunflowers, true);
                SetActiveForGroup(roses, true);
                DisablePermanentlyRemovedObjects();
                break;
            default:
                SetActiveForGroup(mangoes, true);
                SetActiveForGroup(apples, true);
                SetActiveForGroup(sunflowers, true);
                SetActiveForGroup(roses, true);
                DisablePermanentlyRemovedObjects();
                break;
        }
    }

    void RegisterObjects()
    {
        allObjects.Clear();
        Register(mangoes);
        Register(apples);
        Register(sunflowers);
        Register(roses);
    }

    void Register(GameObject[] group)
    {
        if (group == null) return;
        foreach (var go in group)
        {
            if (go == null) continue;
            var obj = go.GetComponent<Level5Object>();
            if (obj != null)
            {
                obj.manager = this;
                obj.pointer = pointer;
                obj.ResetObject();
                allObjects.Add(obj);
            }
        }
    }

    bool AllCurrentInstructionTypeObjectsHaveBees()
    {
        foreach (var obj in allObjects)
            if (obj != null && obj.gameObject.activeInHierarchy && obj.objectType == currentInstructionType && !obj.HasBeeAttached())
                return false;
        return true;
    }

    void SetActiveForGroup(GameObject[] group, bool active) => System.Array.ForEach(group, go => { if (go != null) go.SetActive(active); });

    void EnableTaggedAdditional(string keyword)
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag("Additional"))
            if (obj.name.ToLower().Contains(keyword.ToLower()))
            {
                obj.SetActive(true);
                var l5o = obj.GetComponent<Level5Object>();
                if (l5o != null && !allObjects.Contains(l5o))
                    allObjects.Add(l5o);
            }
    }

    void DisableTaggedAdditional()
    {
        foreach (var obj in GameObject.FindGameObjectsWithTag("Additional"))
            obj.SetActive(false);
    }

    void DisablePermanentlyRemovedObjects()
    {
        if (permanentlyDisabledObjects == null) return;
        foreach (var go in permanentlyDisabledObjects)
            if (go != null) go.SetActive(false);
    }

    void SetInitialTimerForStage() => timer = stageDuration;
    IEnumerator FixErrorLater() { yield return null; error_fix(); }
    void error_fix() { foreach (var goo in error) goo.SetActive(true); }

    public void HandlePickup(Level5Object obj, bool hasBee)
    {
        if (isGameOver || obj == null || !obj.gameObject.activeSelf) return;

        switch (obj.objectType)
        {
            case Level5Object.Type.Mango: mangoCount++; Level5Object.mangoCount++; break;
            case Level5Object.Type.Apple: appleCount++; Level5Object.appleCount++; break;
            case Level5Object.Type.Sunflower: sunflowerCount++; Level5Object.sunflowerCount++; break;
            case Level5Object.Type.Rose: roseCount++; Level5Object.roseCount++; break;
        }
        UpdateCountUI();

        bool isCorrect = obj.objectType == currentInstructionType;
        float delta = isCorrect ? (hasBee ? -0.5f : 1f) : (hasBee ? -1f : -0.5f);
        score += delta;
        ShowFloatingText(isCorrect ? (hasBee ? "-0.5" : "+1") : (hasBee ? "-1" : "-0.5"), obj.transform.position);

        if (isCorrect && !hasBee)
        {
            switch (obj.objectType)
            {
                case Level5Object.Type.Mango: mangoCorrectNoBee++; break;
                case Level5Object.Type.Apple: appleCorrectNoBee++; break;
                case Level5Object.Type.Sunflower: sunflowerCorrectNoBee++; break;
                case Level5Object.Type.Rose: roseCorrectNoBee++; break;
            }
        }
    }

    void UpdateCountUI()
    {
        if (mangoCountText != null) mangoCountText.text = mangoCount.ToString();
        if (appleCountText != null) appleCountText.text = appleCount.ToString();
        if (sunflowerCountText != null) sunflowerCountText.text = sunflowerCount.ToString();
        if (roseCountText != null) roseCountText.text = roseCount.ToString();
    }

    void ShowFloatingText(string content, Vector3 worldPosition)
    {
        var prefab = content switch
        {
            "+1" => floatingTextPlusPrefab,
            "-0.5" => floatingTextMinusPrefab,
            "-1" => floatingTextMinusOnePrefab,
            _ => null
        };
        if (prefab == null || uiCanvas == null) return;

        var screenPos = Camera.main.WorldToScreenPoint(worldPosition + worldOffset) + screenOffset;
        Instantiate(prefab, uiCanvas.transform).transform.position = screenPos;
    }

    private void ApplyCuePermissionsForAttempt()
    {
        bool allowOutline = (currentCueTier >= CueTier.OneCue);
        bool allowBloom = (currentCueTier >= CueTier.OneCue);
        bool allowArrow = (currentCueTier >= CueTier.TwoCues);

        bloomActiveThisAttempt = allowBloom;

        var objs = FindObjectsOfType<Level5Object>(true);
        foreach (var o in objs)
        {
            if (o == null) continue;
            o.SetCuePermissions(allowOutline, allowArrow);
        }

        attemptIndex = (int)currentCueTier;
        Debug.Log($"[L5] ApplyCuePermissions -> tier={currentCueTier} outline={allowOutline} arrow={allowArrow}");
    }

    void EndLevel()
    {
        if (isGameOver) return;
        bool completedByCollectAll = AllCurrentInstructionTypeObjectsHaveBees();

        isGameOver = true;
        isTimerRunning = false;

        float finalScore = score;
        float timerAtEnd = timer;

        float stageScoreMade = 0f;
        int stageScoreMax = 0;

     switch (stageNumber)
{
    // Mango stages
    case 1:
    case 5:
    case 9:
        stageScoreMade = score;              // aggregated score (with negatives)
        stageScoreMax  = mangoInitialNoBee;  // exclude bee objects
        break;

    // Apple stages
    case 2:
    case 6:
    case 10:
        stageScoreMade = score;
        stageScoreMax  = appleInitialNoBee;
        break;

    // Sunflower stages
    case 3:
    case 7:
    case 11:
        stageScoreMade = score;
        stageScoreMax  = sunflowerInitialNoBee;
        break;

    // Rose stages
    case 4:
    case 8:
    case 12:
        stageScoreMade = score;
        stageScoreMax  = roseInitialNoBee;
        break;

    default:
        Debug.LogWarning($"Invalid stageNumber: {stageNumber}");
        stageScoreMade = score;
        stageScoreMax  = 1;
        break;
}

        if (!t1Sent) scoreAtT1 = score;
        if (!t2Sent) scoreAtT2 = score;
        if (!t3Sent) scoreAtT3 = score;

        int csvStageAppearance = currentSelectedIdx + 1; 

        if (LocalCSVLogger.Instance != null)
        {
            try
            {
                if (!savedStagesThisAttempt.Contains(stageNumber))
                {
                    stageStopTime = DateTime.Now;

                    LocalCSVLogger.Instance.SaveStageTimestamps(
                        string.IsNullOrEmpty(playerName) ? (PlayerDataLogger.CurrentPlayerID ?? "Player") : playerName,
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

                    savedStagesThisAttempt.Add(stageNumber);
                    Debug.Log($"[CSV] Stage {stageNumber} (appearance #{csvStageAppearance}) logged (T1/T2/T3): {scoreAtT1},{scoreAtT2},{scoreAtT3}");
                }
                else
                {
                    Debug.Log($"[CSV] Stage {stageNumber} already saved for this attempt — skipping duplicate write.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[CSV] Stage logging failed: " + ex.Message);
            }
        }
        else Debug.LogWarning("[CSV] LocalCSVLogger.Instance not found. Stage not logged.");
        // --------------------------------------------------------------------

        RecordStagePerformance(stageNumber, finalScore, timerAtEnd, completedByCollectAll);

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverPanel_score != null) gameOverPanel_score.text = $"Score: {score:F1}   Time: {Mathf.Max(0, 60f - Mathf.FloorToInt(timer))}";
    }

    private void RecordStagePerformance(int stageIdx, float finalScore, float timerAtEnd, bool completedByCollectAll)
    {
        var relevantTypes = GetActiveObjectTypesForStage(stageIdx);
        int denom = 0;
        int collectedCorrect = 0;

        foreach (var t in relevantTypes)
        {
            switch (t)
            {
                case Level5Object.Type.Mango:
                    denom += mangoInitialNoBee;
                    collectedCorrect += mangoCorrectNoBee;
                    break;
                case Level5Object.Type.Apple:
                    denom += appleInitialNoBee;
                    collectedCorrect += appleCorrectNoBee;
                    break;
                case Level5Object.Type.Sunflower:
                    denom += sunflowerInitialNoBee;
                    collectedCorrect += sunflowerCorrectNoBee;
                    break;
                case Level5Object.Type.Rose:
                    denom += roseInitialNoBee;
                    collectedCorrect += roseCorrectNoBee;
                    break;
            }
        }

        float PF = 0f;
        if (timerAtEnd >= 1f)
        {
            PF = 100f;
        }
        else
        {
            if (denom <= 0)
                PF = 100f;
            else
                PF = ((float)collectedCorrect / (float)denom) * 100f;
            PF = Mathf.Clamp(PF, 0f, 100f);
        }

        if (selectedStages.Contains(stageIdx))
        {
            int pfIndex = selectedStages.IndexOf(stageIdx);
            switch (pfIndex)
            {
                case 0: pf1 = PF; break;
                case 1: pf2 = PF; break;
                case 2: pf3 = PF; break;
            }
        }

        // ----------------- NEW: guarded attempt-PFF write -----------------
        if (currentSelectedIdx >= (selectedStages.Count - 1))
        {
            ComputeFinalPerformance();

            attemptStopTime = DateTime.Now;

            if (!attemptPffSaved)
            {
                if (LocalCSVLogger.Instance != null)
                {
                    try
                    {
                        LocalCSVLogger.Instance.SaveAttemptPFF(
                            string.IsNullOrEmpty(playerName) ? (PlayerDataLogger.CurrentPlayerID ?? "Player") : playerName,
                            levelId,
                            attemptNumber,
                            GetCueTypeLabel(),
                            Pff,
                            attemptStartTime,
                            attemptStopTime
                        );
                        attemptPffSaved = true;
                        Debug.Log($"[CSV] Attempt#{attemptNumber} PFF saved: {Pff:F1}%");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[CSV] Attempt PFF log failed: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("[CSV] LocalCSVLogger.Instance not found. Attempt PFF not logged.");
                }
            }
            else
            {
                Debug.Log($"[CSV] Attempt PFF for attempt#{attemptNumber} already saved - skipping duplicate.");
            }
        }
        // --------------------------------------------------------------------

        if (debugPFText != null)
            debugPFText.text = $"pf1:{pf1:F1} pf2:{pf2:F1} pf3:{pf3:F1} Pff:{Pff:F1}";
    }

    private int GetInitialNoBeeForInstructionType(int stageIdx)
    {
        Level5Object.Type instrType = GetInstructionTypeForStage(stageIdx);
        return instrType switch
        {
            Level5Object.Type.Mango => mangoInitialNoBee,
            Level5Object.Type.Apple => appleInitialNoBee,
            Level5Object.Type.Sunflower => sunflowerInitialNoBee,
            Level5Object.Type.Rose => roseInitialNoBee,
            _ => 0
        };
    }

    private void ComputeFinalPerformance()
    {
        Pff = (0.2f * pf1) + (0.3f * pf2) + (0.5f * pf3);
        Pff = Mathf.Clamp(Pff, 0f, 100f);
        Debug.Log($"[Perf] Final Pff = {Pff:F1}% (pf1={pf1:F1},pf2={pf2:F1},pf3={pf3:F1})");
    }

    public static void ResetPerformance10()
    {
        pf1 = pf2 = pf3 = 0f;
        Pff = 0f;
    }

    private void ResetPerStageCounts()
    {
        mangoCount = appleCount = sunflowerCount = roseCount = 0;
        UpdateCountUI();

        Level5Object.mangoCount = 0;
        Level5Object.appleCount = 0;
        Level5Object.sunflowerCount = 0;
        Level5Object.roseCount = 0;

        mangoCorrectNoBee = appleCorrectNoBee = sunflowerCorrectNoBee = roseCorrectNoBee = 0;
        mangoInitialNoBee = appleInitialNoBee = sunflowerInitialNoBee = roseInitialNoBee = 0;

        score = 0f;
        if (scoreText != null) scoreText.text = $"Score: {score:F1}";
    }

    private void ComputeInitialRelevantNoBeeCounts()
    {
        mangoInitialNoBee = appleInitialNoBee = sunflowerInitialNoBee = roseInitialNoBee = 0;

        var relevantTypes = GetActiveObjectTypesForStage(stageNumber);
        Level5Object[] objs = FindObjectsOfType<Level5Object>(true);

        int countRelevant = 0;
        int countWithBees = 0;
        int countNoBees = 0;

        bool includeAdditional = (stageNumber >= 1 && stageNumber <= 4);

        foreach (var o in objs)
        {
            if (o == null) continue;
            if (!o.gameObject.activeInHierarchy) continue;
            if (!includeAdditional && o.CompareTag("Additional")) continue;
            if (!relevantTypes.Contains(o.objectType)) continue;

            countRelevant++;

            if (o.HasBeeAttached())
                countWithBees++;
            else
            {
                countNoBees++;
                switch (o.objectType)
                {
                    case Level5Object.Type.Mango: mangoInitialNoBee++; break;
                    case Level5Object.Type.Apple: appleInitialNoBee++; break;
                    case Level5Object.Type.Sunflower: sunflowerInitialNoBee++; break;
                    case Level5Object.Type.Rose: roseInitialNoBee++; break;
                }
            }
        }

        Debug.Log($"[Perf] Stage {stageNumber} → includeAdditional={includeAdditional} | relevant={countRelevant}, no-bee={countNoBees}, with-bee={countWithBees}");
        Debug.Log($"[Perf] no-bee breakdown: Mango:{mangoInitialNoBee} Apple:{appleInitialNoBee} Sunflower:{sunflowerInitialNoBee} Rose:{roseInitialNoBee}");
    }

    private void ResetAllObjects()
    {
        Level5Object[] objs = FindObjectsOfType<Level5Object>(true);
        foreach (var o in objs)
        {
            if (o == null) continue;
            o.ResetObject();
            o.SetCuePermissions(currentCueTier >= CueTier.OneCue, currentCueTier >= CueTier.TwoCues);
        }

        bloomGloballyActive = false;
        if (bloom != null) bloom.intensity.Override(minIntensity);
        if (VOLUME != null) { VOLUME.enabled = false; VOLUME.weight = 0f; }
    }

    public void OnRetrySameStageClick()
    {
        CueTier next = currentCueTier;
        if (currentCueTier == CueTier.None) next = CueTier.OneCue;
        else if (currentCueTier == CueTier.OneCue) next = CueTier.TwoCues;
        else next = CueTier.TwoCues;

        attemptNumber++;
        StartClosedLoopRun(next, false, "retry_same_stage");
    }

    public void OnFailedRestartClick()
    {
        CueTier next = currentCueTier;
        if (currentCueTier == CueTier.None) next = CueTier.OneCue;
        else if (currentCueTier == CueTier.OneCue) next = CueTier.TwoCues;
        else next = CueTier.TwoCues;

        attemptNumber++;
        StartClosedLoopRun(next, true, "failed_restart_full_sequence");
    }

    private void StartClosedLoopRun(CueTier nextTier, bool fullRestart, string reason)
    {
        Debug.Log($"[ClosedLoop] attempt#{attemptNumber} -> tier={nextTier} fullRestart={fullRestart} reason={reason}");

        if (bloom != null) bloom.intensity.Override(minIntensity);
        if (VOLUME != null) { VOLUME.enabled = false; VOLUME.weight = 0f; }

        currentCueTier = nextTier;
        attemptIndex = (int)nextTier;

        // ---------- NEW: clear CSV bookkeeping for each fresh attempt ----------
        savedStagesThisAttempt.Clear();
        attemptPffSaved = false;
        // --------------------------------------------------------------------

        isGameOver = false;
        isTimerRunning = false;
        timer = stageDuration;
        score = 0f;

        if (panel_failedlevel != null) panel_failedlevel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (fullRestart)
        {
            ResetPerformance10();
            ResetPerStageCounts();

            GenerateSelectedStagesForAttempt();
            currentSelectedIdx = 0;
            if (selectedStages.Count > 0) stageNumber = selectedStages[currentSelectedIdx];

            ActivateStageObjects();
            RegisterObjects();
            ResetAllObjects();
            ComputeInitialRelevantNoBeeCounts();

            if (instructionPanel != null) instructionPanel.SetActive(true);
            SetInstructionPanelText();
        }
        else
        {
            ResetPerStageCounts();
            ActivateStageObjects();
            RegisterObjects();
            ResetAllObjects();
            ComputeInitialRelevantNoBeeCounts();
            if (instructionPanel != null) instructionPanel.SetActive(true);
            SetInstructionPanelText();
        }

        ApplyCuePermissionsForAttempt();

        var beeScript = BeeFlight1Controller?.GetComponent<BeeFlight1>();
        if (beeScript != null)
        {
            beeScript.ResetAllBees();
            var sitList = new List<Transform>();
            foreach (var obj in allObjects)
                if (obj != null && obj.gameObject.activeInHierarchy && obj.sitPoint != null)
                    sitList.Add(obj.sitPoint);
            beeScript.sitPoints = sitList.ToArray();
        }

        if (pointer != null)
        {
            pointer.transform.localPosition = initialHandPos;
            pointer.transform.localRotation = initialHandRot;
        }

        if (stagetext != null) { stagetext.text = STAGETEXTINSTRUTION[stageNumber]; stagetext.enabled = true; }

        UpdateLayersForStage();

        attemptStartTime = DateTime.Now;
    }

    public void OnGameOverNextClick()
    {
        bool isLastSelectedStage = (currentSelectedIdx >= (selectedStages.Count - 1));

        if (isLastSelectedStage)
        {
            ComputeFinalPerformance();
            lastAttemptPassed = (Pff >= passThreshold);
            Debug.Log($"[ClosedLoop] final selected-stage Pff={Pff:F1} tier={currentCueTier} passed={lastAttemptPassed}");

            if (currentCueTier == CueTier.None)
            {
                if (lastAttemptPassed)
                {
                    AdvanceToNextLevel();
                    return;
                }
                else
                {
                    if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                    if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again(Glow Cue).\nTotalScore = {Pff:F1}%";
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    return;
                }
            }
            else if (currentCueTier == CueTier.OneCue)
            {
                if (lastAttemptPassed)
                {
                    attemptNumber++;
                    StartClosedLoopRun(CueTier.None, true, "passed_with_one_cue_stepdown");
                    return;
                }
                else
                {
                    if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                    if (failedLevelText != null) failedLevelText.text =$"Level failed..\nTry again(Glow +Arrow Cue).\nTotalScore = {Pff:F1}%";
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    return;
                }
            }
            else
            {
                if (lastAttemptPassed)
                {
                    attemptNumber++;
                    StartClosedLoopRun(CueTier.OneCue, true, "passed_with_two_cues_stepdown");
                    return;
                }
                else
                {
                    if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                    if (failedLevelText != null) failedLevelText.text =$"Level failed..\nTry again(Glow +Arrow Cue).\nTotalScore = {Pff:F1}%";
                    if (gameOverPanel != null) gameOverPanel.SetActive(false);
                    return;
                }
            }
        }

        currentSelectedIdx++;
        if (currentSelectedIdx >= selectedStages.Count) currentSelectedIdx = 0;
        stageNumber = selectedStages[currentSelectedIdx];

        isGameOver = false;
        isTimerRunning = false;
        score = 0f;
        if (scoreText != null) scoreText.text = "Score: 0";
        if (timerText != null) timerText.text = "Time: 0";
        if (hand != null) hand.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        ActivateStageObjects();
        RegisterObjects();
        foreach (var obj in allObjects) if (obj != null) obj.ResetObject();
        if (stageNumber >= 5) DisablePermanentlyRemovedObjects();

        var beeScript = BeeFlight1Controller?.GetComponent<BeeFlight1>();
        if (beeScript != null) beeScript.ResetAllBees();
        var sitList = new List<Transform>();
        foreach (var obj in allObjects)
            if (obj != null && obj.gameObject.activeInHierarchy && obj.sitPoint != null)
                sitList.Add(obj.sitPoint);
        if (beeScript != null) beeScript.sitPoints = sitList.ToArray();

        SetInitialTimerForStage();
        SetInstructionPanelText();
        instructionPanel.SetActive(true);
        if (stagetext != null) { stagetext.text = STAGETEXTINSTRUTION[stageNumber]; stagetext.enabled = true; }

        ClearAllOutlines(mangoes);
        ClearAllOutlines(apples);
        ClearAllOutlines(sunflowers);
        ClearAllOutlines(roses);
    }

    void SetInstructionPanelText()
    {
        if (instructionPanelText == null) return;
        instructionPanelText.text = stageNumber switch
        {
            1 => "🍎 Pluck All Yellow Mangoes without bee",
            2 => "🥭 Pluck All Red Apples without bee",
            3 => "🌻 Pluck All White flowers without bee",
            4 => "🌹 Pluck All Pink Roses without bee",
            5 => "🥭 Pluck All Yellow Mangoes without bee",
            6 => "🥭 Pluck All Red Apples without bee",
            7 => "🌹 Pluck All White flowers without bee",
            8 => "🌹 Pluck All Pink Roses without bee",
            9 => "🥭 Pluck All Yellow Mangoes without bee",
            10 => "🥭 Pluck All Red Apples without bee",
            11 => "🥭 Pluck All White flowers without bee",
            12 => "🥭 Pluck All Pink Roses without bee",
            _ => ""
        };
    }

    public float GetRemainingTime() => timer;
    public Level5Object.Type GetCurrentInstructionType() => currentInstructionType;

    void ClearAllOutlines(GameObject[] group)
    {
        if (group == null) return;
        foreach (var go in group)
        {
            if (go == null) continue;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) continue;
            var mats = new List<Material>(renderer.materials);
            mats.RemoveAll(mat => mat != null && mat.shader != null && mat.shader.name.ToLower().Contains("outline"));
            renderer.materials = mats.ToArray();
        }
    }

    public void UpdateLayersForStage()
    {
        Level5Object[] objs = FindObjectsOfType<Level5Object>(true);
        foreach (Level5Object obj in objs)
        {
            bool isRelevant = IsRelevantObject1(obj);
            int targetLayer = isRelevant ? LayerMask.NameToLayer("RelevantGlow") : LayerMask.NameToLayer("Fruit");
            obj.gameObject.layer = targetLayer;
            SetLayerRecursively(obj.transform, targetLayer);
        }
    }

    private void SetLayerRecursively(Transform objTransform, int targetLayer)
    {
        foreach (Transform child in objTransform)
        {
            child.gameObject.layer = targetLayer;
            SetLayerRecursively(child, targetLayer);
        }
    }

    public bool IsRelevantObject2(Level5Object obj)
    {
        int stage = stageNumber;
        switch (stage)
        {
            case 1: return obj.objectType == Level5Object.Type.Mango && !obj.HasBeeAttached();
            case 2: return obj.objectType == Level5Object.Type.Apple && !obj.HasBeeAttached();
            case 3: return obj.objectType == Level5Object.Type.Sunflower && !obj.HasBeeAttached();
            case 4: return obj.objectType == Level5Object.Type.Rose && !obj.HasBeeAttached();
            case 5: return obj.objectType == Level5Object.Type.Mango && !obj.HasBeeAttached();
            case 6: return obj.objectType == Level5Object.Type.Apple && !obj.HasBeeAttached();
            case 7: return obj.objectType == Level5Object.Type.Sunflower && !obj.HasBeeAttached();
            case 8: return obj.objectType == Level5Object.Type.Rose && !obj.HasBeeAttached();
            case 9: return obj.objectType == Level5Object.Type.Mango && !obj.HasBeeAttached();
            case 10: return obj.objectType == Level5Object.Type.Apple && !obj.HasBeeAttached();
            case 11: return obj.objectType == Level5Object.Type.Sunflower && !obj.HasBeeAttached();
            case 12: return obj.objectType == Level5Object.Type.Rose && !obj.HasBeeAttached();
            default: return false;
        }
    }
    public bool IsRelevantObject1(Level5Object obj)
    {
        int stage = stageNumber;
        switch (stage)
        {
            case 3: return obj.objectType == Level5Object.Type.Sunflower && !obj.HasBeeAttached();
            case 4: return obj.objectType == Level5Object.Type.Rose && !obj.HasBeeAttached();
            case 7: return obj.objectType == Level5Object.Type.Sunflower && !obj.HasBeeAttached();
            case 8: return obj.objectType == Level5Object.Type.Rose && !obj.HasBeeAttached();
            case 11: return obj.objectType == Level5Object.Type.Sunflower && !obj.HasBeeAttached();
            case 12: return obj.objectType == Level5Object.Type.Rose && !obj.HasBeeAttached();
            default: return false;
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = UnityEngine.Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }

    private void GenerateSelectedStagesForAttempt()
    {
        selectedStages.Clear();
        List<int> pool = Enumerable.Range(1, 12).ToList();
        for (int i = 0; i < 3; i++)
        {
            int pickIndex = UnityEngine.Random.Range(0, pool.Count);
            selectedStages.Add(pool[pickIndex]);
            pool.RemoveAt(pickIndex);
        }
        currentSelectedIdx = 0;
        if (selectedStages.Count > 0) stageNumber = selectedStages[0];
    }

    private void RefreshBeesForStage()
    {
        var bee = BeeFlight1Controller?.GetComponent<BeeFlight1>();
        if (bee == null)
        {
            Debug.LogWarning("[L5] BeeFlight1 component missing on BeeFlight1Controller!");
            return;
        }

        var sitList = new List<Transform>();
        foreach (var obj in allObjects)
            if (obj != null && obj.gameObject.activeInHierarchy && obj.sitPoint != null) sitList.Add(obj.sitPoint);

        bee.sitPoints = sitList.ToArray();
        bee.ResetAllBees();
        bee.StopAllCoroutines();
    }

    private void AdvanceToNextLevel()
    {
        var l80 = GetComponent<Level6StageManager>();
        if (l80 != null) l80.enabled = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (panel_failedlevel != null) panel_failedlevel.SetActive(false);
        level4_assest.SetActive(false);
        player_5.SetActive(false);
        level5_assest.SetActive(true);

        player_6.SetActive(true);
    }

    public void OnHelpClick()
    {
        if (helpPanel == null || helpInstructionText == null || helpTexts == null)
        {
            if (helpPanel != null) helpPanel.SetActive(!helpPanel.activeSelf);
            return;
        }

        bool isNowActive = !helpPanel.activeSelf;
        helpPanel.SetActive(isNowActive);

        if (isNowActive)
        {
            int idx = Mathf.Clamp(stageNumber, 1, 12) - 1;
            if (idx >= 0 && idx < helpTexts.Length)
                helpInstructionText.text = instructionPanelText != null ? instructionPanelText.text : "";
            else
                helpInstructionText.text = "";
        }
    }

    // ---------- NEW HELPER ----------
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
    // -------------------------------

}
