using UnityEngine;

/// <summary>
/// RankingManagerの動作を検証するためのテスト用クラス
/// </summary>
public class RankingTest : MonoBehaviour
{
    void Update()
    {
        // Sキーでスコア「100」を「TestUser」として送信
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("[Test] Sending score...");
            _ = RankingManager.instance.SubmitScoreWithUpdateName(100, "TestUser");
        }

        // Gキーで現在のランキングTOP5を取得してログに表示
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("[Test] Fetching ranking...");
            FetchAndLogRanking();
        }
    }

    async void FetchAndLogRanking()
    {
        var scores = await RankingManager.instance.GetRanking(5);
        if (scores == null) return;

        foreach (var entry in scores)
        {
            Debug.Log($"Rank: {entry.Rank + 1} | Name: {entry.PlayerName} | Score: {entry.Score}");
        }
    }
}