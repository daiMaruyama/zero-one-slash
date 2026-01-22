using UnityEngine;
using UnityEngine.UI;

public class NewRecordPanelAutoBuilder : MonoBehaviour
{
    [Header("生成先（空ならこのTransform配下）")]
    [SerializeField] Transform _parent;

    [Header("見た目の基準にするText（空ならArial）")]
    [SerializeField] Text _referenceText;

    [Header("パネル位置（上に寄せたいならYを増やす）")]
    [SerializeField] Vector2 _panelAnchoredPos = new Vector2(0, 120);

    [Header("パネルサイズ")]
    [SerializeField] Vector2 _panelSize = new Vector2(980, 540);

    [Header("パネル傾き")]
    [SerializeField] float _panelRotationZ = -8f;

    [Header("ネオン色")]
    [SerializeField] Color _frameColor = new Color(1f, 0.2f, 1f, 1f); // ピンク寄り
    [SerializeField] Color _bgColor = new Color(0.05f, 0.0f, 0.08f, 0.85f);

    [Header("InputField色")]
    [SerializeField] Color _inputBg = new Color(1f, 0.25f, 0.8f, 0.35f);
    [SerializeField] Color _inputText = Color.white;
    [SerializeField] Color _placeholderText = new Color(1f, 1f, 1f, 0.45f);

    [Header("ボタン色")]
    [SerializeField] Color _buttonOk = new Color(0.1f, 1f, 1f, 0.35f);
    [SerializeField] Color _buttonSkip = new Color(1f, 0.2f, 1f, 0.30f);

    [ContextMenu("Generate NewRecord Panel")]
    public void Generate()
    {
        if (_parent == null) _parent = transform;

        Transform existing = _parent.Find("Window_NewRecord");
        if (existing != null)
        {
            Debug.LogWarning("[AutoBuilder] Window_NewRecord already exists.");
            return;
        }

        Font font = GetFont();

        // Root
        GameObject window = CreateUIObject("Window_NewRecord", _parent);
        RectTransform windowRt = window.GetComponent<RectTransform>();
        windowRt.anchorMin = new Vector2(0.5f, 0.5f);
        windowRt.anchorMax = new Vector2(0.5f, 0.5f);
        windowRt.pivot = new Vector2(0.5f, 0.5f);
        windowRt.anchoredPosition = Vector2.zero;
        windowRt.sizeDelta = Vector2.zero;

        CanvasGroup windowCg = window.AddComponent<CanvasGroup>();
        windowCg.alpha = 0f;
        windowCg.interactable = false;
        windowCg.blocksRaycasts = false;

        // PanelRoot
        GameObject panelRoot = CreateUIObject("PanelRoot", window.transform);
        RectTransform panelRt = panelRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = _panelSize;
        panelRt.anchoredPosition = _panelAnchoredPos;
        panelRt.localRotation = Quaternion.Euler(0, 0, _panelRotationZ);

        // BG
        Image bg = panelRoot.AddComponent<Image>();
        bg.color = _bgColor;

        // Frame (疑似ネオン：Outlineで縁を太く)
        Outline frameOutline = panelRoot.AddComponent<Outline>();
        frameOutline.effectColor = new Color(_frameColor.r, _frameColor.g, _frameColor.b, 0.85f);
        frameOutline.effectDistance = new Vector2(6f, -6f);

        // Title
        Text titleText = CreateText("TitleText", panelRoot.transform, "NEW RECORD", font, 52, FontStyle.Bold, _frameColor);
        RectTransform titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(40, -28);
        titleRt.sizeDelta = new Vector2(600, 80);

        AddGlow(titleText, new Color(_frameColor.r, _frameColor.g, _frameColor.b, 0.6f), 2);

        // Score
        Text scoreText = CreateText("ScoreText", panelRoot.transform, "SCORE: 0", font, 68, FontStyle.Bold, Color.white);
        RectTransform scoreRt = scoreText.rectTransform;
        scoreRt.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRt.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRt.pivot = new Vector2(0.5f, 0.5f);
        scoreRt.anchoredPosition = new Vector2(0, 70);
        scoreRt.sizeDelta = new Vector2(900, 90);
        scoreText.alignment = TextAnchor.MiddleCenter;
        AddGlow(scoreText, new Color(1f, 0.4f, 0.85f, 0.55f), 3);

        // Message
        Text messageText = CreateText("MessageText", panelRoot.transform, "ENTER YOUR NAME", font, 26, FontStyle.Bold, new Color(1f, 1f, 1f, 0.75f));
        RectTransform msgRt = messageText.rectTransform;
        msgRt.anchorMin = new Vector2(0.5f, 0.5f);
        msgRt.anchorMax = new Vector2(0.5f, 0.5f);
        msgRt.pivot = new Vector2(0.5f, 0.5f);
        msgRt.anchoredPosition = new Vector2(0, -10);
        msgRt.sizeDelta = new Vector2(900, 40);
        messageText.alignment = TextAnchor.MiddleCenter;

        // InputField (Legacy)
        InputField inputField = CreateLegacyInputField(panelRoot.transform, font);
        RectTransform inputRt = inputField.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0.5f, 0.5f);
        inputRt.anchorMax = new Vector2(0.5f, 0.5f);
        inputRt.pivot = new Vector2(0.5f, 0.5f);
        inputRt.anchoredPosition = new Vector2(0, -70);
        inputRt.sizeDelta = new Vector2(640, 78);

