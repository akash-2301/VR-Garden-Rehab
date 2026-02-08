using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading;

public class mangoscript : MonoBehaviour
{
    public timer timer;
    public enum FruitType { Mango, Apple }
    public FruitType fruitType;

    public Transform pointer;
    public float disappearViewportThreshold = 0.03f; // ~3% of screen size


    public GameObject arrowPrefab;
    private GameObject arrowInstance;
    public Canvas canvas;
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    public Material outlineMaterial;
    public Material outlineMaterial2;
    private Material usedOutlineMat;

    private Rigidbody rb;
    private FixedJoint joint;
    private bool hasFallen = false;
    private bool outlineApplied = false;

    // Shared counters
    public static int mangoCount = 0;
    public static int appleCount = 0;
    public static int totalMangos = 15;
    public static int totalApples = 11;

    public static TextMeshProUGUI mangoText;
    public static TextMeshProUGUI appleText;

    public GameObject floatingTextPrefab; // assign in Inspector  "+1" prefab)
    public Canvas uiCanvas;
    public Vector3 screenOffsett = new Vector3(10f, 0f, 0f); // customizable offset
    public Vector3 worldOffsett = new Vector3(0f, 0.5f, 0f); // vertical lift above fruit


    void SpawnFloatingText()
{
    if (floatingTextPrefab == null || uiCanvas == null) return;

    // Convert world position to screen position
  Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + worldOffsett); // ↑ upward offset

// Optional screen tweak (pixel adjustment)
screenPos += screenOffsett; // slight right shift

GameObject ft = Instantiate(floatingTextPrefab, uiCanvas.transform);
ft.transform.position = screenPos;
}

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        joint = GetComponent<FixedJoint>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        if (arrowPrefab != null && canvas != null)
        {
            arrowInstance = Instantiate(arrowPrefab, canvas.transform);
            arrowInstance.SetActive(false);
        }

    if (fruitType == FruitType.Mango && mangoText == null)
        mangoText = GameObject.Find("MangoText").GetComponent<TextMeshProUGUI>();
    if (fruitType == FruitType.Apple && appleText == null)
        appleText = GameObject.Find("MangoText").GetComponent<TextMeshProUGUI>(); // reuse same object

    }

    void Update()
    {
        if (pointer == null)
{
   
    return;
}
        if (hasFallen)
        {
            if (arrowInstance != null)
                arrowInstance.SetActive(false);
            return;
        }

        // Show arrow
       if (timer.timeRemaining < 10f && arrowInstance != null)
        {
            arrowInstance.SetActive(true);

            // Offset in WORLD units (e.g., 2 units above mango)

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + worldOffset);

            arrowInstance.transform.position = screenPos;
        }
        if (timer.isFruitMode)
        {
            if (timer.timeRemaining <= 0f)
            {

                if (arrowInstance != null)
                    arrowInstance.SetActive(false);
                outlineApplied = false;
            }
        }

       
      // Apply outline below 20s    
    if (!outlineApplied && timer.timeRemaining < 20f)
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // Clone the material so it doesn’t overwrite the shared one
            Material originalOutline = (fruitType == FruitType.Mango) ? outlineMaterial : outlineMaterial2;
            if (originalOutline != null)
            {
                Material instanceMat = new Material(originalOutline);

                if (fruitType == FruitType.Mango)
                {
                    instanceMat.SetColor("_OutlineColor", new Color32(255, 255, 0, 255));
                    instanceMat.SetFloat("_OutlineWidth", 0.001f);
                }
                else // Apple
                {
                    instanceMat.SetColor("_OutlineColor", new Color32(255, 255, 255, 250));
                    instanceMat.SetFloat("_OutlineWidth", 0.04f);
                }

                // Apply to renderer
                Material[] currentMats = mr.materials;
                Material[] newMats = new Material[currentMats.Length + 1];
                for (int i = 0; i < currentMats.Length; i++)
                    newMats[i] = currentMats[i];

                newMats[newMats.Length - 1] = instanceMat;
                mr.materials = newMats;

                usedOutlineMat = instanceMat; // Track it for blinking
                outlineApplied = true;
            }
        }
}
        // Blinking effect
        if (outlineApplied && usedOutlineMat != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * 4f));
            Color currentColor = usedOutlineMat.GetColor("_OutlineColor");
            usedOutlineMat.SetColor("_OutlineColor", new Color(currentColor.r, currentColor.g, currentColor.b, alpha));
        }

        // Check distance
        Vector3 vpObject = Camera.main.WorldToViewportPoint(transform.position);
        Vector3 vpPointer = Camera.main.WorldToViewportPoint(pointer.position);
       

        float distance = Vector2.Distance(
                new Vector2(vpObject.x, vpObject.y),
                new Vector2(vpPointer.x, vpPointer.y)
            );
        

        if (distance < disappearViewportThreshold)
        {
            hasFallen = true;
            HandleFall();
        }
    }

    void HandleFall()
    {
        if (joint != null)
            Destroy(joint,0.01f);
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
         SpawnFloatingText(); // ← show +1 effect

        if (fruitType == FruitType.Mango)
        {
            mangoCount++;
            if (mangoText != null) mangoText.text = "Score: " + mangoCount;
         
        }
        else if (fruitType == FruitType.Apple)
        {
            appleCount++;
            if (appleText != null) appleText.text = "Score: " + appleCount;
          
        }
    }

    
}



