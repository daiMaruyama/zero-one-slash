using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    [Header("ゲームバランス設定")]
    [SerializeField] float timeLimit = 60.0f;
    [SerializeField] float throwCooldown = 0.3f;
    [SerializeField] float nextQuestionDelay = 1.5f;
    [SerializeField] float winningZoomSize = 4.2f;

    [Header("演出設定")]
    [SerializeField] float scoreCountDuration = 1.5f;
    [SerializeField] Ease scoreEaseType = Ease.OutExpo;

    [Header("開始演出")]
    [SerializeField] AudioClip seGameStart;
    [SerializeField] Vector2 startTextOffset = Vector2.zero;
    [SerializeField] float startTextScale = 1.0f;

    int[] questionList = { 32, 40, 50, 60, 36, 20, 16, 81, 101 };

    [Header("UI参照")]
    public CyberText targetText;
    public Text timeText;
    public Slider timeSlider;
    public GameObject[] throwIcons;
    public CyberText scoreText;
    public GameObject resultPanel;
    public Text resultScoreText;

    [Header("エフェクト設定")]
    public GameObject effectSingle;
    public GameObject effectDouble;
    public GameObject effectTriple;
    public GameObject effectBull;
    public GameObject effectMiss;
    public GameObject popupTextPrefab;

    [Header("オーディオ設定")]
    public AudioClip seSingle;
    public AudioClip seDouble;
    public AudioClip seTriple;
    public AudioClip seOuterBull;
    public AudioClip seInnerBull;
    public AudioClip seWin;
    public AudioClip seFail;
    public AudioClip seMiss;
    public AudioClip seResult;
    public AudioClip bgmMain;

    [Range(0f, 1f)] public float baseBgmVolume = 0.5f;

    [Header("ボード参照（Bullズレ対策）")]
    [SerializeField] Transform boardTransform; // ←ダーツボードのTransformを入れる

    // 外部参照用（フォーカス用）
    public int RemainingScore => currentTargetScore;
    public int ThrowsLeft => throwsLeft;

    float currentTime;
    int currentTargetScore;
    int throwsLeft;
    int totalGameScore;
    bool isGameActive;
    bool isInputBlocked;

    public bool CanThrow => isGameActive && !isInputBlocked;

    void Start()
    {
        if (bgmMain != null)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayBGM(bgmMain);
            else
            {
                AudioSource tempSource = gameObject.AddComponent<AudioSource>();
                tempSource.clip = bgmMain;
                tempSource.loop = true;
                tempSource.volume = baseBgmVolume;
                tempSource.Play();
            }
        }

        if (targetText == null) return;

        if (resultPanel != null) resultPanel.SetActive(false);

        totalGameScore = 0;
        currentTime = timeLimit;

        isGameActive = false;
        isInputBlocked = true;

        NextQuestion();

        var starter = gameObject.AddComponent<GameStarter>();
        starter.textOffset = startTextOffset;
        starter.textSizeScale = startTextScale;

        starter.Play(
            () =>
            {
                if (seGameStart != null && AudioManager.instance != null)
                    AudioManager.instance.PlaySE(seGameStart);
            },
            OnStartSequenceComplete
        );
    }

    void OnStartSequenceComplete()
    {
        isGameActive = true;
        isInputBlocked = false;
    }

    void Update()
    {
        if (targetText == null) return;

        if (isGameActive)
        {
            currentTime -= Time.deltaTime;

            if (timeText != null) timeText.text = "TIME " + currentTime.ToString("F1");

            if (timeSlider != null)
            {
                float ratio = currentTime / timeLimit;
                timeSlider.value = ratio;

                if (timeSlider.fillRect != null)
                {
                    Image fillImage = timeSlider.fillRect.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        if (ratio < 0.2f) fillImage.color = Color.red;
                        else if (ratio < 0.5f) fillImage.color = Color.yellow;
                        else fillImage.color = Color.cyan;
                    }
                }
            }

            if (currentTime <= 0)
            {
                currentTime = 0;
                GameOver();
            }
        }
    }

    public void NextQuestion()
    {
        currentTargetScore = questionList[Random.Range(0, questionList.Length)];
        throwsLeft = 3;
        isInputBlocked = false;
        UpdateUI();
    }

    public void ProcessHit(string areaCode, int hitScore, Vector2 hitPosition)
    {
        if (!isGameActive || isInputBlocked) return;
        isInputBlocked = true;

        throwsLeft--;
        UpdateUI();

        Vector3 effectPos = new Vector3(hitPosition.x, hitPosition.y, -0.5f);

        if (areaCode == "OUT")
        {
            StartCoroutine(MissProcessRoutine(effectPos));
            return;
        }

        PlayHitEffect(areaCode, effectPos);

        if (popupTextPrefab)
        {
            GameObject popup = Instantiate(popupTextPrefab, effectPos, Quaternion.identity);
            popup.transform.position = new Vector3(hitPosition.x, hitPosition.y, -3.0f);

            var popupText = popup.GetComponent<HitPopupText>();
            if (popupText != null) popupText.Setup(areaCode, hitScore);
        }

        if (CameraShake.instance)
        {
            if (areaCode.StartsWith("T") || areaCode.Contains("Bull")) CameraShake.instance.Shake(0.2f, 0.1f);
            else CameraShake.instance.Shake(0.1f, 0.05f);
        }

        int tempScore = currentTargetScore - hitScore;

        if (tempScore < 0)
        {
            if (GameEffectsManager.instance != null)
                GameEffectsManager.instance.PlayBustEffect();

            StartCoroutine(FailProcessRoutine("BUST", 0f, seFail));
        }
        else if (tempScore == 0)
        {
            if (GameEffectsManager.instance != null)
                GameEffectsManager.instance.PlayFinishEffect();

            // ✅ Bullズレ対策：ズーム中心を「ボード中心」に固定
            if (CameraController.instance != null)
            {
                Vector3 center = (boardTransform != null) ? boardTransform.position : Vector3.zero;
                CameraController.instance.ZoomIn(center, winningZoomSize, 0.05f);
            }

            PlayHitSound(areaCode);
            WinProcess(areaCode);
        }
        else
        {
            currentTargetScore = tempScore;
            UpdateUI();

            float soundDuration = PlayHitSound(areaCode);

            if (throwsLeft <= 0)
            {
                StartCoroutine(FailProcessRoutine("TURN END", soundDuration, seFail));
            }
            else
            {
                StartCoroutine(CooldownRoutine(throwCooldown));
            }
        }
    }

    IEnumerator MissProcessRoutine(Vector3 effectPos)
    {
        if (effectMiss != null) Instantiate(effectMiss, effectPos, Quaternion.identity);

        if (seMiss != null && AudioManager.instance != null)
            AudioManager.instance.PlaySE(seMiss);

        if (targetText != null) targetText.SetText("MISS");

        yield return new WaitForSeconds(0.4f);

        if (throwsLeft <= 0)
        {
            StartCoroutine(FailProcessRoutine("TURN END", 0f, seFail));
        }
        else
        {
            UpdateUI();
            isInputBlocked = false;
        }
    }

    void PlayHitEffect(string areaCode, Vector3 pos)
    {
        GameObject prefabToSpawn = effectSingle;

        if (areaCode.StartsWith("D")) prefabToSpawn = effectDouble;
        else if (areaCode.StartsWith("T")) prefabToSpawn = effectTriple;
        else if (areaCode.Contains("Bull")) prefabToSpawn = effectBull;

        if (prefabToSpawn != null) Instantiate(prefabToSpawn, pos, Quaternion.identity);
    }

    void WinProcess(string finishingArea)
    {
        int pointsGet = 1;
        string winMessage = "WIN!!";

        if (finishingArea.StartsWith("D") || finishingArea.StartsWith("T") || finishingArea.Contains("Bull"))
        {
            pointsGet = 3;
            winMessage = "GREAT WIN!!";
        }

        totalGameScore += pointsGet;

        // ここも中心ズームなら安定
        if (CameraController.instance != null && GameEffectsManager.instance == null)
        {
            Vector3 center = (boardTransform != null) ? boardTransform.position : Vector3.zero;
            CameraController.instance.ZoomIn(center, winningZoomSize, 0.2f);
        }

        if (BloomManager.instance != null) BloomManager.instance.FlashBloom(pointsGet);

        if (seWin != null && AudioManager.instance != null)
            AudioManager.instance.PlaySE(seWin);

        if (targetText != null) targetText.SetText(winMessage);

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    IEnumerator FailProcessRoutine(string reason, float delay, AudioClip clip)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (clip != null && AudioManager.instance != null)
            AudioManager.instance.PlaySE(clip);

        if (targetText != null) targetText.SetText(reason);

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    void GameOver()
    {
        isGameActive = false;
        isInputBlocked = true;

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            AnimateResultScore();
        }
    }

    void AnimateResultScore()
    {
        if (resultScoreText == null) return;

        int displayScore = 0;
        resultScoreText.text = "SCORE\n0";

        DOTween.To(() => displayScore, x => displayScore = x, totalGameScore, scoreCountDuration)
            .SetEase(scoreEaseType)
            .OnUpdate(() =>
            {
                if (resultScoreText != null)
                    resultScoreText.text = "SCORE\n" + displayScore.ToString("N0");
            })
            .OnComplete(() =>
            {
                if (seResult != null && AudioManager.instance != null)
                    AudioManager.instance.PlaySE(seResult);

                if (resultScoreText != null)
                {
                    resultScoreText.transform.DOScale(1.2f, 0.1f)
                        .SetLoops(2, LoopType.Yoyo)
                        .SetLink(resultScoreText.gameObject);
                }

                CheckAndSaveHighScore();

                var submissionUI = resultPanel.GetComponentInChildren<ResultPanelController>();
                if (submissionUI != null) submissionUI.SetupSubmission(totalGameScore);

            }).SetLink(resultScoreText.gameObject);
    }

    void CheckAndSaveHighScore()
    {
        int currentBest = PlayerPrefs.GetInt("BEST_SCORE", 0);

        if (totalGameScore > currentBest)
        {
            PlayerPrefs.SetInt("BEST_SCORE", totalGameScore);
            PlayerPrefs.Save();

            if (resultScoreText != null)
            {
                resultScoreText.text += "\n<color=red>NEW RECORD!!</color>";
                resultScoreText.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f);
            }
        }
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator CooldownRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isGameActive) isInputBlocked = false;
    }

    IEnumerator NextQuestionDelayRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (CameraController.instance != null) CameraController.instance.ResetCamera(0.5f);
        if (isGameActive) NextQuestion();
    }

    IEnumerator PlaySoundRoutine(AudioClip clip, int count)
    {
        if (clip == null) yield break;

        for (int i = 0; i < count; i++)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlaySE(clip);
            yield return new WaitForSeconds(0.08f);
        }
    }

    float PlayHitSound(string areaCode)
    {
        AudioClip clipToPlay = seSingle;
        int repeatCount = 1;

        if (areaCode == "OUT") return 0f;

        if (areaCode.StartsWith("D"))
        {
            clipToPlay = seDouble;
            repeatCount = 2;
        }
        else if (areaCode.StartsWith("T"))
        {
            clipToPlay = seTriple;
            repeatCount = 3;
        }
        else if (areaCode == "Outer Bull")
        {
            clipToPlay = seOuterBull;
        }
        else if (areaCode == "Inner Bull")
        {
            clipToPlay = seInnerBull;
            repeatCount = 2;
        }

        StartCoroutine(PlaySoundRoutine(clipToPlay, repeatCount));
        return (repeatCount * 0.08f) + 0.1f;
    }

    void UpdateUI()
    {
        if (targetText != null) targetText.SetValue("TARGET: ", currentTargetScore);
        if (scoreText != null) scoreText.SetValue("SCORE: ", totalGameScore);

        if (throwIcons != null)
        {
            for (int i = 0; i < throwIcons.Length; i++)
            {
                throwIcons[i].SetActive(i < throwsLeft);
            }
        }
    }
}
