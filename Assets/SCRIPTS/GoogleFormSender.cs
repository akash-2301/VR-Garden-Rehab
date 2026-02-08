using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GoogleFormSender : MonoBehaviour
{
    public static GoogleFormSender Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private string googleFormURL = "https://script.google.com/macros/s/AKfycbxwkOwlbb24pkfrBtxL4AUEM_1Eop5hH5_dfcDdKOfoqdhhwZaQVKgAwUzfpOvVfrGJ/exec";

    [Header("Game Data")]
    public string playerName = "Player_01";
    public int worldNumber = 1;
    public int level = 1;
    public int stage = 1;
    public float scoreMade = 0;
    public int totalScore = 30;

    private string startTime;
    private string stopTime;
    public string timeStamp = ""; // T1, T2, T3 or ""

    void Start()
    {
        startTime = System.DateTime.Now.ToString("HH:mm:ss");
    }

    public void SetStartTime()
    {
        startTime = System.DateTime.Now.ToString("HH:mm:ss");
    }

    public void SetLevelStage(int currentLevel, int currentStage)
    {
        level = currentLevel;
        stage = currentStage;
    }

    public void SetScore(float made, int total)
    {
        scoreMade = made;
        totalScore = total;
    }

    public void SendData()
    {
        stopTime = System.DateTime.Now.ToString("HH:mm:ss");
        StartCoroutine(PostToGoogle());
    }
    

    IEnumerator PostToGoogle()
    {
        float percentageScore = totalScore > 0 ? ((float)scoreMade / totalScore) * 100f : 0f;
        string formattedScore = $"{scoreMade}/{totalScore}";
        string formattedPercentage = $"{percentageScore:F1}%";

        Dictionary<string, string> formData = new Dictionary<string, string>
        {
            ["PlayerName"] = playerName,
            ["World_No"] = worldNumber.ToString(),
            ["Levels"] = level.ToString(),
            ["Stages"] = stage.ToString(),
            ["Time_Stamp"] = timeStamp,
            ["Start_time"] = startTime,
            ["Stop_time"] = stopTime,
            ["Score"] = formattedScore,
            ["Score_Percentage"] = formattedPercentage
        };

        string jsonData = JsonUtility.ToJson(new SerializableForm(formData));

        UnityWebRequest request = new UnityWebRequest(googleFormURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Data sent to Google Sheet: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error sending data: " + request.error);
        }
    }
       

    [System.Serializable]
    public class SerializableForm
    {
        public string PlayerName;
        public string World_No;
        public string Levels;
        public string Stages;
        public string Time_Stamp;
        public string Start_time;
        public string Stop_time;
        public string Score;
        public string Score_Percentage;

        public SerializableForm(Dictionary<string, string> dict)
        {
            PlayerName = dict["PlayerName"];
            World_No = dict["World_No"];
            Levels = dict["Levels"];
            Stages = dict["Stages"];
            Time_Stamp = dict["Time_Stamp"];
            Start_time = dict["Start_time"];
            Stop_time = dict["Stop_time"];
            Score = dict["Score"];
            Score_Percentage = dict["Score_Percentage"];
        }
        
    }
}
