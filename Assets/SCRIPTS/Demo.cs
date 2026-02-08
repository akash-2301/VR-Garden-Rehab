using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S

        Camera cam = Camera.main;

        // Get camera's right and up directions
        Vector3 camRight = cam.transform.right;
        Vector3 camUp = cam.transform.up;

        // Move in camera's screen plane (perpendicular to forward)
        Vector3 moveDirection = (camRight * horizontal + camUp * vertical).normalized;

        // Apply movement in world space
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }
}

//clamping code
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class Demo : MonoBehaviour
// {
//     public float moveSpeed = 5f;

//     void Update()
//     {
//         float horizontal = Input.GetAxis("Horizontal"); // A/D
//         float vertical = Input.GetAxis("Vertical");     // W/S

//         Camera cam = Camera.main;

//         Vector3 camRight = cam.transform.right;
//         Vector3 camUp = cam.transform.up;

//         Vector3 moveDirection = (camRight * horizontal + camUp * vertical).normalized;

//         // Calculate new world position
//         Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;

//         // Convert to viewport coordinates
//         Vector3 viewportPos = cam.WorldToViewportPoint(newPosition);

//         // Clamp to viewport (0 to 1 on x and y, z must stay positive to be visible)
//         viewportPos.x = Mathf.Clamp(viewportPos.x, 0.01f, 0.99f);
//         viewportPos.y = Mathf.Clamp(viewportPos.y, 0.01f, 0.99f);
//         viewportPos.z = Mathf.Max(viewportPos.z, 0.1f); // Avoid going behind camera

//         // Convert back to world position
//         transform.position = cam.ViewportToWorldPoint(viewportPos);
//     }
// }
