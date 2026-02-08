using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level1Object : MonoBehaviour
{
    public Level1StageManager manager;            // corrected name
    public timer_01 timer;                        // optional; prefer manager.levelTimer if available

    public enum Level1ObjectType { Mango, Apple, Sunflower, Rose }
    public Level1ObjectType objectType;

    public Transform pointer;
    public float disappearViewportThreshold = 0.03f;

    // counts + UI
    public static int mangoCount, appleCount, sunflowerCount, roseCount;
    public static TextMeshProUGUI counterText;
    public static TextMeshProUGUI mangoCountText;
    public static TextMeshProUGUI appleCountText;
    public static TextMeshProUGUI SUNFLOWERCountText;
    public static TextMeshProUGUI ROSECountText;

    public static float score = 0f;

    // state
    private bool hasDisappeared = false;

    // floating text & UI
    public GameObject plusOnePrefab;
    public GameObject minusOnePrefab;
    public GameObject minusPointTwoPrefab;
    public Canvas uiCanvas;
    public Canvas arrow_Canvas;
    public Vector3 screenOffset = new Vector3(30f, 0f, 0f);
    public Vector3 worldOffsett = new Vector3(0f, 0.5f, 0f);

    // arrow
    public GameObject arrowPrefab;
    private GameObject arrowInstance;
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    // outline
    public Material outlineMaterial;
    public Material outlineMaterial2;
    private Material usedOutlineMat;
    private bool outlineApplied = false;

    // cue permissions
    private bool allowBlinkCue = false;
    private bool allowArrowCue = false;

    // reset material (assign default mesh material in inspector)
    public Material resetMaterial;

    // --- lifecycle ---
    private void Awake()
    {
        // try auto-binding manager if not assigned in inspector
        if (manager == null)
            manager = FindObjectOfType<Level1StageManager>();
    }

    private void Start()
    {
        if (arrowPrefab != null && arrow_Canvas != null)
        {
            arrowInstance = Instantiate(arrowPrefab, arrow_Canvas.transform);
            arrowInstance.SetActive(false);
        }

        // ensure resetMaterial falls back to current renderer material
        if (resetMaterial == null)
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) resetMaterial = mr.sharedMaterial;
        }
    }

    private void OnDisable()
    {
        // restore material when disabled to avoid leaving instance materials in scene
        RestoreOriginalMaterial();
        if (arrowInstance != null) arrowInstance.SetActive(false);
        outlineApplied = false;
        usedOutlineMat = null;
    }

    // --- Public API ---

    public void ResetMaterial()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null && resetMaterial != null)
            mr.material = resetMaterial;

        var outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;

        // clear local outline state
        if (usedOutlineMat != null)
        {
            Destroy(usedOutlineMat);
            usedOutlineMat = null;
        }
        outlineApplied = false;
    }

    public void SetCuePermissions(bool allowBlink, bool allowArrow)
    {
        allowBlinkCue = allowBlink;
        allowArrowCue = allowArrow;

        if (!allowBlinkCue && outlineApplied)
            RemoveOutlineImmediate();

        if (!allowArrowCue && arrowInstance != null)
            arrowInstance.SetActive(false);
    }

    public void ResetForRetry()
    {
        hasDisappeared = false;
        // restore visuals
        ResetMaterial();
        if (arrowInstance != null) arrowInstance.SetActive(false);
    }

    // --- Update loop ---

    void Update()
    {
        if (hasDisappeared || pointer == null)
        {
            if (arrowInstance != null) arrowInstance.SetActive(false);
            return;
        }

        if (manager != null && manager.isGameOver)
        {
            if (arrowInstance != null) arrowInstance.SetActive(false);
            outlineApplied = false;
            return;
        }

        float timeRemaining = GetTimeRemainingSafe();

        // decide arrow visibility (only when allowed and in final seconds)
        bool shouldShowArrow = false;
        int stage = 0;
        if (manager != null) stage = manager.GetActiveStageIndex();

        if (allowArrowCue && timeRemaining < 10f)
        {
            switch (stage)
            {
                case 0: if (objectType == Level1ObjectType.Mango) shouldShowArrow = true; break;
                case 1: if (objectType == Level1ObjectType.Apple) shouldShowArrow = true; break;
                case 2: if (objectType == Level1ObjectType.Sunflower) shouldShowArrow = true; break;
                case 3: if (objectType == Level1ObjectType.Rose) shouldShowArrow = true; break;
            }
        }

        if (arrowInstance != null)
        {
            arrowInstance.SetActive(shouldShowArrow);
            if (shouldShowArrow)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector3 screenPos = cam.WorldToScreenPoint(transform.position + worldOffset);
                    arrowInstance.transform.position = screenPos;
                }
            }
        }

        // blink / outline (only when allowed, not yet applied, final seconds, and object relevant)
        if (allowBlinkCue && !outlineApplied && timeRemaining < 20f && ShouldShowVisuals())
        {
            ApplyOutlineInstance();
        }

        // outline pulsing
        if (allowBlinkCue && outlineApplied && usedOutlineMat != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * 4f));
            if (usedOutlineMat.HasProperty("_OutlineColor"))
            {
                Color currentColor = usedOutlineMat.GetColor("_OutlineColor");
                usedOutlineMat.SetColor("_OutlineColor", new Color(currentColor.r, currentColor.g, currentColor.b, alpha));
            }
        }

        // pickup detection (viewport distance)
        var camMain = Camera.main;
        if (camMain == null) return;

        Vector3 vpObject = camMain.WorldToViewportPoint(transform.position);
        Vector3 vpPointer = camMain.WorldToViewportPoint(pointer.position);

        float distance = Vector2.Distance(
            new Vector2(vpObject.x, vpObject.y),
            new Vector2(vpPointer.x, vpPointer.y)
        );

        if (distance < disappearViewportThreshold)
        {
            hasDisappeared = true;
            AddCount(objectType);
           
            gameObject.SetActive(false);
             if (manager != null && manager.sfxSource != null && manager.pluckSFX != null)
{
    manager.sfxSource.PlayOneShot(manager.pluckSFX);
}

            if (arrowInstance != null) arrowInstance.SetActive(false);
        }
    }

    // --- Helpers ---

    private float GetTimeRemainingSafe()
    {
        if (manager != null) return manager.GetTimeRemaining();
        if (timer != null) return timer.timeRemaining;
        return float.PositiveInfinity;
    }

    private void ApplyOutlineInstance()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) return;

        Material baseMat = (objectType == Level1ObjectType.Mango || gameObject.name.ToLower().Contains("mango"))
            ? outlineMaterial : outlineMaterial2;

        if (baseMat == null) return;

        // create instance so we can animate color safely
        Material instanceMat = new Material(baseMat);
        // try to set sensible defaults for common names
        if (gameObject.name.ToLower().Contains("mango"))
        {
            if (instanceMat.HasProperty("_OutlineColor")) instanceMat.SetColor("_OutlineColor", new Color32(255, 255, 0, 255));
            if (instanceMat.HasProperty("_OutlineWidth")) instanceMat.SetFloat("_OutlineWidth", 0.001f);
        }
        else if (gameObject.name.ToLower().Contains("apple"))
        {
            if (instanceMat.HasProperty("_OutlineColor")) instanceMat.SetColor("_OutlineColor", new Color32(255, 255, 255, 250));
            if (instanceMat.HasProperty("_OutlineWidth")) instanceMat.SetFloat("_OutlineWidth", 0.025f);
        }

        // assign instance material (preserve original via resetMaterial)
        mr.material = instanceMat;
        usedOutlineMat = instanceMat;
        outlineApplied = true;
    }

    private void RemoveOutlineImmediate()
    {
        // restore base material
        RestoreOriginalMaterial();
        if (usedOutlineMat != null)
        {
            Destroy(usedOutlineMat);
            usedOutlineMat = null;
        }
        outlineApplied = false;
    }

    private void RestoreOriginalMaterial()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) return;
        if (resetMaterial != null)
            mr.material = resetMaterial;
    }

    private void SpawnFloatingText(float scoreDelta)
    {
        GameObject prefabToUse = null;

        if (scoreDelta == 1f)
            prefabToUse = plusOnePrefab;
        else if (scoreDelta < 0f)
            prefabToUse = minusOnePrefab; // you can adjust which prefab for negative

        if (prefabToUse != null && uiCanvas != null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + worldOffsett);
            screenPos += screenOffset;

            GameObject ft = Instantiate(prefabToUse, uiCanvas.transform);
            ft.transform.position = screenPos;
        }
    }

    private void AddCount(Level1ObjectType type)
    {
        float scoreDelta = 0f;

        if (manager == null)
        {
            Debug.LogWarning("[Level1Object] Manager missing.");
            return;
        }

        bool isRelevant = manager.IsRelevantObject1(this, manager.GetActiveStageIndex());
        if (isRelevant)
        {
            switch (type)
            {
                case Level1ObjectType.Mango: mangoCount++; break;
                case Level1ObjectType.Apple: appleCount++; break;
                case Level1ObjectType.Sunflower: sunflowerCount++; break;
                case Level1ObjectType.Rose: roseCount++; break;
            }
            scoreDelta = 1f;
        }

        score += scoreDelta;

        if (float.IsNaN(score) || float.IsInfinity(score))
        {
            Debug.LogError("[Level1Object] Score invalid! Resetting to 0.");
            score = 0f;
        }

        string safeText = "Score : " + score.ToString("0.0");

        if (counterText != null)
        {
            counterText.SetText(safeText, true);
            counterText.ForceMeshUpdate(true, true);
        }

        SpawnFloatingText(scoreDelta);
    }

    public static int GetCurrentStageCount(int stage)
    {
        switch (stage)
        {
            case 0: return mangoCount;
            case 1: return appleCount;
            case 2: return sunflowerCount;
            case 3: return roseCount;
            default: return 0;
        }
    }

    public static void ResetCounts()
    {
        mangoCount = appleCount = sunflowerCount = roseCount = 0;
        score = 0f;
    }

    private bool ShouldShowVisuals()
    {
        int active = 0;
        if (manager != null) active = manager.GetActiveStageIndex();
        switch (active)
        {
            case 0: return objectType == Level1ObjectType.Mango;
            case 1: return objectType == Level1ObjectType.Apple;
            case 2: return objectType == Level1ObjectType.Sunflower;
            case 3: return objectType == Level1ObjectType.Rose;
            default: return false;
        }
    }
}
