// Level6Object.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

[RequireComponent(typeof(Renderer))]
public class Level6Object : MonoBehaviour
{
    // renamed enum to avoid collision with System.Type
    public enum ObjectType { Mango, Apple, Sunflower, Rose }

    [Header("Identity")]
    public ObjectType objectType = ObjectType.Apple;

     public Level6StageManager manager;
    public Transform pointer;
    public Transform sitPoint;
    [Tooltip("Viewport distance threshold (0..1) below which the object is considered picked)")]
    public float disappearViewportThreshold = 0.03f;

    // Which object-types are considered active for current stage (manager populates)
    public static HashSet<ObjectType> ActiveObjectTypes = new HashSet<ObjectType>();

    [Header("Arrow & Outline Settings")]
    public GameObject arrowPrefab;
    public Canvas arrowCanvas;

    // fallback outline materials (assign appropriate outline shader material per fruit in inspector)
    public Material outlineMaterialMango;
    public Material outlineMaterialApple;
    public Material outlineMaterialSunflower;
    public Material outlineMaterialRose;

    public Vector3 arrowOffset = new Vector3(0f, 2f, 0f);

    [Header("Blink speeds (higher = faster)")]
    public float outlineBlinkSpeed = 5f;
    public float arrowBlinkSpeed = 0.4f;

    [Header("Behavior")]
    [Tooltip("Prefer showing outline/arrow only for current instruction objects. Manager controls permission.")]
    public bool preferSimpleOutline = true;

    // runtime
    private Renderer baseRenderer;
    private Material[] originalMaterials;
    private bool glowEnabled = false;
    private Material[] glowMaterialsInstance;

    // Outline fallback (child object using outline material)
    private GameObject outlineGO = null;
    private Renderer outlineRenderer = null;
    private Material instanceOutlineMat = null;
    private Color outlineBaseColor = Color.white;
    private bool outlineApplied = false;

    // Arrow instance
    private GameObject arrowInstance;
    private RectTransform arrowRect;
    private CanvasGroup arrowCanvasGroup;

    // permissions - these control cue visibility (set by manager)
    private bool outlineAllowed = false;
    private bool arrowAllowed = false;

    // state
    private bool isActive = true;
    private int previousStage = -1;
    private bool isBeeTargeted = false;

    // static counters (kept for scoring)
    public static int mangoCount = 0, appleCount = 0, sunflowerCount = 0, roseCount = 0;

    // cached references
    private Camera mainCam;

    #region Unity lifecycle

    void Awake()
    {
        baseRenderer = GetComponent<Renderer>();
        if (baseRenderer != null)
        {
            var mats = baseRenderer.materials;
            originalMaterials = new Material[mats.Length];
            for (int i = 0; i < mats.Length; i++) originalMaterials[i] = mats[i];
        }

        mainCam = Camera.main;
    }

    void OnEnable()
    {
        // ensure object is in a clean state when enabled
        ResetObject();
    }

    void Start()
    {
        // instantiate arrow prefab into the arrowCanvas if provided
        if (arrowPrefab != null && arrowCanvas != null && arrowInstance == null)
        {
            TryCreateArrowInstance();
            if (arrowInstance != null) arrowInstance.SetActive(false);
        }

        // cache camera if not set earlier
        if (mainCam == null) mainCam = Camera.main;
    }

    void OnDisable()
    {
        // When disabled, hide and detach arrow/outline to avoid dangling references
        if (arrowInstance != null)
        {
            arrowInstance.SetActive(false);
            // keep arrow in canvas for reuse rather than destroying
        }

        // Remove created outline child to avoid leaks while disabled
        if (outlineGO != null)
        {
            DestroyImmediate(outlineGO);
            outlineGO = null;
            outlineRenderer = null;
        }

        // Restore materials
        if (baseRenderer != null && originalMaterials != null)
            baseRenderer.materials = originalMaterials;

        // clear runtime material instances
        glowMaterialsInstance = null;
        instanceOutlineMat = null;
        outlineApplied = false;
        glowEnabled = false;
    }

