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
    public Text targetText;
    public Text timeText;
    public Slider timeSlider;
    public GameObject[] throwIcons;
    public Text scoreText;
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
    public bool CanThrow
    {
        get { return isGameActive && !isInputBlocked; }
    }

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

            if (timeText != null) timeText.text = "TIME " + currentTime.ToString("F1");
            // 時間表示の更新
            if (timeSlider != null)
            {
                float ratio = currentTime / timeLimit;
                timeSlider.value = ratio;

                // スライダーの中身（Fill）の画像を取得して色を変える
                // ※ timeSlider.fillRect が Fill の RectTransform です
                Image fillImage = timeSlider.fillRect.GetComponent<Image>();

                if (ratio < 0.2f) // 残り20%以下（ピンチ！）
                {
                    fillImage.color = Color.red; // 赤くする
                }
                else if (ratio < 0.5f) // 半分切った
                {
                    fillImage.color = Color.yellow; // 黄色
                }
                else
                {
                    fillImage.color = Color.cyan; // 通常は水色
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

        PlayHitSound(areaCode);
        if (hitEffectPrefab && areaCode != "OUT")
        {
            Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
        }

        if (areaCode == "OUT") { }
        else if (areaCode.StartsWith("T") || areaCode.Contains("Bull"))
        {
            if (CameraShake.instance) CameraShake.instance.Shake(0.2f, 0.1f);
        }
        else
        {
            if (CameraShake.instance) CameraShake.instance.Shake(0.1f, 0.05f);
        }

        int tempScore = currentTargetScore - hitScore;
        Debug.Log($"Hit: {areaCode} (-{hitScore}) -> Remaining: {tempScore}");

        // 判定ロジック
        if (tempScore == 0)
        {
            // どんな上がり方でもWinProcessへ送る
            WinProcess(areaCode);
        }
        else if (tempScore < 0)
        {
            FailProcess("Bust!!");
        }
        else
        {
            currentTargetScore = tempScore;

            if (throwsLeft <= 0)
            {
                // 時間切れではないので Turn End に変更
                FailProcess("Turn End");
            }
            else
            {
                StartCoroutine(CooldownRoutine(throwCooldown));
                UpdateUI();
            }
        }
    }

    void WinProcess(string finishingArea)
    {
        // シングル上がり または アウターブル上がり は 1点
        // それ以外（ダブル、トリプル、インナーブル）は 3点

        int pointsGet = 0;

        if (finishingArea.StartsWith("S") || finishingArea == "Outer Bull")
        {
            pointsGet = 1;
        }
        else
        {
            pointsGet = 3;
        }

        totalGameScore += pointsGet;

        if (BloomManager.instance != null)
        {
            BloomManager.instance.FlashBloom(pointsGet);
        }

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
        if (scoreText != null) scoreText.text = "SCORE: " + totalGameScore.ToString();

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