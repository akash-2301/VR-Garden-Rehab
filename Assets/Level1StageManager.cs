using UnityEngine;
using System;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Level1StageManager : MonoBehaviour
{
    private DateTime stageStartTime;
    private DateTime stageStopTime;
    private DateTime attemptStartTime;
    private DateTime attemptStopTime;
    public GameObject[] stageGroups;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timertext;
    public TextMeshProUGUI leveltext;
    public TextMeshProUGUI leveltext2;
    public GameObject panel_instruction;
    public GameObject panel_gameover;
    public GameObject startPrompt;
    public GameObject hand;
    public timer_01 levelTimer;
    public GameObject canvas;
    public TextMeshProUGUI score_gameover;
    public UIFixer uiFixer;
    public Camera cam01;
    public GameObject player_3;
    public GameObject player_4;
    public GameObject level3_assest;
    public GameObject level2_assest;
    public GameObject panel_failedlevel;
    public TextMeshProUGUI failedLevelText;
    public Button failedRestartButton;
    public string[][] stageInstructions = new string[][]
    {
        new string[] {"-Pick all the Yellow MANGOES\n-Place your hand over a mango to pick it up.\n-Time limit is 60 sec"},
        new string[] {"-Pick all the Red APPLES\n-Place your hand over a apple to pick it up.\n-Time limit is 60 sec"},
        new string[] {"-Pick all the Yellow SUNFLOWER\n-Place your hand over a sunflower to pick it up.\n-Time limit is 60 sec "},
        new string[] {"-Pick all the Pink Roses\n-Place your hand over a rose to pick it up.\n-Time limit is 60 sec "},
    };
    public float[] stageTimes = { 60f, 60f, 60f, 60f, 60f };
    public int currentStage = 0;
    public float timeRemaining = 0f;
    private int instructionCounter = 0;
    private bool gameOverShown = false;
    public bool isGameOver = false;
    private bool stageCompleted = false;
    private Vector3 initialHandPosition;
    private Quaternion initialHandRotation;
    private int[] playOrder = new int[3];
    public PostProcessVolume VOLUME;
    private Bloom bloom;
    public bool enableBlinking = true;
    public float blinkSpeed = 15f;
    private float minIntensity = 1f;
    private float maxIntensity = 28f;
    private ColorGrading colorGrading;
    private float originalTemperature;
    private float originalPostExposure;
    private bool colorGradingBackedUp = false;
    public GameObject lightt;
    public int levelId = 1;
    private string playerName = "";
    private bool t1Sent = false;
    private bool t2Sent = false;
    private bool t3Sent = false;
    private float scoreAtT1 = 0f;
    private float scoreAtT2 = 0f;
    private float scoreAtT3 = 0f;
    public List<GameObject> appleGroup;
    public List<GameObject> mangoGroup;
    public List<GameObject> rosegroup;
    public List<GameObject> sunflowerGroup;
    public static float pf1, pf2, pf3, pf4;
    public static float Pff;
    public int[] stageMaxScores = { 14, 11, 8, 8 };
    private int attemptIndex = 0;
    public enum CueTier { None = 0, OneCue = 1, TwoCues = 2 }
    public CueTier currentCueTier = CueTier.TwoCues;
    public int attemptNumber = 1;
    private bool lastAttemptPassed = false;
    public float passThreshold = 70f;
    private bool bloomActiveThisAttempt = false;
    private Outline[] allOutlines;
    public Material mangoMaterial;
    public Material appleMaterial;
    public List<Renderer> mangoRenderers = new List<Renderer>();
    public List<Renderer> appleRenderers = new List<Renderer>();
    public AudioSource sfxSource;
    public AudioClip pluckSFX;


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

    void Start()
    {
        playerName = PlayerDataLogger.CurrentPlayerID ?? "Player";

        level2_assest.SetActive(true);
        ResetAllGroupMaterialsForNextAttempt();
        allOutlines = FindObjectsOfType<Outline>(true);
        initialHandPosition = hand.transform.localPosition;
        initialHandRotation = hand.transform.localRotation;
        if (VOLUME != null && VOLUME.profile != null)
        {
            VOLUME.profile.TryGetSettings(out bloom);
            if (bloom != null)
            {
                bloom.enabled.Override(true);
                bloom.intensity.Override(minIntensity);
            }
            VOLUME.profile.TryGetSettings(out colorGrading);
            if (colorGrading != null) colorGrading.enabled.Override(true);
        }
        playOrder[0] = 0;
        playOrder[1] = 1;
        playOrder[2] = (UnityEngine.Random.value < 0.5f) ? 2 : 3;
        ActivateOnlyCurrentStageGroup();
        Level1Object.counterText = counterText;
        Level1Object.ResetCounts();
        attemptIndex = 0;
        currentCueTier = CueTier.None;
        attemptNumber = 1;
        ApplyCuePermissionsForAttempt();
        attemptStartTime = DateTime.Now;
        
        instructionCounter = 1;
        int activeStage = GetActiveStageIndex();
        instructionText.text = stageInstructions[activeStage][0];
        counterText.enabled = true;
        counterText.text = "Score : 0";
        timertext.text = "Time : 00:00";
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        t1Sent = t2Sent = t3Sent = false;
        SetStageInteractable(false);
    }

    void Update()
    {
        timeRemaining = (levelTimer != null) ? levelTimer.timeRemaining : timeRemaining;
        if (levelTimer == null || !levelTimer.timeCounting || gameOverShown) return;
        float tr = levelTimer.timeRemaining;
        int secLeft = Mathf.FloorToInt(tr);
        int activeStage = GetActiveStageIndex();
        if (!t1Sent && (secLeft == 20 || tr < 20f))
        {
            scoreAtT1 = Level1Object.score;
            t1Sent = true;
        }
        if (!t2Sent && (secLeft == 10 || tr < 10f))
        {
            scoreAtT2 = Level1Object.score;
            t2Sent = true;
        }
        if (!t3Sent && (secLeft <= 0 || tr <= 0f))
        {
            scoreAtT3 = Level1Object.score;
            t3Sent = true;
        }
        if (activeStage >= 2 && bloomActiveThisAttempt && VOLUME != null && bloom != null)
        {
            if (levelTimer.timeRemaining < 20f && levelTimer.timeRemaining >= 0f && enableBlinking && bloom.enabled.value && (activeStage == 2 || activeStage == 3))
            {
                VOLUME.enabled = true;
                VOLUME.weight = 1;
                float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
                float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
                bloom.intensity.Override(intensity);
            }
            else
            {
                bloom.intensity.Override(minIntensity);
            }
        }
        int targetThisStage = GetStageTargetCount(activeStage);
        if (!isGameOver && (Level1Object.GetCurrentStageCount(activeStage) >= targetThisStage || levelTimer.timeRemaining <= 0f))
        {
            stageCompleted = true;
            TheGameover();
        }
    }

    private void ActivateOnlyCurrentStageGroup()
    {
        if (stageGroups == null || stageGroups.Length == 0) return;
        int activeIndex = GetActiveStageIndex();
        for (int i = 0; i < stageGroups.Length; i++)
        {
            if (stageGroups[i] != null)
                stageGroups[i].SetActive(i == activeIndex);
        }
    }
  
    public void NextAttempt()
    {
        ResetGroup(appleGroup);
        ResetGroup(mangoGroup);
        ResetGroup(rosegroup);
        ResetGroup(sunflowerGroup);
    }

    private void ResetGroup(List<GameObject> group)
    {
        foreach (var obj in group)
        {
            Level1Object lo = obj.GetComponent<Level1Object>();
            if (lo != null) lo.ResetMaterial();
        }
    }
//logic for correct interactabe objects 
    private void SetStageInteractable(bool enable)
    {
        if (stageGroups == null) return;
        foreach (var root in stageGroups)
        {
            if (root == null) continue;
            var levelObjs = root.GetComponentsInChildren<Level1Object>(true);
            foreach (var lo in levelObjs)
            {
                if (lo == null) continue;
                lo.enabled = enable;
            }
            var cols = root.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols) if (c != null) c.enabled = enable;
            var cols2 = root.GetComponentsInChildren<Collider2D>(true);
            foreach (var c in cols2) if (c != null) c.enabled = enable;
        }
    }

    //trigger after stage ends