    void OnDestroy()
    {
        // cleanup any materials we created ourselves
        if (instanceOutlineMat != null)
        {
            Destroy(instanceOutlineMat);
            instanceOutlineMat = null;
        }
        if (glowMaterialsInstance != null)
        {
            for (int i = 0; i < glowMaterialsInstance.Length; i++)
            {
                if (glowMaterialsInstance[i] != null)
                {
                    Destroy(glowMaterialsInstance[i]);
                }
            }
            glowMaterialsInstance = null;
        }

        // arrowInstance is parented to arrowCanvas — leave Unity to handle it
        if (arrowInstance != null && arrowInstance.transform.parent == transform)
        {
            Destroy(arrowInstance);
            arrowInstance = null;
        }
    }

    void Update()
    {
        // don't run cues if manager says game over or pointer/manager missing
        if (!isActive || pointer == null || manager == null || manager.isGameOver)
        {
            if (arrowInstance) arrowInstance.SetActive(false);
            return;
        }

        // Reset cues when stage changes (prevents stale outlines/arrows)
        if (previousStage != manager.stageNumber)
        {
            HideOutline();
            DisableGlow();
            HideArrow();
            previousStage = manager.stageNumber;
        }

        EvaluateCuesEveryFrame();

        // pickup distance check (viewport-space)
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 vpObj = cam.WorldToViewportPoint(transform.position);
        Vector3 vpPointer = cam.WorldToViewportPoint(pointer.position);
        float distance = Vector2.Distance(new Vector2(vpObj.x, vpObj.y), new Vector2(vpPointer.x, vpPointer.y));
        if (distance < disappearViewportThreshold)
        {
            isActive = false;
            bool hasBee = HasBeeAttached();

            // detach and disable bee if attached
            if (hasBee && sitPoint != null && sitPoint.childCount > 0)
            {
                Transform bee = sitPoint.GetChild(0);
                bee.SetParent(null);
                bee.gameObject.SetActive(false);
            }

            manager?.HandlePickup(this, hasBee);

            if (arrowInstance != null) arrowInstance.SetActive(false);
            gameObject.SetActive(false);
            if (manager != null && manager.sfxSource != null && manager.pluckSFX != null)
{
    manager.sfxSource.PlayOneShot(manager.pluckSFX);
}
            
        }
    }

    #endregion

    #region Per-frame cue evaluation

    void EvaluateCuesEveryFrame()
    {
        if (manager == null) return;

        float timeLeft = manager.GetRemainingTime();
        bool isInstructionObject = (objectType.ToString() == manager.GetCurrentInstructionType().ToString()) ? true : (objectType == manager.GetCurrentInstructionType());
        // above line is defensive in case manager's type wasn't updated yet; using enum equality below is primary
        bool beeAttached = HasBeeAttached();
        bool isActiveInstruction = ActiveObjectTypes.Contains(objectType);

        // ---------- ARROW ----------
        if (arrowInstance != null)
        {
            bool showArrowCondition = arrowAllowed &&
                                      isActiveInstruction &&
                                      !beeAttached &&
                                      manager.IsRelevantObject2(this) &&
                                      timeLeft < manager.arrowThreshold;

            if (showArrowCondition)
            {
                // blinking
                int v = Mathf.FloorToInt(Time.time * arrowBlinkSpeed) % 2;
                bool visible = (v == 0);
                arrowInstance.SetActive(visible);

                if (visible)
                {
                    PositionArrowOnCanvas();
                }
            }
            else
            {
                arrowInstance.SetActive(false);
            }
        }

        // ---------- OUTLINE ----------
        bool shouldShowOutline = outlineAllowed &&
                                 (objectType == manager.GetCurrentInstructionType()) &&
                                 !beeAttached &&
                                 timeLeft < manager.outlineThreshold &&
                                 (objectType == ObjectType.Mango || objectType == ObjectType.Apple);

        if (shouldShowOutline && !outlineApplied)
        {
            ShowOutline();
        }
        else if (!shouldShowOutline && outlineApplied)
        {
            HideOutline();
        }

        // outline pulsing
        if (outlineApplied && !beeAttached)
        {
            float blink = (Mathf.Sin(Time.time * outlineBlinkSpeed) + 1f) * 0.5f; // 0..1
            if (instanceOutlineMat != null && instanceOutlineMat.HasProperty("_OutlineColor"))
            {
                Color pulsed = outlineBaseColor * (0.6f + blink * 0.8f);
                Color prev = instanceOutlineMat.GetColor("_OutlineColor");
                pulsed.a = prev.a;
                instanceOutlineMat.SetColor("_OutlineColor", pulsed);
                if (instanceOutlineMat.HasProperty("_OutlineWidth"))
                {
                    float baseW = 0.03f;
                    try { baseW = instanceOutlineMat.GetFloat("_OutlineWidth"); } catch { baseW = 0.03f; }
                    float w = baseW * Mathf.Lerp(0.8f, 1.3f, blink);
                    instanceOutlineMat.SetFloat("_OutlineWidth", w);
                }
            }
        }

        // ---------- GLOW ----------
        bool shouldGlow = (objectType == ObjectType.Rose || objectType == ObjectType.Sunflower) &&
                          !beeAttached &&
                          timeLeft < manager.outlineThreshold &&
                          manager.bloomActiveThisAttempt;

        if (shouldGlow && !glowEnabled) EnableGlow();
        else if (!shouldGlow && glowEnabled) DisableGlow();
    }

