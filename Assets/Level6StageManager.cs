// Level6StageManager - FINAL (updated)
// Replaces previous Level6StageManager; includes CSV/player-name/stage-order/PF fixes.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using TMPro;

public class Level6StageManager : MonoBehaviour
{
    [Header("Cue Timing (Level5-like defaults)")]
    public float outlineThreshold = 20f;
    public float arrowThreshold = 10f;

    private List<Level6Object> currentlyOutlined = new List<Level6Object>();
    private List<Level6Object> currentlyArrowed = new List<Level6Object>();
    private List<Level6Object> currentlyGlowing = new List<Level6Object>();

    private bool bloomGloballyActive = false;

    [Header("UI")]
    public GameObject lightt;
    public TextMeshProUGUI timerText;
    public GameObject congoPanel;
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
    public GameObject[] mangoes, apples, sunflowers, roses;
    public GameObject BeeFlightController;
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

    [Header("Instruction Panels")]
    public GameObject panel_mango;
    public GameObject panel_apple;
    public GameObject panel_sunflower;
    public GameObject panel_rose;

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

    [Header("Post Processing")]
    public PostProcessVolume VOLUME;
    private Bloom bloom;
    public bool enableBlinking = true;
    public float blinkSpeed = 15f;
    private float minIntensity = 1f;
    private float maxIntensity = 12f;

    // internal
    private List<Level6Object> allObjects = new List<Level6Object>();
    private Level6Object.ObjectType currentInstructionType;
    public float timer;
    private float score = 0f;
    private bool isTimerRunning = false;
    public bool isGameOver = false;
    private Vector3 initialHandPos;
    private Quaternion initialHandRot;
    private int mangoCount = 0, appleCount = 0, sunflowerCount = 0, roseCount = 0;
    public Camera cam06;

    private readonly string[] STAGETEXTINSTRUTION = {
        "Stage 0","Stage 1","Stage 2","Stage 3","Stage 4",
        "Stage 5","Stage 6","Stage 7","Stage 8",
        "Stage 9","Stage 10","Stage 11","Stage 12"
    };

    // attempt & PF system
    public enum CueTier { None = 0, OneCue = 1, TwoCues = 2 }
    [SerializeField] private CueTier currentCueTier = CueTier.None;
    [SerializeField] private int attemptIndex = 0;
    public int attemptNumber = 1;

    public bool bloomActiveThisAttempt = false;
    private bool lastAttemptPassed = false;

    [Header("Help UI")]
    public Button helpButton;
    public GameObject helpPanel;
    public TextMeshProUGUI helpInstructionText;
    [TextArea] public string[] helpTexts = new string[12];

    [Header("Random Stage Selection")]
    private List<int> selectedStages = new List<int>();
    private int currentStageIndex = 0; // 0..2

    // performance storage
    public float pf1 = 0f, pf2 = 0f, pf3 = 0f;
    public float Pff = 0f;
    public float passThreshold = 70f;

    // per-stage counts
    private int mangoCorrectNoBee = 0;
    private int appleCorrectNoBee = 0;
    private int sunflowerCorrectNoBee = 0;
    private int roseCorrectNoBee = 0;

    private int mangoInitialNoBee = 0;
    private int appleInitialNoBee = 0;
    private int sunflowerInitialNoBee = 0;
    private int roseInitialNoBee = 0;

    // CSV / timing tracking (T1/T2/T3)
    private DateTime attemptStartTime;
    private DateTime attemptStopTime;
    private DateTime stageStartTime;
    private DateTime stageStopTime;
    private bool t1Sent = false, t2Sent = false, t3Sent = false;
    private float scoreAtT1 = 0f, scoreAtT2 = 0f, scoreAtT3 = 0f;

    [Header("Player/CSV")]
    // prefer empty; will fallback to PlayerDataLogger.CurrentPlayerID if present
    private string playerName = "";
    public int levelId = 6;

    // CSV duplicate-prevention bookkeeping
    private HashSet<int> savedStagesThisAttempt = new HashSet<int>();
    private bool attemptPffSaved = false;

    public AudioSource sfxSource;
    public AudioClip pluckSFX;

