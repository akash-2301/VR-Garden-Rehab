
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 40f;
    public float fadeSpeed = 1.5f;
    private TextMeshProUGUI text;
    private Color originalColor;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        originalColor = text.color;
        Destroy(gameObject, 1f); // Auto-destroy after 1 second
    }

    void Update()
    {
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // Fade out
        originalColor.a -= fadeSpeed * Time.deltaTime;
        text.color = originalColor;
    }
}
