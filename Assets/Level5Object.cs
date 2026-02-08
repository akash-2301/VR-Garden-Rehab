using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Level5Object : MonoBehaviour
{
    public enum Type { Mango, Apple, Sunflower, Rose }
    public Type objectType;
    private int previousStage = -1;

    [Header("References (set in inspector)")]
    public Level5StageManager manager;
    public Transform pointer;
    public Transform sitPoint;

    [Header("Pickup / UI")]
    [Tooltip("Viewport distance threshold for pickup (World->Viewport distance)")]
    public float disappearViewportThreshold = 0.03f; // ~3% of screen size

    // Active types set by manager when stage begins
    public static HashSet<Type> ActiveObjectTypes = new HashSet<Type>();

    [Header("Arrow & Outline Settings")]
    public GameObject arrowPrefab;
    public Canvas arrowCanvas; // required if using UI arrow
    public Material outlineMaterialMango;
    public Material outlineMaterialApple;
    public Material outlineMaterialSunflower;
    public Material outlineMaterialRose;

    public Vector3 arrowOffset = new Vector3(0f, 2f, 0f);

    private GameObject arrowInstance;
    private Material usedOutlineMat;
    private bool outlineApplied = false;
    private bool isActive = true;

    // Cue permissions (set by manager via SetCuePermissions)
    private bool outlineAllowed = false;
    private bool arrowAllowed = false;

    // Countdown thresholds (tune in inspector)
    [Header("Cue timing (seconds before end)")]
    public float outlineShowBeforeEnd = 20f; // show outline when timeLeft < this
    public float arrowShowBeforeEnd = 10f;   // show arrow when timeLeft < this

    // Static counts (used by manager / PF logic)
    public static int mangoCount = 0;
    public static int appleCount = 0;
    public static int sunflowerCount = 0;
    public static int roseCount = 0;

    void OnEnable()
    {
        // Reset visuals/state whenever object is enabled (stage start or reset)
        ResetObject();
    }

    void Start()
    {
        // Instantiate arrow UI under arrowCanvas if provided
        if (arrowPrefab != null && arrowCanvas != null)
        {
            // instantiate once per object
            arrowInstance = Instantiate(arrowPrefab, arrowCanvas.transform);
            arrowInstance.SetActive(false);
        }
    }

    void OnDestroy()
    {
        // cleanup runtime allocations to avoid leaks / orphaned GameObjects
        if (arrowInstance != null)
        {
            Destroy(arrowInstance);
            arrowInstance = null;
        }

        if (usedOutlineMat != null)
        {
            Destroy(usedOutlineMat);
            usedOutlineMat = null;
        }
    }

    void Update()
    {
        // Defensive: ensure manager exists
        if (manager == null)
        {
            if (arrowInstance) arrowInstance.SetActive(false);
            return;
        }

        if (!isActive || pointer == null || manager.isGameOver)
        {
            if (arrowInstance) arrowInstance.SetActive(false);
            return;
        }

        // If stage changed, remove old outline so we start clean
        if (previousStage != manager.stageNumber)
        {
            RemoveOutline();
            previousStage = manager.stageNumber;
        }

        HandleArrowAndOutline();

        Camera cam = Camera.main;
        if (cam == null) return;

        // Check pointer/object distance in viewport space for pickup
        Vector3 vpObject = cam.WorldToViewportPoint(transform.position);
        Vector3 vpPointer = cam.WorldToViewportPoint(pointer.position);

        float distance = Vector2.Distance(
            new Vector2(vpObject.x, vpObject.y),
            new Vector2(vpPointer.x, vpPointer.y)
        );

        // ✅ Pickup when pointer close enough
        if (distance < disappearViewportThreshold)
        {
            isActive = false;

            bool hasBee = HasBeeAttached();
            if (hasBee && sitPoint != null && sitPoint.childCount > 0)
            {
                Transform bee = sitPoint.GetChild(0);
                bee.SetParent(null);
                bee.gameObject.SetActive(false);
            }

            manager.HandlePickup(this, hasBee);

            if (arrowInstance != null)
                arrowInstance.SetActive(false);

            gameObject.SetActive(false);
            if (manager != null && manager.sfxSource != null && manager.pluckSFX != null)
{
    manager.sfxSource.PlayOneShot(manager.pluckSFX);
}
        }
    }

    void HandleArrowAndOutline()
    {
        float timeLeft = manager.GetRemainingTime();
        bool isInstructionObject = (objectType == manager.GetCurrentInstructionType());
        bool beeAttached = HasBeeAttached();

        // ---------- ARROW ----------
        if (arrowInstance != null)
        {
            bool showArrow = arrowAllowed && isInstructionObject && !beeAttached && manager.IsRelevantObject2(this) && timeLeft < arrowShowBeforeEnd;
            arrowInstance.SetActive(showArrow);

            if (showArrow)
            {
                // Position properly for Canvas
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + arrowOffset);
                RectTransform canvasRect = arrowCanvas != null ? arrowCanvas.GetComponent<RectTransform>() : null;
                RectTransform arrowRect = arrowInstance.GetComponent<RectTransform>();

                if (canvasRect != null && arrowRect != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos,
                        arrowCanvas.renderMode == RenderMode.ScreenSpaceCamera ? arrowCanvas.worldCamera : null,
                        out localPoint);
                    arrowRect.anchoredPosition = localPoint;
                }
                else
                {
                    arrowInstance.transform.position = screenPos;
                }
            }
        }

        // ---------- OUTLINE APPLY ----------
        if (!outlineApplied && outlineAllowed && isInstructionObject && !beeAttached && timeLeft < outlineShowBeforeEnd)
        {
            ApplyOutline();
        }

        // ---------- OUTLINE BLINK ----------
        if (outlineApplied && usedOutlineMat != null && isInstructionObject && !beeAttached)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * 4f));
            if (usedOutlineMat.HasProperty("_OutlineColor"))
            {
                Color color = usedOutlineMat.GetColor("_OutlineColor");
                usedOutlineMat.SetColor("_OutlineColor", new Color(color.r, color.g, color.b, alpha));
            }
        }

        // ---------- REMOVE OUTLINE ----------
        if (outlineApplied && (!outlineAllowed || !isInstructionObject || beeAttached || timeLeft >= outlineShowBeforeEnd))
        {
            RemoveOutline();
        }
    }

    void ApplyOutline()
    {
        if (!TryGetComponent<MeshRenderer>(out MeshRenderer mr)) return;

        Material outlineMat = GetOutlineMaterialForType();
        if (outlineMat == null) return;

        // create a runtime instance so alpha changes don't affect the shared asset
        Material instanceMat = new Material(outlineMat);
        SetOutlineColor(instanceMat);

        var currentMats = mr.materials;
        var newMats = new Material[currentMats.Length + 1];
        currentMats.CopyTo(newMats, 0);
        newMats[newMats.Length - 1] = instanceMat;
        mr.materials = newMats;

        usedOutlineMat = instanceMat;
        outlineApplied = true;
    }

    void RemoveOutline()
    {
        if (!TryGetComponent<MeshRenderer>(out MeshRenderer mr)) return;
        if (mr.materials == null || mr.materials.Length == 0) return;

        bool removed = false;

        if (usedOutlineMat != null)
        {
            var mats = new List<Material>(mr.materials);
            if (mats.Contains(usedOutlineMat))
            {
                mats.Remove(usedOutlineMat);
                mr.materials = mats.ToArray();
                // destroy runtime instance to free memory
                Destroy(usedOutlineMat);
                removed = true;
            }
        }

        // Fallback: if we didn't find the runtime instance, try to remove any material with "outline" in shader name (defensive)
        if (!removed)
        {
            var mats = new List<Material>(mr.materials);
            int removedIndex = -1;
            for (int i = mats.Count - 1; i >= 0; i--)
            {
                var mat = mats[i];
                if (mat != null && mat.shader != null && mat.shader.name.ToLower().Contains("outline"))
                {
                    removedIndex = i;
                    // attempt to destroy only if it is not a shared asset (best-effort)
                    mats.RemoveAt(i);
                    break;
                }
            }
            if (removedIndex >= 0)
            {
                mr.materials = mats.ToArray();
                // we cannot reliably Destroy(mat) here because it might be an asset reference; keep safe.
                removed = true;
            }
        }

        outlineApplied = false;
        usedOutlineMat = null;
    }

    Material GetOutlineMaterialForType()
    {
        switch (objectType)
        {
            case Type.Mango: return outlineMaterialMango;
            case Type.Apple: return outlineMaterialApple;
            case Type.Sunflower: return outlineMaterialSunflower != null ? outlineMaterialSunflower : outlineMaterialApple;
            case Type.Rose: return outlineMaterialRose != null ? outlineMaterialRose : outlineMaterialApple;
            default: return outlineMaterialApple;
        }
    }

    void SetOutlineColor(Material mat)
    {
        // Only set properties if the shader actually has them
        if (mat == null) return;
        if (mat.HasProperty("_OutlineColor"))
        {
            if (objectType == Type.Mango)
                mat.SetColor("_OutlineColor", new Color32(255, 255, 0, 255));
            else if (objectType == Type.Apple)
                mat.SetColor("_OutlineColor", new Color32(255, 255, 255, 250));
            else if (objectType == Type.Sunflower)
                mat.SetColor("_OutlineColor", new Color32(255, 220, 0, 255));
            else if (objectType == Type.Rose)
                mat.SetColor("_OutlineColor", new Color32(255, 100, 120, 255));
        }

        if (mat.HasProperty("_OutlineWidth"))
        {
            if (objectType == Type.Mango) mat.SetFloat("_OutlineWidth", 0.001f);
            else if (objectType == Type.Apple) mat.SetFloat("_OutlineWidth", 0.04f);
            else mat.SetFloat("_OutlineWidth", 0.025f);
        }
    }

    public void ResetObject()
    {
        isActive = true;

        // Clean outline
        RemoveOutline();

        // Reset state
        outlineApplied = false;
        usedOutlineMat = null;

        // Detach bees if any remain
        if (sitPoint != null && sitPoint.childCount > 0)
        {
            for (int i = sitPoint.childCount - 1; i >= 0; i--)
            {
                Transform child = sitPoint.GetChild(i);
                child.SetParent(null);
                child.gameObject.SetActive(false);
            }
        }

        // Disable arrow
        if (arrowInstance != null)
            arrowInstance.SetActive(false);
    }

    public void SetCuePermissions(bool allowOutline, bool allowArrow)
    {
        outlineAllowed = allowOutline;
        arrowAllowed = allowArrow;

        // Immediately remove outline/arrow if disallowed
        if (!outlineAllowed && outlineApplied) RemoveOutline();
        if (arrowInstance != null && !arrowAllowed) arrowInstance.SetActive(false);
    }

    public bool HasBeeAttached()
    {
        return sitPoint != null && sitPoint.childCount > 0;
    }
}
