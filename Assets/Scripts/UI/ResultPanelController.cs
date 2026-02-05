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
    [SerializeField] GameObject[] hideOnShowTargets;

    int _score;
    bool[] _hideOnShowStates;
    bool _hideOnShowStored;

    void Awake()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(OnClickSubmit);
    }

    void OnEnable()
    {
        SetHideOnShowTargetsVisible(false);
    }

    void OnDisable()
    {
        SetHideOnShowTargetsVisible(true);
    }

    public void SetupSubmission(int score)
    {
        _score = score;

        if (submitRoot != null) submitRoot.SetActive(true);

        if (statusText != null)

    void SetHideOnShowTargetsVisible(bool visible)
    {
        if (hideOnShowTargets == null || hideOnShowTargets.Length == 0) return;

        if (!visible)
        {
            if (_hideOnShowStates == null || _hideOnShowStates.Length != hideOnShowTargets.Length)
                _hideOnShowStates = new bool[hideOnShowTargets.Length];

            for (int i = 0; i < hideOnShowTargets.Length; i++)
            {
                if (hideOnShowTargets[i] == null) continue;
                _hideOnShowStates[i] = hideOnShowTargets[i].activeSelf;
                hideOnShowTargets[i].SetActive(false);
            }

            _hideOnShowStored = true;
            return;
        }

        if (!_hideOnShowStored) return;

        for (int i = 0; i < hideOnShowTargets.Length; i++)
        {
            if (hideOnShowTargets[i] == null) continue;
            hideOnShowTargets[i].SetActive(_hideOnShowStates[i]);
        }
    }
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
