using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    // =================================================
    // 設定項目
    // =================================================
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

    // 出題されるターゲットスコアのリスト
    int[] questionList = { 32, 40, 50, 60, 36, 20, 16, 81, 101 };

    [Header("UI参照")]
    // CyberTextに変更
    public CyberText targetText;
    public Text timeText;
    public Slider timeSlider;
    public GameObject[] throwIcons;
    // CyberTextに変更
    public CyberText scoreText;
    public GameObject resultPanel;
    public Text resultScoreText;

    [Header("エフェクト設定")]
    public GameObject hitEffectPrefab;  // ヒット時のエフェクト
    public GameObject popupTextPrefab;  // ダメージポップアップ

    [Header("オーディオ設定")]
    public AudioClip seSingle;
    public AudioClip seDouble;
    public AudioClip seTriple;
    public AudioClip seOuterBull;
    public AudioClip seInnerBull;
    public AudioClip seWin;
    public AudioClip seFail; // バースト/失敗時
    public AudioClip seMiss; // 的外
    public AudioClip seResult;
    public AudioClip bgmMain;

    // 音量調整 (AudioManagerがない場合の初期値)
    [Range(0f, 1f)] public float baseBgmVolume = 0.5f;

    // 内部変数
    AudioSource audioSourceSE;
    AudioSource audioSourceBGM;

    float currentTime;
    int currentTargetScore;
    int throwsLeft;
    int totalGameScore;
    bool isGameActive;
    bool isInputBlocked; // 演出中などで操作をブロックするフラグ

    // 外部から投擲可能か確認するプロパティ
    public bool CanThrow
    {
        get { return isGameActive && !isInputBlocked; }
    }

    void Start()
    {
        // AudioSourceを追加
        audioSourceSE = gameObject.AddComponent<AudioSource>();
        audioSourceBGM = gameObject.AddComponent<AudioSource>();

        // BGM再生（AudioManager経由）
        if (AudioManager.instance != null && bgmMain != null)
        {
            AudioManager.instance.PlayBGM(bgmMain);
        }

        // 音量初期化
        SyncVolume();

        // ゲームステート初期化
        if (resultPanel != null) resultPanel.SetActive(false);
        totalGameScore = 0;
        currentTime = timeLimit;
        isGameActive = true;
        isInputBlocked = false;

        // 最初の出題
        NextQuestion();
    }

    void Update()
    {
        // 音量設定を常に同期
        SyncVolume();

        if (isGameActive)
        {
            // 時間計測
            currentTime -= Time.deltaTime;

            // UI更新
            if (timeText != null) timeText.text = "TIME " + currentTime.ToString("F1");

            if (timeSlider != null)
            {
                float ratio = currentTime / timeLimit;
                timeSlider.value = ratio;
                // 残り時間でゲージの色を変える
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

            // タイムアップ判定
            if (currentTime <= 0)
            {
                currentTime = 0;
                GameOver();
            }
        }
    }

    // AudioManagerの音量を反映
    void SyncVolume()
    {
        float targetBGM = baseBgmVolume;
        float targetSE = 1.0f;
        if (AudioManager.instance != null)
        {
            targetBGM = AudioManager.instance.bgmVolume;
            targetSE = AudioManager.instance.seVolume;
        }
        if (audioSourceBGM != null) audioSourceBGM.volume = targetBGM;
        if (audioSourceSE != null) audioSourceSE.volume = targetSE;
    }

    // 次の問題を設定
    public void NextQuestion()
    {
        currentTargetScore = questionList[Random.Range(0, questionList.Length)];
        throwsLeft = 3;
        isInputBlocked = false;
        UpdateUI();
    }

    // ----------------------------------------------------------------
    // ヒット処理メインロジック
    // ----------------------------------------------------------------
    public void ProcessHit(string areaCode, int hitScore, Vector2 hitPosition)
    {
        if (!isGameActive || isInputBlocked) return;
        isInputBlocked = true; // 連続ヒット防止

        throwsLeft--;
        UpdateUI();

        // 1. OUT（的の外）の判定
        if (areaCode == "OUT")
        {
            // ヒット音は鳴らさず、カザキリ音のみ再生
            StartCoroutine(FailProcessRoutine("MISS", 0f, seMiss));
            return;
        }

        // 2. ヒット演出生成
        if (hitEffectPrefab) Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
        if (popupTextPrefab)
        {
            GameObject popup = Instantiate(popupTextPrefab, hitPosition, Quaternion.identity);
            popup.transform.position = new Vector3(hitPosition.x, hitPosition.y, -3.0f);
        }

        // カメラシェイク（強い役なら大きく揺らす）
        if (CameraShake.instance)
        {
            if (areaCode.StartsWith("T") || areaCode.Contains("Bull")) CameraShake.instance.Shake(0.2f, 0.1f);
            else CameraShake.instance.Shake(0.1f, 0.05f);
        }

        // 3. スコア計算
        int tempScore = currentTargetScore - hitScore;

        if (tempScore < 0)
        {
            // === バースト（0を下回った） ===

            // バースト演出（赤フラッシュ等）を実行
            if (GameEffectsManager.instance != null)
            {
                GameEffectsManager.instance.PlayBustEffect();
            }

            // ヒット音は鳴らさず、即座に失敗音を再生
            StartCoroutine(FailProcessRoutine("BUST", 0f, seFail));
        }
        else if (tempScore == 0)
        {
            // === 勝利（ぴったり0） ===

            // フィニッシュ演出（スローモーション）を実行
            if (GameEffectsManager.instance != null)
            {
                GameEffectsManager.instance.PlayFinishEffect();
            }

            // カメラをヒット位置へズーム
            if (CameraController.instance != null)
            {
                CameraController.instance.ZoomIn(new Vector3(hitPosition.x, hitPosition.y, 0), winningZoomSize, 0.05f);
            }

            // ヒット音を鳴らしてから勝利処理へ
            PlayHitSound(areaCode);
            WinProcess(areaCode);
        }
        else
        {
            // === 継続 ===
            currentTargetScore = tempScore;
            UpdateUI();

            // ヒット音を再生し、演出にかかる時間を取得
            float soundDuration = PlayHitSound(areaCode);

            if (throwsLeft <= 0)
            {
                // 残り回数切れ（ターン終了）
                // 音の再生が終わるのを待ってから失敗処理へ
                StartCoroutine(FailProcessRoutine("TURN END", soundDuration, seFail));
            }
            else
            {
                // 次の投擲へ（クールダウン）
                StartCoroutine(CooldownRoutine(throwCooldown));
            }
        }
    }

    // 勝利処理
    void WinProcess(string finishingArea)
    {
        // フィニッシュエリアに応じた加点
        int pointsGet = (finishingArea.StartsWith("S") || finishingArea == "Outer Bull") ? 1 : 3;
        totalGameScore += pointsGet;

        // エフェクトマネージャーがない場合の保険（カメラズーム）
        if (CameraController.instance != null && GameEffectsManager.instance == null)
        {
            CameraController.instance.ZoomIn(Vector3.zero, winningZoomSize, 0.2f);
        }

        if (BloomManager.instance != null) BloomManager.instance.FlashBloom(pointsGet);

        // ファンファーレとテキスト表示
        if (seWin) audioSourceSE.PlayOneShot(seWin);

        // CyberTextで更新
        if (targetText != null) targetText.SetText("WIN!!");

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    // 失敗・バースト・ミス処理用コルーチン
    // delay: 再生待ち時間
    // clip: 再生するSE
    IEnumerator FailProcessRoutine(string reason, float delay, AudioClip clip)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (clip != null) audioSourceSE.PlayOneShot(clip);

        // CyberTextで更新
        if (targetText != null) targetText.SetText(reason);

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    // タイムアップ処理
    void GameOver()
    {
        isGameActive = false;
        isInputBlocked = true;
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            AnimateResultScore(); // スコア演出開始
        }
    }

    // リザルト画面のスコア演出
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
                if (seResult) audioSourceSE.PlayOneShot(seResult);
                // 完了時に拡大演出
                if (resultScoreText != null)
                {
                    resultScoreText.transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo).SetLink(resultScoreText.gameObject);
                }
            }).SetLink(resultScoreText.gameObject);
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 投擲間隔のクールダウン
    IEnumerator CooldownRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (isGameActive) isInputBlocked = false;
    }

    // 次の問題までの待機
    IEnumerator NextQuestionDelayRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (CameraController.instance != null) CameraController.instance.ResetCamera(0.5f);
        if (isGameActive) NextQuestion();
    }

    // 連続音再生（トリプルヒット時など）
    IEnumerator PlaySoundRoutine(AudioClip clip, int count)
    {
        if (clip == null) yield break;
        for (int i = 0; i < count; i++)
        {
            audioSourceSE.pitch = Random.Range(0.95f, 1.05f); // ピッチを揺らす
            audioSourceSE.PlayOneShot(clip);
            yield return new WaitForSeconds(0.08f);
        }
    }

    // ヒット音再生処理
    // 戻り値: 演出完了までの予想時間(秒)
    float PlayHitSound(string areaCode)
    {
        AudioClip clipToPlay = seSingle;
        int repeatCount = 1;

        if (areaCode == "OUT") return 0f;

        // エリアコードに応じたSE選択
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

        // 演出時間を計算して返す
        return (repeatCount * 0.08f) + 0.1f;
    }

    // UI更新
    void UpdateUI()
    {
        // CyberTextで更新（グリッチあり）
        if (targetText != null) targetText.SetValue("TARGET: ", currentTargetScore);
        if (scoreText != null) scoreText.SetValue("SCORE: ", totalGameScore);

        // 残り投擲アイコンの表示制御
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