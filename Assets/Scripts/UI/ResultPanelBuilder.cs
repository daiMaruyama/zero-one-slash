using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルトパネルのボタンをネオンレッドテーマ + ホバー演出でスタイルする
/// 併せて、InGame用の簡易設定パネル（BGM/SE）を生成する。
/// </summary>
[DefaultExecutionOrder(-5)]
public class ResultPanelBuilder : MonoBehaviour
{
    static readonly Color NeonRed = new Color(1f, 0.196f, 0.137f);

    [Header("InGame設定パネル")]
    [SerializeField] bool pauseGameWhileSettingsOpen;

    GameObject _settingsPanel;
    Button _settingsButton;

    void Awake()
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
            StyleButton(btn);

        EnsureInGameSettingsUI();
    }

    void EnsureInGameSettingsUI()
    {
        Button retryButton = FindButtonByName("RetryButton");
        if (retryButton == null) return;

        // ===== SETTINGSボタンを追加 =====
        GameObject settingsBtnGO = Instantiate(retryButton.gameObject, retryButton.transform.parent);
        settingsBtnGO.name = "SettingsButton";

        RectTransform settingsRT = settingsBtnGO.transform as RectTransform;
        RectTransform retryRT = retryButton.transform as RectTransform;
        if (settingsRT != null && retryRT != null)
            settingsRT.anchoredPosition = new Vector2(0f, retryRT.anchoredPosition.y);

        Text label = settingsBtnGO.GetComponentInChildren<Text>(true);
        if (label != null) label.text = "SETTING";

        _settingsButton = settingsBtnGO.GetComponent<Button>();
        if (_settingsButton != null)
        {
            _settingsButton.onClick = new Button.ButtonClickedEvent();
            _settingsButton.onClick.AddListener(OpenSettingsPanel);
            StyleButton(_settingsButton);
        }

        // ===== SETTINGSパネル本体 =====
        _settingsPanel = BuildSettingsPanel();
        if (_settingsPanel != null)
            _settingsPanel.SetActive(false);
    }

    GameObject BuildSettingsPanel()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject root = CreateUIObject("InGameSettingsPanel", transform);
        RectTransform rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        Image blockerImg = root.AddComponent<Image>();
        blockerImg.color = new Color(0f, 0f, 0f, 0.7f);

        Button blockerBtn = root.AddComponent<Button>();
        blockerBtn.transition = Selectable.Transition.None;
        blockerBtn.onClick.AddListener(CloseSettingsPanel);

        GameObject window = CreateUIObject("Window", root.transform);
        RectTransform winRT = window.GetComponent<RectTransform>();
        winRT.anchorMin = new Vector2(0.5f, 0.5f);
        winRT.anchorMax = new Vector2(0.5f, 0.5f);
        winRT.pivot = new Vector2(0.5f, 0.5f);
        winRT.anchoredPosition = Vector2.zero;
        winRT.sizeDelta = new Vector2(780, 430);

        Image winImg = window.AddComponent<Image>();
        winImg.color = new Color(0.04f, 0.07f, 0.13f, 0.95f);
        Outline outline = window.AddComponent<Outline>();
        outline.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.45f);
        outline.effectDistance = new Vector2(2, -2);

        // blockerクリックがWindow内に抜けないように受け止める
        Button windowBtn = window.AddComponent<Button>();
        windowBtn.transition = Selectable.Transition.None;
        windowBtn.onClick.AddListener(() => { });

        Text title = CreateText("Title", window.transform, "SETTING", font, 42, FontStyle.BoldAndItalic, Color.white);
        RectTransform titleRT = title.rectTransform;
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -32);
        titleRT.sizeDelta = new Vector2(520, 56);
        title.alignment = TextAnchor.MiddleCenter;
        EnsureGlow(title.gameObject, NeonRed, 0.55f, 2);

        Slider bgmSlider = CreateSliderRow(window.transform, "BGM", new Vector2(0, -150), font);
        Slider seSlider = CreateSliderRow(window.transform, "SE", new Vector2(0, -240), font);

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = AudioManager.instance != null ? AudioManager.instance.bgmVolume : 0.5f;
            bgmSlider.onValueChanged.AddListener(v =>
            {
                if (AudioManager.instance != null) AudioManager.instance.SetBgmVolume(v);
            });
        }

        if (seSlider != null)
        {
            seSlider.minValue = 0f;
            seSlider.maxValue = 1f;
            seSlider.value = AudioManager.instance != null ? AudioManager.instance.seVolume : 0.5f;
            seSlider.onValueChanged.AddListener(v =>
            {
                if (AudioManager.instance != null) AudioManager.instance.SetSeVolume(v);
            });
        }

        Button closeBtn = CreateSimpleButton(window.transform, "CloseButton", "CLOSE", font);
        if (closeBtn != null)
        {
            RectTransform closeRT = closeBtn.transform as RectTransform;
            closeRT.anchorMin = new Vector2(0.5f, 0f);
            closeRT.anchorMax = new Vector2(0.5f, 0f);
            closeRT.pivot = new Vector2(0.5f, 0f);
            closeRT.anchoredPosition = new Vector2(0f, 22f);
            closeRT.sizeDelta = new Vector2(230f, 60f);

            closeBtn.onClick.AddListener(CloseSettingsPanel);
            StyleButton(closeBtn);
        }

        return root;
    }

    Slider CreateSliderRow(Transform parent, string label, Vector2 anchoredPos, Font font)
    {
        GameObject row = CreateUIObject(label + "Row", parent);
        RectTransform rowRT = row.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 1f);
        rowRT.anchorMax = new Vector2(0.5f, 1f);
        rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = anchoredPos;
        rowRT.sizeDelta = new Vector2(640, 72);

        Text labelText = CreateText(label + "Label", row.transform, label, font, 28, FontStyle.Bold, Color.white);
        RectTransform labelRT = labelText.rectTransform;
        labelRT.anchorMin = new Vector2(0f, 0.5f);
        labelRT.anchorMax = new Vector2(0f, 0.5f);
        labelRT.pivot = new Vector2(0f, 0.5f);
        labelRT.anchoredPosition = new Vector2(0f, 0f);
        labelRT.sizeDelta = new Vector2(120, 56);
        labelText.alignment = TextAnchor.MiddleLeft;

        GameObject sliderGO = CreateUIObject(label + "Slider", row.transform);
        RectTransform sliderRT = sliderGO.GetComponent<RectTransform>();
        sliderRT.anchorMin = new Vector2(1f, 0.5f);
        sliderRT.anchorMax = new Vector2(1f, 0.5f);
        sliderRT.pivot = new Vector2(1f, 0.5f);
        sliderRT.anchoredPosition = new Vector2(0, 0);
        sliderRT.sizeDelta = new Vector2(470, 30);

        Slider slider = sliderGO.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;

        GameObject bgGO = CreateUIObject("Background", sliderGO.transform);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.15f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderGO.transform);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.offsetMin = new Vector2(10, 7);
        fillAreaRT.offsetMax = new Vector2(-10, -7);

        GameObject fillGO = CreateUIObject("Fill", fillArea.transform);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.8f);

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderGO.transform);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = Vector2.zero;
        handleAreaRT.anchorMax = Vector2.one;
        handleAreaRT.offsetMin = new Vector2(10, 0);
        handleAreaRT.offsetMax = new Vector2(-10, 0);

        GameObject handleGO = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRT = handleGO.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0.5f, 0.5f);
        handleRT.anchorMax = new Vector2(0.5f, 0.5f);
        handleRT.pivot = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(24, 38);
        Image handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;

        slider.targetGraphic = handleImg;
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;

        return slider;
    }

    Button CreateSimpleButton(Transform parent, string name, string text, Font font)
    {
        GameObject buttonGO = CreateUIObject(name, parent);

        Image img = buttonGO.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.08f);

        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = img;

        Text label = CreateText("Label", buttonGO.transform, text, font, 24, FontStyle.Bold, Color.white);
        RectTransform labelRT = label.rectTransform;
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        label.alignment = TextAnchor.MiddleCenter;

        return button;
    }

    void OpenSettingsPanel()
    {
        if (_settingsPanel == null) return;

        if (_settingsButton != null) _settingsButton.interactable = false;
        _settingsPanel.SetActive(true);

        if (pauseGameWhileSettingsOpen)
            Time.timeScale = 0f;
    }

    void CloseSettingsPanel()
    {
        if (_settingsPanel == null) return;

        _settingsPanel.SetActive(false);
        if (_settingsButton != null) _settingsButton.interactable = true;

        if (pauseGameWhileSettingsOpen)
            Time.timeScale = 1f;
    }

    Button FindButtonByName(string exactName)
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            if (btn != null && btn.name == exactName)
                return btn;
        }
        return null;
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    Text CreateText(string name, Transform parent, string text, Font font,
        int size, FontStyle style, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    void StyleButton(Button button)
    {
        if (button == null) return;

        // 背景トーン（TitleのネオンUIに寄せる）
        var bg = button.GetComponent<Image>();
        if (bg != null)
            bg.color = new Color(0.09f, 0.13f, 0.2f, 0.88f);

        // フレーム
        var outline = button.GetComponent<Outline>();
        if (outline == null) outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.6f);
        outline.effectDistance = new Vector2(3f, -3f);

        // ラベル：白太字 + ネオングロー
        var label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = FontStyle.BoldAndItalic;
            EnsureGlow(label.gameObject, NeonRed, 0.6f, 2);
        }

        // ホバー/プレス演出
        var nrb = button.gameObject.GetComponent<NeonResultButton>();
        if (nrb == null) nrb = button.gameObject.AddComponent<NeonResultButton>();
        nrb.Init(button.GetComponent<Image>());
    }

    void EnsureGlow(GameObject go, Color baseColor, float alpha, int strength)
    {
        Shadow shadow = null;
        Outline outline = null;

        foreach (var s in go.GetComponents<Shadow>())
        {
            if (s is Outline o) outline = o;
            else shadow = s;
        }

        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        shadow.effectDistance = new Vector2(strength, -strength);

        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.6f);
        outline.effectDistance = new Vector2(strength, -strength);
    }
}