public void TheGameover()
{
    if (isGameOver) return;

    SetStageInteractable(false);

    if (colorGrading != null && colorGradingBackedUp)
    {
        colorGrading.temperature.Override(originalTemperature);
        colorGrading.postExposure.Override(originalPostExposure);
    }

    isGameOver = true;
    gameOverShown = true;

    int activeStage = GetActiveStageIndex();
    RecordStagePerformance(activeStage, Level1Object.score);

    string pid = !string.IsNullOrEmpty(playerName) ? playerName : PlayerDataLogger.CurrentPlayerID ?? "Player";

    if (!t1Sent) scoreAtT1 = Level1Object.score;
    if (!t2Sent) scoreAtT2 = Level1Object.score;
    if (!t3Sent) scoreAtT3 = Level1Object.score;

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
        catch (System.Exception) { }
    }

    int lastStageIndex = playOrder.Length - 1;

    if (currentStage == lastStageIndex)
    {
        ComputeFinalPerformance();

       if (LocalCSVLogger.Instance != null)
        {
            try
            {
                attemptStopTime = DateTime.Now;
                string pidForAttempt = !string.IsNullOrEmpty(playerName) ? playerName : PlayerDataLogger.CurrentPlayerID ?? "Player";

                // Write a single-row PFF entry with only OverallPFF filled (other columns blank)
                LocalCSVLogger.Instance.SaveAttemptPFF(
                    pidForAttempt,
                    levelId,
                    attemptNumber,
                    GetCueTypeLabel(),
                    Pff,
                    attemptStartTime,
                    attemptStopTime
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CSV] SaveAttemptPFF failed: " + ex.Message);
            }
        }
    }

    if (levelTimer != null) levelTimer.timeCounting = false;
    if (hand != null) hand.SetActive(false);

    score_gameover.gameObject.SetActive(true);
    panel_gameover.SetActive(true);

    int timeUsed = Mathf.FloorToInt(stageTimes[activeStage] - timeRemaining);
    int minutes = timeUsed / 60;
    int seconds = timeUsed % 60;

    score_gameover.text = $"Score: {Level1Object.score:F1}   Time : {minutes:00}:{seconds:00}";

    Level1Object.ResetCounts();
}