    #endregion

    #region Outline API

    public void ShowOutline()
    {
        if (outlineApplied) return;
        ApplyOutline();
    }

    public void HideOutline()
    {
        if (!outlineApplied) return;
        RemoveOutline();
    }

    void ApplyOutline()
    {
        if (outlineApplied) return;

        // create outline material instance based on type
        Material src = GetOutlineMaterialForType();
        if (src != null)
        {
            instanceOutlineMat = new Material(src);
            SetOutlineColor(instanceOutlineMat);
            if (instanceOutlineMat.HasProperty("_OutlineColor")) outlineBaseColor = instanceOutlineMat.GetColor("_OutlineColor");
            else outlineBaseColor = GetDefaultOutlineColorForType();
        }
        else
        {
            // fallback: simple unlit color material
            instanceOutlineMat = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            outlineBaseColor = Color.white;
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();

        if (mf != null && mf.sharedMesh != null)
        {
            CreateOutlineChildFromMeshFilter(mf, instanceOutlineMat);
        }
        else if (smr != null && smr.sharedMesh != null)
        {
            CreateOutlineChildFromSkinnedMesh(smr, instanceOutlineMat);
        }
        else
        {
            // Append the outline material to base renderer as final fallback
            AppendOutlineMaterialFallback();
        }

        outlineApplied = true;
    }

    void RemoveOutline()
    {
        if (!outlineApplied) return;

        if (outlineGO != null)
        {
            Destroy(outlineGO);
            outlineGO = null;
            outlineRenderer = null;
        }
        else
        {
            if (baseRenderer != null && originalMaterials != null)
            {
                baseRenderer.materials = originalMaterials;
            }
        }

        // destroy instance material we created (safe since we created it)
        if (instanceOutlineMat != null)
        {
            Destroy(instanceOutlineMat);
            instanceOutlineMat = null;
        }

        outlineApplied = false;
    }

    #endregion

    #region Outline Helpers

    void CreateOutlineChildFromMeshFilter(MeshFilter mf, Material outlineMatInstance)
    {
        outlineGO = new GameObject(name + "_Outline");
        outlineGO.transform.SetParent(transform, false);
        outlineGO.transform.localPosition = Vector3.zero;
        outlineGO.transform.localRotation = Quaternion.identity;
        outlineGO.transform.localScale = Vector3.one * 1.02f;

        var childMF = outlineGO.AddComponent<MeshFilter>();
        childMF.sharedMesh = mf.sharedMesh;

        var childMR = outlineGO.AddComponent<MeshRenderer>();
        childMR.sharedMaterials = new Material[] { outlineMatInstance };
        childMR.shadowCastingMode = ShadowCastingMode.Off;
        childMR.receiveShadows = false;
        childMR.lightProbeUsage = LightProbeUsage.Off;

        outlineRenderer = childMR;

        void SetLayerRecursivelyLocal(GameObject g, int layer)
        {
            if (g == null) return;
            g.layer = layer;
            foreach (Transform t in g.transform)
                SetLayerRecursivelyLocal(t.gameObject, layer);
        }

        int myLayer = gameObject.layer;
        SetLayerRecursivelyLocal(outlineGO, myLayer);
    }

    void CreateOutlineChildFromSkinnedMesh(SkinnedMeshRenderer smr, Material outlineMatInstance)
    {
        outlineGO = new GameObject(name + "_Outline");
        outlineGO.transform.SetParent(transform, false);
        outlineGO.transform.localPosition = Vector3.zero;
        outlineGO.transform.localRotation = Quaternion.identity;
        outlineGO.transform.localScale = Vector3.one * 1.02f;

        var childSMR = outlineGO.AddComponent<SkinnedMeshRenderer>();
        childSMR.sharedMesh = smr.sharedMesh;
        childSMR.bones = smr.bones;
        childSMR.rootBone = smr.rootBone;
        childSMR.sharedMaterials = new Material[] { outlineMatInstance };
        childSMR.updateWhenOffscreen = true;
        childSMR.shadowCastingMode = ShadowCastingMode.Off;
        childSMR.receiveShadows = false;
        childSMR.lightProbeUsage = LightProbeUsage.Off;

        outlineRenderer = childSMR;

        void SetLayerRecursivelyLocal(GameObject g, int layer)
        {
            if (g == null) return;
            g.layer = layer;
            foreach (Transform t in g.transform)
                SetLayerRecursivelyLocal(t.gameObject, layer);
        }

        int myLayer = gameObject.layer;
        SetLayerRecursivelyLocal(outlineGO, myLayer);
    }

    void AppendOutlineMaterialFallback()
    {
        if (baseRenderer == null) return;
        if (instanceOutlineMat == null) instanceOutlineMat = new Material(GetOutlineMaterialForType() ?? outlineMaterialApple);
        var mats = new List<Material>(baseRenderer.materials);
        mats.Add(instanceOutlineMat);
        baseRenderer.materials = mats.ToArray();
    }

    #endregion

    #region Glow (emission)

    public void EnableGlow()
    {
        if (glowEnabled) return;
        if (baseRenderer == null) baseRenderer = GetComponent<Renderer>();
        if (baseRenderer == null) return;

        var baseMats = originalMaterials != null ? originalMaterials : baseRenderer.materials;
        glowMaterialsInstance = new Material[baseMats.Length];
        for (int i = 0; i < baseMats.Length; i++)
        {
            Material inst = new Material(baseMats[i]);
            if (inst.HasProperty("_EmissionColor"))
            {
                inst.EnableKeyword("_EMISSION");
                Color baseCol = inst.HasProperty("_Color") ? inst.GetColor("_Color") : Color.white;
                inst.SetColor("_EmissionColor", baseCol * 1.5f);
            }
            glowMaterialsInstance[i] = inst;
        }

        if (outlineGO == null && instanceOutlineMat != null)
        {
            List<Material> combined = new List<Material>(glowMaterialsInstance);
            combined.Add(instanceOutlineMat);
            baseRenderer.materials = combined.ToArray();
        }
        else
        {
            baseRenderer.materials = glowMaterialsInstance;
        }

        glowEnabled = true;
    }

    public void DisableGlow()
    {
        if (!glowEnabled) return;
        if (baseRenderer == null) baseRenderer = GetComponent<Renderer>();
        if (baseRenderer == null) return;

        if (outlineGO == null && instanceOutlineMat != null && originalMaterials != null)
        {
            List<Material> combined = new List<Material>(originalMaterials);
            combined.Add(instanceOutlineMat);
            baseRenderer.materials = combined.ToArray();
        }
        else if (originalMaterials != null)
        {
            baseRenderer.materials = originalMaterials;
        }

        // destroy glow instances we created
        if (glowMaterialsInstance != null)
        {
            for (int i = 0; i < glowMaterialsInstance.Length; i++)
            {
                if (glowMaterialsInstance[i] != null)
                    Destroy(glowMaterialsInstance[i]);
            }
            glowMaterialsInstance = null;
        }

        glowEnabled = false;
    }

    #endregion

    #region Arrow

    private void TryCreateArrowInstance()
    {
        if (arrowPrefab == null || arrowCanvas == null) return;
        if (arrowInstance != null) return;

        arrowInstance = Instantiate(arrowPrefab, arrowCanvas.transform);
        arrowRect = arrowInstance.GetComponent<RectTransform>();
        arrowCanvasGroup = arrowInstance.GetComponent<CanvasGroup>() ?? arrowInstance.AddComponent<CanvasGroup>();
        arrowInstance.SetActive(false);
    }

    private void PositionArrowOnCanvas()
    {
        if (arrowInstance == null || arrowRect == null || arrowCanvas == null) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 worldPos = transform.position + arrowOffset;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = arrowCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return;

        Vector2 localPoint;
        bool ok = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            arrowCanvas.renderMode == RenderMode.ScreenSpaceCamera ? arrowCanvas.worldCamera : null,
            out localPoint
        );

        if (ok)
            arrowRect.anchoredPosition = localPoint;
        else
            arrowInstance.transform.position = screenPos;
    }

