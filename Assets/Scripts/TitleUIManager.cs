using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class TitleUIManager : MonoBehaviour
{
    [Header("グループ参照")]
    [SerializeField] GameObject titleUIGroup;
    [SerializeField] GameObject settingsWindowRoot;
    [SerializeField] Transform settingsPanelContent;

    [Header("ボタン参照")]
    [SerializeField] Button openSettingsButton;
    [SerializeField] Button closeSettingsButton;

    [Header("演出設定")]
    [SerializeField] float animationSpeed = 0.3f;

    void Start()
    {
        if (settingsWindowRoot) settingsWindowRoot.SetActive(false);
        if (settingsPanelContent) settingsPanelContent.localScale = Vector3.zero;
        if (titleUIGroup) titleUIGroup.SetActive(true);

        if (openSettingsButton) openSettingsButton.onClick.AddListener(OnOpenSettings);
        if (closeSettingsButton) closeSettingsButton.onClick.AddListener(OnCloseSettings);
    }

    void OnOpenSettings()
    {
        // 処理の最初で即座に選択状態を解除する
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
        // ここでも念のため解除（×ボタン自体のハイライト残り防止）
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

    void OnDestroy()
    {
        if (settingsPanelContent) settingsPanelContent.DOKill();
    }
}