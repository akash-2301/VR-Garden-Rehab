using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;


public class Level4StageManager : MonoBehaviour
{
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverPanel_score;
    public GameObject startPrompt;
    public GameObject instructionPanel;

    public Transform pointer;
    public GameObject[] mangoes;
    public GameObject[] apples;
    public GameObject[] sunflowers;
    public GameObject[] roses;
    public GameObject[] inactiveMangoes;
    public GameObject[] inactiveApples;
    public GameObject[] inactiveSunflowers;
    public GameObject[] inactiveRoses;
    public GameObject hand;

    public GameObject floatingTextPlusPrefab;
    public GameObject floatingTextMinusPrefab;

    public TextMeshProUGUI mangoCountText;
    public TextMeshProUGUI appleCountText;
    public TextMeshProUGUI sunflowerCountText;
    public TextMeshProUGUI roseCountText;
    private int mangoCount = 0;
    private int appleCount = 0;
    private int sunflowerCount = 0;
    private int roseCount = 0;
    public GameObject mangoPanelImage;
    public GameObject applePanelImage;
    public GameObject sunflowerPanelImage;
    public GameObject rosePanelImage;
   public GameObject player_4;
    public GameObject player_5;
    public GameObject level3_assest;
    public GameObject level4_assest;






    private List<Level4Object> allActiveObjects = new List<Level4Object>();

    private float timer = 60f;
    private int lastSecond = -1;
    private HashSet<GameObject> usedInactiveObjects = new HashSet<GameObject>();


    private int score = 0;
    public bool isGameOver = false;

    private string[] fixedInstructions = { "Pluck only Apple", "Pluck only Mango", "Pluck only Rose", "Pluck only Sunflower" };
    private Level4Object.Type[] fixedTypes = {
        Level4Object.Type.Apple,
        Level4Object.Type.Mango,
        Level4Object.Type.Rose,
        Level4Object.Type.Sunflower
    };

    private List<string> dynamicInstructions = new List<string>();
    private List<Level4Object.Type> dynamicTypes = new List<Level4Object.Type>();

    private int fixedCycleIndex = 0;
    private bool fixedCycleDone = false;
    private Level4Object.Type currentInstructionType;

    public Canvas uiCanvas;                 // Assign in inspector
    public Vector3 screenOffset = new Vector3(30f, 0f, 0f);
    public Vector3 worldOffset = new Vector3(0f, 0.5f, 0f);

    void Start()
    {
        
        RegisterObjects(mangoes);
        RegisterObjects(apples);
        RegisterObjects(sunflowers);
        RegisterObjects(roses);
    }

    void Update()
    {
        if (isGameOver) return;

        timer -= Time.deltaTime;


        int curSecond = Mathf.FloorToInt(timer);
        if (curSecond != lastSecond)
        {
            timerText.text = $"Time: {curSecond}";
            lastSecond = curSecond;
        }

        scoreText.text = $"Score: {score}";


        if (timer <= 0 || score > 200)
        {
            EndLevel();
        }
    }

    void UpdateInstruction()
    {
        if (!fixedCycleDone)
        {
            instructionText.text = fixedInstructions[fixedCycleIndex];
            currentInstructionType = fixedTypes[fixedCycleIndex];
            fixedCycleIndex++;

            if (fixedCycleIndex >= fixedInstructions.Length)
            {
                fixedCycleDone = true;
                ShuffleInstructions();
            }
        }
        else
        {
            if (dynamicInstructions.Count == 0) ShuffleInstructions();

            int rand = Random.Range(0, dynamicInstructions.Count);
            instructionText.text = dynamicInstructions[rand];
            currentInstructionType = dynamicTypes[rand];

            dynamicInstructions.RemoveAt(rand);
            dynamicTypes.RemoveAt(rand);
        }
    }

