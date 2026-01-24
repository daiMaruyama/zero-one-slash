using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class ResultPanelController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject submitRoot;
    [SerializeField] InputField nameInput;
    [SerializeField] Button submitButton;
    [SerializeField] Text statusText;

    int _score;

    void Awake()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(OnClickSubmit);
    }

    public void SetupSubmission(int score)
    {
        _score = score;

        if (submitRoot != null) submitRoot.SetActive(true);

        if (statusText != null)
            statusText.text = "名前を入力して送信";
    }

    async void OnClickSubmit()
    {
        if (submitButton != null) submitButton.interactable = false;

        string playerName = nameInput != null ? nameInput.text : "Unknown";

        if (statusText != null)
            statusText.text = "送信中...";

        await SubmitAsync(playerName);

        if (statusText != null)
            statusText.text = "送信完了！";

        // 二度押し防止nara
        // if (submitRoot != null) submitRoot.SetActive(false);
    }

    async Task SubmitAsync(string playerName)
    {
        if (RankingManager.instance == null) return;

        await RankingManager.instance.SubmitScoreWithUpdateName(_score, playerName);
    }
}
