using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Ranking : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("スコアの数値を表示するテキスト")]
    public Text scoreValueText;

    [Header("演出設定")]
    [SerializeField] float countDuration = 1.0f;
    [SerializeField] Ease countEase = Ease.OutExpo;

    // ウィンドウが表示されるたびに自動で実行される
    void OnEnable()
    {
        ShowHighScore();
    }

    void ShowHighScore()
    {
        // 1. 保存データをロード（なければ0）
        int bestScore = PlayerPrefs.GetInt("BEST_SCORE", 0);

        // 2. テキスト更新（DOTweenでカウントアップ）
        if (scoreValueText != null)
        {
            int currentDisplay = 0;

            // 0からベストスコアまで増えるアニメーション
            DOTween.To(() => currentDisplay, x => currentDisplay = x, bestScore, countDuration)
                .SetEase(countEase)
                .OnUpdate(() =>
                {
                    scoreValueText.text = currentDisplay.ToString("N0"); // 3桁区切り
                });
        }
    }

    // （デバッグ用）スコアをリセットしたい時に呼ぶ
    public void ResetScore()
    {
        PlayerPrefs.DeleteKey("BEST_SCORE");
        ShowHighScore();
    }
}