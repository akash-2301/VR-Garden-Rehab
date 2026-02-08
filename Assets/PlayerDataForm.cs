using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerDataForm : MonoBehaviour
{
    public TMP_InputField playerIdInput;
    public TMP_InputField ageInput;
    public TMP_InputField genderInput;
    public TMP_InputField postStrokePeriodInput;
    public TMP_InputField strokeDeficitInput;
    public TMP_InputField strokeAffectedSideInput;
    public Button nextButton;
    public GameObject playerInfoPanel;
    public GameObject nextUIPanel;

    void Start()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
    }

    private void OnNext()
    {
        string playerId = playerIdInput != null ? playerIdInput.text.Trim() : "";
        string age = ageInput != null ? ageInput.text.Trim() : "";
        string gender = genderInput != null ? genderInput.text.Trim() : "";
        string post = postStrokePeriodInput != null ? postStrokePeriodInput.text.Trim() : "";
        string deficit = strokeDeficitInput != null ? strokeDeficitInput.text.Trim() : "";
        string side = strokeAffectedSideInput != null ? strokeAffectedSideInput.text.Trim() : "";
        if (string.IsNullOrEmpty(playerId)) playerId = System.Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
        PlayerDataLogger.Instance.SavePlayerData(playerId, age, gender, post, deficit, side);
        LocalCSVLogger.Instance.CreateSessionFileForPlayer(playerId);
        if (playerInfoPanel != null) playerInfoPanel.SetActive(false);
        if (nextUIPanel != null) nextUIPanel.SetActive(true);
    }
}