    void Start()
    {
         playerName = PlayerDataLogger.CurrentPlayerID ?? "Player";
        SetInstructionPanelText();
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
        if (stagetext != null) stagetext.enabled = false;

        GenerateSelectedStagesForAttempt(); // pick 3 random stages for the first attempt
        currentStageIndex = 0;
        if (selectedStages.Count > 0)
            stageNumber = selectedStages[currentStageIndex];

        // set current instruction type and ActiveObjectTypes early so objects can evaluate immediately
        currentInstructionType = GetInstructionTypeForStage(stageNumber);
        Level6Object.ActiveObjectTypes.Clear();
        foreach (var t in GetActiveObjectTypesForStage(stageNumber))
            Level6Object.ActiveObjectTypes.Add(t);

        ActivateStageObjects();
        RegisterObjects();

        SetInitialTimerForStage();
        StartCoroutine(FixErrorLater());

        if (helpPanel != null) helpPanel.SetActive(false);
        if (helpButton != null)
        {
            helpButton.onClick.RemoveListener(OnHelpClick);
            helpButton.onClick.AddListener(OnHelpClick);
        }

        currentCueTier = CueTier.None;
        attemptIndex = 0;
        attemptNumber = 1;
        ApplyCuePermissionsForAttempt();
        if (lightt != null) lightt.transform.rotation = Quaternion.Euler(168f, 2.7f, 0f);

        // record attempt start time
        attemptStartTime = DateTime.Now;

        // init CSV bookkeeping
        savedStagesThisAttempt.Clear();
        attemptPffSaved = false;
    }

    private void OnDestroy()
    {
        if (helpButton != null) helpButton.onClick.RemoveListener(OnHelpClick);
    }

    public void OnHelpClick()
    {
        if (helpPanel == null) return;

        bool show = !helpPanel.activeSelf;
        helpPanel.SetActive(show);

        if (show)
        {
            int idx = Mathf.Clamp(stageNumber, 1, 12) - 1;
            if (helpInstructionText != null && helpTexts != null && idx < helpTexts.Length)
            {
                // display the current instruction text (keeps behavior simple)
                helpInstructionText.text = instructionPanelText != null ? instructionPanelText.text : helpTexts[idx];
            }
        }
    }

    private void GenerateSelectedStagesForAttempt()
    {
        selectedStages.Clear();
        // Keep first stage fixed to Mango (1) if you prefer; else randomize all three.
        selectedStages.Add(1);

        List<int> pool = Enumerable.Range(2, 11).ToList(); // 2..12
        for (int i = 0; i < 2 && pool.Count > 0; i++)
        {
            int pickIndex = UnityEngine.Random.Range(0, pool.Count);
            selectedStages.Add(pool[pickIndex]);
            pool.RemoveAt(pickIndex);
        }

        currentStageIndex = 0;
        if (selectedStages.Count > 0)
            stageNumber = selectedStages[0];
    }

    void Update()
    {
        if (isGameOver || !isTimerRunning) return;

        timer -= Time.deltaTime;
        if (timerText != null) timerText.text = $"Time: {Mathf.Max(0, Mathf.FloorToInt(timer))}";
        if (scoreText != null) scoreText.text = $"Score: {score:F1}";

        int secLeft = Mathf.FloorToInt(timer);
        if (!t1Sent && secLeft == 20) { scoreAtT1 = score; t1Sent = true; }
        if (!t2Sent && secLeft == 10) { scoreAtT2 = score; t2Sent = true; }
        if (!t3Sent && secLeft <= 0) { scoreAtT3 = score; t3Sent = true; }

        UpdateDynamicCues();

        if (bloom != null)
        {
            if (bloomGloballyActive && enableBlinking)
            {
                VOLUME.enabled = true;
                VOLUME.weight = 1f;
                float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
                float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
                bloom.intensity.Override(intensity);
            }
            else
            {
                bloom.intensity.Override(minIntensity);
                if (VOLUME != null) { VOLUME.enabled = false; VOLUME.weight = 0f; }
            }
        }

        if (timer <= 0f || AllCurrentInstructionTypeObjectsHaveBees())
        {
            EndLevel();
        }
    }