    public void ShowArrow()
    {
        if (arrowInstance == null && arrowPrefab != null && arrowCanvas != null)
            TryCreateArrowInstance();

        if (arrowInstance != null)
            arrowInstance.SetActive(true);
    }

    public void HideArrow()
    {
        if (arrowInstance != null) arrowInstance.SetActive(false);
    }

    #endregion

    #region Permissions & Reset

    public void SetCuePermissions(bool allowOutline, bool allowArrow)
    {
        outlineAllowed = allowOutline;
        arrowAllowed = allowArrow;

        // Immediately hide cues that are no longer allowed
        if (!outlineAllowed && outlineApplied) HideOutline();
        if (!arrowAllowed && arrowInstance != null) arrowInstance.SetActive(false);
    }

    public void ResetObject()
    {
        isActive = true;
        // Hide cues and reset visuals
        HideOutline();
        DisableGlow();
        HideArrow();

        // detach any bee child, but do not destroy bee object - manager/bee system handles pooling
        if (sitPoint != null && sitPoint.childCount > 0)
        {
            Transform child = sitPoint.GetChild(0);
            child.SetParent(null);
        }

        if (baseRenderer != null && originalMaterials != null)
            baseRenderer.materials = originalMaterials;

        outlineApplied = false;
        glowEnabled = false;
    }