//function assigned to instruction panel 'Next' button
    public void OnInstructionNextClick()
    {
        HandPositionReceiver receiver = FindObjectOfType<HandPositionReceiver>();
        if (receiver != null) receiver.RefreshMapping(cam01);
        int activeStage = GetActiveStageIndex();
        if (activeStage == 2 && colorGrading != null)
        {
            if (!colorGradingBackedUp)
            {
                originalTemperature = colorGrading.temperature.value;
                originalPostExposure = colorGrading.postExposure.value;
                colorGradingBackedUp = true;
            }
            colorGrading.active = true;
            colorGrading.enabled.Override(true);
            colorGrading.temperature.Override(-22.9f);
            colorGrading.postExposure.Override(-2.2f);
        }
        timertext.enabled = true;
        string[] currentInstructions = stageInstructions[activeStage];
        if (instructionCounter < currentInstructions.Length)
        {
            instructionText.text = currentInstructions[instructionCounter];
            instructionCounter++;
        }
        else
        {
            panel_instruction.SetActive(false);
            instructionCounter = 0;
            StartCoroutine(BeginStage());
        }
    }

    IEnumerator BeginStage()
    {
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        int activeStage = GetActiveStageIndex();
        levelTimer.timeRemaining = stageTimes[activeStage];
        levelTimer.timeCounting = false;
        Time.timeScale = 0f;
        startPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        startPrompt.SetActive(false);
        stageStartTime = DateTime.Now;
        ActivateOnlyCurrentStageGroup();
        UpdateLayersForStage();
        ApplyCuePermissionsForAttempt();
        Level1Object.counterText = counterText;
        if (hand != null) hand.SetActive(true);
        gameOverShown = false;
        isGameOver = false;
        stageCompleted = false;
        levelTimer.timerText.gameObject.SetActive(true);
        counterText.gameObject.SetActive(true);
        Time.timeScale = 1f;
        levelTimer.timeCounting = true;
        SetStageInteractable(true);
    }
