using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;
using UnityEngine.SocialPlatforms.Impl;



public class FlowerStageManager : MonoBehaviour
{
    private ColorGrading colorGrading;

// Define desired values per stage (customize these)
public float[] stageTemperatures = { 20f, 0f };      // Sunflower, Rose, cos
    public float[] stageExposures = { 1f, 0.5f };

    public TextMeshProUGUI stageInfoText;
private string[] stageLabels = {
    "Level 1 : Stage 3",  // Sunflower
    "Level 1 : Stage 4",  // Rose
    "Level 1 : Stage 5"   // cos
};



    private bool allowGameOverCheck = false;
    public timer flowerTimer;
    public GameObject sunflower;
    public GameObject rose;
   

    public GameObject panel_instruction;
    public GameObject panel_gameover;
    public GameObject startPrompt;

    public GameObject player_2;
    public GameObject player_3;
    public GameObject HAND;
    public GameObject level2_assest;

    public HandPositionReceiver handPositionReceivercam;





    public PostProcessVolume VOLUME;
    private Bloom bloom;
    // Blinking settings
    public bool enableBlinking = true;
    public float blinkSpeed = 15f;       // Speed of blinking
    private float minIntensity = 1f;     // Minimum glow
    private float maxIntensity = 28f;    // Maximum glow







    private bool gameOverShown = false;

    public TextMeshProUGUI timerText;
    public TextMeshProUGUI flowerText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI timeLeftText;
    public string[][] stageInstructions = new string[][]
    {
        new string[] {  },
        new string[] { "Collect all the Pink Roses \n Time limit is 60 sec"}
        
    };

    private GameObject[] flowerObjects;
    private float[] stageTimes = { 60f, 60f, 60f };
    private string[] flowerNames = { "Sunflower", "Rose" };

    private int currentStage = 0;
    private int instructionCounter = 0;
    public Camera cam02;


    


   void Start()
{
    level2_assest.SetActive(false);
    flowerObjects = new GameObject[] { sunflower, rose };
    FlowerScript.flowerText = flowerText;

    if (VOLUME.profile.TryGetSettings(out bloom))
    {
        bloom.enabled.Override(true);
        bloom.intensity.Override(minIntensity);
    }

    // ✅ Assign colorGrading here
    if (VOLUME.profile.TryGetSettings(out colorGrading))
    {
        colorGrading.enabled.Override(true);
        colorGrading.temperature.Override(stageTemperatures[0]);
        colorGrading.postExposure.Override(stageExposures[0]);
    }
}


    void Update()
    {
FlowerScript test = flowerObjects[currentStage].GetComponentInChildren<FlowerScript>();


        if (flowerTimer.timeRemaining < 20f && flowerTimer.timeRemaining >= 0f && enableBlinking && bloom != null && bloom.enabled.value)
        {
            float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f; // Smooth 0–1 wave
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
            bloom.intensity.Override(intensity);
        }
        else if (bloom != null)
        {
            bloom.intensity.Override(minIntensity); // Reset to base when not blinking
        }



        if (!flowerTimer.timeCounting || gameOverShown || !allowGameOverCheck)
            return;





        if ((GetCurrentStageCount() >= 14) || (flowerTimer.timeRemaining <= 0f))
        {
//              GoogleFormSender formSender = FindObjectOfType<GoogleFormSender>();
//         if (formSender != null)
// {
//     if (currentStage == 0)
//     {
//         formSender.SetLevelStage(1, 3);
//     }
//     else
//     {
//         formSender.SetLevelStage(1, 4);
//     }

//     formSender.SetScore(GetCurrentStageCount(), 14);
//     formSender.SendData();
// }
            flowerTimer.timeCounting = false;
            gameOverShown = true;
            HAND.SetActive(false);



            if (panel_gameover != null)
            {
                Transform parent = panel_gameover.transform.parent;
                while (parent != null && !parent.gameObject.activeSelf)
                {
                    parent.gameObject.SetActive(true);
                    parent = parent.parent;
                }

                panel_gameover.SetActive(true);
            }

            timeLeftText.gameObject.SetActive(true);
            int timeUsed = Mathf.FloorToInt(stageTimes[currentStage] - flowerTimer.timeRemaining);
            int minutes = timeUsed / 60;
            int seconds = timeUsed % 60;
            timeLeftText.text = $"Score: {GetCurrentStageCount()}   Time : {minutes:00}:{seconds:00}";
        }


    }

  
    private int GetCurrentStageCount()
    {
        switch (currentStage)
        {
            case 0: return FlowerScript.sunflowerCount;
            case 1: return FlowerScript.roseCount;
        
            default: return 0;
        }
    }