        // Buttons
        Button skipButton = CreateButton(panelRoot.transform, font, "SKIP", _buttonSkip);
        RectTransform skipRt = skipButton.GetComponent<RectTransform>();
        skipRt.anchorMin = new Vector2(0.5f, 0.5f);
        skipRt.anchorMax = new Vector2(0.5f, 0.5f);
        skipRt.pivot = new Vector2(0.5f, 0.5f);
        skipRt.anchoredPosition = new Vector2(-180, -170);
        skipRt.sizeDelta = new Vector2(220, 72);

        Button okButton = CreateButton(panelRoot.transform, font, "OK", _buttonOk);
        RectTransform okRt = okButton.GetComponent<RectTransform>();
        okRt.anchorMin = new Vector2(0.5f, 0.5f);
        okRt.anchorMax = new Vector2(0.5f, 0.5f);
        okRt.pivot = new Vector2(0.5f, 0.5f);
        okRt.anchoredPosition = new Vector2(180, -170);
        okRt.sizeDelta = new Vector2(220, 72);

        // Controller bind
        //NewRecordPanelController controller = window.AddComponent<NewRecordPanelController>();
        //controller.Bind(windowCg, panelRt, titleText, scoreText, messageText, inputField, okButton, skipButton);

        // 最前面に
        window.transform.SetAsLastSibling();

        Debug.Log("[AutoBuilder] Window_NewRecord generated.");
    }

    Font GetFont()
    {
        if (_referenceText != null && _referenceText.font != null) return _referenceText.font;
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
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
        t.alignment = TextAnchor.MiddleLeft;
        return t;
    }

    void AddGlow(Text text, Color glowColor, int strength)
    {
        if (text == null) return;

        Shadow sh = text.gameObject.AddComponent<Shadow>();
        sh.effectColor = glowColor;
        sh.effectDistance = new Vector2(strength, -strength);

        Outline ol = text.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * 0.75f);
        ol.effectDistance = new Vector2(strength, -strength);
    }

    InputField CreateLegacyInputField(Transform parent, Font font)
    {
        GameObject root = CreateUIObject("NameInput", parent);

        Image bg = root.AddComponent<Image>();
        bg.color = _inputBg;

        Outline ol = root.AddComponent<Outline>();
        ol.effectColor = new Color(_frameColor.r, _frameColor.g, _frameColor.b, 0.55f);
        ol.effectDistance = new Vector2(3f, -3f);

        InputField input = root.AddComponent<InputField>();
        input.contentType = InputField.ContentType.Standard;
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = 12;

        // Text
        Text text = CreateText("Text", root.transform, "", font, 34, FontStyle.Bold, _inputText);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;

        RectTransform textRt = text.rectTransform;
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.offsetMin = new Vector2(24, 10);
        textRt.offsetMax = new Vector2(-24, -10);

        // Placeholder
        Text placeholder = CreateText("Placeholder", root.transform, "ENTER NAME", font, 34, FontStyle.Italic, _placeholderText);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.raycastTarget = false;

        RectTransform phRt = placeholder.rectTransform;
        phRt.anchorMin = new Vector2(0f, 0f);
        phRt.anchorMax = new Vector2(1f, 1f);
        phRt.pivot = new Vector2(0.5f, 0.5f);
        phRt.offsetMin = new Vector2(24, 10);
        phRt.offsetMax = new Vector2(-24, -10);

        input.textComponent = text;
        input.placeholder = placeholder;

        return input;
    }

    Button CreateButton(Transform parent, Font font, string label, Color bgColor)
    {
        GameObject root = CreateUIObject("Button_" + label, parent);

        Image img = root.AddComponent<Image>();
        img.color = bgColor;

        Outline ol = root.AddComponent<Outline>();
        ol.effectColor = new Color(_frameColor.r, _frameColor.g, _frameColor.b, 0.5f);
        ol.effectDistance = new Vector2(3f, -3f);

        Button button = root.AddComponent<Button>();
        button.targetGraphic = img;

        Text t = CreateText("Label", root.transform, label, font, 34, FontStyle.Bold, Color.white);
        t.alignment = TextAnchor.MiddleCenter;

        RectTransform tRt = t.rectTransform;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;

        AddGlow(t, new Color(1f, 0.45f, 0.9f, 0.5f), 2);

        return button;
    }
}
