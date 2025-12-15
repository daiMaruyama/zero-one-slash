using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TitleUIManager : MonoBehaviour
{
    [Header("グループ参照")]
    [SerializeField] GameObject titleUIGroup;      // タイトル画面のUI（ロゴやボタンなど全部）
    [SerializeField] GameObject settingsWindowRoot; // 設定ウィンドウの親（Window_Setting）
    [SerializeField] Transform settingsPanelContent; // ウィンドウの中身（動かすネオン枠）

    [Header("ボタン参照（コードでイベント登録用）")]
    [SerializeField] Button openSettingsButton;    // 左下の「SETTING」ボタン
    [SerializeField] Button closeSettingsButton;   // ウィンドウ内の「×」ボタン

    [Header("演出設定")]
    [SerializeField] private float animationSpeed = 0.3f;

    void Start()
    {
        // 1. 初期化（ウィンドウは隠し、タイトルは表示）
        if (settingsWindowRoot) settingsWindowRoot.SetActive(false);
        if (settingsPanelContent) settingsPanelContent.localScale = Vector3.zero;
        if (titleUIGroup) titleUIGroup.SetActive(true);

        // 2. ボタンにイベントを登録（これがやりたかった部分！）
        if (openSettingsButton)
        {
            openSettingsButton.onClick.AddListener(OnOpenSettings);
        }

        if (closeSettingsButton)
        {
            closeSettingsButton.onClick.AddListener(OnCloseSettings);
        }
    }

    // 設定を開く時の処理
    private void OnOpenSettings()
    {
        // タイトルUIを非表示
        if (titleUIGroup) titleUIGroup.SetActive(false);

        // ウィンドウを表示してアニメーション
        if (settingsWindowRoot) settingsWindowRoot.SetActive(true);
        if (settingsPanelContent)
        {
            settingsPanelContent.localScale = Vector3.zero;
            settingsPanelContent.DOScale(1f, animationSpeed).SetEase(Ease.OutBack);
        }
    }

    // 設定を閉じる時の処理
    private void OnCloseSettings()
    {
        // ウィンドウを縮小アニメーション
        if (settingsPanelContent)
        {
            settingsPanelContent.DOScale(0f, animationSpeed)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    // アニメ終了後にウィンドウを完全非表示
                    if (settingsWindowRoot) settingsWindowRoot.SetActive(false);

                    // タイトルUIを復活
                    if (titleUIGroup) titleUIGroup.SetActive(true);
                });
        }
        else
        {
            // パネル参照がない場合のフォールバック（即時切り替え）
            if (settingsWindowRoot) settingsWindowRoot.SetActive(false);
            if (titleUIGroup) titleUIGroup.SetActive(true);
        }
    }

    // お掃除（シーン移動時などにイベント解除）
    void OnDestroy()
    {
        if (settingsPanelContent) settingsPanelContent.DOKill();
    }
}