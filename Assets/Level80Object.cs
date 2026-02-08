using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Level80Object : MonoBehaviour
{
    public enum ObjectType { Mango, Apple, Sunflower, Rose }
    public ObjectType objectType;

    [Header("References")]
    public Level80StageManager manager;
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

    bool hasDisappeared = false;
    GameObject arrowInstance;
    bool outlineApplied = false;
    Material usedOutlineMat;
    Material lastUsedOutlineMat;

    bool allowBlinkCue = false;
    bool allowArrowCue = false;

    public static int appleCount = 0;
    public static int mangoCount = 0;
    public static int roseCount = 0;
    public static int sunflowerCount = 0;

    private void OnEnable()
    {
        RemoveOutlineNow();

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

        if (manager == null)
            manager = FindObjectOfType<Level80StageManager>();

        if (manager != null)
        {
            int sNum = Mathf.Clamp(manager.stageNumber, 5, 10);
            int idx = sNum - 5;

            int currentCount = 0;
            int maxAllowed = 0;

            switch (objectType)
            {
                case ObjectType.Mango:
                    currentCount = mangoCount;
                    maxAllowed = SafeArray(Level80StageManager.mangoLimits, idx);
                    break;
                case ObjectType.Apple:
                    currentCount = appleCount;
                    maxAllowed = SafeArray(Level80StageManager.appleLimits, idx);
                    break;
                case ObjectType.Sunflower:
                    currentCount = sunflowerCount;
                    maxAllowed = SafeArray(Level80StageManager.sunflowerLimits, idx);
                    break;
                case ObjectType.Rose:
                    currentCount = roseCount;
                    maxAllowed = SafeArray(Level80StageManager.roseLimits, idx);
                    break;
            }

            UpdatePanelUI(currentCount, maxAllowed);
        }

        if (manager != null && !manager.IsRelevantObject(this))
            gameObject.layer = LayerMask.NameToLayer("Fruit");
    }

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
            arrowInstance = Instantiate(arrowPrefab, arrowCanvas.transform);
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

    private void HandleOutlineBlink()
    {
        if (manager == null) return;

        bool shouldShowOutline = manager.ShouldShowOutline(this);

        if (shouldShowOutline && !outlineApplied) ApplyOutline();
        else if (!shouldShowOutline && outlineApplied) RemoveOutlineNow();

        if (outlineApplied && usedOutlineMat != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * 4f));
            Color c = usedOutlineMat.GetColor("_OutlineColor");
            usedOutlineMat.SetColor("_OutlineColor", new Color(c.r, c.g, c.b, alpha));
        }

        if (outlineApplied && lastUsedOutlineMat != GetOutlineMat())
        {
            RemoveOutlineNow();
            ApplyOutline();
        }
    }

    private void ApplyOutline()
    {
        Material outlineMat = GetOutlineMat();
        if (outlineMat == null)
        {
            Debug.LogWarning($"[L80Obj] Missing outline material for {objectType}");
            return;
        }

        if (TryGetComponent<MeshRenderer>(out var mr))
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
        if (!TryGetComponent<MeshRenderer>(out var mr)) return;

        var mats = mr.materials;
        if (mats == null || mats.Length == 0) return;

        bool removed = false;

        if (usedOutlineMat != null)
        {
            var newList = new System.Collections.Generic.List<Material>();
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == usedOutlineMat)
                {
                    removed = true;
                    continue;
                }
                newList.Add(mats[i]);
            }

            if (removed)
            {
                mr.materials = newList.ToArray();
                Destroy(usedOutlineMat);
            }
        }

        if (!removed && mats.Length > 1)
        {
            var newMats = new Material[mats.Length - 1];
            for (int i = 0; i < newMats.Length; i++) newMats[i] = mats[i];
            mr.materials = newMats;
            removed = true;
        }

        usedOutlineMat = null;
        lastUsedOutlineMat = null;
        outlineApplied = false;
    }

    private void HandlePickup()
    {
        if (Camera.main == null || manager == null) return;

        Vector3 vpObject = Camera.main.WorldToViewportPoint(transform.position);
        Vector3 vpPointer = Camera.main.WorldToViewportPoint(pointer.position);
        float distance = Vector2.Distance(new Vector2(vpObject.x, vpObject.y), new Vector2(vpPointer.x, vpPointer.y));

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

    public void UpdatePanelUI(int current, int maxFromManager)
    {
        if (countText != null)
            countText.text = maxFromManager > 0 ? $"{current}/{maxFromManager}" : current.ToString();

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
        GameObject go = Instantiate(prefab, uiCanvas.transform);
        go.transform.position = screenPos;
    }

    public void ResetVisuals()
    {
        RemoveOutlineNow();
        hasDisappeared = false;
        if (arrowInstance != null) arrowInstance.SetActive(false);
        if (manager == null) manager = FindObjectOfType<Level80StageManager>();

        if (manager == null) return;

        int sNum = Mathf.Clamp(manager.stageNumber, 5, 10);
        int idx = sNum - 5;

        int currentCount = 0;
        int maxAllowed = 0;

        switch (objectType)
        {
            case ObjectType.Mango:
                currentCount = mangoCount;
                maxAllowed = SafeArray(Level80StageManager.mangoLimits, idx);
                break;
            case ObjectType.Apple:
                currentCount = appleCount;
                maxAllowed = SafeArray(Level80StageManager.appleLimits, idx);
                break;
            case ObjectType.Sunflower:
                currentCount = sunflowerCount;
                maxAllowed = SafeArray(Level80StageManager.sunflowerLimits, idx);
                break;
            case ObjectType.Rose:
                currentCount = roseCount;
                maxAllowed = SafeArray(Level80StageManager.roseLimits, idx);
                break;
        }

        UpdatePanelUI(currentCount, maxAllowed);
    }

    private int SafeArray(int[] arr, int idx)
    {
        if (arr == null || arr.Length == 0) return 0;
        if (idx < 0 || idx >= arr.Length) return 0;
        return arr[idx];
    }

    public void ResetInternalState()
    {
        RemoveOutlineNow();
        hasDisappeared = false;
        outlineApplied = false;
        usedOutlineMat = null;
        lastUsedOutlineMat = null;

        if (TryGetComponent<MeshRenderer>(out MeshRenderer mr)) mr.enabled = true;
        if (arrowInstance != null) arrowInstance.SetActive(false);
        if (circleImage != null) circleImage.enabled = false;
    }

    public static void ResetCounts()
    {
        appleCount = 0;
        mangoCount = 0;
        roseCount = 0;
        sunflowerCount = 0;
    }
}
