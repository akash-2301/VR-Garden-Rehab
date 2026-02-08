using UnityEngine;
using TMPro;

public class timer_01 : MonoBehaviour
{
    public float timeRemaining = 60f;
    public bool timeCounting = false;

    public TextMeshProUGUI timerText;
    public GameObject panel_gameover;
    public TextMeshProUGUI scoreText;
    

    private bool gameOverShown = false;
    private Level1StageManager stageManager;

    void OnEnable()
    {
        timeCounting = true;
        gameOverShown = false;
        stageManager = FindObjectOfType<Level1StageManager>();

        if (timerText != null)
            UpdateTimerDisplay();
        if (panel_gameover != null)
            panel_gameover.SetActive(false);
        if (scoreText != null)
            scoreText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!timeCounting || gameOverShown) return;

        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerDisplay();
        }
        else
        {
            timeRemaining = 0;

            if (!gameOverShown && stageManager != null)
            {
                Debug.Log("Timer ended, calling TheGameover()");
                gameOverShown = true;
                
            }


        }
          
        
    }

    void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"Time: {minutes:00}:{seconds:00}";
    }

   
}