    void ShuffleInstructions()
    {
        dynamicInstructions = new List<string>(fixedInstructions);
        dynamicTypes = new List<Level4Object.Type>(fixedTypes);
    }

    void RegisterObjects(GameObject[] group)
    {
        foreach (GameObject go in group)
        {
            Level4Object obj = go.GetComponent<Level4Object>();
            if (obj != null)
            {
                obj.manager = this;
                obj.pointer = pointer;
                allActiveObjects.Add(obj);
            }
        }
    }

    public void HandlePickup(Level4Object obj)
    {
        if (isGameOver) return;

        bool correct = obj.objectType == currentInstructionType;

        if (correct)
        {
            score += 1;
            timer += 3f;
            ShowFloatingText("+3s", obj.transform.position);
            switch (obj.objectType)
            {
                case Level4Object.Type.Mango:
                    mangoCount++;
                    mangoCountText.text = $"{mangoCount}";
                    mangoPanelImage.SetActive(true); // ✅ Show image
                    break;

                case Level4Object.Type.Apple:
                    appleCount++;
                    appleCountText.text = $"{appleCount}";
                    applePanelImage.SetActive(true);
                    break;

                case Level4Object.Type.Sunflower:
                    sunflowerCount++;
                    sunflowerCountText.text = $"{sunflowerCount}";
                    sunflowerPanelImage.SetActive(true);
                    break;

                case Level4Object.Type.Rose:
                    roseCount++;
                    roseCountText.text = $"{roseCount}";
                    rosePanelImage.SetActive(true);
                    break;
            }
        }
        else
        {
            timer -= 1f;
            ShowFloatingText("-1s", obj.transform.position);
        }

        obj.gameObject.SetActive(false);
        EnableRandomInactiveOfType(obj.objectType);
    }
    void EnableRandomInactiveOfType(Level4Object.Type type)
    {
        GameObject[] sourceArray = null;

        switch (type)
        {
            case Level4Object.Type.Mango: sourceArray = inactiveMangoes; break;
            case Level4Object.Type.Apple: sourceArray = inactiveApples; break;
            case Level4Object.Type.Sunflower: sourceArray = inactiveSunflowers; break;
            case Level4Object.Type.Rose: sourceArray = inactiveRoses; break;
        }

        if (sourceArray == null)
        {
            Debug.LogError($"[Spawn Error] No inactive array assigned for {type}");
            return;
        }

        List<GameObject> candidates = new List<GameObject>();
        foreach (var go in sourceArray)
        {
            if (go != null && !go.activeSelf && !usedInactiveObjects.Contains(go))
            {
                candidates.Add(go);
            }
        }

        if (candidates.Count > 0)
        {
            GameObject chosen = candidates[Random.Range(0, candidates.Count)];
            usedInactiveObjects.Add(chosen);
            chosen.SetActive(true);

            // 🔁 Reset state after enabling
            Level4Object obj = chosen.GetComponent<Level4Object>();
            if (obj != null)
            {
                obj.pointer = pointer;
                obj.manager = this;
                obj.ResetState(); // 💡 You can use this method inside Level4Object to reset its logic (see below)
            }

            Debug.Log($"[Spawned] Activated and reset object {chosen.name}");
        }
        else
        {
            Debug.LogWarning($"[Spawn] No unused inactive objects left for {type}");
        }
    }
    void ResetOriginalObjectsOfType(Level4Object.Type type)
    {
        GameObject[] group = null;

        switch (type)
        {
            case Level4Object.Type.Mango: group = mangoes; break;
            case Level4Object.Type.Apple: group = apples; break;
            case Level4Object.Type.Sunflower: group = sunflowers; break;
            case Level4Object.Type.Rose: group = roses; break;
        }

        if (group == null) return;

        foreach (GameObject go in group)
        {
            if (go != null)
            {
                Level4Object obj = go.GetComponent<Level4Object>();
                if (obj != null)
                {
                    obj.ResetObject(); // 💥 Reset state here
                }

                if (!go.activeSelf)
                    go.SetActive(true);
            }
        }
    }





