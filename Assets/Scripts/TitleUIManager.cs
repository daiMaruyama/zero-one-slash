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
    [SerializeField] RectTransform settingsPanelContent;
    [SerializeField] Button openSettingsButton;
    [SerializeField] Button closeSettingsButton;

    [Header("ランキングウィンドウ参照")]
    [SerializeField] GameObject rankingWindowRoot;
    [SerializeField] RectTransform rankingPanelContent;
    [SerializeField] Button openRankingButton;
    [SerializeField] Button closeRankingButton;

    [Header("演出設定")]
    [SerializeField] float animationDuration = 0.28f;
    [SerializeField] float openStartScale = 0.92f;
    [SerializeField] Ease openEase = Ease.OutCubic;
    [SerializeField] Ease closeEase = Ease.InCubic;

    CanvasGroup _settingsGroup;
    CanvasGroup _rankingGroup;

    bool _isAnimating;

    void Start()
    {
        _settingsGroup = GetOrAddCanvasGroup(settingsWindowRoot);
        _rankingGroup = GetOrAddCanvasGroup(rankingWindowRoot);

        InitializeWindow(settingsWindowRoot, settingsPanelContent, _settingsGroup);
        InitializeWindow(rankingWindowRoot, rankingPanelContent, _rankingGroup);

        if (titleUIGroup) titleUIGroup.SetActive(true);

        if (openSettingsButton) openSettingsButton.onClick.AddListener(OnOpenSettings);
        if (closeSettingsButton) closeSettingsButton.onClick.AddListener(OnCloseSettings);

        if (openRankingButton) openRankingButton.onClick.AddListener(OnOpenRanking);
        if (closeRankingButton) closeRankingButton.onClick.AddListener(OnCloseRanking);
    }

    CanvasGroup GetOrAddCanvasGroup(GameObject root)
    {
        if (root == null) return null;

        var g = root.GetComponent<CanvasGroup>();
        if (g == null) g = root.AddComponent<CanvasGroup>();
        return g;
    }

    void InitializeWindow(GameObject root, RectTransform panel, CanvasGroup group)
    {
        if (root == null || group == null) return;

        root.SetActive(false);

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (panel != null)
        {
            panel.localScale = Vector3.one * openStartScale;
        }
    }

    void OnOpenSettings()
    {
        if (_isAnimating) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (titleUIGroup) titleUIGroup.SetActive(false);

        OpenWindow(settingsWindowRoot, settingsPanelContent, _settingsGroup);
    }

    void OnCloseSettings()
    {
        if (_isAnimating) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        CloseWindow(settingsWindowRoot, settingsPanelContent, _settingsGroup, () =>
        {
            if (titleUIGroup) titleUIGroup.SetActive(true);
        });
    }

    void OnOpenRanking()
    {
        if (_isAnimating) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        if (titleUIGroup) titleUIGroup.SetActive(false);

        OpenWindow(rankingWindowRoot, rankingPanelContent, _rankingGroup);

        if (rankingWindowRoot != null)
        {
            var rankingDisplay = rankingWindowRoot.GetComponentInChildren<RankingPanelController>();
            if (rankingDisplay != null) rankingDisplay.Refresh();
        }
    }

    void OnCloseRanking()
    {
        if (_isAnimating) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        CloseWindow(rankingWindowRoot, rankingPanelContent, _rankingGroup, () =>
        {
            if (titleUIGroup && (settingsWindowRoot == null || !settingsWindowRoot.activeSelf))
            {
                titleUIGroup.SetActive(true);
            }
        });
    }

    void OpenWindow(GameObject root, RectTransform panel, CanvasGroup group)
    {
        if (root == null || group == null) return;

        _isAnimating = true;

        root.SetActive(true);

        group.DOKill();
        if (panel != null) panel.DOKill();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (panel != null)
        {
            panel.localScale = Vector3.one * openStartScale;
        }

        Sequence seq = DOTween.Sequence();

        seq.Append(group.DOFade(1f, animationDuration).SetEase(openEase));

        if (panel != null)
        {
            seq.Join(panel.DOScale(1f, animationDuration).SetEase(openEase));
        }

        seq.OnComplete(() =>
        {
            group.interactable = true;
            group.blocksRaycasts = true;
            _isAnimating = false;
        });
    }

    void CloseWindow(GameObject root, RectTransform panel, CanvasGroup group, System.Action onClosed)
    {
        if (root == null || group == null) return;

        _isAnimating = true;

        group.DOKill();
        if (panel != null) panel.DOKill();

        group.interactable = false;
        group.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();

        seq.Append(group.DOFade(0f, animationDuration).SetEase(closeEase));

        if (panel != null)
        {
            seq.Join(panel.DOScale(openStartScale, animationDuration).SetEase(closeEase));
        }

        seq.OnComplete(() =>
        {
            root.SetActive(false);
            _isAnimating = false;
            onClosed?.Invoke();
        });
    }

    void OnDestroy()
    {
        if (_settingsGroup != null) _settingsGroup.DOKill();
        if (_rankingGroup != null) _rankingGroup.DOKill();

        if (settingsPanelContent != null) settingsPanelContent.DOKill();
        if (rankingPanelContent != null) rankingPanelContent.DOKill();
    }
}
