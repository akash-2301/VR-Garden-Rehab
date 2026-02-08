using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level3Object : MonoBehaviour
{
    public Level3StageManager managaer;
    public timer_02 timer;

    public enum Level3ObjectType
    {
        Mango,
        Apple,
        Sunflower,
        Rose,
    }

    public Level3ObjectType objectType;
    public Transform pointer;
    public float disappearViewportThreshold = 0.03f;

    public static int mangoCount, appleCount, sunflowerCount, roseCount;
    public static TextMeshProUGUI counterText;
    public static TextMeshProUGUI mangoCountText;
    public static TextMeshProUGUI appleCountText;
    public static TextMeshProUGUI SUNFLOWERCountText;
    public static TextMeshProUGUI ROSECountText;

    public static float score = 0f;

    private bool hasDisappeared = false;

    public GameObject plusOnePrefab;
    public GameObject minusOnePrefab;
    public GameObject minusPointTwoPrefab;
    public Canvas uiCanvas;
    public Canvas arrow_Canvas;
    public Vector3 screenOffset = new Vector3(30f, 0f, 0f);
    public Vector3 worldOffsett = new Vector3(0f, 0.5f, 0f);

    public GameObject arrowPrefab;
    private GameObject arrowInstance;
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    public Material outlineMaterial;
    public Material outlineMaterial2;
    private Material usedOutlineMat;
    private bool outlineApplied = false;

    private bool allowBlinkCue = false;
    private bool allowArrowCue = false;
    public Material resetMaterial;

    public void ResetMaterial()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null && resetMaterial != null)
            mr.material = resetMaterial;

        Outline outline = GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }

    public void SetCuePermissions(bool allowBlink, bool allowArrow)
    {
        allowBlinkCue = allowBlink;
        allowArrowCue = allowArrow;
    }

    public void ResetForRetry()
    {
        hasDisappeared = false;
        outlineApplied = false;
        if (arrowInstance != null) arrowInstance.SetActive(false);
    }

    private void Start()
    {
        if (arrowPrefab != null && arrow_Canvas != null)
        {
            arrowInstance = Instantiate(arrowPrefab, arrow_Canvas.transform);
            arrowInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (hasDisappeared || pointer == null)
        {
            if (arrowInstance != null) arrowInstance.SetActive(false);
            return;
        }

        if (managaer != null && managaer.isGameOver)
        {
            if (arrowInstance != null) arrowInstance.SetActive(false);
            outlineApplied = false;
            return;
        }

        bool shouldShowArrow = false;
        int activeStage = (managaer != null) ? managaer.GetActiveStageIndex() : 0;

        if (allowArrowCue && timer != null && timer.timeRemaining < 10f)
        {
            switch (activeStage)
            {
                case 0: if (objectType == Level3ObjectType.Mango) shouldShowArrow = true; break;
                case 1: if (objectType == Level3ObjectType.Apple) shouldShowArrow = true; break;
                case 2: if (objectType == Level3ObjectType.Sunflower) shouldShowArrow = true; break;
                case 3: if (objectType == Level3ObjectType.Rose) shouldShowArrow = true; break;
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

        if (allowBlinkCue && !outlineApplied && timer != null && timer.timeRemaining < 20f && ShouldShowVisuals())
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material baseMat = (objectType == Level3ObjectType.Mango || gameObject.name.ToLower().Contains("mango"))
                    ? outlineMaterial : outlineMaterial2;

                if (baseMat != null)
                {
                    Material instanceMat = new Material(baseMat);

                    if (gameObject.name.ToLower().Contains("mango"))
                    {
                        instanceMat.SetColor("_OutlineColor", new Color32(255, 255, 0, 255));
                        instanceMat.SetFloat("_OutlineWidth", 0.001f);
                    }
                    else if (gameObject.name.ToLower().Contains("apple"))
                    {
                        instanceMat.SetColor("_OutlineColor", new Color32(255, 255, 255, 250));
                        instanceMat.SetFloat("_OutlineWidth", 0.025f);
                    }

                    mr.material = instanceMat;
                    usedOutlineMat = instanceMat;
                    outlineApplied = true;
                }
            }
        }

        if (allowBlinkCue && outlineApplied && usedOutlineMat != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * 4f));
            Color currentColor = usedOutlineMat.GetColor("_OutlineColor");
            usedOutlineMat.SetColor("_OutlineColor", new Color(currentColor.r, currentColor.g, currentColor.b, alpha));
        }

        var camMain = Camera.main;
        if (camMain == null) return;
        if (pointer == null) return;

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
            if (managaer != null && managaer.sfxSource != null && managaer.pluckSFX != null)
{
    managaer.sfxSource.PlayOneShot(managaer.pluckSFX);
}

            if (arrowInstance != null) arrowInstance.SetActive(false);
        }
    }

    private bool ShouldShowVisuals()
    {
        int activeStage = (managaer != null) ? managaer.GetActiveStageIndex() : 0;
        switch (activeStage)
        {
            case 0: return objectType == Level3ObjectType.Mango;
            case 1: return objectType == Level3ObjectType.Apple;
            case 2: return objectType == Level3ObjectType.Sunflower;
            case 3: return objectType == Level3ObjectType.Rose;
            default: return false;
        }
    }

    private void SpawnFloatingText(float scoreDelta)
    {
        GameObject prefabToUse = null;

        if (Mathf.Approximately(scoreDelta, 1f))
            prefabToUse = plusOnePrefab;
        else if (Mathf.Approximately(scoreDelta, -0.5f))
            prefabToUse = minusOnePrefab;
        else if (Mathf.Approximately(scoreDelta, -0.2f))
            prefabToUse = minusPointTwoPrefab;

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

    private void AddCount(Level3ObjectType type)
    {
        float scoreDelta = 0f;
        int activeStage = (managaer != null) ? managaer.GetActiveStageIndex() : 0;

        switch (type)
        {
            case Level3ObjectType.Mango:
                if (activeStage == 0) { mangoCount++; scoreDelta = 1f; }
                else { scoreDelta = -0.5f; }
                break;
            case Level3ObjectType.Apple:
                if (activeStage == 1) { appleCount++; scoreDelta = 1f; }
                else { scoreDelta = -0.5f; }
                break;
            case Level3ObjectType.Sunflower:
                if (activeStage == 2) { sunflowerCount++; scoreDelta = 1f; }
                else { scoreDelta = -0.5f; }
                break;
            case Level3ObjectType.Rose:
                if (activeStage == 3) { roseCount++; scoreDelta = 1f; }
                else { scoreDelta = -0.5f; }
                break;
        }

        score += scoreDelta;
        if (float.IsNaN(score) || float.IsInfinity(score))
        {
            score = 0f;
        }

        if (counterText != null)
        {
            counterText.text = $"Score : {score:F1}";
        }

        SpawnFloatingText(scoreDelta);

        if (managaer != null)
        {
            managaer.UpdateObjectCountUI();
        }
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
}
