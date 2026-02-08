using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Level8Object : MonoBehaviour
{
    public enum ObjectType { Mango, Apple, Sunflower, Rose }
    public ObjectType objectType;

    [Header("References")]
    public Level8StageManager manager;
    public Transform pointer;
    public Canvas uiCanvas;
    public Canvas arrowCanvas;

    [Header("Arrow & Outline")]
    public GameObject arrowPrefab;
    public Material mangoOutlineMat;
    public Material appleOutlineMat;
    public Material sunflowerOutlineMat;
    public Material roseOutlineMat;
    public Vector3 arrowWorldOffset = new Vector3(0f, 2f, 0f);

    [Header("Floating Text")]
    public GameObject plusOnePrefab;
    public GameObject minusOnePrefab;
    public Vector3 scoreWorldOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 screenOffset = new Vector3(30f, 0f, 0f);

    [Header("Panel UI")]
    public TextMeshProUGUI countText;
    public Image circleImage;

    [Header("Pickup Settings")]
    public float disappearViewportThreshold = 0.03f;

    // Runtime
    private bool hasDisappeared = false;
    private GameObject arrowInstance;
    private bool outlineApplied = false;
    private Material usedOutlineMat;
    private Material lastUsedOutlineMat;

    // Cue permissions (set by StageManager per attempt)
    private bool allowBlinkCue = false;
    private bool allowArrowCue = false;

    // Global counts
    public static int appleCount = 0;
    public static int mangoCount = 0;
    public static int roseCount = 0;
    public static int sunflowerCount = 0;

    public void SetCuePermissions(bool allowBlink, bool allowArrow)
    {
        allowBlinkCue = allowBlink;
        allowArrowCue = allowArrow;

        if (!allowBlinkCue && outlineApplied)
            RemoveOutlineNow();
        if (!allowArrowCue && arrowInstance != null)
            arrowInstance.SetActive(false);
    }

    private void Start()
    {
        if (arrowPrefab && arrowCanvas)
        {
            arrowInstance = Instantiate(arrowPrefab, arrowCanvas.transform, false);
            arrowInstance.SetActive(false);
        }

        if (circleImage != null)
            circleImage.enabled = false;
    }

    private void Update()
    {
        if (manager != null && allowBlinkCue)
            HandleOutlineBlink();

        if (hasDisappeared || pointer == null || manager == null || manager.isGameOver)
        {
            if (arrowInstance) arrowInstance.SetActive(false);
            return;
        }

        HandleArrow();
        HandlePickup();
    }

    // Arrow
    private void HandleArrow()
    {
        if (arrowInstance == null || Camera.main == null || manager == null) return;

        bool shouldShow = allowArrowCue && manager.timer < 10f && manager.IsRelevantObject(this);
        arrowInstance.SetActive(shouldShow);

        if (shouldShow)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + arrowWorldOffset);
            arrowInstance.transform.position = screenPos;
        }
    }

    // Outline / blink
    private void HandleOutlineBlink()
    {
        if (manager == null) return;

        bool shouldShowOutline = manager.ShouldShowOutline(this);

        if (shouldShowOutline && !outlineApplied)
            ApplyOutline();
        else if (!shouldShowOutline && outlineApplied)
            RemoveOutlineNow();

        if (outlineApplied && usedOutlineMat != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * 4f));
            Color c = usedOutlineMat.GetColor("_OutlineColor");
            usedOutlineMat.SetColor("_OutlineColor", new Color(c.r, c.g, c.b, alpha));
        }

        Material source = GetOutlineMat();
        if (outlineApplied && lastUsedOutlineMat != source)
        {
            RemoveOutlineNow();
            ApplyOutline();
        }
    }

    private void ApplyOutline()
    {
        Material outlineMat = GetOutlineMat();
        if (outlineMat != null && TryGetComponent<MeshRenderer>(out var mr))
        {
            Material inst = new Material(outlineMat);
            var baseMats = mr.materials;
            var newMats = new Material[baseMats.Length + 1];
            for (int i = 0; i < baseMats.Length; i++) newMats[i] = baseMats[i];
            newMats[newMats.Length - 1] = inst;
            mr.materials = newMats;

            usedOutlineMat = inst;
            lastUsedOutlineMat = outlineMat;
            outlineApplied = true;
        }
    }

    private void RemoveOutlineNow()
    {
        if (!outlineApplied) return;

        if (TryGetComponent<MeshRenderer>(out var mr))
        {
            var mats = mr.materials;
            if (mats.Length > 1)
            {
                var newMats = new Material[mats.Length - 1];
                for (int i = 0; i < newMats.Length; i++) newMats[i] = mats[i];
                mr.materials = newMats;
            }
        }
        usedOutlineMat = null;
        lastUsedOutlineMat = null;
        outlineApplied = false;
    }

    // Pickup
    private void HandlePickup()
    {
        if (Camera.main == null || pointer == null) return;

        Vector3 vpObject = Camera.main.WorldToViewportPoint(transform.position);
        Vector3 vpPointer = Camera.main.WorldToViewportPoint(pointer.position);
        float distance = Vector2.Distance(
            new Vector2(vpObject.x, vpObject.y),
            new Vector2(vpPointer.x, vpPointer.y)
        );

        if (distance < disappearViewportThreshold)
        {
            hasDisappeared = true;
            manager.HandlePickup(this);

            if (arrowInstance) arrowInstance.SetActive(false);
            gameObject.SetActive(false);
            if (manager != null && manager.sfxSource != null && manager.pluckSFX != null)
{
    manager.sfxSource.PlayOneShot(manager.pluckSFX);
}
        }
    }

    private Material GetOutlineMat()
    {
        switch (objectType)
        {
            case ObjectType.Mango: return mangoOutlineMat;
            case ObjectType.Apple: return appleOutlineMat;
            case ObjectType.Sunflower: return sunflowerOutlineMat;
            case ObjectType.Rose: return roseOutlineMat;
            default: return null;
        }
    }

    // UI helpers
    public void UpdatePanelUI(int current, int maxFromManager)
    {
        if (countText != null)
        {
            if (maxFromManager > 0) countText.SetText($"{current}/{maxFromManager}", true);
            else countText.SetText(current.ToString(), true);
        }

        if (circleImage != null)
        {
            bool overLimit = (maxFromManager > 0) && (current > maxFromManager);
            circleImage.enabled = overLimit;
        }
    }

    public void ShowFloatingText(float delta)
    {
        GameObject prefab = delta > 0 ? plusOnePrefab : minusOnePrefab;
        if (prefab == null || uiCanvas == null || Camera.main == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + scoreWorldOffset) + screenOffset;
        GameObject go = Instantiate(prefab, uiCanvas.transform, false);
        go.transform.position = screenPos;
    }

    // Resets
    public void ResetVisuals()
    {
        RemoveOutlineNow();
        hasDisappeared = false;
        if (arrowInstance != null) arrowInstance.SetActive(false);

        int sNum = Mathf.Clamp(manager != null ? manager.stageNumber : 1, 1, 4);

        int currentCount = 0;
        int maxAllowed = 0;

        switch (objectType)
        {
            case ObjectType.Mango:
                currentCount = mangoCount;
                maxAllowed = Level8StageManager.mangoLimits[sNum];
                break;
            case ObjectType.Apple:
                currentCount = appleCount;
                maxAllowed = Level8StageManager.appleLimits[sNum];
                break;
            case ObjectType.Sunflower:
                currentCount = sunflowerCount;
                maxAllowed = Level8StageManager.sunflowerLimits[sNum];
                break;
            case ObjectType.Rose:
                currentCount = roseCount;
                maxAllowed = Level8StageManager.roseLimits[sNum];
                break;
        }

        UpdatePanelUI(currentCount, maxAllowed);
    }

    public void ResetInternalState()
    {
        hasDisappeared = false;
        outlineApplied = false;
        usedOutlineMat = null;
        lastUsedOutlineMat = null;

        if (TryGetComponent<MeshRenderer>(out MeshRenderer mr))
            mr.enabled = true;

        if (arrowInstance != null)
            arrowInstance.SetActive(false);

        if (circleImage != null)
            circleImage.enabled = false;
    }
}
