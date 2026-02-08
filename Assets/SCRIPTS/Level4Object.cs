using UnityEngine;
using TMPro;



public class Level4Object : MonoBehaviour
{
    public Level4StageManager manager;
    public enum Type { Mango, Apple, Sunflower, Rose }
    public Type objectType;
    public Transform pointer;
    public float disappearViewportThreshold = 0.03f; // ~3% of screen size

    private bool isActive = true;
    bool hasBeenPicked = false;
    void OnEnable()
    {
        isActive = true;

        // Optional safety check: reassign pointer and manager if null
        if (pointer == null)
            pointer = FindObjectOfType<Level4StageManager>()?.pointer;

        if (manager == null)
            manager = FindObjectOfType<Level4StageManager>();
    }
    public void ResetObject()
    {
        isActive = true;
        pointer = FindObjectOfType<Level4StageManager>().pointer; // Reassign pointer if needed
        manager = FindObjectOfType<Level4StageManager>();
    }
    void Update()
    {
        if (!isActive || pointer == null || manager.isGameOver) return;

         // Check distance
      Vector3 vpObject = Camera.main.WorldToViewportPoint(transform.position);
Vector3 vpPointer = Camera.main.WorldToViewportPoint(pointer.position);

float distance = Vector2.Distance(
    new Vector2(vpObject.x, vpObject.y),
    new Vector2(vpPointer.x, vpPointer.y)
);

        if (distance < disappearViewportThreshold) 
        {
            isActive = false;
            manager.HandlePickup(this);
        }
    }

    public void MarkInactive()
    {
        isActive = false;
        gameObject.SetActive(false); // not used anymore
    }
    public void ResetState()
{
    isActive = true;
    // Add more resets here if needed (like visuals, animations, etc.)
}
}
