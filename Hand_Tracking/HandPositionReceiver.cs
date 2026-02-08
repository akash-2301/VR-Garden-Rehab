using UnityEngine;
using System.Collections.Generic;

public class HandPositionReceiver : MonoBehaviour
{
    public Camera mainCam;
    public Transform handObject;
    public float zDepth = 4.57f; // Constant depth used for mapping

    private Vector3[] mappedWorldCorners = new Vector3[4];
    private List<Vector2> calibratedCoords;

    public void InitializeMapping(Camera cam, List<Vector2> cameraCoords)
    {
        if (cam == null || cameraCoords == null || cameraCoords.Count < 4)
        {
            Debug.LogError("[INIT] Missing camera or calibration data.");
            return; 
        }

        calibratedCoords = cameraCoords;

        mappedWorldCorners[0] = cam.ScreenToWorldPoint(new Vector3(0, 0, zDepth));                         // Bottom Left
        mappedWorldCorners[1] = cam.ScreenToWorldPoint(new Vector3(0, cam.pixelHeight, zDepth));           // Top Left
        mappedWorldCorners[2] = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, zDepth)); // Top Right
        mappedWorldCorners[3] = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, 0, zDepth));            // Bottom Right

    }




    void Update()
    {
        if (calibratedCoords == null || calibratedCoords.Count < 4) return;
        if (mainCam == null || handObject == null) return;

        Vector2 latest = HandCalibrationManager.latestHandPos;
        if (latest == Vector2.zero) return;

        Vector3 mappedWorld = BilinearMapToWorld(latest);
        handObject.position = mappedWorld;

        Debug.DrawLine(mainCam.transform.position, mappedWorld, Color.green);
 

    }

    Vector3 BilinearMapToWorld(Vector2 camPos)
    {
        Vector2 bl = calibratedCoords[0];
        Vector2 tl = calibratedCoords[1];
        Vector2 tr = calibratedCoords[2];
        Vector2 br = calibratedCoords[3];

        float u = Mathf.InverseLerp(bl.x, br.x, camPos.x);
        float v = Mathf.InverseLerp(bl.y, tl.y, camPos.y);

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        Vector3 world =
            (1 - u) * (1 - v) * mappedWorldCorners[0] +
            (1 - u) * v * mappedWorldCorners[1] +
            u * v * mappedWorldCorners[2] +
            u * (1 - v) * mappedWorldCorners[3];

        return world;
    }
    public void RefreshMapping(Camera cam1)
{
    if (cam1 != null &&
        HandCalibrationManager.calibratedCameraCoords != null &&
        HandCalibrationManager.calibratedCameraCoords.Count == 4)
    {
        InitializeMapping(cam1, HandCalibrationManager.calibratedCameraCoords);
        Debug.Log("[Receiver] Mapping refreshed manually.");
    }
    else
    {
        Debug.LogWarning("[Receiver] Cannot refresh mapping – camera or calibration missing.");
    }
}


#if UNITY_EDITOR
void OnDrawGizmos()
{
    if (mainCam != null && calibratedCoords != null && calibratedCoords.Count == 4)
    {
        // Force recalculation in case mapping wasn't updated yet
        InitializeMapping(mainCam, calibratedCoords);
    }

    if (mappedWorldCorners == null || mappedWorldCorners.Length < 4) return;

    Gizmos.color = Color.cyan;
    Gizmos.DrawSphere(mappedWorldCorners[0], 0.1f);
    Gizmos.DrawSphere(mappedWorldCorners[1], 0.1f);
    Gizmos.DrawSphere(mappedWorldCorners[2], 0.1f);
    Gizmos.DrawSphere(mappedWorldCorners[3], 0.1f);

    Gizmos.DrawLine(mappedWorldCorners[0], mappedWorldCorners[1]);
    Gizmos.DrawLine(mappedWorldCorners[1], mappedWorldCorners[2]);
    Gizmos.DrawLine(mappedWorldCorners[2], mappedWorldCorners[3]);
    Gizmos.DrawLine(mappedWorldCorners[3], mappedWorldCorners[0]);
}
#endif

}
