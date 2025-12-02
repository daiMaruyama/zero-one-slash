using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // === 設定項目 ===
    [Header("ゲーム設定")]
    [SerializeField] float timeLimit = 60.0f;
    [SerializeField] float throwCooldown = 0.3f;
    [SerializeField] float nextQuestionDelay = 1.5f;

    int[] questionList = { 32, 40, 50, 60, 36, 20, 16, 81, 101 };

    [Header("UI設定")]
    public Text targetText;      // 中央の残りスコア
    public Text infoText;        // 右下の情報（投数、スコア）
    public Text timeText;        // 制限時間
    public GameObject resultPanel;
    public Text resultScoreText;

    [Header("エフェクト設定")]
    public GameObject hitEffectPrefab;

    [Header("オーディオ設定")]
    public AudioClip seSingle;
    public AudioClip seDouble;
    public AudioClip seTriple;
    public AudioClip seOuterBull;
    public AudioClip seInnerBull;
    public AudioClip seWin;
    public AudioClip seFail;
    public AudioClip seMiss;
    public AudioClip bgmMain;

    AudioSource audioSourceSE;
    AudioSource audioSourceBGM;

    float currentTime;
    int currentTargetScore;
    int throwsLeft;
    int totalGameScore;
    bool isGameActive;
    bool isInputBlocked;

    void Start()
    {
        audioSourceSE = gameObject.AddComponent<AudioSource>();
        audioSourceBGM = gameObject.AddComponent<AudioSource>();

        if (bgmMain != null)
        {
            audioSourceBGM.clip = bgmMain;
            audioSourceBGM.loop = true;
            audioSourceBGM.volume = 0.5f;
            audioSourceBGM.Play();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        totalGameScore = 0;
        currentTime = timeLimit;
        isGameActive = true;
        isInputBlocked = false;

        NextQuestion();
    }

    void Update()
    {
        if (isGameActive)
        {
            currentTime -= Time.deltaTime;

            // 時間表示の更新
            if (timeText != null)
            {
                timeText.text = "Time: " + currentTime.ToString("F1");
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

        // 音とエフェクト
        PlayHitSound(areaCode);
        if (hitEffectPrefab && areaCode != "OUT")
        {
            Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
        }

        // カメラシェイク
        if (areaCode == "OUT")
        {
            // アウトは揺らさない
        }
        else if (areaCode.StartsWith("T") || areaCode.Contains("Bull"))
        {
            if (CameraShake.instance) CameraShake.instance.Shake(0.2f, 0.1f);
        }
        else
        {
            if (CameraShake.instance) CameraShake.instance.Shake(0.1f, 0.05f);
        }

        // 計算
        int tempScore = currentTargetScore - hitScore;
        Debug.Log($"Hit: {areaCode} (-{hitScore}) -> Remaining: {tempScore}");

        // === 判定ロジックの整理 ===

        if (tempScore == 0)
        {
            // パターン1: ピッタリ0になった（Win判定へ）
            // 条件: Double, Triple, BullのいずれかならOK
            if (areaCode.StartsWith("D") || areaCode.StartsWith("T") || areaCode.Contains("Bull"))
            {
                WinProcess(areaCode);
            }
            else
            {
                // Singleで0になってしまった（Single Out）
                FailProcess("Single Out...");
            }
        }
        else if (tempScore < 0)
        {
            // パターン2: 0を下回った（Bust）
            FailProcess("Bust!!");
        }
        else
        {
            // パターン3: まだ0になっていない（継続）
            currentTargetScore = tempScore;

            if (throwsLeft <= 0)
            {
                // 3投投げきってしまった（Time Over / Lose）
                FailProcess("Time Over...");
            }
            else
            {
                // まだ投げられる（Next Throw）
                StartCoroutine(CooldownRoutine(throwCooldown));
                UpdateUI();
            }
        }
    }

    void WinProcess(string finishingArea)
    {
        // ダブル、トリプル、ブル上がりは3点、万が一設定変更でシングル許可した場合は1点
        int pointsGet = (finishingArea.StartsWith("S")) ? 1 : 3;
        totalGameScore += pointsGet;

        if (seWin) audioSourceSE.PlayOneShot(seWin);
        if (targetText != null) targetText.text = "WIN!!";

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    void FailProcess(string reason)
    {
        if (seFail) audioSourceSE.PlayOneShot(seFail);
        if (targetText != null) targetText.text = reason;

        StartCoroutine(NextQuestionDelayRoutine(nextQuestionDelay));
    }

    void GameOver()
    {
        isGameActive = false;
        isInputBlocked = true;

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            if (resultScoreText != null)
            {
                resultScoreText.text = "SCORE\n" + totalGameScore;
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
        if (isGameActive) NextQuestion();
    }

    void PlayHitSound(string areaCode)
    {
        AudioClip clipToPlay = seSingle;

        if (areaCode == "OUT") clipToPlay = seMiss;
        else if (areaCode.StartsWith("D")) clipToPlay = seDouble;
        else if (areaCode.StartsWith("T")) clipToPlay = seTriple;
        else if (areaCode == "Outer Bull") clipToPlay = seOuterBull;
        else if (areaCode == "Inner Bull") clipToPlay = seInnerBull;

        audioSourceSE.pitch = Random.Range(0.95f, 1.05f);
        if (clipToPlay != null) audioSourceSE.PlayOneShot(clipToPlay);
    }

    void UpdateUI()
    {
        if (targetText != null) targetText.text = "TARGET: " + currentTargetScore.ToString();
        if (infoText != null) infoText.text = $"あと {throwsLeft} 投 / Total: {totalGameScore} pt";
    }
}