using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RankingPanelController : MonoBehaviour
{
    [SerializeField] Transform entryContainer;
    [SerializeField] GameObject entryPrefab;
    [SerializeField] Text infoText;

    /// <summary>
    /// TitleUIManagerから呼び出され、リストを最新にする
    /// </summary>
    public async void Refresh()
    {
        infoText.text = "LOADING...";

        foreach (Transform child in entryContainer) Destroy(child.gameObject);

        var results = await RankingManager.instance.GetRanking(10);

        if (results == null || results.Count == 0)
        {
            infoText.text = "NO DATA";
            return;
        }

        infoText.text = "";
        foreach (var entry in results)
        {
            var row = Instantiate(entryPrefab, entryContainer).GetComponent<RankingEntryRow>();
            row.SetData(entry.Rank + 1, entry.PlayerName, (int)entry.Score);
        }
    }
}