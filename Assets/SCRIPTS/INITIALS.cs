using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.GlobalIllumination;
using Unity.Mathematics;
using UnityEngine.SocialPlatforms.Impl;



public class INITIALS : MonoBehaviour
{
     public timer flowerTimer;
    public GameObject canvas1;//canvas associated with start screen.
    public GameObject canvas_1;//canvas associated with player screen.
    
    public GameObject panel_1;
    public GameObject panel_2;
    public GameObject lightt;
    public GameObject sunflower;
    
    public GameObject player_1;
    public GameObject player_2;
    public GameObject startPrompt; // Assign in Inspector (your "Start" text/button UI)
    public Camera cam03;
    public Camera cameraa;

   

    

    
    private int counter = 0;
    public bool isAppleStageInstructions = false;






    // public Animation sunflowerAnimation;

    public TextMeshProUGUI instruction1_text;
    public TextMeshProUGUI instruction2_text;
   
    public TextMeshProUGUI alert;
    public GameObject mango;
    public GameObject apple;
    public GameObject sunfflower;
    public GameObject Timerr;
    private Vector3 initialHandPos;
    private Quaternion initialHandRot;
     public Transform pointer;




    void Start()
    {
        initialHandPos = pointer.transform.localPosition;
        initialHandRot = pointer.transform.localRotation;
        pausegame();
        alert.gameObject.SetActive(false);
        player_2.SetActive(false);
        startPrompt.SetActive(false);

    }
    private void Update()
    {
        if (flowerTimer.timeRemaining < 25f)
        {
            alert.gameObject.SetActive(true);
            alert.text = $"Hurry up! Only 25s left!";
            if (flowerTimer.timeRemaining < 22f) { alert.gameObject.SetActive(false); }
        }


    }
    public void clickStartButton() // game starts by clicking button 
    {
        canvas1.SetActive(false);
        Time.timeScale = 1f;
        cameraa.enabled = false;

    }

    public void pausegame() //pause the game anywhere in the program
    {

        player_1.SetActive(false);

    }

    public void restart() //on clicking the restart button
    {

        SceneManager.LoadScene("garden_main");
    }
    public void quit9() // for quiting the game 
    {
        Application.Quit();
    }


    





    public void ResumeGameAfterAppleInstructions()
    {

        StartCoroutine(StartCountdownWithDelay());

    }
    private IEnumerator StartCountdownWithDelay()
    {

        if (startPrompt != null)
            startPrompt.SetActive(true); // Show the Start! text/button

        yield return new WaitForSecondsRealtime(3f); // Wait for 2 seconds even if game paused

        if (startPrompt != null)// Hide Start text
            startPrompt.SetActive(false);
            Time.timeScale = 1f;
        // GoogleFormSender.Instance.SetStartTime();  <---------------------------------------------------------------------------------------

        

        // Now start the timer
        flowerTimer.timeCounting = true;
        flowerTimer.timeRemaining = 60f;

        // Update the UI text manually (optional if timer.cs does it anyway)
        if (canvas_1.transform.Find("timer")?.TryGetComponent(out TextMeshProUGUI timerText) == true)
        {
            int minutes = Mathf.FloorToInt(flowerTimer.timeRemaining / 60);
            int seconds = Mathf.FloorToInt(flowerTimer.timeRemaining % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }


public void OnSharedInstructionClick()
{
    

     HandPositionReceiver receiver = FindObjectOfType<HandPositionReceiver>();
    if (receiver != null)
    {
        receiver.RefreshMapping(cam03);
    }

    if (!isAppleStageInstructions)
        {
            // Do nothing: panel already contains instruction1 in Editor
            panel_1.SetActive(false);
            // GoogleFormSender.Instance.SetStartTime();     <-------------------------------------------------------------------------
            Time.timeScale = 1f;
        }
        else
        {
            // Dynamically show instruction2

            panel_2.SetActive(false);
            ResumeGameAfterAppleInstructions();
        }
}













    public void next_button()
    {
          
        pointer.transform.localPosition = initialHandPos;
        pointer.transform.localRotation = initialHandRot;
        if (lightt != null)
        {
            lightt.transform.rotation = Quaternion.Euler(41.3f, -225.2f, 0f); // Replace with your desired angles
        }
        mango.SetActive(false);
        apple.SetActive(true);
        isAppleStageInstructions = true;
        

        // Update MangoText to say "Apple:"
        if (mangoscript.appleText != null)
        {
            mangoscript.appleText.text = "Score: 0";
            mangoscript.appleCount = 0;
        }
        flowerTimer.isFruitMode = true;
        // Reset timer
        flowerTimer.timeRemaining = 60f;
        flowerTimer.timeCounting = true;




        timer t = FindObjectOfType<timer>();
        if (t != null)
        {
            t.enabled = false;  // Force-reset
            t.enabled = true;   // Re-enable so OnEnable() resets visuals
        }


        mangoscript.mangoCount = 0; // Optional, to prevent timer.cs from triggering early

        Transform mangotxt = canvas_1.transform.Find("MangoText");
        Transform timerr = canvas_1.transform.Find("timer");
        Transform panell = canvas_1.transform.Find("Panel"); // panel_gameover

        Transform handd = player_1.transform.Find("Hand");
        Transform levell = canvas_1.transform.Find("level");

        if (mangotxt != null)
        {
            mangotxt.gameObject.SetActive(true);
            mangotxt.GetComponent<TextMeshProUGUI>().text = "Score: 0"; // change label
        }

        if (timerr != null) timerr.gameObject.SetActive(true);
        if (panell != null) panell.gameObject.SetActive(false);
        if (handd != null)
        {
            handd.gameObject.SetActive(true);

        }
       panel_2.SetActive(true);



        if (levell != null)
        {
            levell.GetComponent<TextMeshProUGUI>().text = "Level 1: Stage 2";
        }

        
    }

   public void next_button_stage3()
{
    
    
        apple.SetActive(false);
    pointer.transform.localPosition = initialHandPos;
        pointer.transform.localRotation = initialHandRot;
        sunflower.SetActive(true);
    
    player_1.SetActive(false);
    canvas_1.SetActive(false);
    apple.SetActive(false);

    // Reset flower counts BEFORE enabling player_2
    FlowerScript.sunflowerCount = 0;
    FlowerScript.roseCount = 0;
   

    player_2.SetActive(true); // Now safe to enable
    Timerr.SetActive(true);   // Timer object (used by FlowerStageManager)

    // Optionally, ensure timer doesn't count until ready
    flowerTimer.timeCounting = false;
}
       



       public void OnGameOverNextClick()
{
    if (!isAppleStageInstructions)//intially set to false..(not) sets true so first this function runs
    {
        next_button(); // Move from mango to apple
    }
    else
    {
        next_button_stage3(); // Move from apple to sunflower
    }
}
  

} 

        







  





    



  