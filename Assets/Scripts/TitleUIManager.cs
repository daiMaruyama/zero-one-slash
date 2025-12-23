using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class TitleUIManager : MonoBehaviour
{
    [Header("グループ参照")]
    [SerializeField] GameObject titleUIGroup;

    [Header("設定ウィンドウ参照")]
    [SerializeField] GameObject settingsWindowRoot;
    [SerializeField] Transform settingsPanelContent;
    [SerializeField] Button openSettingsButton;
    [SerializeField] Button closeSettingsButton;

    // ランキングウィンドウ
    [Header("ランキングウィンドウ参照")]
    [SerializeField] GameObject rankingWindowRoot;
    [SerializeField] Transform rankingPanelContent;
    [SerializeField] Button openRankingButton;
    [SerializeField] Button closeRankingButton;

    [Header("演出設定")]
    [SerializeField] float animationSpeed = 0.3f;

    void Start()
    {
        // 初期化: 設定ウィンドウ
        if (settingsWindowRoot) settingsWindowRoot.SetActive(false);
        if (settingsPanelContent) settingsPanelContent.localScale = Vector3.zero;

        // 初期化: ランキングウィンドウ
        if (rankingWindowRoot) rankingWindowRoot.SetActive(false);
        if (rankingPanelContent) rankingPanelContent.localScale = Vector3.zero;

        if (titleUIGroup) titleUIGroup.SetActive(true);

        // ボタン登録: 設定
        if (openSettingsButton) openSettingsButton.onClick.AddListener(OnOpenSettings);
        if (closeSettingsButton) closeSettingsButton.onClick.AddListener(OnCloseSettings);

        // ボタン登録: ランキング
        if (openRankingButton) openRankingButton.onClick.AddListener(OnOpenRanking);
        if (closeRankingButton) closeRankingButton.onClick.AddListener(OnCloseRanking);
    }

    // --- 設定ウィンドウの処理 ---
    void OnOpenSettings()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (titleUIGroup) titleUIGroup.SetActive(false);

        if (settingsWindowRoot) settingsWindowRoot.SetActive(true);
        if (settingsPanelContent)
        {
            settingsPanelContent.localScale = Vector3.zero;
            settingsPanelContent.DOScale(1f, animationSpeed).SetEase(Ease.OutBack);
        }
    }

    void OnCloseSettings()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (settingsPanelContent)
        {
            settingsPanelContent.DOScale(0f, animationSpeed)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (settingsWindowRoot) settingsWindowRoot.SetActive(false);
                    if (titleUIGroup) titleUIGroup.SetActive(true);
                });
        }
        else
        {
            if (settingsWindowRoot) settingsWindowRoot.SetActive(false);
            if (titleUIGroup) titleUIGroup.SetActive(true);
        }
    }

    // ランキングウィンドウの処理
    void OnOpenRanking()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (titleUIGroup) titleUIGroup.SetActive(false);

        if (rankingWindowRoot)
        {
            rankingWindowRoot.SetActive(true);

            // パネル内のコントローラーを探して最新データへの更新を命令する
            var rankingDisplay = rankingWindowRoot.GetComponentInChildren<RankingPanelController>();
            if (rankingDisplay != null) rankingDisplay.Refresh();
        }

        if (rankingPanelContent)
        {
            rankingPanelContent.localScale = Vector3.zero;
            rankingPanelContent.DOScale(1f, animationSpeed).SetEase(Ease.OutBack);
        }
    }

    void OnCloseRanking()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (rankingPanelContent)
        {
            rankingPanelContent.DOScale(0f, animationSpeed)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    if (rankingWindowRoot) rankingWindowRoot.SetActive(false);
                    // 設定ウィンドウが開いていなければタイトルを戻す（念の為）
                    if (titleUIGroup && (!settingsWindowRoot || !settingsWindowRoot.activeSelf))
                    {
                        titleUIGroup.SetActive(true);
                    }
                });
        }
        else
        {
            if (rankingWindowRoot) rankingWindowRoot.SetActive(false);
            if (titleUIGroup) titleUIGroup.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (settingsPanelContent) settingsPanelContent.DOKill();
        if (rankingPanelContent) rankingPanelContent.DOKill();
    }
}