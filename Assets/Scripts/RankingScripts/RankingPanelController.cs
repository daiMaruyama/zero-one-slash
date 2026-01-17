using UnityEngine;
using UnityEngine.UI;

public class RankingPanelController : MonoBehaviour
{
    [SerializeField] Transform entryContainer;
    [SerializeField] GameObject entryPrefab;
    [SerializeField] Text infoText;

    public async void Refresh()
    {
        if (infoText != null) infoText.text = "LOADING...";

        if (entryContainer != null)
        {
            foreach (Transform child in entryContainer)
                Destroy(child.gameObject);
        }

        if (RankingManager.instance == null)
        {
            if (infoText != null) infoText.text = "NO MANAGER";
            return;
        }

        var results = await RankingManager.instance.GetRanking(10);

        if (results == null || results.Count == 0)
        {
            if (infoText != null) infoText.text = "NO DATA";
            return;
        }

        if (infoText != null) infoText.text = "";

        foreach (var entry in results)
        {
            var rowObj = Instantiate(entryPrefab, entryContainer);
            var row = rowObj.GetComponent<RankingEntryRow>();

            if (row != null)
            {
                string name = string.IsNullOrEmpty(entry.PlayerName) ? "Unknown" : entry.PlayerName;
                row.SetData(entry.Rank + 1, name, (int)entry.Score);
            }
        }
    }
}