    public void UpdateDynamicCues()
    {
        var objs = FindObjectsOfType<Level6Object>(true);
        bool anyGlowNeeded = false;

        foreach (var o in objs)
        {
            if (o == null || !o.gameObject.activeInHierarchy) continue;

            bool hasBee = o.HasBeeAttached();
            bool targeted = o.IsBeeTargeted();

            // OUTLINE for Mango/Apple
            bool eligibleForOutline = (o.objectType == Level6Object.ObjectType.Mango || o.objectType == Level6Object.ObjectType.Apple)
                                      && !hasBee && !targeted && (timer < outlineThreshold);

            if (eligibleForOutline && o.OutlineAllowed)
            {
                o.ShowOutline();
                if (!currentlyOutlined.Contains(o)) currentlyOutlined.Add(o);
            }
            else
            {
                o.HideOutline();
                currentlyOutlined.Remove(o);
            }

            // GLOW for Rose/Sunflower
            bool eligibleForGlow = (o.objectType == Level6Object.ObjectType.Rose || o.objectType == Level6Object.ObjectType.Sunflower)
                                   && !hasBee && !targeted && (timer < outlineThreshold) && bloomActiveThisAttempt;

            if (eligibleForGlow)
            {
                o.EnableGlow();
                anyGlowNeeded = true;
                if (!currentlyGlowing.Contains(o)) currentlyGlowing.Add(o);
            }
            else
            {
                o.DisableGlow();
                currentlyGlowing.Remove(o);
            }

            // ARROW (instruction objects only)
            bool isInstructionObject = IsRelevantObject2(o);
            bool eligibleForArrow = isInstructionObject && !hasBee && !targeted && (timer < arrowThreshold);

            if (eligibleForArrow && o.ArrowAllowed)
            {
                o.ShowArrow();
                if (!currentlyArrowed.Contains(o)) currentlyArrowed.Add(o);
            }
            else
            {
                o.HideArrow();
                currentlyArrowed.Remove(o);
            }
        }

        bloomGloballyActive = anyGlowNeeded && enableBlinking && bloomActiveThisAttempt;

        if (Time.frameCount % 180 == 0)
            Debug.Log($"[L6Manager] UpdateDynamicCues: timer={timer:F1} tier={currentCueTier} outlined={currentlyOutlined.Count} arrows={currentlyArrowed.Count} glow={currentlyGlowing.Count}");
    }

    public void ReevaluateCuesImmediately() => UpdateDynamicCues();

    public void OnFirstNextClick()
    {
        helpButton.gameObject.SetActive(true);
        HandPositionReceiver receiver = FindObjectOfType<HandPositionReceiver>();
        if (receiver != null) receiver.RefreshMapping(cam06);
        if (instructionPanel != null) instructionPanel.SetActive(false);
        StartCoroutine(BeginStage());
    }

    IEnumerator BeginStage()
    {
        var bees = BeeFlightController?.GetComponent<BeeFlight>();
        if (bees != null) bees.StopAllCoroutines();

        if (pointer != null)
        {
            pointer.transform.localPosition = initialHandPos;
            pointer.transform.localRotation = initialHandRot;
        }

        Time.timeScale = 0f;
        if (startPrompt != null) startPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        if (startPrompt != null) startPrompt.SetActive(false);

        if (timerText != null) timerText.gameObject.SetActive(true);
        if (scoreText != null) scoreText.gameObject.SetActive(true);
        if (hand != null) hand.SetActive(true);

        currentInstructionType = GetInstructionTypeForStage(stageNumber);

        Level6Object.ActiveObjectTypes.Clear();
        foreach (var objType in GetActiveObjectTypesForStage(stageNumber))
            Level6Object.ActiveObjectTypes.Add(objType);

        ResetPerStageCounts();
        ComputeInitialRelevantNoBeeCounts(); // now counts only relevant types for this stage

        // reset T flags and capture stageStartTime
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        stageStartTime = DateTime.Now;

        Time.timeScale = 1f;
        isTimerRunning = true;

        UpdateLayersForStage();

        ApplyCuePermissionsForAttempt();
        RefreshBeesForStage();

        Debug.Log($"[L6] Stage begin: selectedIndex={currentStageIndex} stageNumber={stageNumber} initialNoBee M:{mangoInitialNoBee} A:{appleInitialNoBee} S:{sunflowerInitialNoBee} R:{roseInitialNoBee}");
    }

    private Level6Object.ObjectType GetInstructionTypeForStage(int stage) => stage switch
    {
        1 => Level6Object.ObjectType.Mango,
        2 => Level6Object.ObjectType.Apple,
        3 => Level6Object.ObjectType.Sunflower,
        4 => Level6Object.ObjectType.Rose,
        5 => Level6Object.ObjectType.Mango,
        6 => Level6Object.ObjectType.Apple,
        7 => Level6Object.ObjectType.Sunflower,
        8 => Level6Object.ObjectType.Rose,
        9 => Level6Object.ObjectType.Mango,
        10 => Level6Object.ObjectType.Apple,
        11 => Level6Object.ObjectType.Sunflower,
        12 => Level6Object.ObjectType.Rose,
        _ => Level6Object.ObjectType.Mango
    };

