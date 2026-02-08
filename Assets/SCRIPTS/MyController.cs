    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.PostProcessing;
    using UnityEngine.UI;

    public class MyController : MonoBehaviour
    {

public GameObject panel_instruction;
    public Transform Cam;
        public Button next_1;
        public GameObject panel_2;

        public GameObject canvas3;
        public GameObject canvas2;
        public GameObject instruction_panel;
        public GameObject player3;
        public GameObject player4;

        public GameObject player;
        //camera position access
        public Level3StageManager level3;
        public Level1StageManager level1;
        public Level8StageManager level8;
        public GameObject leveltext;
        public Animator playerAnimator;


        //LEVEL 3
        public GameObject PANEL_INSTRUCTION;
        public PostProcessVolume VOLUME;
        private ColorGrading colorGrading; // local reference

        public bool isPaused = false;
        public TextMeshProUGUI stagetext;
        private Vector3 initialCamPosition;
        private Quaternion initialCamRotation;
        private Vector3 initialPlayerPosition;
        private Quaternion initialPlayerRotation;
        public Animator camAnimator; // assign in Inspector


        private void Start()
        {
            // VOLUME.profile.TryGetSettings(out colorGrading);
            // Record camera/player positions as the new default reset point

        }


        public void ReplayIntroAnimation()
        {
            if (camAnimator != null)
            {
                camAnimator.enabled = true;                  // make sure it's active
                camAnimator.Rebind();                        // reset animator state
                camAnimator.Update(0f);                      // apply reset immediately
                camAnimator.Play("YourClipName", 0, 0f);     // restart animation from frame 0
            }
        }








        public void button_start()
        {
            Time.timeScale = 0f;

        }
        public void pause_animanation()
        {
            player.GetComponent<Animator>().enabled = false;
            instruction_panel.SetActive(true);
        }
        public void pause_animation2()
        {
            player3.GetComponent<Animator>().enabled = false;

            panel_instruction.SetActive(true);

            level3.counterText.text = "Score : 0";
            level3.timertext.text = "Time : 00:00";

            canvas3.SetActive(true);
            level3.ShowInstructionPanel();






        }
        public void PauseAndShowInstructions()
    {
        
        if (canvas3 != null)
            canvas3.SetActive(true);


        playerAnimator.speed = 0;
        isPaused = true;

        // ✅ Show instruction panel through Level3 manager
        if (level3 != null)
            level3.ShowInstructionPanel();

        Debug.Log("Canvas3 forced active — instructions displayed.");
    }

        void Update()
    {
            
            transform.position = Cam.position;

            // Resume animation only when stage 1 starts
          if (isPaused && level3.GetActiveStageIndex() >= 2)
            {
                playerAnimator.speed = 1;
                isPaused = false;
                Debug.Log("Animation resumed — current stage " + level3.GetActiveStageIndex());
            }
            if (isPaused && level1.currentStage == 2)
            {
                playerAnimator.speed = 1; // Resume animation
                isPaused = false;
            }
        }
        public void ResetCameraAndPlayer()
        {
            // Reset player animator speed/state
            playerAnimator.speed = 1;
            isPaused = false;

            // Optional: reset animators to initial state
            if (player.GetComponent<Animator>() != null)
                player.GetComponent<Animator>().Rebind();
            if (player3.GetComponent<Animator>() != null)
                player3.GetComponent<Animator>().Rebind();
            if (player4.GetComponent<Animator>() != null)
                player4.GetComponent<Animator>().Rebind();

            // Reset camera to initial position/rotation
            
        }


        public void LEVEL3_CONT()
    {
        instruction_panel.SetActive(true);
        level8.enabled = true;
            
            player4.GetComponent<Animator>().enabled = false;
        
        
        }
        public void Level4()
        {
            player4.GetComponent<Animator>().enabled = false;
            canvas3.SetActive(true); // Show bee camera canvas
            player.SetActive(false); // Hide main player camera
            player3.SetActive(false); // Hide main player camera2

            // Start coroutine to switch back after 10 seconds
            StartCoroutine(SwitchBackAfterDelay());
        }

        private IEnumerator SwitchBackAfterDelay()
        {
            yield return new WaitForSeconds(4f); // Wait for 10 seconds
            player.SetActive(true);// Show main player camera
            player3.SetActive(true);// Show main player camera
            VOLUME.enabled = true; // turn on the volume
            if (colorGrading != null)
                colorGrading.active = true;
            Destroy(canvas3); // Hide bee camera canvas
            instruction_panel.SetActive(true);
            stagetext.enabled = true;
        }
        public void LEVEL5()
    {
        Level6StageManager textt = GetComponent<Level6StageManager>();
        textt.SetInstructionPanelText();
            
            instruction_panel.SetActive(true);
            
            player4.GetComponent<Animator>().enabled = false;


        }

        public void countinuegame()
    {
          var l80 = GetComponent<Level1StageManager>();
                    if (l80 != null) l80.enabled = true;

        Time.timeScale = 1f;
           
            canvas2.SetActive(false);
        }
        public void ShowInstructions2()
        {




            canvas3.SetActive(true);
            level1.ShowInstructionPanel();
            leveltext.SetActive(true);  //assign mangogroup
            playerAnimator.speed = 0; // Pause animation
            isPaused = true;
            


        }
        public void pause_animation1()
        {
            player3.GetComponent<Animator>().enabled = false;



            level1.counterText.text = "Score : 0";
            level1.timertext.text = "Time : 00:00";

            canvas3.SetActive(true);
            level1.ShowInstructionPanel();






        }
        
    }
        


