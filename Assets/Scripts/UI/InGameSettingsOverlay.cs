using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// InGameプレイ中に常時表示するSETTINGボタンと、
/// BGM/SE調整 + Retry/Title 遷移を行う設定パネルをランタイム生成する。
/// </summary>
public class InGameSettingsOverlay : MonoBehaviour
{
    [SerializeField] bool pauseGameWhileOpen;

    static readonly Color NeonRed = new Color(1f, 0.196f, 0.137f);

    Transform _uiRoot;
    GameObject _panel;
    Button _openButton;
    bool _isOpen;
    bool _isGameplayActive;

    public void Setup(Transform uiRoot)
    {
        if (uiRoot == null) return;
        if (_openButton != null) return;

        _uiRoot = uiRoot;
        BuildOpenButton();
        BuildPanel();

        SetGameplayActive(false);
    }

    public void SetGameplayActive(bool active)
    {
        _isGameplayActive = active;
        if (_openButton == null) return;

        if (!active)
        {
            ForceClosePanel();
            _openButton.gameObject.SetActive(false);
            return;
        }

        if (!_isOpen)
            _openButton.gameObject.SetActive(true);

        _openButton.transform.SetAsLastSibling();
    }

    public void ForceClosePanel()
    {
        if (_panel != null)
            _panel.SetActive(false);

        if (_openButton != null)
        {
            _openButton.interactable = true;
            _openButton.gameObject.SetActive(_isGameplayActive);
        }

        if (pauseGameWhileOpen)
            Time.timeScale = 1f;

        _isOpen = false;
    }

