using UnityEngine;
using UnityEngine.UI;

public class ResultPanelController : MonoBehaviour
{
    [SerializeField] InputField nameInputField;
    [SerializeField] Button submitButton;
    [SerializeField] Text statusText;

    int scoreToSubmit;

    void Start()
    {
        if (submitButton) submitButton.onClick.AddListener(OnSubmit);
        if (nameInputField) nameInputField.text = PlayerPrefs.GetString("PLAYER_DISPLAY_NAME", "PLAYER");

        // ç≈èâÇÕì¸óÕÇéÛÇØïtÇØÇ»Ç¢
        SetUIState(false);
    }

    public void SetupSubmission(int score)
    {
        scoreToSubmit = score;
        SetUIState(true);
        if (statusText) statusText.text = "ENTER YOUR NAME";
    }

    void SetUIState(bool active)
    {
        if (nameInputField) nameInputField.interactable = active;
        if (submitButton) submitButton.interactable = active;
    }

    async void OnSubmit()
    {
        SetUIState(false);
        if (statusText) statusText.text = "SENDING...";

        string playerName = nameInputField.text;
        PlayerPrefs.SetString("PLAYER_DISPLAY_NAME", playerName);

        await RankingManager.instance.SubmitScoreWithUpdateName(scoreToSubmit, playerName);

        if (statusText) statusText.text = "SCORE SUBMITTED";
    }
}