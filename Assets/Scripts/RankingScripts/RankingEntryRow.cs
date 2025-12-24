using UnityEngine;
using UnityEngine.UI;

public class RankingEntryRow : MonoBehaviour
{
    [SerializeField] Text rankText;
    [SerializeField] Text nameText;
    [SerializeField] Text scoreText;

    public void SetData(int rank, string playerName, int score)
    {
        rankText.text = rank.ToString();
        nameText.text = playerName;
        scoreText.text = score.ToString("N0");

        // 1位の文字色を金にする等の処理（Shadowがついていても色は変わります）
        if (rank == 1) rankText.color = new Color(1f, 0.85f, 0f);
    }
}