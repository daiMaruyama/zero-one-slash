using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    // 設定項目
    [Header("ゲームバランス設定")]
    [Tooltip("制限時間（秒）")]
    [SerializeField] float timeLimit = 60.0f;
    [Tooltip("投げてから次が投げられるまでの待機時間")]
    [SerializeField] float throwCooldown = 0.3f;
    [Tooltip("正解/失敗してから次の問題が出るまでの時間")]
    [SerializeField] float nextQuestionDelay = 1.5f;
    [Tooltip("勝利時にカメラがズームする強さ")]
    [SerializeField] float winningZoomSize = 4.2f;

    [Header("演出設定")]
    [Tooltip("リザルト画面でスコアがカウントアップする時間")]
    [SerializeField] float scoreCountDuration = 1.5f;
    [SerializeField] Ease scoreEaseType = Ease.OutExpo;

    // 開始演出
    [Header("開始演出")]
    [SerializeField] AudioClip seGameStart;
    [Tooltip("Ready/Goテキストの表示位置調整 (x, y)")]
    [SerializeField] Vector2 startTextOffset = Vector2.zero;
    [Tooltip("Ready/Goテキストのサイズ倍率")]
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

    // 内部変数
    float currentTime;
    int currentTargetScore;
    int throwsLeft;
    int totalGameScore;
    bool isGameActive;
    bool isInputBlocked;

    public bool CanThrow
    {
        get { return isGameActive && !isInputBlocked; }
    }

    void Start()
    {
        // 修正: AudioManagerへBGM再生を依頼
        // ゲームシーン単体でのテスト時など、AudioManagerがいない場合のみ一時的に再生機能を持たせる
        if (bgmMain != null)
        {
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayBGM(bgmMain);
            }
            else
            {
                // AudioManager不在時のフォールバック処理
                AudioSource tempSource = gameObject.AddComponent<AudioSource>();
                tempSource.clip = bgmMain;
                tempSource.loop = true;
                tempSource.volume = baseBgmVolume;
                tempSource.Play();
            }
        }

        // ここでUIチェックを行う
        // UI設定が漏れていても、BGM再生後にここで止まるので音の確認は可能
        if (targetText == null) return;

        // 初期化処理
        if (resultPanel != null) resultPanel.SetActive(false);

        totalGameScore = 0;
        currentTime = timeLimit;

        isGameActive = false;
        isInputBlocked = true;

        NextQuestion();

        // 演出担当スクリプトを追加
        var starter = gameObject.AddComponent<GameStarter>();

        starter.textOffset = startTextOffset;
        starter.textSizeScale = startTextScale;

        starter.Play(
            () => {
                // スタート音もAudioManager経由で再生
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
        // UIがない場合は動かさない
        if (targetText == null) return;

        // 音量同期処理はAudioManagerに任せているため削除

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
        }

        if (CameraShake.instance)
        {
            if (areaCode.StartsWith("T") || areaCode.Contains("Bull")) CameraShake.instance.Shake(0.2f, 0.1f);
            else CameraShake.instance.Shake(0.1f, 0.05f);
        }

        int tempScore = currentTargetScore - hitScore;

        if (tempScore < 0)
        {
            // AudioManagerでSE再生
            // バーストエフェクト再生
            if (GameEffectsManager.instance != null)
            {
                GameEffectsManager.instance.PlayBustEffect();
            }
            StartCoroutine(FailProcessRoutine("BUST", 0f, seFail));
        }
        else if (tempScore == 0)
        {
            if (GameEffectsManager.instance != null)
            {
                GameEffectsManager.instance.PlayFinishEffect();
            }

            if (CameraController.instance != null)
            {
                CameraController.instance.ZoomIn(new Vector3(hitPosition.x, hitPosition.y, 0), winningZoomSize, 0.05f);
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

        // AudioManagerでSE再生
        if (seMiss != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seMiss);

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

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, pos, Quaternion.identity);
        }
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

        if (CameraController.instance != null && GameEffectsManager.instance == null)
        {
            CameraController.instance.ZoomIn(Vector3.zero, winningZoomSize, 0.2f);
        }

        if (BloomManager.instance != null) BloomManager.instance.FlashBloom(pointsGet);

        // AudioManagerでSE再生
        if (seWin != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seWin);

        if (targetText != null) targetText.SetText(winMessage);

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    IEnumerator FailProcessRoutine(string reason, float delay, AudioClip clip)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        // AudioManagerでSE再生
        if (clip != null && AudioManager.instance != null) AudioManager.instance.PlaySE(clip);

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
            .OnUpdate(() => { if (resultScoreText != null) resultScoreText.text = "SCORE\n" + displayScore.ToString("N0"); })
            .OnComplete(() =>
            {
                if (seResult != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seResult);

                if (resultScoreText != null)
                {
                    resultScoreText.transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo).SetLink(resultScoreText.gameObject);
                }

                CheckAndSaveHighScore();

                // スコア演出終了後に名前入力と送信を許可する
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
            // AudioManager経由でSEを再生
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
                if (i < throwsLeft) throwIcons[i].SetActive(true);
                else throwIcons[i].SetActive(false);
            }
        }
    }
}