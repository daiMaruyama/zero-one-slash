using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    // === 設定項目 ===
    [Header("ゲーム設定")]
    [SerializeField] float timeLimit = 60.0f;
    [SerializeField] float throwCooldown = 0.3f;
    [SerializeField] float nextQuestionDelay = 1.5f;
    [SerializeField] float winningZoomSize = 4.2f;

    [Header("アニメーション設定")]
    [SerializeField] float scoreCountDuration = 1.5f;
    [SerializeField] Ease scoreEaseType = Ease.OutExpo;

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
    public AudioClip seResult; // 追加: リザルト発表用の音
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

        PlayHitSound(areaCode);

        if (hitEffectPrefab && areaCode != "OUT")
        {
            Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
        }

        if (popupTextPrefab != null && areaCode != "OUT")
        {
            GameObject popup = Instantiate(popupTextPrefab, hitPosition, Quaternion.identity);
            popup.transform.position = new Vector3(hitPosition.x, hitPosition.y, -3.0f);
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

        if (tempScore == 0)
        {
            WinProcess(areaCode);
        }
        else if (tempScore < 0)
        {
            FailProcess("Bust!!");
        }
        else
        {
            currentTargetScore = tempScore;
            UpdateUI();

            if (throwsLeft <= 0)
            {
                FailProcess("Turn End");
            }
            else
            {
                StartCoroutine(CooldownRoutine(throwCooldown));
            }
        }
    }

    void WinProcess(string finishingArea)
    {
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

        if (CameraController.instance != null)
        {
            CameraController.instance.ZoomIn(Vector3.zero, winningZoomSize, 0.2f);
        }

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
            AnimateResultScore();
        }
    }

    void AnimateResultScore()
    {
        if (resultScoreText == null) return;

        int displayScore = 0;
        resultScoreText.text = "SCORE\n0";

        DOTween.To(
            () => displayScore,
            x => displayScore = x,
            totalGameScore,
            scoreCountDuration
        )
        .SetEase(scoreEaseType)
        .OnUpdate(() =>
        {
            resultScoreText.text = "SCORE\n" + displayScore.ToString("N0");
        })
        .OnComplete(() =>
        {
            // 修正: リザルト専用の音を鳴らす
            if (seResult) audioSourceSE.PlayOneShot(seResult);

            resultScoreText.transform.DOScale(1.2f, 0.1f).SetLoops(2, LoopType.Yoyo);
        });
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

        if (CameraController.instance != null)
        {
            CameraController.instance.ResetCamera(0.5f);
        }

        if (isGameActive) NextQuestion();
    }

    IEnumerator PlaySoundRoutine(AudioClip clip, int count)
    {
        if (clip == null) yield break;

        for (int i = 0; i < count; i++)
        {
            audioSourceSE.pitch = Random.Range(0.95f, 1.05f);
            audioSourceSE.PlayOneShot(clip);
            yield return new WaitForSeconds(0.08f);
        }
    }

    void PlayHitSound(string areaCode)
    {
        AudioClip clipToPlay = seSingle;
        int repeatCount = 1;

        if (areaCode == "OUT")
        {
            clipToPlay = seMiss;
        }
        else if (areaCode.StartsWith("D"))
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