     public void OnSharedInstructionClick()
    {
         HandPositionReceiver receiver = FindObjectOfType<HandPositionReceiver>();
    if (receiver != null)
    {
        receiver.RefreshMapping(cam02);
    }


        
        string[] currentInstructions = stageInstructions[currentStage];

        if (instructionCounter < currentInstructions.Length)
        {
            instructionText.text = currentInstructions[instructionCounter];
            instructionCounter++;
        }
        else
        {
            panel_instruction.SetActive(false);
            instructionCounter = 0;

            player_2.GetComponent<Animator>().enabled = false;
            StartCoroutine(ShowStartPromptAndBegin());
        }
    }

    private IEnumerator ShowStartPromptAndBegin()
    {
        flowerTimer.isFruitMode = false; // ✅ Switch to flower mode

        startPrompt.SetActive(true);
        flowerTimer.timeCounting = false;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(2f);
        flowerTimer.timeCounting = true;

        startPrompt.SetActive(false);

        // ✅ Ensure TextMeshProUGUI is enabled, not just GameObject
        
        if (flowerText != null)
        {
            flowerText.gameObject.SetActive(true);
            flowerText.enabled = true;
        }

        if (timerText != null)
        {
            timerText.gameObject.SetActive(true);
            timerText.enabled = true;
        }
        if (stageInfoText != null)
{
    stageInfoText.gameObject.SetActive(true); // ✅ Make sure it's visible
    stageInfoText.text = stageLabels[currentStage]; // ✅ Set correct label
}

        HAND.SetActive(true);
         FlowerScript flowerScript = flowerObjects[currentStage].GetComponent<FlowerScript>();
    if (flowerScript != null)
    {
        flowerScript.pointer = HAND.transform;
    }
    else
    {
        Debug.LogError($"❌ FlowerScript not found on {flowerObjects[currentStage].name}");
    }


        // Reset flower counts
        FlowerScript.sunflowerCount = 0;
        FlowerScript.roseCount = 0;
        

        flowerTimer.timeRemaining = stageTimes[currentStage];
        flowerText.text = $"{flowerNames[currentStage]}: 0";
        gameOverShown = false;

        // ✅ Prevent game over until next frame
        allowGameOverCheck = false;
        yield return null;
        Time.timeScale = 1f;
        // GoogleFormSender.Instance.SetStartTime();                     <-----------------------------------------------------------------------------------------------------------------------------------
        flowerTimer.timeCounting = true;
        flowerTimer.gameOverShown = false;
        allowGameOverCheck = true;
    }
 public void NextStage()
{
   
        if (currentStage == 1)
        {
            panel_gameover.SetActive(false);
            player_2.SetActive(false);
            level2_assest.SetActive(true);

            player_3.SetActive(true);



        }
        panel_gameover.SetActive(false);
    

    // Reset ALL static counts before proceeding
    FlowerScript.sunflowerCount = 0;
    FlowerScript.roseCount = 0;
    

    if (currentStage < flowerObjects.Length - 1)
    {
        flowerObjects[currentStage].SetActive(false);
        currentStage++;
        flowerObjects[currentStage].SetActive(true);

        HAND.SetActive(false);
        timerText.gameObject.SetActive(false);    // hide instead of disabling the TMP component
        flowerText.gameObject.SetActive(false);

            if (colorGrading != null)
            {

                colorGrading.enabled.Override(true);  // ✅ Enable for other stages
                colorGrading.temperature.Override(stageTemperatures[currentStage]);
                colorGrading.postExposure.Override(stageExposures[currentStage]);
            }





            // Reset timer here
            flowerTimer.timeRemaining = stageTimes[currentStage];
        flowerTimer.timeCounting = false; // Don't count until instructions are done

        ShowInstructionPanel();
    }
   else
{
    // Only show game over if this was truly the final stage AND we finished it
    if (gameOverShown == false) {
        panel_gameover.SetActive(true);
        gameOverShown = true;
       
    }
}
}

    void ShowInstructionPanel()
    {
        Time.timeScale = 0f;
        panel_instruction.SetActive(true);
        instructionCounter = 0;
        instructionText.text = stageInstructions[currentStage][instructionCounter];
        instructionCounter++;
    }
}