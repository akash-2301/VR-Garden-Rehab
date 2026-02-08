using UnityEngine;

public class UIFixer : MonoBehaviour
{
    public GameObject gameOverPanel;
    public GameObject instructionPanel;
    public RectTransform scoreText;
    public RectTransform timerText;
    public RectTransform startButton;
    public RectTransform button1;
    public RectTransform button2;

    // Store original transforms
    private Vector3 origPos_gameOver, origPos_instruction, origPos_score, origPos_timer, origPos_start, origPos_btn1, origPos_btn2;
    private Quaternion origRot_gameOver, origRot_instruction, origRot_score, origRot_timer, origRot_start, origRot_btn1, origRot_btn2;
    private Vector3 origScale_gameOver, origScale_instruction, origScale_score, origScale_timer, origScale_start, origScale_btn1, origScale_btn2;

    private bool originalsSaved = false;

    public void ApplyUIFixForStage(int stageNumber)
    {
        if (stageNumber == 2 || stageNumber == 3)
        {
            // Save originals once
            if (!originalsSaved)
            {
                SaveOriginals();
                originalsSaved = true;
            }

            // Game Over Panel
            gameOverPanel.transform.localPosition = new Vector3(7.91f, 14.49f, 1.62f);
            gameOverPanel.transform.localRotation = Quaternion.Euler(-14.14f, 52.65f, -3.4f);
            gameOverPanel.transform.localScale = new Vector3(0.74f, 0.614f, 0.536f);

            // Instruction Panel
            instructionPanel.transform.localPosition = new Vector3(10.68f, 16.15f, -0.37f);
            instructionPanel.transform.localRotation = Quaternion.Euler(-14.14f, 52.65f, -3.4f);
            instructionPanel.transform.localScale = new Vector3(0.792f, 0.657f, 0.574f);

            // Score Text
            scoreText.localPosition = new Vector3(-559.3f, -436f, -13.03f);
            scoreText.localRotation = Quaternion.Euler(-23f, 35.32f, -5.3f);
            scoreText.localScale = new Vector3(2.1479f, 2.1479f, 2.1479f);

            // Timer Text
            timerText.localPosition = new Vector3(838f, -559f, -12f);
            timerText.localRotation = Quaternion.Euler(30.94f, -23.95f, -5.5f);
            timerText.localScale = new Vector3(2.14f, 2.14f, 2.14f);

            // Start Button
            startButton.localPosition = new Vector3(99f, -9f, 101f);
            startButton.localRotation = Quaternion.Euler(9.29f, -11.76f, -4.45f);
            startButton.localScale = new Vector3(2.7641f, 2.7641f, 2.7641f);

            // Button 1
            button1.localPosition = new Vector3(518f, -166f, -65f);
            button1.localRotation = Quaternion.identity;
            button1.localScale = new Vector3(3.645f, 6.33f, 3.758f);

            // Button 2
            button2.localPosition = new Vector3(-594f, -217.5f, 144f);
            button2.localRotation = Quaternion.identity;
            button2.localScale = new Vector3(3.594f, 6.241f, 3.705f);
        }
    }

    public void RestoreOriginalUI()
    {
        if (!originalsSaved) return; // No originals saved yet

        // Restore Game Over Panel
        gameOverPanel.transform.localPosition = origPos_gameOver;
        gameOverPanel.transform.localRotation = origRot_gameOver;
        gameOverPanel.transform.localScale = origScale_gameOver;

        // Restore Instruction Panel
        instructionPanel.transform.localPosition = origPos_instruction;
        instructionPanel.transform.localRotation = origRot_instruction;
        instructionPanel.transform.localScale = origScale_instruction;

        // Restore Score Text
        scoreText.localPosition = origPos_score;
        scoreText.localRotation = origRot_score;
        scoreText.localScale = origScale_score;

        // Restore Timer Text
        timerText.localPosition = origPos_timer;
        timerText.localRotation = origRot_timer;
        timerText.localScale = origScale_timer;

        // Restore Start Button
        startButton.localPosition = origPos_start;
        startButton.localRotation = origRot_start;
        startButton.localScale = origScale_start;

        // Restore Button 1
        button1.localPosition = origPos_btn1;
        button1.localRotation = origRot_btn1;
        button1.localScale = origScale_btn1;

        // Restore Button 2
        button2.localPosition = origPos_btn2;
        button2.localRotation = origRot_btn2;
        button2.localScale = origScale_btn2;
    }

    private void SaveOriginals()
    {
        origPos_gameOver = gameOverPanel.transform.localPosition;
        origRot_gameOver = gameOverPanel.transform.localRotation;
        origScale_gameOver = gameOverPanel.transform.localScale;

        origPos_instruction = instructionPanel.transform.localPosition;
        origRot_instruction = instructionPanel.transform.localRotation;
        origScale_instruction = instructionPanel.transform.localScale;

        origPos_score = scoreText.localPosition;
        origRot_score = scoreText.localRotation;
        origScale_score = scoreText.localScale;

        origPos_timer = timerText.localPosition;
        origRot_timer = timerText.localRotation;
        origScale_timer = timerText.localScale;

        origPos_start = startButton.localPosition;
        origRot_start = startButton.localRotation;
        origScale_start = startButton.localScale;

        origPos_btn1 = button1.localPosition;
        origRot_btn1 = button1.localRotation;
        origScale_btn1 = button1.localScale;

        origPos_btn2 = button2.localPosition;
        origRot_btn2 = button2.localRotation;
        origScale_btn2 = button2.localScale;
    }
}
