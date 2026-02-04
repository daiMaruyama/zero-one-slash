using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    // 問題リスト（動的生成）
    int[] questionList;
    const int MaxThreeDartTotal = 180;

    // 3投で物理的に作れない点数 && 1
    static readonly HashSet<int> UnreachableThreeDartTotals = new HashSet<int>
    {
        1, 163, 166, 169, 172, 173, 175, 176, 178, 179
    };

    [Header("UI参照")]
    public CyberText targetText;
    public Text timeText;
    public Slider timeSlider;
    public GameObject[] throwIcons;
    public CyberText scoreText;
    public GameObject resultPanel;

    // リザルト用テキスト（3つに分離）
    [SerializeField] Text resultScoreText;
    [SerializeField] Text resultStatusText;
    [SerializeField] Text resultRankText;

    [Header("ランキング入力")]
    [SerializeField] NewRecordPanelController newRecordPanel;

    [Header("デバッグ")]
    [SerializeField] bool debugForceShowNameInput = false;
    [SerializeField] KeyCode debugOpenKey = KeyCode.F2;

    [Header("ランキング判定設定")]
    [SerializeField] float rankingCheckTimeoutSeconds = 1.2f;

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

    [Header("BGMテンポ演出")]
    [SerializeField] bool useBgmPitch = true;
    [SerializeField] float pitchStartTimeRatio = 0.3f; // 残り%から上げ始める
    [SerializeField] float pitchMax = 1.15f;
    [SerializeField] float pitchFollowSpeed = 2.5f;     // 追従スピード
    [SerializeField] float pitchReturnDuration = 0.8f;
    [SerializeField] Ease pitchReturnEase = Ease.OutCubic;

    Tween _pitchReturnTween;


    AudioSource _bgmSource;
    float _bgmBasePitch = 1.0f;
    float _currentPitch = 1.0f;

    [Header("ボード参照")]
    [SerializeField] Transform boardTransform;

    public int RemainingScore => currentTargetScore;
    public int ThrowsLeft => throwsLeft;

    float currentTime;
    int currentTargetScore;
    int throwsLeft;
    int totalGameScore;
    bool isGameActive;
    bool isInputBlocked;

    bool _isNewRecordThisRun = false;
    bool _isGameOver = false;

    public bool CanThrow => isGameActive && !isInputBlocked;

    /// <summary>
    /// ゲーム開始時の初期化を行い、開始演出を再生する。
    /// </summary>
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

        // BGMのAudioSourceを掴む（AudioManagerがある前提）
        if (AudioManager.instance != null)
        {
            _bgmSource = AudioManager.instance.BgmSource;
            if (_bgmSource != null)
            {
                _bgmBasePitch = _bgmSource.pitch;
                _currentPitch = _bgmBasePitch;
                AudioManager.instance.SetBgmPitch(_bgmBasePitch);
            }
        }

        if (targetText == null) return;
        if (resultPanel != null) resultPanel.SetActive(false);

        totalGameScore = 0;
        currentTime = timeLimit;
        isGameActive = false;
        isInputBlocked = true;

        // リスト生成して開始
        questionList = BuildQuestionList();
        NextQuestion();

        var starter = gameObject.AddComponent<GameStarter>();
        starter.textOffset = startTextOffset;
        starter.textSizeScale = startTextScale;

        starter.Play(
            () => { if (seGameStart != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seGameStart); },
            OnStartSequenceComplete
        );
    }

    void OnStartSequenceComplete()
    {
        isGameActive = true;
        isInputBlocked = false;
    }

    /// <summary>
    /// ゲーム中のタイマー更新、UI更新、終了判定を行う。
    /// </summary>
    void Update()
    {
        if (debugForceShowNameInput && Input.GetKeyDown(debugOpenKey)) OpenNameInputPanel_Debug(totalGameScore);

        if (targetText == null || !isGameActive) return;

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
            UpdateBgmPitch();
        }

        if (currentTime <= 0)
        {
            currentTime = 0;
            GameOver();
        }
    }

    /// <summary>
    /// 次のターゲットスコアをランダムに設定し、投げ数をリセットする。
    /// </summary>
    public void NextQuestion()
    {
        currentTargetScore = questionList[Random.Range(0, questionList.Length)];
        throwsLeft = 3;
        isInputBlocked = false;
        UpdateUI();
    }

    /// <summary>
    /// ダーツのヒット結果を処理し、続行・クリア・バースト・ミスを判定する。
    /// </summary>
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

        // スコア表示用ポップアップ
        if (popupTextPrefab)
        {
            GameObject popup = Instantiate(popupTextPrefab, new Vector3(hitPosition.x, hitPosition.y, -3.0f), Quaternion.identity);
            var popupText = popup.GetComponent<HitPopupText>();
            if (popupText != null) popupText.Setup(areaCode, hitScore);
        }

        if (CameraShake.instance)
        {
            if (areaCode.StartsWith("T") || areaCode.Contains("Bull")) CameraShake.instance.Shake(0.2f, 0.1f);
            else CameraShake.instance.Shake(0.1f, 0.05f);
        }

        int tempScore = currentTargetScore - hitScore;

        if (tempScore < 0) // バースト
        {
            if (GameEffectsManager.instance != null) GameEffectsManager.instance.PlayBustEffect();
            StartCoroutine(FailProcessRoutine("BUST", 0f, seFail));
        }
        else if (tempScore == 0) // クリア
        {
            if (GameEffectsManager.instance != null) GameEffectsManager.instance.PlayFinishEffect();
            if (CameraController.instance != null)
            {
                Vector3 center = (boardTransform != null) ? boardTransform.position : Vector3.zero;
                CameraController.instance.ZoomIn(center, winningZoomSize, 0.05f);
            }
            PlayHitSound(areaCode);
            WinProcess(areaCode);
        }
        else // 続行
        {
            currentTargetScore = tempScore;
            UpdateUI();
            float soundDuration = PlayHitSound(areaCode);

            if (throwsLeft <= 0) StartCoroutine(FailProcessRoutine("TURN END", soundDuration, seFail));
            else StartCoroutine(CooldownRoutine(throwCooldown));
        }
    }

    /// <summary>
    /// ミス時の演出とUI更新を行う。
    /// </summary>
    IEnumerator MissProcessRoutine(Vector3 effectPos)
    {
        if (effectMiss != null) Instantiate(effectMiss, effectPos, Quaternion.identity);
        if (seMiss != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seMiss);
        if (targetText != null) targetText.SetText("MISS");

        yield return new WaitForSeconds(0.4f);

        if (throwsLeft <= 0) StartCoroutine(FailProcessRoutine("TURN END", 0f, seFail));
        else { UpdateUI(); isInputBlocked = false; }
    }

    /// <summary>
    /// ヒットエリアに応じたエフェクトを再生する。
    /// </summary>
    void PlayHitEffect(string areaCode, Vector3 pos)
    {
        GameObject prefab = effectSingle;
        if (areaCode.StartsWith("D")) prefab = effectDouble;
        else if (areaCode.StartsWith("T")) prefab = effectTriple;
        else if (areaCode.Contains("Bull")) prefab = effectBull;
        if (prefab != null) Instantiate(prefab, pos, Quaternion.identity);
    }

    /// <summary>
    /// クリア時のスコア加算と演出を行う。
    /// </summary>
    void WinProcess(string finishingArea)
    {
        int pointsGet = (finishingArea.StartsWith("D") || finishingArea.StartsWith("T") || finishingArea.Contains("Bull")) ? 500 : 100;
        string winMessage = pointsGet == 500 ? "GREAT WIN!!" : "WIN!!";

        totalGameScore += pointsGet;

        if (BloomManager.instance != null) BloomManager.instance.FlashBloom(pointsGet);
        if (seWin != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seWin);
        if (targetText != null) targetText.SetText(winMessage);

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    /// <summary>
    /// 失敗時の演出を遅延付きで再生し、次の問題へ遷移する。
    /// </summary>
    IEnumerator FailProcessRoutine(string reason, float delay, AudioClip clip)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (clip != null && AudioManager.instance != null) AudioManager.instance.PlaySE(clip);
        if (targetText != null) targetText.SetText(reason);
        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    /// <summary>
    /// ゲーム終了時の結果表示とランキング入力判定を行う。
    /// </summary>
    async void GameOver()
    {
        //if (AudioManager.instance != null) AudioManager.instance.ResetBgmPitch();
        //_currentPitch = _bgmBasePitch;
        StartBgmPitchReturnSmooth();

        if (_isGameOver) return;
        _isGameOver = true;
        isGameActive = false;
        isInputBlocked = true;

        if (resultPanel != null) resultPanel.SetActive(false);

        bool shouldOpenNameInput = debugForceShowNameInput;
        if (!shouldOpenNameInput && RankingManager.instance != null)
        {
            shouldOpenNameInput = await SafeShouldOpenNameInputAsync(totalGameScore, 10, rankingCheckTimeoutSeconds);
        }

        _isNewRecordThisRun = shouldOpenNameInput;

        if (shouldOpenNameInput && newRecordPanel != null)
        {
            newRecordPanel.Open(totalGameScore, () => ShowResultPanel());
            return;
        }
        ShowResultPanel();
    }

    /// <summary>
    /// ランキング入力を開くべきかをタイムアウト付きで判定する。
    /// </summary>
    async Task<bool> SafeShouldOpenNameInputAsync(int score, int topN, float timeoutSeconds)
    {
        if (RankingManager.instance == null || Application.internetReachability == NetworkReachability.NotReachable) return false;
        try
        {
            Task<bool> task = RankingManager.instance.ShouldOpenNameInputAsync(score, topN);
            Task finished = await Task.WhenAny(task, Task.Delay((int)(Mathf.Max(0.05f, timeoutSeconds) * 1000f)));
            return (finished == task) ? await task : false;
        }
        catch { return false; }
    }

    /// <summary>
    /// リザルト画面のスコアカウントアップ演出を行う。
    /// </summary>
    void AnimateResultScore()
    {
        if (resultScoreText == null) return;

        int displayScore = 0;
        resultScoreText.text = "0";
        if (resultStatusText != null) resultStatusText.text = "";
        if (resultRankText != null) resultRankText.text = "";

        // スコアカウントアップ演出
        DOTween.To(() => displayScore, x => displayScore = x, totalGameScore, scoreCountDuration)
            .SetEase(scoreEaseType)
            .OnUpdate(() => { if (resultScoreText != null) resultScoreText.text = displayScore.ToString("N0"); })
            .OnComplete(() =>
            {
                if (seResult != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seResult);
                resultScoreText.transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo);

                if (RankingManager.instance != null)
                {
                    int rank = RankingManager.instance.LastSubmittedRank;
                    double best = RankingManager.instance.LastSubmittedScore;

                    // ステータステキスト（新記録等）
                    if (resultStatusText != null)
                    {
                        if (totalGameScore >= (int)best && totalGameScore > 0) { resultStatusText.text = "NEW RECORD!!"; resultStatusText.color = Color.red; }
                        else if (_isNewRecordThisRun) { resultStatusText.text = "RANK IN!!"; resultStatusText.color = new Color(1f, 0.5f, 0f); }

                        if (resultStatusText.text != "")
                        {
                            resultStatusText.transform.localScale = Vector3.zero;
                            resultStatusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
                        }
                    }

                    // 順位テキスト
                    if (resultRankText != null && rank >= 0 && rank < 10)
                    {
                        resultRankText.text = rank == 0 ? "BEST OF BEST!!" : $"RANKING: #{rank + 1}";
                        resultRankText.color = rank == 0 ? Color.yellow : Color.cyan;
                        resultRankText.transform.localScale = Vector3.zero;
                        resultRankText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.3f);
                    }
                }

                if (resultPanel != null)
                {
                    var submissionUI = resultPanel.GetComponentInChildren<ResultPanelController>();
                    if (submissionUI != null) submissionUI.SetupSubmission(totalGameScore);
                }
            }).SetLink(resultScoreText.gameObject);
    }

    public void RetryGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    /// <summary>
    /// 投げ間隔のクールダウン後に入力を再開する。
    /// </summary>
    IEnumerator CooldownRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isGameActive) isInputBlocked = false;
    }

    /// <summary>
    /// 次の問題へ進むまでの待機時間を挟んでカメラをリセットする。
    /// </summary>
    IEnumerator NextQuestionDelayRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (CameraController.instance != null) CameraController.instance.ResetCamera(0.5f);
        if (isGameActive) NextQuestion();
    }

    /// <summary>
    /// ヒットエリアに応じたSEを複数回再生し、再生時間を返す。
    /// </summary>
    float PlayHitSound(string areaCode)
    {
        AudioClip clip = seSingle;
        int count = 1;
        if (areaCode.StartsWith("D")) { clip = seDouble; count = 2; }
        else if (areaCode.StartsWith("T")) { clip = seTriple; count = 3; }
        else if (areaCode == "Outer Bull") clip = seOuterBull;
        else if (areaCode == "Inner Bull") { clip = seInnerBull; count = 2; }
        StartCoroutine(PlaySoundRoutine(clip, count));
        return (count * 0.08f) + 0.1f;
    }

    /// <summary>
    /// 効果音を指定回数だけ時間差で再生する。
    /// </summary>
    IEnumerator PlaySoundRoutine(AudioClip clip, int count)
    {
        if (clip == null) yield break;
        for (int i = 0; i < count; i++)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlaySE(clip);
            yield return new WaitForSeconds(0.08f);
        }
    }

    /// <summary>
    /// ターゲット/スコア/残り投数のUIを更新する。
    /// </summary>
    void UpdateUI()
    {
        if (targetText != null) targetText.SetValue("TARGET: ", currentTargetScore);
        if (scoreText != null) scoreText.SetValue("SCORE: ", totalGameScore);
        if (throwIcons != null) for (int i = 0; i < throwIcons.Length; i++) throwIcons[i].SetActive(i < throwsLeft);
    }

    void ShowResultPanel() { if (resultPanel != null) { resultPanel.SetActive(true); AnimateResultScore(); } }

    /// <summary>
    /// デバッグ用に名前入力パネルを強制表示する。
    /// </summary>
    void OpenNameInputPanel_Debug(int score)
    {
        if (newRecordPanel == null) return;
        if (resultPanel != null) resultPanel.SetActive(false);
        _isNewRecordThisRun = true;
        newRecordPanel.Open(score, () => ShowResultPanel());
    }

    /// <summary>
    /// 3投で到達可能なスコアのみを含む問題リストを作成する。
    /// </summary>
    int[] BuildQuestionList()
    {
        var list = new List<int>();
        for (int i = 1; i <= MaxThreeDartTotal; i++)
        {
            if (UnreachableThreeDartTotals.Contains(i)) continue;
            list.Add(i);
        }
        return list.Count == 0 ? new int[] { 32 } : list.ToArray();
    }

    /// <summary>
    /// 残り時間に応じてBGMのピッチを徐々に上げる。
    /// </summary>
    void UpdateBgmPitch()
    {
        if (!useBgmPitch) return;
        if (_bgmSource == null) return;

        float targetPitch = _bgmBasePitch;

        if (isGameActive)
        {
            float ratio = Mathf.Clamp01(currentTime / timeLimit); // 1 -> 0
            if (ratio <= pitchStartTimeRatio)
            {
                float t = 1f - (ratio / pitchStartTimeRatio);     // 0 -> 1
                targetPitch = Mathf.Lerp(_bgmBasePitch, pitchMax, t);
            }
        }

        // 自然に追従
        float dt = Time.unscaledDeltaTime;
        _currentPitch = Mathf.MoveTowards(_currentPitch, targetPitch, pitchFollowSpeed * dt);

        AudioManager.instance.SetBgmPitch(_currentPitch);
    }

    /// <summary>
    /// ゲーム終了時にBGMピッチをベースへ滑らかに戻す。
    /// </summary>
    void StartBgmPitchReturnSmooth()
    {
        if (!useBgmPitch) return;
        if (_bgmSource == null) return;

        // 既に戻し中なら一旦止める
        if (_pitchReturnTween != null && _pitchReturnTween.IsActive())
            _pitchReturnTween.Kill();

        // 現在値からベースへ滑らかに戻す
        _currentPitch = _bgmSource.pitch;

        _pitchReturnTween = DOTween.To(
                () => _currentPitch,
                x =>
                {
                    _currentPitch = x;
                    if (AudioManager.instance != null)
                        AudioManager.instance.SetBgmPitch(_currentPitch);
                },
                _bgmBasePitch,
                pitchReturnDuration
            )
            .SetEase(pitchReturnEase)
            .SetUpdate(true); // timeScale=0 でも動く（保険）
    }
}