    public void OnNextClick()
    {
        instructionPanel.SetActive(false);
        StartCoroutine(BeginStage());
    }

    IEnumerator BeginStage()
    {
        Time.timeScale = 0f;
        startPrompt.SetActive(true);
        yield return new WaitForSecondsRealtime(2f);
        startPrompt.SetActive(false);
        Time.timeScale = 1f;
        StartCoroutine(InstructionCycle());

        timer = 60f;

        fixedCycleIndex = 0;
        fixedCycleDone = false;
        UpdateInstruction();

        hand.SetActive(true);
        timerText.gameObject.SetActive(true);
        scoreText.gameObject.SetActive(true);
        instructionText.enabled = true;
    }
    IEnumerator InstructionCycle()
    {
        while (!isGameOver)
        {
            yield return new WaitForSeconds(25f); // or 10f

            // ✅ Apply reset for all types
            Level4Object.Type[] allTypes = {
            Level4Object.Type.Mango,
            Level4Object.Type.Apple,
            Level4Object.Type.Sunflower,
            Level4Object.Type.Rose
        };

            foreach (Level4Object.Type type in allTypes)
            {
                CleanupSpawnedInactiveObjectsOfType(type);
                ResetOriginalObjectsOfType(type);
                ClearUsedInactiveObjects(type);
            }
            HideAllPanelImages();

            // ✅ Show new instruction
            UpdateInstruction();
        }
    }
    void HideAllPanelImages()
    {
        mangoPanelImage.SetActive(false);
        applePanelImage.SetActive(false);
        sunflowerPanelImage.SetActive(false);
        rosePanelImage.SetActive(false);
    }
    void ClearUsedInactiveObjects(Level4Object.Type type)
    {
        GameObject[] group = null;

        switch (type)
        {
            case Level4Object.Type.Mango: group = inactiveMangoes; break;
            case Level4Object.Type.Apple: group = inactiveApples; break;
            case Level4Object.Type.Sunflower: group = inactiveSunflowers; break;
            case Level4Object.Type.Rose: group = inactiveRoses; break;
        }

        if (group == null) return;

        foreach (var go in group)
        {
            usedInactiveObjects.Remove(go);
        }

        Debug.Log($"[Reset] Cleared used-inactive list for {type}");
    }


    void CleanupSpawnedInactiveObjectsOfType(Level4Object.Type type)
    {
        GameObject[] group = null;

        switch (type)
        {
            case Level4Object.Type.Mango: group = inactiveMangoes; break;
            case Level4Object.Type.Apple: group = inactiveApples; break;
            case Level4Object.Type.Sunflower: group = inactiveSunflowers; break;
            case Level4Object.Type.Rose: group = inactiveRoses; break;
        }

        if (group == null) return;

        foreach (GameObject go in group)
        {
            if (go != null && go.activeSelf)
            {
                go.SetActive(false);
            }
        }
    }

    void ShowFloatingText(string content, Vector3 worldPosition)
    {
        GameObject prefab = (content == "+3s") ? floatingTextPlusPrefab : floatingTextMinusPrefab;
        if (prefab != null && uiCanvas != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition + worldOffset);
            screenPos += screenOffset;

            GameObject ft = Instantiate(prefab, uiCanvas.transform);
            ft.transform.position = screenPos;
        }
    }
    void EndLevel()
    {
        isGameOver = true;
        
        gameOverPanel.SetActive(true);
       gameOverPanel_score.text = $"Score: {score:F1}    Time Left: {Mathf.Max(0, Mathf.FloorToInt(timer))}";
    }
    public void nextstage()
    {
            gameOverPanel.SetActive(false);

            Destroy(level3_assest);
            level4_assest.SetActive(true);
            player_4.SetActive(false);

           
            player_5.SetActive(true);
    }

}