    private List<Level6Object.ObjectType> GetActiveObjectTypesForStage(int stage)
    {
        if (stage == 1) return new List<Level6Object.ObjectType> { Level6Object.ObjectType.Mango };
        if (stage == 2) return new List<Level6Object.ObjectType> { Level6Object.ObjectType.Apple };
        if (stage == 3) return new List<Level6Object.ObjectType> { Level6Object.ObjectType.Sunflower };
        if (stage == 4) return new List<Level6Object.ObjectType> { Level6Object.ObjectType.Rose };
        if (stage == 5 || stage == 6) return new List<Level6Object.ObjectType> { Level6Object.ObjectType.Mango, Level6Object.ObjectType.Apple };
        if (stage == 7 || stage == 8) return new List<Level6Object.ObjectType> { Level6Object.ObjectType.Sunflower, Level6Object.ObjectType.Rose };
        return new List<Level6Object.ObjectType> { Level6Object.ObjectType.Mango, Level6Object.ObjectType.Apple, Level6Object.ObjectType.Sunflower, Level6Object.ObjectType.Rose };
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
            case 2: SetActiveForGroup(apples, true); EnableTaggedAdditional("Apple"); break;
            case 3: SetActiveForGroup(sunflowers, true); EnableTaggedAdditional("Sunflower"); break;
            case 4: SetActiveForGroup(roses, true); EnableTaggedAdditional("roses"); break;
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
            var obj = go.GetComponent<Level6Object>();
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
                var l6o = obj.GetComponent<Level6Object>();
                if (l6o != null && !allObjects.Contains(l6o))
                    allObjects.Add(l6o);
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

    void SetInitialTimerForStage() => timer = 60f;
    IEnumerator FixErrorLater() { yield return null; error_fix(); }
    void error_fix() { if (error != null) foreach (var goo in error) goo.SetActive(true); }

    // Pickup handling
    public void HandlePickup(Level6Object obj, bool hasBee)
    {
        if (isGameOver || !obj.gameObject.activeSelf) return;

        switch (obj.objectType)
        {
            case Level6Object.ObjectType.Mango: mangoCount++; Level6Object.mangoCount++; break;
            case Level6Object.ObjectType.Apple: appleCount++; Level6Object.appleCount++; break;
            case Level6Object.ObjectType.Sunflower: sunflowerCount++; Level6Object.sunflowerCount++; break;
            case Level6Object.ObjectType.Rose: roseCount++; Level6Object.roseCount++; break;
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
                case Level6Object.ObjectType.Mango: mangoCorrectNoBee++; break;
                case Level6Object.ObjectType.Apple: appleCorrectNoBee++; break;
                case Level6Object.ObjectType.Sunflower: sunflowerCorrectNoBee++; break;
                case Level6Object.ObjectType.Rose: roseCorrectNoBee++; break;
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

        var objs = FindObjectsOfType<Level6Object>(true);
        foreach (var o in objs)
        {
            if (o == null) continue;
            o.SetCuePermissions(allowOutline, allowArrow);
        }

        attemptIndex = (int)currentCueTier;

        Debug.Log($"[L6Manager] ApplyCuePermissions -> tier={currentCueTier} allowBloom={bloomActiveThisAttempt} outlineThreshold={outlineThreshold}, arrowThreshold={arrowThreshold}");
    }

    void EndLevel()
    {
        if (isGameOver) return;
        isGameOver = true;
        isTimerRunning = false;

        float finalScore = score;
        float timerAtEnd = timer;

        // stageStopTime and t3 capture if not already
        stageStopTime = DateTime.Now;
        if (!t3Sent) { scoreAtT3 = finalScore; t3Sent = true; }

        // Record PF and write CSV for this stage (important: we pass relative stage index+1 for CSV Stage)
        RecordStagePerformance(currentStageIndex, finalScore, timerAtEnd);

        // write stage CSV (guard duplicates)
        try
        {
            int stageDenom = GetStageDenominator(stageNumber);
            if (LocalCSVLogger.Instance != null)
            {
                if (!savedStagesThisAttempt.Contains(stageNumber))
                {
                    string pName = ResolvePlayerName();
                    int stageOrder = Mathf.Clamp(currentStageIndex + 1, 1, 3); // Stage column = 1/2/3
                    LocalCSVLogger.Instance.SaveStageTimestamps(
                        /*playerName*/ pName,
                        /*level*/ levelId,
                        /*stage (write relative order)*/ stageOrder,
                        /*cue*/ GetCueTypeLabel(),
                        /*scoreT1*/ scoreAtT1,
                        /*scoreT2*/ scoreAtT2,
                        /*scoreT3*/ scoreAtT3,
                        /*denom*/ stageDenom,
                        /*start*/ stageStartTime,
                        /*stop*/ stageStopTime
                    );
                    savedStagesThisAttempt.Add(stageNumber);
                    Debug.Log($"[L6 CSV] Stage CSV saved: player={pName} level={levelId} stageOrder={stageOrder}");
                }
                else
                {
                    Debug.Log($"[L6 CSV] Stage {stageNumber} already saved this attempt - skipping duplicate.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[L6] Stage CSV write failed: " + ex.Message);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverPanel_score != null) gameOverPanel_score.text = $"Score: {score:F1}   Time: {Mathf.Max(0, 60f - Mathf.FloorToInt(timer))}";
    }

    private int GetStageDenominator(int stage)
    {
        // Denoms copied from Level5 mapping (adjust if different)
        switch (stage)
        {
            case 1: return 7;
            case 2: return 7;
            case 3: return 6;
            case 4: return 6;
            case 5: return 9;
            case 6: return 9;
            case 7: return 9;
            case 8: return 9;
            case 9: return 9;
            case 10: return 9;
            case 11: return 6;
            case 12: return 6;
            default: return 8;
        }
    }

    private void RecordStagePerformance(int stageIndex, float finalScore, float timerAtEnd)
    {
        // Determine initial available no-bee objects for this stage's instruction type
        Level6Object.ObjectType instr = GetInstructionTypeForStage(stageNumber);
        int stageMaxAvailableNoBee = instr switch
        {
            Level6Object.ObjectType.Mango => mangoInitialNoBee,
            Level6Object.ObjectType.Apple => appleInitialNoBee,
            Level6Object.ObjectType.Sunflower => sunflowerInitialNoBee,
            Level6Object.ObjectType.Rose => roseInitialNoBee,
            _ => 0
        };

        float pct = 0f;

        // NEW RULE: if timerRemaining >= 1s at stage end => PF = 100%
        if (timerAtEnd >= 1f)
        {
            pct = 100f;
        }
        else
        {
            if (stageMaxAvailableNoBee <= 0)
                pct = 100f;
            else
                pct = Mathf.Clamp((finalScore / (float)stageMaxAvailableNoBee) * 100f, 0f, 100f);
        }

        Debug.Log($"[L6] Stage {stageNumber} (selectedIdx {stageIndex}) PF = {pct:F1}% (score={finalScore:F1}, initialNoBee={stageMaxAvailableNoBee}, timerAtEnd={timerAtEnd:F1})");

        // store into pf1/pf2/pf3 by currentStageIndex (0..2)
        switch (stageIndex)
        {
            case 0: pf1 = pct; break;
            case 1: pf2 = pct; break;
            case 2: pf3 = pct; break;
            default: break;
        }

        if (debugPFText != null)
            debugPFText.text = $"pf1:{pf1:F1} pf2:{pf2:F1} pf3:{pf3:F1} Pff:{Pff:F1}";

        // if this was the last of the three, compute final performance and save attempt PFF CSV (guarded)
        if (stageIndex >= (selectedStages.Count - 1))
        {
            ComputeFinalPerformance();

            attemptStopTime = DateTime.Now;

            if (!attemptPffSaved)
            {
                try
                {
                    if (LocalCSVLogger.Instance != null)
                    {
                        string pName = ResolvePlayerName();
                        LocalCSVLogger.Instance.SaveAttemptPFF(
                            /*playerName*/ pName,
                            /*level*/ levelId,
                            /*attemptNumber*/ attemptNumber,
                            /*cueKey*/ GetCueTypeLabel(),
                            /*pff*/ Pff,
                            /*start*/ attemptStartTime,
                            /*stop*/ attemptStopTime
                        );
                        attemptPffSaved = true;
                        Debug.Log($"[L6 CSV] Attempt PFF saved: player={pName} Pff={Pff:F1}%");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[L6] Attempt PFF CSV write failed: " + ex.Message);
                }
            }
            else
            {
                Debug.Log("[L6 CSV] Attempt PFF already saved for this attempt - skipping duplicate.");
            }
        }
    }

    private void ComputeFinalPerformance()
    {
        Pff = (0.2f * pf1) + (0.3f * pf2) + (0.5f * pf3);
        Pff = Mathf.Clamp(Pff, 0f, 100f);
        Debug.Log($"[L6] Final Pff = {Pff:F1}%");
        if (debugPFText != null) debugPFText.text = $"pf1:{pf1:F1} pf2:{pf2:F1} pf3:{pf3:F1} Pff:{Pff:F1}";
    }

    public void ResetPerformance()
    {
        pf1 = pf2 = pf3 = 0f;
        Pff = 0f;
    }

    private void ResetPerStageCounts()
    {
        mangoCount = appleCount = sunflowerCount = roseCount = 0;
        Level6Object.mangoCount = 0;
        Level6Object.appleCount = 0;
        Level6Object.sunflowerCount = 0;
        Level6Object.roseCount = 0;

        mangoCorrectNoBee = appleCorrectNoBee = sunflowerCorrectNoBee = roseCorrectNoBee = 0;
        mangoInitialNoBee = appleInitialNoBee = sunflowerInitialNoBee = roseInitialNoBee = 0;

        score = 0f;
        timer = 60f;

        UpdateCountUI();
        if (scoreText != null) scoreText.text = "Score: 0.0";
        if (timerText != null) timerText.text = "Time: 60";

        Debug.Log("[L6] ResetPerStageCounts complete - score and timer reset");
    }

    private void ComputeInitialRelevantNoBeeCounts()
    {
        mangoInitialNoBee = appleInitialNoBee = sunflowerInitialNoBee = roseInitialNoBee = 0;
        var relevantTypes = GetActiveObjectTypesForStage(stageNumber);
        var objs = FindObjectsOfType<Level6Object>(true);

        foreach (var o in objs)
        {
            if (o == null) continue;
            if (!o.gameObject.activeInHierarchy) continue;

            // ignore Additional-tagged objects for multi-type stages >=5 (keep consistent with Level5)
            if (stageNumber >= 5 && o.CompareTag("Additional")) continue;

            if (!relevantTypes.Contains(o.objectType)) continue;

            if (!o.HasBeeAttached())
            {
                switch (o.objectType)
                {
                    case Level6Object.ObjectType.Mango: mangoInitialNoBee++; break;
                    case Level6Object.ObjectType.Apple: appleInitialNoBee++; break;
                    case Level6Object.ObjectType.Sunflower: sunflowerInitialNoBee++; break;
                    case Level6Object.ObjectType.Rose: roseInitialNoBee++; break;
                }
            }
        }

        Debug.Log($"[L6] Initial no-bee counts -> Mango:{mangoInitialNoBee} Apple:{appleInitialNoBee} Sun:{sunflowerInitialNoBee} Rose:{roseInitialNoBee}");
    }

    private void ResetAllObjects()
    {
        var objs = FindObjectsOfType<Level6Object>(true);
        foreach (var o in objs)
        {
            if (o == null) continue;
            o.ResetObject();
            o.SetCuePermissions(currentCueTier >= CueTier.OneCue, currentCueTier >= CueTier.TwoCues);

            o.HideOutline();
            o.HideArrow();
            o.DisableGlow();
        }

        currentlyOutlined.Clear();
        currentlyArrowed.Clear();
        currentlyGlowing.Clear();
        bloomGloballyActive = false;
    }

    private void StartNewAttempt(CueTier nextTier, bool fullRestart, string reason)
    {
        Debug.Log($"[L6] StartNewAttempt -> tier={nextTier} fullRestart={fullRestart} reason={reason}");

        isGameOver = false;
        isTimerRunning = false;

        if (bloom != null) bloom.intensity.Override(minIntensity);
        if (VOLUME != null) { VOLUME.enabled = false; VOLUME.weight = 0f; }

        currentCueTier = nextTier;
        attemptIndex = (int)currentCueTier;
        attemptNumber++;

        // reset CSV bookkeeping for this new attempt
        savedStagesThisAttempt.Clear();
        attemptPffSaved = false;

        if (fullRestart)
        {
            GenerateSelectedStagesForAttempt();
            ResetPerformance();
            attemptStartTime = DateTime.Now;
        }

        currentStageIndex = 0;
        if (selectedStages.Count > 0)
            stageNumber = selectedStages[currentStageIndex];

        ResetPerStageCounts();

        timer = 60f;
        score = 0f;
        if (timerText != null) timerText.text = "Time: 60";
        if (scoreText != null) scoreText.text = "Score: 0.0";

        if (hand != null) hand.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (panel_failedlevel != null) panel_failedlevel.SetActive(false);
        if (instructionPanel != null) instructionPanel.SetActive(true);

        ActivateStageObjects();
        RegisterObjects();
        ResetAllObjects();
        ComputeInitialRelevantNoBeeCounts();

        ApplyCuePermissionsForAttempt();
        RefreshBeesForStage();

        if (pointer != null)
        {
            pointer.transform.localPosition = initialHandPos;
            pointer.transform.localRotation = initialHandRot;
        }

        UpdateLayersForStage();
        if (stagetext != null)
        {
            stagetext.text = STAGETEXTINSTRUTION[stageNumber];
            stagetext.enabled = true;
        }
        FindObjectOfType<MidpointAnimatorHelper>()?.PlayFromMarkedMidpoint();

        SetInstructionPanelText();

        Debug.Log($"[L6] StartNewAttempt complete - attempt#{attemptNumber} tier={currentCueTier} fullRestart={fullRestart}");
    }

    public void OnRetrySameStageClick()
    {
        CueTier next = currentCueTier;
        if (currentCueTier == CueTier.None) next = CueTier.OneCue;
        else if (currentCueTier == CueTier.OneCue) next = CueTier.TwoCues;
        else next = CueTier.TwoCues;

        StartNewAttempt(next, false, "retry_same_sequence");
    }

    public void OnFailedRestartClick()
    {
        CueTier next = currentCueTier;
        if (currentCueTier == CueTier.None) next = CueTier.OneCue;
        else if (currentCueTier == CueTier.OneCue) next = CueTier.TwoCues;
        else next = CueTier.TwoCues;

        StartNewAttempt(next, true, "failed_restart_full_sequence");
    }

    private void RefreshBeesForStage()
    {
        var beeScript = BeeFlightController?.GetComponent<BeeFlight>();
        if (beeScript == null)
        {
            Debug.LogWarning("[L6Manager] BeeFlight component not found!");
            return;
        }

        var sitList = new List<Transform>();
        foreach (var obj in allObjects)
        {
            if (obj != null && obj.gameObject.activeInHierarchy && obj.sitPoint != null)
            {
                sitList.Add(obj.sitPoint);
            }
        }

        Debug.Log($"[L6Manager] Refreshing bees for stage {stageNumber} with {sitList.Count} sit points");

        beeScript.RefreshBeesForNewStage(sitList.ToArray());
    }

    public void OnGameOverNextClick()
    {
        if (currentStageIndex < (selectedStages.Count - 1))
        {
            currentStageIndex++;
            if (selectedStages.Count > 0)
                stageNumber = selectedStages[currentStageIndex];

            Debug.Log($"[L6] Moving to stage {currentStageIndex + 1}/3 (stage #{stageNumber})");

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

            RefreshBeesForStage();
            SetInitialTimerForStage();
            SetInstructionPanelText();

            if (instructionPanel != null) instructionPanel.SetActive(true);
            if (stagetext != null)
            {
                stagetext.text = STAGETEXTINSTRUTION[stageNumber];
                stagetext.enabled = true;
            }

            ClearAllOutlines(mangoes);
            ClearAllOutlines(apples);
            ClearAllOutlines(sunflowers);
            ClearAllOutlines(roses);

            return;
        }

        ComputeFinalPerformance();
        lastAttemptPassed = (Pff >= passThreshold);

        Debug.Log($"[L6] All 3 stages complete. Pff={Pff:F1}% tier={currentCueTier} passed={lastAttemptPassed}");

        if (currentCueTier == CueTier.None)
        {
            if (lastAttemptPassed)
            {
                Debug.Log("[L6] PASSED with no cues -> Next Level!");
                if (level4_assest != null) level4_assest.SetActive(false);
                if (level5_assest != null) level5_assest.SetActive(false);
                if (player_5 != null) player_5.SetActive(false);
                if (player_6 != null) player_6.SetActive(true);
                if (congoPanel != null) congoPanel.SetActive(true);

                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                if (panel_failedlevel != null) panel_failedlevel.SetActive(false);
                if (scoreText != null) scoreText.enabled = false;
                if (timerText != null) timerText.enabled = false;

                return;
            }
            else
            {
                Debug.Log("[L6] FAILED with no cues -> Adding one cue");
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
                Debug.Log("[L6] PASSED with one cue -> Stepping down to no cues (full restart)");
                StartNewAttempt(CueTier.None, true, "step_down_from_one_cue");
                return;
            }
            else
            {
                Debug.Log("[L6] FAILED with one cue -> Adding arrows");
                if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again(Glow +Arrow Cue).\nTotalScore = {Pff:F1}%";
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                return;
            }
        }
        else // TwoCues
        {
            if (lastAttemptPassed)
            {
                Debug.Log("[L6] PASSED with two cues -> Stepping down to one cue (full restart)");
                StartNewAttempt(CueTier.OneCue, true, "step_down_from_two_cues");
                return;
            }
            else
            {
                Debug.Log("[L6] FAILED with two cues -> Stay at two cues");
                if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again.\nTotalScore = {Pff:F1}%";
                if (gameOverPanel != null) gameOverPanel.SetActive(false);
                return;
            }
        }
    }

    public void SetInstructionPanelText()
    {
        if (instructionPanelText == null) return;
        instructionPanelText.text = stageNumber switch
        {
            1 => "🍎 Pluck All Yellow Mangoes without bee",
            2 => "🥭 Pluck All Oranges without bee",
            3 => "🌻 Pluck All Blue flowers without bee",
            4 => "🌹 Pluck All Pink Roses without bee",
            5 => "🥭 Pluck All Yellow Mangoes without bee",
            6 => "🥭 Pluck All Oranges without bee",
            7 => "🌹 Pluck All Blue flowers without bee",
            8 => "🌹 Pluck All Pink Roses without bee",
            9 => "🥭 Pluck All Yellow Mangoes without bee",
            10 => "🥭 Pluck All Oranges without bee",
            11 => "🥭 Pluck All Blue flowers without bee",
            12 => "🥭 Pluck All Pink Roses without bee",
            _ => ""
        };
    }

    public float GetRemainingTime() => timer;
    public Level6Object.ObjectType GetCurrentInstructionType() => currentInstructionType;

    void ClearAllOutlines(GameObject[] group)
    {
        if (group == null) return;
        foreach (var go in group)
        {
            if (go == null) continue;
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) continue;
            var mats = new List<Material>(renderer.materials);
            mats.RemoveAll(mat => mat != null && mat.shader != null && mat.shader.name == "Custom/OutlineSimple");
            renderer.materials = mats.ToArray();
        }
    }

    public void UpdateLayersForStage()
    {
        Level6Object[] objs = FindObjectsOfType<Level6Object>(true);
        foreach (Level6Object obj in objs)
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

    public bool IsRelevantObject1(Level6Object obj)
    {
        int stage = stageNumber;
        switch (stage)
        {
            case 3: return obj.objectType == Level6Object.ObjectType.Sunflower && !obj.HasBeeAttached();
            case 4: return obj.objectType == Level6Object.ObjectType.Rose && !obj.HasBeeAttached();
            case 7: return obj.objectType == Level6Object.ObjectType.Sunflower && !obj.HasBeeAttached();
            case 8: return obj.objectType == Level6Object.ObjectType.Rose && !obj.HasBeeAttached();
            case 11: return obj.objectType == Level6Object.ObjectType.Sunflower && !obj.HasBeeAttached();
            case 12: return obj.objectType == Level6Object.ObjectType.Rose && !obj.HasBeeAttached();
            default: return false;
        }
    }

    public bool IsRelevantObject2(Level6Object obj)
    {
        int stage = stageNumber;
        switch (stage)
        {
            case 1: return obj.objectType == Level6Object.ObjectType.Mango && !obj.HasBeeAttached();
            case 2: return obj.objectType == Level6Object.ObjectType.Apple && !obj.HasBeeAttached();
            case 3: return obj.objectType == Level6Object.ObjectType.Sunflower && !obj.HasBeeAttached();
            case 4: return obj.objectType == Level6Object.ObjectType.Rose && !obj.HasBeeAttached();
            case 5: return obj.objectType == Level6Object.ObjectType.Mango && !obj.HasBeeAttached();
            case 6: return obj.objectType == Level6Object.ObjectType.Apple && !obj.HasBeeAttached();
            case 7: return obj.objectType == Level6Object.ObjectType.Sunflower && !obj.HasBeeAttached();
            case 8: return obj.objectType == Level6Object.ObjectType.Rose && !obj.HasBeeAttached();
            case 9: return obj.objectType == Level6Object.ObjectType.Mango && !obj.HasBeeAttached();
            case 10: return obj.objectType == Level6Object.ObjectType.Apple && !obj.HasBeeAttached();
            case 11: return obj.objectType == Level6Object.ObjectType.Sunflower && !obj.HasBeeAttached();
            case 12: return obj.objectType == Level6Object.ObjectType.Rose && !obj.HasBeeAttached();
            default: return false;
        }
    }

    // Bee notification API
    public void NotifyObjectTargeted(Level6Object target)
    {
        if (target == null) return;

        var bf = BeeFlightController?.GetComponent<BeeFlight>();
        if (bf != null && bf.lastActiveObject != null)
            RemoveRelevantGlowFrom(bf.lastActiveObject);

        AddRelevantGlowTo(target);
        ReevaluateCuesImmediately();
    }

    public void NotifyObjectAssigned(Level6Object target)
    {
        if (target == null) return;
        RemoveRelevantGlowFrom(target);
        ReevaluateCuesImmediately();
    }

    public void NotifyObjectReleased(Level6Object target)
    {
        if (target == null) return;
        AddRelevantGlowTo(target);
        ReevaluateCuesImmediately();
    }

    private void AddRelevantGlowTo(Level6Object obj)
    {
        if (obj == null) return;
        int glowLayer = LayerMask.NameToLayer("RelevantGlow");
        obj.gameObject.layer = glowLayer;
        SetLayerRecursively(obj.transform, glowLayer);
    }

    private void RemoveRelevantGlowFrom(Level6Object obj)
    {
        if (obj == null) return;
        int normalLayer = LayerMask.NameToLayer("Fruit");
        obj.gameObject.layer = normalLayer;
        SetLayerRecursively(obj.transform, normalLayer);
    }

    // ---------------------------
    // Helpers
    // ---------------------------
    private string ResolvePlayerName()
    {
        // Prefer PlayerDataLogger.CurrentPlayerID if available, else use the inspector playerName (or "Player")
        try
        {
            var pdType = Type.GetType("PlayerDataLogger");
            if (pdType != null)
            {
                var prop = pdType.GetProperty("CurrentPlayerID", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (prop != null)
                {
                    var val = prop.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
        }
        catch { /*ignore reflection failures*/ }

       
        return "Player";
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
    // -------------------------------
}