// function assigned to next stage button
    public void NextStage()
    {
        if (bloom != null) bloom.intensity.Override(minIntensity);
        if (colorGrading != null && colorGradingBackedUp)
        {
            colorGrading.temperature.Override(originalTemperature);
            colorGrading.postExposure.Override(originalPostExposure);
        }
        if (panel_gameover != null) panel_gameover.SetActive(false);
        if (hand != null)
        {
            hand.transform.localPosition = initialHandPosition;
            hand.transform.localRotation = initialHandRotation;
            hand.SetActive(false);
        }
        counterText.enabled = false;
        timertext.enabled = false;
        int lastStageIndex = playOrder.Length - 1;
        if (currentStage == lastStageIndex)
        {
            ComputeFinalPerformance();
            lastAttemptPassed = (Pff > passThreshold);
            if (currentCueTier == CueTier.None)
            {
                if (lastAttemptPassed)
                {
                    currentStage = 0;
                    if (panel_failedlevel != null) panel_failedlevel.SetActive(false);
                    if (panel_gameover != null) panel_gameover.SetActive(false);
                    if (lightt != null) lightt.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    if (player_3 != null) player_3.SetActive(false);
                    var l80 = GetComponent<Level3StageManager>();
                    if (l80 != null) l80.enabled = true;
                    if (player_4 != null) player_4.SetActive(true);
                    if (level3_assest != null) level3_assest.SetActive(true);
                }
                else
                {
                    if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                    if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again(Glow Cue).\nTotalScore = {Pff:F1}%";
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
                    if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                    if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again(Glow +Arrow Cue).\nTotalScore = {Pff:F1}%";
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
                    if (panel_failedlevel != null) panel_failedlevel.SetActive(true);
                    if (failedLevelText != null) failedLevelText.text = $"Level failed..\nTry again..\nTotalScore = {Pff:F1}%";
                }
            }
            gameOverShown = true;
            isGameOver = true;
            return;
        }
        Level1Object.ResetCounts();
        currentStage++;
        int active = GetActiveStageIndex();
        if (lightt != null)
        {
            if (active == 1) lightt.transform.rotation = Quaternion.Euler(50f, -245.5f, 0f);
            else if (active == 2) lightt.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
        }
        ActivateOnlyCurrentStageGroup();
        if (active == 2 && colorGrading != null)
        {
            if (!colorGradingBackedUp)
            {
                originalTemperature = colorGrading.temperature.value;
                originalPostExposure = colorGrading.postExposure.value;
                colorGradingBackedUp = true;
            }
            colorGrading.temperature.Override(-22.9f);
            colorGrading.postExposure.Override(-2.2f);
        }
        else if (active == 3 && colorGrading != null)
        {
            if (!colorGradingBackedUp)
            {
                originalTemperature = colorGrading.temperature.value;
                originalPostExposure = colorGrading.postExposure.value;
                colorGradingBackedUp = true;
            }
            colorGrading.temperature.Override(100f);
            colorGrading.postExposure.Override(-1.2f);
        }
        gameOverShown = false;
        isGameOver = false;
        if (currentStage == 0 || currentStage == 1 || currentStage == 3) ShowInstructionPanel();
    }

    public void ShowInstructionPanel()
    {
        Level1Object.counterText = counterText;
        counterText.enabled = true;
        counterText.text = "Score : 0";
        timertext.text = "Time : 00:00";
        panel_instruction.SetActive(true);
        int active = GetActiveStageIndex();
        if (active == 2 || active == 3) leveltext2.gameObject.SetActive(true);
        instructionText.text = stageInstructions[active][0];
        instructionCounter = 1;
    }
