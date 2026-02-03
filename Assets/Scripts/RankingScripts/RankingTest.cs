using UnityEngine;

public class RankingTest : MonoBehaviour
{
    async void Start()
    {
        // 送信テスト（スコア 123）
        await RankingManager.instance.SubmitScoreWithUpdateName(123, "TestPlayer");

        // 取得テスト
        var list = await RankingManager.instance.GetRanking(10);

        if (list == null)
        {
            Debug.Log("ランキング取得失敗");
            return;
        }

        foreach (var e in list)
        {
            Debug.Log($"Rank:{e.Rank + 1} Name:{e.PlayerName} Score:{(int)e.Score}");
        }
    }
}
