using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// タイトル画面の4メニューボタン管理
/// GAME START / HOW TO PLAY / RANKING / SETTING
/// </summary>
public class TitleUIManager : MonoBehaviour
{
    [Header("タイトルのボタン管理")]
    [SerializeField] CanvasGroup titleButtonsGroup;

    [Header("パネル表示時に隠すUI")]
    [SerializeField] CanvasGroup titleLogoGroup;

    [Header("メニューボタン（自動生成される）")]
    [SerializeField] Button gameStartButton;
    [SerializeField] Button howToPlayButton;
    [SerializeField] Button rankingButton;
    [SerializeField] Button settingsButton;
    NeonMenuButton[] menuButtons;

    [Header("パネル")]
    [SerializeField] SettingsPanelAnimator settingsPanel;
    [SerializeField] Button closeSettingsButton;

    [SerializeField] SettingsPanelAnimator rankingPanel;
    [SerializeField] Button closeRankingButton;

    [SerializeField] SettingsPanelAnimator howToPlayPanel;
    [SerializeField] Button closeHowToPlayButton;

    [Header("遷移")]
    [SerializeField] TitleController titleController;

    bool menuListenersRegistered;

    /// <summary>
    /// TitleMenuBuilderから呼ばれる。自動生成されたボタンをセットする
    /// </summary>
    public void SetupMenu(CanvasGroup btnGroup, Button start, Button howto, Button ranking, Button settings, NeonMenuButton[] neonBtns)
    {
        titleButtonsGroup = btnGroup;
        gameStartButton = start;
        howToPlayButton = howto;
        rankingButton = ranking;
        settingsButton = settings;
        menuButtons = neonBtns;

        RegisterMenuListeners();
    }

    void RegisterMenuListeners()
    {
        if (menuListenersRegistered) return;
        menuListenersRegistered = true;

        if (gameStartButton != null) gameStartButton.onClick.AddListener(OnGameStart);
        if (howToPlayButton != null) howToPlayButton.onClick.AddListener(OpenHowToPlay);
        if (rankingButton != null) rankingButton.onClick.AddListener(OpenRanking);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
    }

    void Awake()
    {
        if (settingsPanel != null) settingsPanel.HideInstant();
        if (rankingPanel != null) rankingPanel.HideInstant();
        if (howToPlayPanel != null) howToPlayPanel.HideInstant();

        SetTitleButtonsVisible(true);
        SetTitleLogoVisible(true);
    }

    void Start()
    {
        RegisterMenuListeners();

        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
        if (closeRankingButton != null) closeRankingButton.onClick.AddListener(CloseRanking);
        if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);

        SetupBlocker(settingsPanel, CloseSettings);
        SetupBlocker(rankingPanel, CloseRanking);
        SetupBlocker(howToPlayPanel, CloseHowToPlay);

        PlayMenuEntrance();
    }

    // ===== 入場演出 =====

    void PlayMenuEntrance()
    {
        if (menuButtons == null) return;
        foreach (var btn in menuButtons)
        {
            if (btn != null) btn.PlayEntrance();
        }
    }

    // ===== GAME START =====

    void OnGameStart()
    {
        ClearSelect();
        SetTitleButtonsVisible(false);
        SetTitleLogoVisible(false);

        if (titleController != null)
            titleController.StartGame();
    }

    // ===== HOW TO PLAY =====

    void OpenHowToPlay()
    {
        if (howToPlayPanel == null) return;
        ClearSelect();

        if (settingsPanel != null) settingsPanel.HideInstant();
        if (rankingPanel != null) rankingPanel.HideInstant();

        SetTitleButtonsVisible(false);
        SetTitleLogoVisible(false);
        howToPlayPanel.Open();
    }

    void CloseHowToPlay()
    {
        ClearSelect();
        if (howToPlayPanel != null) howToPlayPanel.Close();
        SetTitleButtonsVisible(true);
        SetTitleLogoVisible(true);
        ResetAllButtons();
    }

    // ===== RANKING =====

    void OpenRanking()
    {
        if (rankingPanel == null) return;
        ClearSelect();

        if (settingsPanel != null) settingsPanel.HideInstant();
        if (howToPlayPanel != null) howToPlayPanel.HideInstant();

        SetTitleButtonsVisible(false);
        SetTitleLogoVisible(false);
        rankingPanel.Open();

        var controller = rankingPanel.GetComponentInChildren<RankingPanelController>(true);
        if (controller != null) controller.Refresh();
    }

    void CloseRanking()
    {
        ClearSelect();
        if (rankingPanel != null) rankingPanel.Close();
        SetTitleButtonsVisible(true);
        SetTitleLogoVisible(true);
        ResetAllButtons();
    }

    // ===== SETTING =====

    void OpenSettings()
    {
        if (settingsPanel == null) return;
        ClearSelect();

        if (rankingPanel != null) rankingPanel.HideInstant();
        if (howToPlayPanel != null) howToPlayPanel.HideInstant();

        SetTitleButtonsVisible(false);
        SetTitleLogoVisible(false);
        settingsPanel.Open();
    }

    void CloseSettings()
    {
        ClearSelect();
        if (settingsPanel != null) settingsPanel.Close();
        SetTitleButtonsVisible(true);
        SetTitleLogoVisible(true);
        ResetAllButtons();
    }

    // ===== ユーティリティ =====

    void SetTitleButtonsVisible(bool visible)
    {
        if (titleButtonsGroup == null) return;

        titleButtonsGroup.alpha = visible ? 1f : 0f;
        titleButtonsGroup.interactable = visible;
        titleButtonsGroup.blocksRaycasts = visible;
    }

    void SetTitleLogoVisible(bool visible)
    {
        if (titleLogoGroup == null) return;

        titleLogoGroup.alpha = visible ? 1f : 0f;
        titleLogoGroup.blocksRaycasts = visible;
    }

    void ResetAllButtons()
    {
        if (menuButtons == null) return;
        foreach (var btn in menuButtons)
        {
            if (btn != null) btn.ResetState();
        }
    }

    void ClearSelect()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// パネル内の「Blocker」子オブジェクトにクリックで閉じる機能をセット
    /// Blockerはパネル背景の外側（全画面）のレイキャスト受け
    /// </summary>
    void SetupBlocker(SettingsPanelAnimator panel, UnityEngine.Events.UnityAction closeAction)
    {
        if (panel == null) return;
        Transform blocker = panel.transform.Find("Blocker");
        if (blocker == null) return;

        Button blockerBtn = blocker.GetComponent<Button>();
        if (blockerBtn == null)
            blockerBtn = blocker.gameObject.AddComponent<Button>();

        blockerBtn.transition = Selectable.Transition.None;
        var nav = blockerBtn.navigation;
        nav.mode = Navigation.Mode.None;
        blockerBtn.navigation = nav;

        blockerBtn.onClick.AddListener(closeAction);
    }

    void OnDestroy()
    {
        if (gameStartButton != null) gameStartButton.onClick.RemoveListener(OnGameStart);
        if (howToPlayButton != null) howToPlayButton.onClick.RemoveListener(OpenHowToPlay);
        if (rankingButton != null) rankingButton.onClick.RemoveListener(OpenRanking);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);

        if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveListener(CloseSettings);
        if (closeRankingButton != null) closeRankingButton.onClick.RemoveListener(CloseRanking);
        if (closeHowToPlayButton != null) closeHowToPlayButton.onClick.RemoveListener(CloseHowToPlay);
    }
}
