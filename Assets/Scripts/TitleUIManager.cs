using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TitleUIManager : MonoBehaviour
{
    [Header("タイトルのボタン群（Windowは入れない）")]
    [SerializeField] CanvasGroup titleButtonsGroup;

    [Header("設定パネル")]
    [SerializeField] SettingsPanelAnimator settingsPanel;
    [SerializeField] Button openSettingsButton;
    [SerializeField] Button closeSettingsButton;

    [Header("ランキングパネル")]
    [SerializeField] SettingsPanelAnimator rankingPanel;
    [SerializeField] Button openRankingButton;
    [SerializeField] Button closeRankingButton;

    void Awake()
    {
        if (settingsPanel != null) settingsPanel.HideInstant();
        if (rankingPanel != null) rankingPanel.HideInstant();

        SetTitleButtonsVisible(true);
    }

    void Start()
    {
        if (openSettingsButton != null) openSettingsButton.onClick.AddListener(OpenSettings);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);

        if (openRankingButton != null) openRankingButton.onClick.AddListener(OpenRanking);
        if (closeRankingButton != null) closeRankingButton.onClick.AddListener(CloseRanking);
    }

    void OpenSettings()
    {
        ClearSelect();

        if (rankingPanel != null) rankingPanel.HideInstant();

        SetTitleButtonsVisible(false);

        if (settingsPanel != null) settingsPanel.Open();
    }

    void CloseSettings()
    {
        ClearSelect();

        if (settingsPanel != null) settingsPanel.Close();

        SetTitleButtonsVisible(true);
    }

    void OpenRanking()
    {
        ClearSelect();

        if (settingsPanel != null) settingsPanel.HideInstant();

        SetTitleButtonsVisible(false);

        if (rankingPanel != null) rankingPanel.Open();

        if (rankingPanel != null)
        {
            var controller = rankingPanel.GetComponentInChildren<RankingPanelController>(true);
            if (controller != null) controller.Refresh();
        }
    }

    void CloseRanking()
    {
        ClearSelect();

        if (rankingPanel != null) rankingPanel.Close();

        SetTitleButtonsVisible(true);
    }

    void SetTitleButtonsVisible(bool visible)
    {
        if (titleButtonsGroup == null) return;

        titleButtonsGroup.alpha = visible ? 1f : 0f;
        titleButtonsGroup.interactable = visible;
        titleButtonsGroup.blocksRaycasts = visible;
    }

    void ClearSelect()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    void OnDestroy()
    {
        if (openSettingsButton != null) openSettingsButton.onClick.RemoveListener(OpenSettings);
        if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveListener(CloseSettings);

        if (openRankingButton != null) openRankingButton.onClick.RemoveListener(OpenRanking);
        if (closeRankingButton != null) closeRankingButton.onClick.RemoveListener(CloseRanking);
    }
}
