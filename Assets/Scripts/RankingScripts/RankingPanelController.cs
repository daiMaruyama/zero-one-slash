using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Services.Leaderboards.Models;

public class RankingPanelController : MonoBehaviour
{
    [SerializeField] Transform entryContainer;
    [SerializeField] GameObject entryPrefab;
    [SerializeField] Text infoText;

    public async void Refresh(int limit = 10)
    {
        if (infoText != null) infoText.text = "LOADING...";

        if (entryContainer != null)
        {
            for (int i = entryContainer.childCount - 1; i >= 0; i--)
                Destroy(entryContainer.GetChild(i).gameObject);
        }

        if (RankingManager.instance == null)
        {
            if (infoText != null) infoText.text = "RANKING SYSTEM NOT FOUND";
            return;
        }

        List<LeaderboardEntry> results;
        try
        {
            results = await RankingManager.instance.GetRanking(limit);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Ranking] Refresh failed: {e.Message}");
            if (infoText != null) infoText.text = "OFFLINE";
            return;
        }

        if (results == null || results.Count == 0)
        {
            if (infoText != null) infoText.text = "NO DATA";
            return;
        }

        if (infoText != null) infoText.text = "";

        foreach (LeaderboardEntry entry in results)
        {
            if (entryPrefab == null || entryContainer == null) break;

            var go = Instantiate(entryPrefab, entryContainer);
            go.SetActive(true);
            var row = go.GetComponent<RankingEntryRow>();
            if (row != null)
            {
                int rank = entry.Rank + 1;
                string raw = entry.PlayerName;
                if (!string.IsNullOrEmpty(raw))
                {
                    int hashIdx = raw.LastIndexOf('#');
                    if (hashIdx > 0) raw = raw.Substring(0, hashIdx);
                }
                string name = string.IsNullOrEmpty(raw) ? "Unknown" : raw;
                int score = (int)entry.Score;

                row.SetData(rank, name, score);
            }
        }
    }
}
