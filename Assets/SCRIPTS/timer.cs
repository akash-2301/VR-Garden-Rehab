using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering.PostProcessing;

public class timer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoretext;
    public GameObject panel;
    public GameObject player;
    public bool isFruitMode = true;

    public float timeRemaining = 60f;
    public bool isCountingDown = true;
    public bool timeCounting = true;

    // public PostProcessVolume VOLUME;
    private Vignette vignettee;

    private bool isActive = false;
    public bool gameOverShown = false;
    private int scoreAtT1 = 0;
    private int scoreAtT2 = 0;


    private void Start()
    {
        // if (VOLUME.profile.TryGetSettings(out vignettee))
        // {
        //     vignettee.enabled.Override(false);
        // }
    }

    void OnEnable()
    {
        timeCounting = true;
        timeRemaining = 60f;
        isActive = true;
        gameOverShown = false;

        if (scoretext != null)
            scoretext.gameObject.SetActive(false);
        if (panel != null)
            panel.SetActive(false);
        if (vignettee != null)
            vignettee.enabled.Override(false);

        // // Invoke score sends
        // Invoke("SendT1", 40f);
        // Invoke("SendT2", 50f);
        // Invoke("SendT3Final", 60f);
    }

    // void OnDisable()
    // {
    //     isActive = false;
    //     CancelInvoke("SendT1");
    //     CancelInvoke("SendT2");
    //     CancelInvoke("SendT3Final");
    // }

    void Update()
    {
        if (!isActive || !isCountingDown || gameOverShown || !timeCounting)
            return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;

            if (isFruitMode)
            {
                bool mangoDone = mangoscript.mangoCount == mangoscript.totalMangos;
                bool appleDone = mangoscript.appleCount == mangoscript.totalApples;

                if (timeRemaining <= 0 || mangoDone || appleDone)
                {
                    // GoogleFormSender formSender = FindObjectOfType<GoogleFormSender>();<--------------------------------------------------------------------------------------------

                    // if (appleDone || !mangoDone)
                    // {
                    //     formSender.SetLevelStage(1, 2);
                    //     formSender.SetScore(mangoscript.appleCount, 11);
                    // }
                    // else
                    // {
                    //     formSender.SetLevelStage(1, 1);
                    //     formSender.SetScore(mangoscript.mangoCount, 15);
                    // }

                    // CancelInvoke("SendT1");
                    // CancelInvoke("SendT2");
                    // CancelInvoke("SendT3Final");

                    // formSender.timeStamp = ""; // Final row, no label
                    // formSender.SendData();

                    ShowGameOver();
                    player.GetComponent<Animator>().enabled = false;
                }
            }
            else
            {
                if (timeRemaining <= 0)
                {
                    ShowGameOver();
                }
            }
        }

        UpdateTimerText();
    }

    void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }

   void ShowGameOver()
{
    gameOverShown = true;

    int timeUsed = Mathf.FloorToInt(60f - timeRemaining);
    int minutes = timeUsed / 60;
    int seconds = timeUsed % 60;

    string score = "0";

    if (mangoscript.mangoCount > 0)
        score = $"{mangoscript.mangoCount}";
    else if (mangoscript.appleCount > 0)
        score = $"{mangoscript.appleCount}";


    if (scoretext != null)
    {
        scoretext.text = $"Score: {score}   Time: {minutes:00}:{seconds:00}";
        scoretext.gameObject.SetActive(true);
    }

    if (panel != null)
        panel.SetActive(true);

    // vignettee.enabled.Override(true);

//     // 🔥 FINAL ROW TO GOOGLE FORM:
//     GoogleFormSender.Instance.timeStamp = ""; // Blank = final summary row
//     int finalScore = GetActiveStageScore();
//     int finalTotal = GetActiveStageTotal();
//     GoogleFormSender.Instance.SetScore(finalScore, finalTotal);
//     GoogleFormSender.Instance.SendData();
// }

//    void SendT1()
// {
//     scoreAtT1 = GetActiveStageScore();
//     GoogleFormSender.Instance.SetScore(scoreAtT1, GetActiveStageTotal());
//     GoogleFormSender.Instance.timeStamp = "T1";
//     GoogleFormSender.Instance.SendData();
// }

// void SendT2()
// {
//     int scoreNow = GetActiveStageScore();
//     int delta = scoreNow - scoreAtT1;
//     scoreAtT2 = scoreNow;

//     GoogleFormSender.Instance.SetScore(delta, GetActiveStageTotal() - scoreAtT1);
//     GoogleFormSender.Instance.timeStamp = "T2";
//     GoogleFormSender.Instance.SendData();
// }

// void SendT3Final()
// {
//     int scoreNow = GetActiveStageScore();
//     int delta = scoreNow - scoreAtT2;

//     GoogleFormSender.Instance.SetScore(delta, GetActiveStageTotal() - scoreAtT2);
//     GoogleFormSender.Instance.timeStamp = "T3";
//     GoogleFormSender.Instance.SendData();
// }

//     void SetScoreForGoogle()
//     {
//         if (isFruitMode)
//         {
//             if (mangoscript.mangoCount < mangoscript.totalMangos)
//             {
//                 GoogleFormSender.Instance.SetLevelStage(1, 1);
//                 GoogleFormSender.Instance.SetScore(mangoscript.mangoCount, 15);
//             }
//             else
//             {
//                 GoogleFormSender.Instance.SetLevelStage(1, 2);
//                 GoogleFormSender.Instance.SetScore(mangoscript.appleCount, 11);
//             }
//         }

}
//   int GetActiveStageScore()
// {
//     // Return apple score if in apple stage
//     if (mangoscript.totalApples > 0)
//         return mangoscript.appleCount;

//     // Else return mango score
//     if (mangoscript.totalMangos > 0)
//         return mangoscript.mangoCount;

//     return 0;
// }


// int GetActiveStageTotal()
// {
//     // Return apple total if in apple stage
//     if (mangoscript.totalApples > 0)
//         return mangoscript.totalApples;

//     // Else return mango total
//     if (mangoscript.totalMangos > 0)
//         return mangoscript.totalMangos;

//     return 0;
// }

}