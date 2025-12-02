using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // テスト（残り点数）
    int[] questionList = { 32, 40, 50, 60, 36, 20, 16, 81, 101 };

    [Header("UI設定")]
    public Text targetText;      // 中央の残りスコア表示
    public Text infoText;        // 右下の情報表示（投数、トータルスコア）

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

    // BGM用
    public AudioClip bgmMain;

    private AudioSource audioSourceSE;
    private AudioSource audioSourceBGM;

    // ゲーム内変数
    int currentTargetScore; // 現在の問題の残りスコア
    int throwsLeft;         // 1ターンの残り投数（3スタート）
    int totalGameScore;     // プレイヤーの獲得ポイント（クリア時の報酬）

    void Start()
    {
        // AudioSourceのセットアップ
        audioSourceSE = gameObject.AddComponent<AudioSource>();
        audioSourceBGM = gameObject.AddComponent<AudioSource>();

        // BGM再生
        if (bgmMain != null)
        {
            audioSourceBGM.clip = bgmMain;
            audioSourceBGM.loop = true;
            audioSourceBGM.volume = 0.5f;
            audioSourceBGM.Play();
        }

        totalGameScore = 0;
        NextQuestion();
    }

    // 次の問題へ
    public void NextQuestion()
    {
        // 問題をランダム選択
        currentTargetScore = questionList[Random.Range(0, questionList.Length)];

        // 投数をリセット
        throwsLeft = 3;

        UpdateUI();
    }

    // DartsBoardから呼ばれる処理
    public void ProcessHit(string areaCode, int hitScore, Vector2 hitPosition)
    {
        // 1. 投げる回数を減らす
        throwsLeft--;

        // 2. 音とエフェクトの再生
        PlayHitSound(areaCode);
        if (hitEffectPrefab) Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);

        // 3. 引き算処理
        int tempScore = currentTargetScore - hitScore;
        Debug.Log($"Hit: {areaCode} (-{hitScore}) 残り: {tempScore}");

        if (tempScore == 0)
        {
            // === クリア（上がり！） ===
            WinProcess(areaCode);
        }
        else if (tempScore < 0)
        {
            // === バースト（引きすぎ） ===
            FailProcess("Bust!!");
        }
        else
        {
            // === まだ途中 ===
            // スコアを更新して続行
            currentTargetScore = tempScore;

            if (throwsLeft <= 0)
            {
                // 3投使い切ってしまった
                FailProcess("Time Over...");
            }
            else
            {
                UpdateUI();
            }
        }
    }

    // クリア時の処理
    void WinProcess(string finishingArea)
    {
        // シングル上がり = 1点
        // ダブル・トリプル・ブル上がり = 3点
        int pointsGet = 0;

        if (finishingArea.StartsWith("S"))
        {
            pointsGet = 1;
            Debug.Log("Single Finish! (+1pt)");
        }
        else
        {
            // D, T, Inner Bull, Outer Bull
            pointsGet = 3;
            Debug.Log("Nice Finish! (+3pt)");
        }

        totalGameScore += pointsGet;

        // 演出
        if (seWin) audioSourceSE.PlayOneShot(seWin);
        targetText.text = "WIN!!";

        // 少し待って次へ
        Invoke("NextQuestion", 1.5f);
    }

    // 失敗時の処理
    void FailProcess(string reason)
    {
        // 演出
        if (seFail) audioSourceSE.PlayOneShot(seFail);
        targetText.text = reason;

        // ポイント加算なしで次へ
        Invoke("NextQuestion", 1.5f);
    }

    // 音の出し分けロジック
    void PlayHitSound(string areaCode)
    {
        AudioClip clipToPlay = seSingle; // デフォルト

        if (areaCode.StartsWith("D")) clipToPlay = seDouble;
        else if (areaCode.StartsWith("T")) clipToPlay = seTriple;
        else if (areaCode == "Outer Bull") clipToPlay = seOuterBull;
        else if (areaCode == "Inner Bull") clipToPlay = seInnerBull;

        // 音程をランダムにずらして自然にする
        audioSourceSE.pitch = Random.Range(0.95f, 1.05f);
        if (clipToPlay != null) audioSourceSE.PlayOneShot(clipToPlay);
    }

    void UpdateUI()
    {
        if (targetText != null) targetText.text = currentTargetScore.ToString();

        if (infoText != null) infoText.text = $"あと {throwsLeft} 投 / Total: {totalGameScore} pt";
    }
}