    void BuildOpenButton()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject btnGO = CreateUIObject("InGameSettingsButton", _uiRoot);
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-36f, -28f);
        rt.sizeDelta = new Vector2(190f, 56f);

        Image bg = btnGO.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.12f, 0.18f, 0.82f);
        Outline border = btnGO.AddComponent<Outline>();
        border.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.55f);
        border.effectDistance = new Vector2(2f, -2f);

        _openButton = btnGO.AddComponent<Button>();
        _openButton.targetGraphic = bg;
        _openButton.transition = Selectable.Transition.None;
        _openButton.onClick.AddListener(OpenPanel);

        Text label = CreateText("Label", btnGO.transform, "SETTING", font, 22, FontStyle.BoldAndItalic, Color.white);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        label.alignment = TextAnchor.MiddleCenter;

        Shadow glow = label.gameObject.AddComponent<Shadow>();
        glow.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.5f);
        glow.effectDistance = new Vector2(2f, -2f);
    }

    void BuildPanel()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _panel = CreateUIObject("InGameSettingsPanelRuntime", _uiRoot);
        RectTransform prt = _panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        Image blocker = _panel.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.62f);

        Button blockerBtn = _panel.AddComponent<Button>();
        blockerBtn.transition = Selectable.Transition.None;
        blockerBtn.onClick.AddListener(ClosePanel);

        GameObject window = CreateUIObject("Window", _panel.transform);
        RectTransform wrt = window.GetComponent<RectTransform>();
        wrt.anchorMin = new Vector2(0.5f, 0.5f);
        wrt.anchorMax = new Vector2(0.5f, 0.5f);
        wrt.pivot = new Vector2(0.5f, 0.5f);
        wrt.anchoredPosition = Vector2.zero;
        wrt.sizeDelta = new Vector2(760f, 460f);

        Image winBg = window.AddComponent<Image>();
        winBg.color = new Color(0.05f, 0.08f, 0.14f, 0.95f);
        Outline outline = window.AddComponent<Outline>();
        outline.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.4f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button windowBlock = window.AddComponent<Button>();
        windowBlock.transition = Selectable.Transition.None;
        windowBlock.onClick.AddListener(() => { });

        Text title = CreateText("Title", window.transform, "SETTING", font, 40, FontStyle.BoldAndItalic, Color.white);
        RectTransform trt = title.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 1f);
        trt.anchorMax = new Vector2(0.5f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0, -28f);
        trt.sizeDelta = new Vector2(440, 56);
        title.alignment = TextAnchor.MiddleCenter;

        GameObject titleLine = CreateUIObject("TitleLine", window.transform);
        RectTransform lineRT = titleLine.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.08f, 1f);
        lineRT.anchorMax = new Vector2(0.92f, 1f);
        lineRT.pivot = new Vector2(0.5f, 1f);
        lineRT.anchoredPosition = new Vector2(0f, -86f);
        lineRT.sizeDelta = new Vector2(0f, 2f);
        Image lineImg = titleLine.AddComponent<Image>();
        lineImg.color = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.55f);

        Slider bgm = CreateSlider(window.transform, "BGM", new Vector2(0, -130f), font);
        Slider se = CreateSlider(window.transform, "SE", new Vector2(0, -215f), font);

        if (bgm != null)
        {
            bgm.minValue = 0f;
            bgm.maxValue = 1f;
            bgm.value = AudioManager.instance != null ? AudioManager.instance.bgmVolume : 0.5f;
            bgm.onValueChanged.AddListener(v => { if (AudioManager.instance != null) AudioManager.instance.SetBgmVolume(v); });
        }

        if (se != null)
        {
            se.minValue = 0f;
            se.maxValue = 1f;
            se.value = AudioManager.instance != null ? AudioManager.instance.seVolume : 0.5f;
            se.onValueChanged.AddListener(v => { if (AudioManager.instance != null) AudioManager.instance.SetSeVolume(v); });
        }

        Button retryBtn = CreateTextButton(window.transform, "Retry", "RETRY", font);
        RectTransform rrt = retryBtn.transform as RectTransform;
        rrt.anchorMin = new Vector2(0.5f, 0f);
        rrt.anchorMax = new Vector2(0.5f, 0f);
        rrt.pivot = new Vector2(1f, 0f);
        rrt.anchoredPosition = new Vector2(-16f, 20f);
        rrt.sizeDelta = new Vector2(240, 60);
        retryBtn.onClick.AddListener(() =>
        {
            if (pauseGameWhileOpen) Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });

        Button titleBtn = CreateTextButton(window.transform, "Title", "TITLE", font);
        RectTransform tr = titleBtn.transform as RectTransform;
        tr.anchorMin = new Vector2(0.5f, 0f);
        tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot = new Vector2(0f, 0f);
        tr.anchoredPosition = new Vector2(16f, 20f);
        tr.sizeDelta = new Vector2(240, 60);
        titleBtn.onClick.AddListener(() =>
        {
            if (pauseGameWhileOpen) Time.timeScale = 1f;
            SceneManager.LoadScene("Title");
        });

        _panel.SetActive(false);
    }

    Slider CreateSlider(Transform parent, string name, Vector2 pos, Font font)
    {
        GameObject row = CreateUIObject(name + "Row", parent);
        RectTransform rrt = row.GetComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.5f, 1f);
        rrt.anchorMax = new Vector2(0.5f, 1f);
        rrt.pivot = new Vector2(0.5f, 1f);
        rrt.anchoredPosition = pos;
        rrt.sizeDelta = new Vector2(560f, 70f);

        Text label = CreateText(name + "Label", row.transform, name, font, 26, FontStyle.Bold, Color.white);
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0f, 0.5f);
        lrt.anchorMax = new Vector2(0f, 0.5f);
        lrt.pivot = new Vector2(0f, 0.5f);
        lrt.anchoredPosition = Vector2.zero;
        lrt.sizeDelta = new Vector2(110f, 50f);
        label.alignment = TextAnchor.MiddleLeft;

        GameObject sliderGO = CreateUIObject(name + "Slider", row.transform);
        RectTransform srt = sliderGO.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(1f, 0.5f);
        srt.anchorMax = new Vector2(1f, 0.5f);
        srt.pivot = new Vector2(1f, 0.5f);
        srt.anchoredPosition = Vector2.zero;
        srt.sizeDelta = new Vector2(420f, 28f);

        Slider slider = sliderGO.AddComponent<Slider>();

        GameObject bg = CreateUIObject("Background", sliderGO.transform);
        RectTransform bgrt = bg.GetComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.16f);

        GameObject fillArea = CreateUIObject("Fill Area", sliderGO.transform);
        RectTransform fart = fillArea.GetComponent<RectTransform>();
        fart.anchorMin = Vector2.zero;
        fart.anchorMax = Vector2.one;
        fart.offsetMin = new Vector2(10, 7);
        fart.offsetMax = new Vector2(-10, -7);

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        RectTransform frt = fill.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.8f);

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderGO.transform);
        RectTransform hart = handleArea.GetComponent<RectTransform>();
        hart.anchorMin = Vector2.zero;
        hart.anchorMax = Vector2.one;
        hart.offsetMin = new Vector2(10, 0);
        hart.offsetMax = new Vector2(-10, 0);

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform hrt = handle.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0.5f, 0.5f);
        hrt.anchorMax = new Vector2(0.5f, 0.5f);
        hrt.pivot = new Vector2(0.5f, 0.5f);
        hrt.sizeDelta = new Vector2(22f, 36f);
        Image hImg = handle.AddComponent<Image>();
        hImg.color = Color.white;

        slider.targetGraphic = hImg;
        slider.fillRect = frt;
        slider.handleRect = hrt;

        return slider;
    }

    Button CreateTextButton(Transform parent, string name, string label, Font font)
    {
        GameObject go = CreateUIObject(name, parent);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.13f, 0.2f, 0.85f);
        Outline o = go.AddComponent<Outline>();
        o.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.5f);
        o.effectDistance = new Vector2(2f, -2f);

        Button b = go.AddComponent<Button>();
        b.targetGraphic = bg;
        b.transition = Selectable.Transition.None;

        Text t = CreateText("Label", go.transform, label, font, 22, FontStyle.BoldAndItalic, Color.white);
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        t.alignment = TextAnchor.MiddleCenter;

        return b;
    }

    void OpenPanel()
    {
        if (_panel == null) return;

        _openButton.interactable = false;
        _openButton.gameObject.SetActive(false);

        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();

        if (pauseGameWhileOpen) Time.timeScale = 0f;

        _isOpen = true;
    }

    void ClosePanel()
    {
        ForceClosePanel();
    }

    void LateUpdate()
    {
        if (_isOpen && _panel != null && _panel.activeSelf)
            _panel.transform.SetAsLastSibling();
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    Text CreateText(string name, Transform parent, string text, Font font, int size, FontStyle style, Color color)
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
}
