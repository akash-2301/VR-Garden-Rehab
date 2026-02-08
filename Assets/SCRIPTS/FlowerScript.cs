using UnityEngine;
using TMPro;
using UnityEngine.XR;

public class FlowerScript : MonoBehaviour
{
    public enum FlowerType { Sunflower, Rose }
    public FlowerType flowerType;

    [Header("References")]
    public timer timer;
    public Transform pointer;

    [Header("Arrow")]
    public GameObject arrowPrefab;
    private GameObject arrowInstance;
    public Canvas canvas;
    public Vector3 worldOffset = new Vector3(0, 2f, 0);

    [Header("Floating Text")]
    public GameObject floatingTextPrefab;
    public Canvas uiCanvas;
    public Vector3 screenOffset = new Vector3(10f, 0f, 0f);
    public Vector3 worldOffsetText = new Vector3(0f, 0.5f, 0f);

    [Header("Pickup Settings")]
    public float disappearViewportThreshold = 0.03f; // ~3% of screen size


    private bool hasDisappeared = false;

    // Static counts
    public static int sunflowerCount = 0;
    public static int roseCount = 0;

    public static TextMeshProUGUI flowerText;

    void Start()
    {
        Debug.Log($"{name} FlowerScript Started. Active: {gameObject.activeInHierarchy}, Enabled: {enabled}");

        // Set up arrow
        if (arrowPrefab != null && canvas != null)
        {
            arrowInstance = Instantiate(arrowPrefab, canvas.transform);
            arrowInstance.SetActive(false);
        }

        // Text reference (shared)
        if (flowerText == null)
        {
            flowerText = GameObject.Find("sunflowertext").GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
{
    Debug.LogWarning($"{name} is not active in hierarchy");
}

        Debug.Log($"[{name}] Using Camera: {Camera.main.name}");

        if (pointer == null)
        {
            Debug.LogError($"[{name}] Pointer is NULL!");
            return;
        }

        if (hasDisappeared)
        {
            if (arrowInstance != null)
                arrowInstance.SetActive(false);
            return;
        }

        // Show arrow based on timer
        if (timer != null && timer.timeRemaining < 10f && arrowInstance != null)
        {
            arrowInstance.SetActive(true);
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + worldOffset);
            arrowInstance.transform.position = screenPos;
        }

        // Hide arrow if time is up
        if (timer != null && timer.timeRemaining <= 0f && arrowInstance != null)
        {
            arrowInstance.SetActive(false);
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
            if (distance < 0.03f) Debug.Log("Picked condition MET distance is "+ distance);
else Debug.Log("Too far to pick");

            Debug.Log("proximity is checked and in range  ");

            hasDisappeared = true;
            HandlePickup();
        }
    }

    void HandlePickup()
    {
        // Floating +1 text
        SpawnFloatingText();

        // Update count and UI
        switch (flowerType)
        {
            case FlowerType.Sunflower:
                sunflowerCount++;
                flowerText.text = $"Sunflower: {sunflowerCount}";
                break;
            case FlowerType.Rose:
                roseCount++;
                flowerText.text = $"Rose: {roseCount}";
                break;
        }

        // Hide arrow and object
        if (arrowInstance != null)
            Destroy(arrowInstance);

        Destroy(gameObject);
        Debug.Log("the gameobject should destroy");
    }

    void SpawnFloatingText()
    {
        if (floatingTextPrefab == null || uiCanvas == null) return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + worldOffsetText);
        screenPos += screenOffset;

        GameObject ft = Instantiate(floatingTextPrefab, uiCanvas.transform);
        ft.transform.position = screenPos;
    }
}
