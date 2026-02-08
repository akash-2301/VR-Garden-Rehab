using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class HandCalibrationManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject calibrationPanel;
    public GameObject canvas;
    public GameObject player;
    public GameObject initialscript;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI stepText;
    public Slider progressSlider;
    public Image statusIcon;
    public Sprite loadingSprite;
    public Sprite checkmarkSprite;

    [Header("Main Camera")]
    public Camera mainCam;
    public Camera cam02;

    [Header("Calibration Settings")]
    public float sliderSpeed = 2f;
    public float requiredHoldTime = 3f;
    public float movementThreshold = 20f;
    public float checkmarkDuration = 1f;

    public static List<Vector2> calibratedCameraCoords = new List<Vector2>();
    private  bool isMapped = false;

    private Vector2 lastRecordedPos = Vector2.zero;
    public static Vector2 latestHandPos = Vector2.zero;
    private bool waitingToRecord = true;
    private bool isCalibrating = false;
    
    private float stayTime = 0f;
    private float targetSliderValue = 0f;
    private int stepIndex = 0;
    private string[] instructions = {
        "Move your hand to the bottom-left",
        "Move your hand to the upper-left",
        "Move your hand to the upper-right",
        "Move your hand to the bottom-right"
    };

    // Networking
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool running = false;

    private bool showCheckmarkTemporarily = false;

    void Start()
    {
        
      
        calibrationPanel.SetActive(true);
        instructionText.text = instructions[stepIndex];
        stepText.text = $"Step {stepIndex + 1}/4";
        progressSlider.minValue = 0f;
        progressSlider.maxValue = 1f;
        progressSlider.value = 0f;

        lastRecordedPos = latestHandPos;
        waitingToRecord = true;

        try
        {
            client = new TcpClient("127.0.0.1", 5010);
            stream = client.GetStream();
            running = true;
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log("[CALIBRATION] Socket connected");
        }
        catch (SocketException e)
        {
            Debug.LogError("[CALIBRATION] Socket error: " + e.Message);
        }
    }

    void Update()
{
    

    if (Mathf.Abs(progressSlider.value - targetSliderValue) > 0.001f)
        {
            progressSlider.value = Mathf.MoveTowards(progressSlider.value, targetSliderValue, sliderSpeed * Time.deltaTime);
        }

    if (stepIndex >= instructions.Length || latestHandPos == Vector2.zero)
        return;

    float dist = Vector2.Distance(latestHandPos, lastRecordedPos);
  

    if (dist < movementThreshold)
        {
            stayTime += Time.deltaTime;

            if (stayTime >= requiredHoldTime && waitingToRecord)
            {
                Debug.Log($"Step {stepIndex} completed. HandPos: {latestHandPos}");
                calibratedCameraCoords.Add(latestHandPos);
                waitingToRecord = false;

                targetSliderValue = (stepIndex + 1) / (float)instructions.Length;
                lastRecordedPos = latestHandPos;
                stayTime = 0f;

                stepIndex++;
                Debug.Log("Moving to next step: " + stepIndex);

                if (stepIndex < instructions.Length)
                {
                    instructionText.text = instructions[stepIndex];
                    stepText.text = $"Step {stepIndex + 1}/4";
                    StartCoroutine(ShowCheckmarkThenLoading());
                }
                else
                {
                    instructionText.text = "Calibration Complete!";
                    StartCoroutine(FinishCalibration());
                }
            }
        }
        else
        {
            if (!waitingToRecord)
            {
                Debug.Log("Resetting for next recording...");
                waitingToRecord = true;  // <== THIS IS CRUCIAL
            }

            stayTime = 0f;
            lastRecordedPos = latestHandPos;
            statusIcon.sprite = loadingSprite;
        }

    if (!showCheckmarkTemporarily && statusIcon.sprite == loadingSprite)
    {
        statusIcon.transform.Rotate(Vector3.forward * -400f * Time.deltaTime);
    }
}


   IEnumerator ShowCheckmarkThenLoading()
{
    showCheckmarkTemporarily = true;
    statusIcon.sprite = checkmarkSprite;
    statusIcon.transform.rotation = Quaternion.identity;
    yield return new WaitForSeconds(checkmarkDuration);

    if (stepIndex < instructions.Length)
    {
        statusIcon.sprite = loadingSprite;
        showCheckmarkTemporarily = false;
        waitingToRecord = true; // <== ADD THIS TO ALLOW NEXT STEP
    }
}


    IEnumerator FinishCalibration()
    {
        yield return new WaitForSecondsRealtime(1f);

        canvas.SetActive(false);
        player.SetActive(true);
        Time.timeScale = 0f;
        cam02.enabled = false;
         initialscript.SetActive(true);

        Debug.Log("[CALIBRATION COMPLETE] Points Collected:");
        foreach (var pt in calibratedCameraCoords)
            Debug.Log($"  {pt}");

        HandPositionReceiver receiver = FindObjectOfType<HandPositionReceiver>();
if (receiver != null)
    receiver.InitializeMapping(mainCam, calibratedCameraCoords);
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[1024];
        while (running)
        {
            try
            {
                stream.Write(Encoding.ASCII.GetBytes("get"));
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);

               HandData data = JsonUtility.FromJson<HandData>(json);
latestHandPos = data.handDetected ? new Vector2(data.x, data.y) : Vector2.zero;

                
            }
            catch (System.Exception e)
            {
                Debug.LogError("[CALIBRATION] ReceiveLoop error: " + e.Message);
            }

            Thread.Sleep(30);
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        try { receiveThread?.Abort(); } catch { }
        stream?.Close();
        client?.Close();
    }

    [System.Serializable]
    public class HandData
    {
        public float x;
        public float y;
        public bool handDetected;
    }
}
