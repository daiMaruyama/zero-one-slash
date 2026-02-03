using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class NewRecordPanelController : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] SettingsPanelAnimator animator;
    [SerializeField] CanvasGroup canvasGroup;          // 任意（無くても動く）
    [SerializeField] RectTransform panelRoot;          // 任意（無くても動く）
    [SerializeField] Text titleText;                   // "NEW RECORD!!"
    [SerializeField] Text scoreText;                   // "SCORE: 123"
    [SerializeField] Text statusText;                  // "ENTER YOUR NAME" / "UPLOADING..." 等（追加推奨）
    [SerializeField] InputField nameInput;             // 名前入力
    [SerializeField] Button submitButton;              // OK
    [SerializeField] Button skipButton;                // SKIP（任意）
    [SerializeField] Text submitButtonText;            // OK文字（任意）

    [Header("演出")]
    [SerializeField] bool useUnscaledTime = true;
    [SerializeField] float uploadTimeoutSec = 8.0f;    // 通信が帰ってこない時の保険
    [SerializeField] int nameMaxLength = 12;

    [Header("SE（任意）")]
    [SerializeField] AudioClip seOpen;
    [SerializeField] AudioClip seSubmit;
    [SerializeField] AudioClip seSuccess;
    [SerializeField] AudioClip seError;
    [SerializeField] AudioClip seClose;

    int _score;
    Action _onFinished;
    bool _isBusy;
    bool _isOpen;

    Tween _pulseTween;

    void Awake()
    {
        if (animator == null) animator = GetComponent<SettingsPanelAnimator>();
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>(true);
        if (panelRoot == null) panelRoot = GetComponent<RectTransform>();

        // 入力の変化に合わせてOKボタンのON/OFF
        if (nameInput != null)
            nameInput.onValueChanged.AddListener(OnNameChanged);

        gameObject.SetActive(false);

        // Test時のみ有効にする
        //PlayerPrefs.DeleteKey("AUTO_USER_NAME");
        //PlayerPrefs.DeleteKey("AUTO_USER_PASS");
        //PlayerPrefs.Save();
    }

    void OnNameChanged(string value)
    {
        bool hasName = !string.IsNullOrWhiteSpace(value);

        if (submitButton != null)
            submitButton.interactable = hasName;
    }


    void Update()
    {
        // PCデバッグ用：Enterで送信（入力中のみ）
        if (!_isOpen) return;
        if (_isBusy) return;

        if (nameInput != null && nameInput.isFocused)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSubmit();
            }
        }
    }

    public void Open(int score, Action onFinished)
    {
        _score = score;
        _onFinished = onFinished;
        _isBusy = false;
        _isOpen = true;

        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // 最前面

        // CanvasGroup保険（見えない事故防止）
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        // UI文言
        if (titleText != null) titleText.text = "NEW RECORD!!";
        if (scoreText != null) scoreText.text = $"SCORE: {_score}";
        SetStatus("ENTER YOUR NAME");

        // 入力初期化
        if (nameInput != null)
        {
            nameInput.characterLimit = nameMaxLength;
            nameInput.text = "";
            nameInput.interactable = true;
            nameInput.ActivateInputField();
            OnNameChanged(nameInput.text); // 最初はOK押せない
        }

        // ボタン初期化
        SetInteractable(true);

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmit);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkip);
        }

        // オープン演出
        if (animator != null) animator.Open();

        PlaySE(seOpen);
        StartTitlePulse();
    }

    void OnSubmit()
    {
        if (_isBusy) return;
        _isBusy = true;

        string rawName = (nameInput != null) ? nameInput.text : "";
        string playerName = SanitizePlayerName(rawName);

        // 空っぽは弾く（演出つき）
        if (string.IsNullOrEmpty(playerName) || playerName == "Unknown")
        {
            _isBusy = false;
            ShakeInputError();
            SetStatus("NAME IS EMPTY!");
            PlaySE(seError);
            return;
        }

        // 送信状態へ
        SetInteractable(false);
        SetStatus("UPLOADING...");
        if (submitButtonText != null) submitButtonText.text = "UPLOADING...";

        PlaySE(seSubmit);

        // 送信開始
        StartCoroutine(SubmitRoutine(playerName));
    }

    IEnumerator SubmitRoutine(string playerName)
    {
        // RankingManager無し = オフライン扱い
        if (RankingManager.instance == null)
        {
            SetStatus("OFFLINE (NO RANKING)");
            PlaySE(seError);
            yield return Wait(0.7f);
            CloseInternal();
            yield break;
        }

        var task = RankingManager.instance.SubmitScoreWithUpdateName(_score, playerName);

        float t = uploadTimeoutSec;

        // タイムアウト付き待機（フリーズ根絶）
        while (task != null && !task.IsCompleted && t > 0f)
        {
            t -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        // タイムアウト
        if (task != null && !task.IsCompleted)
        {
            SetStatus("TIMEOUT... SKIP");
            PlaySE(seError);
            yield return Wait(0.7f);
            CloseInternal();
            yield break;
        }

        // 失敗
        if (task != null && task.IsFaulted)
        {
            Debug.LogError($"[NewRecordPanel] Upload failed: {task.Exception}");
            SetStatus("UPLOAD FAILED");
            PlaySE(seError);
            yield return Wait(0.7f);
            CloseInternal();
            yield break;
        }

        // 成功演出
        SetStatus("SAVED!");
        PlaySE(seSuccess);
        PunchTitle();

        yield return Wait(0.55f);

        CloseInternal();
    }

    void OnSkip()
    {
        if (_isBusy) return;
        _isBusy = true;

        SetStatus("SKIPPED");
        PlaySE(seClose);

        StartCoroutine(CloseDelayRoutine());
    }

    IEnumerator CloseDelayRoutine()
    {
        yield return Wait(0.15f);
        CloseInternal();
    }

    void CloseInternal()
    {
        _isOpen = false;
        StopTitlePulse();

        if (animator != null)
        {
            animator.Close(); // OnCompleteで非アクティブ化
        }
        else
        {
            gameObject.SetActive(false);
        }

        _onFinished?.Invoke();
        _onFinished = null;
    }

    public void HideInstant()
    {
        _isOpen = false;
        _isBusy = false;
        StopTitlePulse();

        if (animator != null) animator.HideInstant();
        else gameObject.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // =========================
    // 小物
    // =========================

    string SanitizePlayerName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Unknown";

        // 空白除去
        string s = raw.Replace(" ", "")
                      .Replace("　", "")
                      .Replace("\n", "")
                      .Replace("\r", "")
                      .Replace("\t", "");

        if (string.IsNullOrEmpty(s)) return "Unknown";

        // 長さ制限
        if (s.Length > nameMaxLength) s = s.Substring(0, nameMaxLength);

        return s;
    }

    void SetInteractable(bool on)
    {
        if (submitButton != null) submitButton.interactable = on;
        if (skipButton != null) skipButton.interactable = on;
        if (nameInput != null) nameInput.interactable = on;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = on;
            canvasGroup.blocksRaycasts = on;
        }

        if (submitButtonText != null && on) submitButtonText.text = "OK";
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    void ShakeInputError()
    {
        if (nameInput == null) return;
        var rt = nameInput.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.DOKill();
        rt.DOShakeAnchorPos(0.25f, 10f, 20, 90f, false, true)
          .SetUpdate(useUnscaledTime);
    }

    void StartTitlePulse()
    {
        if (titleText == null) return;

        titleText.transform.DOKill();
        _pulseTween = titleText.transform.DOScale(1.05f, 0.6f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(useUnscaledTime);
    }

    void StopTitlePulse()
    {
        if (_pulseTween != null && _pulseTween.IsActive()) _pulseTween.Kill();
        _pulseTween = null;

        if (titleText != null)
        {
            titleText.transform.DOKill();
            titleText.transform.localScale = Vector3.one;
        }
    }

    void PunchTitle()
    {
        if (titleText == null) return;

        titleText.transform.DOKill();
        titleText.transform.localScale = Vector3.one;
        titleText.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 8, 0.8f)
            .SetUpdate(useUnscaledTime);
    }

    IEnumerator Wait(float sec)
    {
        if (!useUnscaledTime)
        {
            yield return new WaitForSeconds(sec);
            yield break;
        }

        float t = sec;
        while (t > 0f)
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }
    }

    void PlaySE(AudioClip clip)
    {
        if (clip == null) return;
        if (AudioManager.instance != null) AudioManager.instance.PlaySE(clip);
    }
}
