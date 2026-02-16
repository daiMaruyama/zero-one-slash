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
    public AudioClip seFail;      // いままで通り：BUST用（そのまま）
    public AudioClip seMiss;      // いままで通り：OUT(MISS)用（そのまま）
    public AudioClip seResult;
    public AudioClip bgmMain;

    // 3投使い切って足りない（NO OUT）専用
    // 既存のフィールド名を一切変えず、追加だけするのでInspectorは壊れません
    public AudioClip seNoOut;

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

    [Header("名前入力中は隠す（モーダル化）")]
    [SerializeField] GameObject[] hideWhileNameInput;

    // 触れなくしたい物がある場合だけ（例：ボードCollider / 投げ処理Script 等）
    [SerializeField] Behaviour[] disableWhileNameInput;

    bool _isNameInputOpen;
    bool _isResultOpen;

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

    InGameSettingsOverlay _inGameSettingsOverlay;

    public bool CanThrow => isGameActive && !isInputBlocked;

    // 表示文言はここで統一（バーっぽく短く）
    const string TextMiss = "MISS";
    const string TextBust = "BUST";
    const string TextNoOut = "NO OUT"; // ← TURN END の代替（自然）

    // Streak（ちょい回復 + GREAT 3回で+1秒ドン）

    [Header("Streak")]
    [SerializeField] bool useStreak = true;

    [SerializeField] float timeHealWin = 0.2f;          // WINでちょい回復
    [SerializeField] float timeHealGreat = 0.35f;       // GREATでちょい回復
    [SerializeField] int greatBankGoal = 3;             // GREAT何回でドン回復
    [SerializeField] float bankBonusBase = 1.0f;        // ドン回復（基本）
    [SerializeField] float bankBonusStreak10 = 1.5f;    // streak>=10のドン回復
    [SerializeField] float bankBonusStreak20 = 2.0f;    // streak>=20のドン回復

    [Header("Streak UI")]
    [SerializeField] Text streakText;                 // 常駐STREAK表示（任意）
    [SerializeField] GameObject timePopupPrefab;      // TimePopupPrefab
    [SerializeField] RectTransform timePopupAnchor;   // TIME付近のアンカー
    [SerializeField] bool hideStreakWhenZero = true;

    int _streak = 0;
    int _greatBank = 0;

    void ResetStreak()
    {
        _greatBank = 0;
        SetStreak(0); // UIも更新する
    }

    bool IsGreatArea(string areaCode)
    {
        if (string.IsNullOrEmpty(areaCode)) return false;
        return areaCode.StartsWith("D") || areaCode.StartsWith("T") || areaCode.Contains("Bull");
    }

    void ApplyStreakTimeHeal(string areaCode)
    {
        if (!useStreak || !isGameActive) return;

        bool isGreat = IsGreatArea(areaCode);
        SetStreak(_streak + (isGreat ? 2 : 1));

        // --- 調整1: 基礎回復の階段を緩くする ---
        // 3コンボ（1問正解程度）から回復開始。10コンボで100%回復。
        float baseHealMultiplier = _streak < 3 ? 0f : (_streak < 10 ? 0.5f : 1f);
        AddTimeWithPopup((isGreat ? timeHealGreat : timeHealWin) * baseHealMultiplier);

        if (!isGreat) return;

        // 2. GREAT BANK（3回ヒットでドン回復）
        _greatBank++;
        if (_greatBank < Mathf.Max(1, greatBankGoal)) return;

        _greatBank = 0;

        // --- 調整2: ボーナス発生の閾値を下げ、最大値に絶対的なブレーキをかける ---
        float bonus = bankBonusBase;

        if (_streak >= 12) // 20 → 12 に引き下げ（ここが実質の無双ゾーン）
        {
            // 12コンボ以降も少しずつ伸びるが、2.5秒で絶対に止める
            float extra = (_streak - 12) * 0.05f;
            // 【重要】3.0s → 2.5s に下げることで、演出時間による消費が上回り、いつか必ず終わるようになります
            bonus = Mathf.Min(bankBonusStreak20 + extra, 2.5f);
        }
        else if (_streak >= 6) // 10 → 6 に引き下げ（3問ノーミスくらいで到達）
        {
            bonus = bankBonusStreak10;
        }
        else
        {
            // 序盤でも、GREATを3回出せば 0.5秒 くらいはご褒美をあげて退屈を防ぐ
            bonus = 0.5f;
        }

        AddTimeWithPopup(bonus);

        // 演出のトリガーも12コンボからにして、早めに気分を盛り上げる
        if (_streak >= 12 && BloomManager.instance != null)
        {
            BloomManager.instance.FlashBloom(1000);
        }
    }

    /// <summary>
    /// ゲーム開始時の初期化を行い、開始演出を再生する。
    /// </summary>
    void Start()
    {
        SetupInGameSettingsOverlay();

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

        if (_inGameSettingsOverlay != null)
        {
            _inGameSettingsOverlay.ForceClosePanel();
            _inGameSettingsOverlay.SetGameplayActive(false);
        }

        ResetStreak();

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

    void SetupInGameSettingsOverlay()
    {
        Transform uiRoot = null;

        if (timeText != null)
            uiRoot = timeText.canvas != null ? timeText.canvas.transform : null;

        if (uiRoot == null && targetText != null)
            uiRoot = targetText.transform.root;

        if (uiRoot == null) return;

        _inGameSettingsOverlay = GetComponent<InGameSettingsOverlay>();
        if (_inGameSettingsOverlay == null)
            _inGameSettingsOverlay = gameObject.AddComponent<InGameSettingsOverlay>();

        _inGameSettingsOverlay.Setup(uiRoot);
        _inGameSettingsOverlay.BindResultPanel(resultPanel);
        _inGameSettingsOverlay.SetGameplayActive(false);
    }

    void OnStartSequenceComplete()
    {
        isGameActive = true;
        isInputBlocked = false;

        if (_inGameSettingsOverlay != null)
            _inGameSettingsOverlay.SetGameplayActive(true);
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
            // 3投目MISSは NO OUT を最優先で即出し（BUST並みの反応速度）
            if (throwsLeft <= 0)
            {
                ResetStreak();

                if (GameEffectsManager.instance != null) GameEffectsManager.instance.PlayNoOutEffect();

                // MISS演出だけは残してもOK（ただし表示は NO OUT を優先）
                if (effectMiss != null) Instantiate(effectMiss, effectPos, Quaternion.identity);
                if (seMiss != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seMiss);

                // NO OUT を即表示・即SE
                if (targetText != null) targetText.SetText(TextNoOut);
                if (seNoOut != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seNoOut);

                StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
            }
            else
            {
                StartCoroutine(MissProcessRoutine(effectPos));
            }
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
            ResetStreak();

            if (GameEffectsManager.instance != null) GameEffectsManager.instance.PlayBustEffect();
            StartCoroutine(FailProcessRoutine(TextBust, 0f, seFail)); // seFailはBUST専用として扱う
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

            // 投げ切り（= NO OUT確定）は、ヒットSEを鳴らさず NO OUT を最優先で即出し
            if (throwsLeft <= 0)
            {
                ResetStreak();

                if (GameEffectsManager.instance != null)
                    GameEffectsManager.instance.PlayNoOutEffect();

                if (targetText != null)
                    targetText.SetText(TextNoOut);

                if (seNoOut != null && AudioManager.instance != null)
                    AudioManager.instance.PlaySE(seNoOut);

                StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
            }
            else
            {
                // 続行できるときだけヒットSE
                PlayHitSound(areaCode);
                StartCoroutine(CooldownRoutine(throwCooldown));
            }
        }
    }

    /// <summary>
    /// ミス時の演出とUI更新を行う。
    /// </summary>
    IEnumerator MissProcessRoutine(Vector3 effectPos)
    {
        ResetStreak();

        // MISS専用の軽いパネル（BUSTと同じoverlayを色違いで使う）
        if (GameEffectsManager.instance != null) GameEffectsManager.instance.PlayMissEffect();

        if (effectMiss != null) Instantiate(effectMiss, effectPos, Quaternion.identity);
        if (seMiss != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seMiss);
        if (targetText != null) targetText.SetText(TextMiss);

        yield return new WaitForSeconds(0.4f);

        //// ここも投げ切りなら NO OUT（音と表示）
        //if (throwsLeft <= 0)
        //{
        //    if (GameEffectsManager.instance != null) GameEffectsManager.instance.PlayNoOutEffect();
        //    StartCoroutine(FailProcessRoutine(TextNoOut, 0f, seNoOut));
        //}
        //else
        //{
        //    UpdateUI();
        //    isInputBlocked = false;
        //}
        UpdateUI();
        isInputBlocked = false;
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
        // streak回復（寿司打）
        ApplyStreakTimeHeal(finishingArea);

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
        StartBgmPitchReturnSmooth();

        if (_isGameOver) return;
        _isGameOver = true;
        isGameActive = false;
        isInputBlocked = true;

        if (_inGameSettingsOverlay != null)
        {
            _inGameSettingsOverlay.ForceClosePanel();
            _inGameSettingsOverlay.SetGameplayActive(false);
        }

        ResetStreak();

        if (resultPanel != null) resultPanel.SetActive(false);

        bool shouldOpenNameInput = debugForceShowNameInput;
        if (!shouldOpenNameInput && RankingManager.instance != null)
        {
            shouldOpenNameInput = await SafeShouldOpenNameInputAsync(totalGameScore, 10, rankingCheckTimeoutSeconds);
        }

        _isNewRecordThisRun = shouldOpenNameInput;

        if (shouldOpenNameInput && newRecordPanel != null)
        {
            // 名前入力中は背面を隠す/無効化して、見た目と操作を整理する
            SetNameInputOpen(true);

            newRecordPanel.Open(totalGameScore, () =>
            {
                // 入力が終わったらリザルトへ
                // 要望によりリザルト中も背面を隠したいので SetNameInputOpen(false) は呼びません
                ShowResultPanel();
            });
            return;
        }

        // 名前入力しないルートでも、リザルト表示中は背面を隠すように変更
        SetNameInputOpen(true);
        ShowResultPanel();

        //// リザルトでも点滅アニメーションが残ってしまうのを防止
        //var board = FindObjectOfType<DartsBoard>();
        //if (board != null)
        //{
        //    board.ForceClearGuide();
        //}
        foreach (var h in FindObjectsOfType<SegmentHighlighter>())
        {
            h.HideGuideAndDestroy(0.1f);   // 0でもOK
        }
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
            .OnUpdate(() =>
            {
                if (resultScoreText != null)
                    resultScoreText.text = "SCORE: " + displayScore.ToString("N0");
            })
            .OnComplete(() =>
            {
                if (seResult != null && AudioManager.instance != null) AudioManager.instance.PlaySE(seResult);
                resultScoreText.transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo);

                if (RankingManager.instance != null)
                {
                    int rank = RankingManager.instance.LastSubmittedRank;
                    double best = RankingManager.instance.LastSubmittedScore;

                    bool isRankIn = (rank >= 0 && rank < 10);

                    if (resultStatusText != null)
                    {
                        if (isRankIn || _isNewRecordThisRun)
                        {
                            if (totalGameScore >= (int)best && (int)best > 0)
                            {
                                resultStatusText.text = "NEW RECORD!!";
                                resultStatusText.color = new Color(1f, 0.196f, 0.137f);
                            }
                            else
                            {
                                resultStatusText.text = "RANK IN!!";
                                resultStatusText.color = new Color(1f, 0.5f, 0f);
                            }

                            if (resultStatusText.text != "")
                            {
                                resultStatusText.transform.localScale = Vector3.zero;
                                resultStatusText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
                            }
                        }
                    }

                    if (resultRankText != null && isRankIn)
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
    public void GoTitle() => SceneManager.LoadScene("Title");

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

    void ShowResultPanel()
    {
        if (_inGameSettingsOverlay != null)
        {
            _inGameSettingsOverlay.ForceClosePanel();
            _inGameSettingsOverlay.SetGameplayActive(false);
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            AnimateResultScore();
        }
    }

    /// <summary>
    /// デバッグ用に名前入力パネルを強制表示する。
    /// </summary>
    void OpenNameInputPanel_Debug(int score)
    {
        if (newRecordPanel == null) return;
        if (resultPanel != null) resultPanel.SetActive(false);

        _isNewRecordThisRun = true;

        SetNameInputOpen(true);

        newRecordPanel.Open(score, () =>
        {
            ShowResultPanel();
        });
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
                float t = 1f - (ratio / pitchStartTimeRatio);      // 0 -> 1
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

    void SetNameInputOpen(bool isOpen)
    {
        if (_isNameInputOpen == isOpen) return;
        _isNameInputOpen = isOpen;

        if (hideWhileNameInput != null)
        {
            for (int i = 0; i < hideWhileNameInput.Length; i++)
            {
                var go = hideWhileNameInput[i];
                if (go != null) go.SetActive(!isOpen);
            }
        }

        if (disableWhileNameInput != null)
        {
            for (int i = 0; i < disableWhileNameInput.Length; i++)
            {
                var b = disableWhileNameInput[i];
                if (b != null) b.enabled = !isOpen;
            }
        }
    }
    void SetStreak(int value)
    {
        int prevStreak = _streak;
        _streak = Mathf.Max(0, value);

        if (streakText == null) return;

        if (hideStreakWhenZero && _streak <= 0)
        {
            streakText.gameObject.SetActive(false);
            return;
        }

        streakText.gameObject.SetActive(true);
        streakText.text = $"COMBO: {_streak}";

        // 1. コンボが増えた時だけ「弾む」アニメーション
        if (_streak > prevStreak)
        {
            // 一旦リセットしてから、1.2倍に膨らんで戻る
            streakText.transform.DOKill();
            streakText.transform.localScale = Vector3.one;
            streakText.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 5, 0.5f);

            // 2. コンボ数に応じて色を変える（熱量を出す）
            Color streakColor = Color.white;
            if (_streak >= 30) streakColor = new Color(1f, 0f, 1f); // 30〜：ピンク/マゼンタ（神）
            else if (_streak >= 20) streakColor = Color.red;       // 20〜：赤（激熱）
            else if (_streak >= 10) streakColor = Color.yellow;    // 10〜：黄（ノリノリ）
            else if (_streak >= 5) streakColor = Color.cyan;      // 5〜：水色（コンボ開始）

            streakText.color = streakColor;

            // 3. 高コンボ時は画面を少し揺らす（GMから直接呼ぶ）
            if (_streak >= 10 && CameraShake.instance != null)
            {
                CameraShake.instance.Shake(0.1f, 0.05f * (_streak / 10f));
            }
        }
    }

    // 秒を足して、PopUpも出す（バランスは後で調整すればOK）
    void AddTimeWithPopup(float addSeconds)
    {
        if (addSeconds <= 0f) return;

        currentTime += addSeconds; // 最大時間なしでOKならクランプしない
        ShowTimePopup(addSeconds);
    }

    void ShowTimePopup(float addSeconds)
    {
        if (timePopupPrefab == null || timePopupAnchor == null) return;

        GameObject go = Instantiate(timePopupPrefab, timePopupAnchor);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }

        string msg = $"+{addSeconds:0.0}s";

        var popup = go.GetComponent<TimePopupText>();
        if (popup != null) popup.Play(msg);
    }

}