// logic for relevant layers attaching
    public void UpdateLayersForStage()
    {
        int active = GetActiveStageIndex();
        Level1Object[] allObjects = FindObjectsOfType<Level1Object>(true);
        foreach (Level1Object obj in allObjects)
        {
            bool isRelevant = IsRelevantObject1(obj, active);
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
// mapping relevant object
    public bool IsRelevantObject1(Level1Object obj, int stageIndexOverride)
    {
        int stageToCheck = stageIndexOverride;
        switch (stageToCheck)
        {
            case 0: return obj.objectType == Level1Object.Level1ObjectType.Mango;
            case 1: return obj.objectType == Level1Object.Level1ObjectType.Apple;
            case 2: return obj.objectType == Level1Object.Level1ObjectType.Sunflower;
            case 3: return obj.objectType == Level1Object.Level1ObjectType.Rose;
            default: return false;
        }
    }
// PFFF logic
    private void RecordStagePerformance(int stageIndex, float rawScore)
    {
        float denom = (stageIndex >= 0 && stageIndex < stageMaxScores.Length) ? stageMaxScores[stageIndex] : 8f;
        float pct = Mathf.Clamp((rawScore / denom) * 100f, 0f, 100f);
        int playPosition = -1;
        for (int i = 0; i < playOrder.Length; i++)
        {
            if (playOrder[i] == stageIndex) { playPosition = i; break; }
        }
        if (playPosition == 0) pf1 = pct;
        else if (playPosition == 1) pf2 = pct;
        else if (playPosition == 2) pf3 = pct;
        else
        {
            switch (stageIndex)
            {
                case 0: pf1 = pct; break;
                case 1: pf2 = pct; break;
                case 2: pf3 = pct; break;
                case 3: pf4 = pct; break;
            }
        }
    }

    private void ComputeFinalPerformance()
    {
        Pff = (0.2f * pf1) + (0.3f * pf2) + (0.5f * pf3);
    }

    public void OnFailedRestartClick()
    {
        CueTier next = currentCueTier;
        if (currentCueTier == CueTier.None) next = CueTier.OneCue;
        else if (currentCueTier == CueTier.OneCue) next = CueTier.TwoCues;
        else next = CueTier.TwoCues;
        attemptNumber++;
        StartClosedLoopRun(next, "fail");
    }

    private void ApplyCuePermissionsForAttempt()
    {
        bool allowBlink = (currentCueTier >= CueTier.OneCue);
        bool allowArrow = (currentCueTier >= CueTier.TwoCues);
        bloomActiveThisAttempt = allowBlink;
        Level1Object[] objs = FindObjectsOfType<Level1Object>(true);
        foreach (var obj in objs) obj.SetCuePermissions(allowBlink, allowArrow);
    }

    public void UpdateObjectCountUI()
    {
        if (counterText != null)
        {
            string safeText = "Score : " + Level1Object.score.ToString("0.0");
            counterText.SetText(safeText, true);
        }
    }

    public float GetTimeRemaining()
    {
        return levelTimer != null ? levelTimer.timeRemaining : 0f;
    }

    private int GetStageTargetCount(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stageMaxScores.Length) return 8;
        return stageMaxScores[stageIndex];
    }

    public int GetActiveStageIndex()
    {
        if (playOrder == null || playOrder.Length == 0) return currentStage;
        int idx = Mathf.Clamp(currentStage, 0, playOrder.Length - 1);
        return playOrder[idx];
    }
// Logic for next attempt if one fails 
    private void StartClosedLoopRun(CueTier nextTier, string reason)
    {
        ResetAllGroupMaterialsForNextAttempt();
        NextAttempt();
        Level1Object.ResetCounts();
        pf1 = pf2 = pf3 = pf4 = 0f;
        Pff = 0f;
        currentCueTier = nextTier;
        ApplyCuePermissionsForAttempt();
        if (player_3 != null) player_3.SetActive(true);
        if (level3_assest != null) level3_assest.SetActive(true);
        if (player_4 != null) player_4.SetActive(false);
        currentStage = 0;
        ActivateOnlyCurrentStageGroup();
        if (panel_failedlevel != null) panel_failedlevel.SetActive(false);
        if (panel_gameover != null) panel_gameover.SetActive(false);
        counterText.enabled = false;
        timertext.enabled = false;
        if (leveltext != null) leveltext.gameObject.SetActive(false);
        if (leveltext2 != null) leveltext2.gameObject.SetActive(false);
        if (hand != null)
        {
            hand.transform.localPosition = initialHandPosition;
            hand.transform.localRotation = initialHandRotation;
            hand.SetActive(false);
        }
        foreach (Level1Object obj in FindObjectsOfType<Level1Object>(true))
        {
            if (obj == null) continue;
            obj.gameObject.SetActive(true);
            obj.ResetForRetry();
        }
        if (levelTimer != null)
        {
            levelTimer.timeCounting = false;
            levelTimer.timeRemaining = stageTimes[GetActiveStageIndex()];
        }
        attemptStartTime = DateTime.Now;
     
        t1Sent = t2Sent = t3Sent = false;
        scoreAtT1 = scoreAtT2 = scoreAtT3 = 0f;
        FindObjectOfType<MyController>()?.ResetCameraAndPlayer();
        FindObjectOfType<MyController>()?.ReplayIntroAnimation();
        FindObjectOfType<MidpointAnimatorHelper>()?.PlayFromMarkedMidpoint();
        isGameOver = false;
        gameOverShown = false;
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
}