    #endregion

    #region Bee helpers

    public bool HasBeeAttached()
    {
        if (sitPoint != null && sitPoint.childCount > 0)
        {
            Transform bee = sitPoint.GetChild(0);
            var bf = manager?.BeeFlightController?.GetComponent<BeeFlight>();
            if (bf == null)
            {
                // BeeFlight missing — safer to return false and log once
                return false;
            }

            try
            {
                if (bf.beesInFlight != null)
                    return !bf.beesInFlight.Contains(bee);
            }
            catch
            {
                return true;
            }
        }
        return false;
    }

    public void OnBeeTargeted()
    {
        // bee about to go to this object (may change soon) — mark targeted, hide cues and notify manager
        isBeeTargeted = true;
        HideOutline();
        HideArrow();
        DisableGlow();
        manager?.ReevaluateCuesImmediately();
        manager?.NotifyObjectTargeted(this);
    }

    public void OnBeeAssigned()
    {
        // bee is assigned (object now has bee) - clear targeted flag, hide cues and notify manager
        isBeeTargeted = false;
        HideOutline();
        HideArrow();
        DisableGlow();
        manager?.ReevaluateCuesImmediately();
        manager?.NotifyObjectAssigned(this);
    }

    public void OnBeeReleased()
    {
        // when bee leaves this object - clear targeted flag and notify manager so cues can return
        isBeeTargeted = false;
        manager?.ReevaluateCuesImmediately();
        manager?.NotifyObjectReleased(this);
    }

    // expose read-only for manager
    public bool IsBeeTargeted() => isBeeTargeted;

    #endregion

    #region Utilities

    Material GetOutlineMaterialForType()
    {
        switch (objectType)
        {
            case ObjectType.Mango: return outlineMaterialMango ?? outlineMaterialApple;
            case ObjectType.Apple: return outlineMaterialApple;
            case ObjectType.Sunflower: return outlineMaterialSunflower ?? outlineMaterialApple;
            case ObjectType.Rose: return outlineMaterialRose ?? outlineMaterialApple;
            default: return outlineMaterialApple;
        }
    }

    Color GetDefaultOutlineColorForType()
    {
        if (objectType == ObjectType.Mango) return new Color(1f, 1f, 0f, 1f);
        else if (objectType == ObjectType.Apple) return new Color(1f, 1f, 1f, 1f);
        else if (objectType == ObjectType.Sunflower) return new Color(1f, 0.94f, 0.55f, 1f);
        else if (objectType == ObjectType.Rose) return new Color(1f, 0.6f, 0.7f, 1f);
        return Color.white;
    }

    void SetOutlineColor(Material mat)
    {
        if (mat == null) return;
        if (!mat.HasProperty("_OutlineColor")) return;
        Color col = GetDefaultOutlineColorForType();
        mat.SetColor("_OutlineColor", col);
        if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 0.03f);
    }

    #endregion

    // debugging helpers
    public bool OutlineAllowed => outlineAllowed;
    public bool ArrowAllowed => arrowAllowed;
}
