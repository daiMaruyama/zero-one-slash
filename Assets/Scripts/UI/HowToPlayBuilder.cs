using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 遊び方パネルを複数ページスライド形式で自動生成
/// 各ページ: 左=説明テキスト / 右=スクショ画像
/// タイトルに「HOW TO PLAY  1/4」ページ表示
/// ナビボタンはNeonMenuButton風ホバー付き
/// </summary>
[DefaultExecutionOrder(-5)]
public class HowToPlayBuilder : MonoBehaviour
{
    [Header("必須設定")]
    [SerializeField] Font menuFont;
    [SerializeField] SettingsPanelAnimator howToPlayPanel;

    [Header("スクリーンショット（ページ順）")]
    [SerializeField] Sprite imgRule;
    [SerializeField] Sprite imgScoring;
    [SerializeField] Sprite imgStreak;
    [SerializeField] Sprite imgFail;

    [Header("色")]
    [SerializeField] Color neonRed = new Color(1f, 0.196f, 0.137f);

    [Header("フォントサイズ")]
    [SerializeField] int titleFontSize = 36;
    [SerializeField] int headerFontSize = 22;
    [SerializeField] int bodyFontSize = 26;
    [SerializeField] int navFontSize = 16;

    [Header("レイアウト")]
    [SerializeField] float imageRatio = 0.44f;
    [SerializeField] float bodyLineSpacing = 1.2f;

    struct PageDef
    {
        public string header;
        public string body;
    }

    void Awake()
    {
        if (howToPlayPanel == null) return;
        Build();
    }

    void Build()
    {
        Transform panelTransform = howToPlayPanel.transform;
        Font font = menuFont != null ? menuFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform contentParent = FindPanelBackground(panelTransform);

        RectTransform bgRT = contentParent as RectTransform;
        float panelWidth = bgRT != null ? bgRT.sizeDelta.x : 900f;

        float padH = 50f;
        float padTop = 56f;
        float padBottom = 30f;
        float pageWidth = panelWidth - padH * 2;

        // ===== コンテンツルート =====
        GameObject contentGO = CreateUIObject("HowToPlayContent", contentParent);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(padH, padBottom);
        contentRT.offsetMax = new Vector2(-padH, -padTop);

        // ===== タイトル「HOW TO PLAY  1/4」 =====
        Text titleText = CreateText("Title", contentGO.transform, "HOW TO PLAY  1/4", font, titleFontSize,
            FontStyle.BoldAndItalic, Color.white);
        RectTransform titleRT = titleText.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(0, 44);
        titleText.alignment = TextAnchor.MiddleCenter;

        // 赤ライン
        CreateRedLine(contentGO.transform, -48f);

        // ===== ページ表示エリア =====
        float topArea = 58f;       // タイトル+ライン+余白
        float bottomArea = 42f;    // ナビ

        GameObject viewportGO = CreateUIObject("Viewport", contentGO.transform);
        RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(0, bottomArea);
        viewportRT.offsetMax = new Vector2(0, -topArea);

        Image viewportBg = viewportGO.AddComponent<Image>();
        viewportBg.color = Color.clear;
        viewportGO.AddComponent<RectMask2D>();

        // ===== ページ定義 =====
        Sprite[] sprites = { imgRule, imgScoring, imgStreak, imgFail };

        PageDef[] pageDefs =
        {
            new PageDef
            {
                header = "RULE",
                body = "ボードをタップして\nターゲットスコアを\n<b>3投以内</b>にゼロにしよう！"
            },
            new PageDef
            {
                header = "SCORING",
                body = "<b>MASTER OUT</b>\n  D / T / Bull で上がり\n  → <color=#FF3223>500 pts</color>\n\n" +
                       "<b>SINGLE OUT</b>\n  シングルのみで上がり\n  → <color=#00E5FF>100 pts</color>"
            },
            new PageDef
            {
                header = "STREAK",
                body = "ヒットでコンボが蓄積！\n\n" +
                       "GREAT (D / T / Bull)  <color=#FF3223>+2</color>\n" +
                       "SINGLE  <color=#00E5FF>+1</color>\n\n" +
                       "コンボが溜まると\n<b>制限時間が延長</b>！"
            },
            new PageDef
            {
                header = "MISS",
                body = "<color=#FF6666>BUST</color>       スコアオーバー\n\n" +
                       "<color=#FF6666>MISS</color>        エリア外タップ\n\n" +
                       "<color=#FF6666>NO OUT</color>    3投で届かない\n\n" +
                       "→ コンボリセット"
            },
        };

        int pageCount = pageDefs.Length;

        // ===== ページコンテナ =====
        GameObject pagesGO = CreateUIObject("PagesContainer", viewportGO.transform);
        RectTransform pagesRT = pagesGO.GetComponent<RectTransform>();
        pagesRT.anchorMin = new Vector2(0, 0);
        pagesRT.anchorMax = new Vector2(0, 1);
        pagesRT.pivot = new Vector2(0, 0.5f);
        pagesRT.anchoredPosition = Vector2.zero;
        pagesRT.sizeDelta = new Vector2(pageWidth * pageCount, 0);

        RectTransform[] pageRTs = new RectTransform[pageCount];
        for (int i = 0; i < pageCount; i++)
        {
            pageRTs[i] = BuildPage(pagesGO.transform, pageDefs[i], sprites[i], font, i, pageWidth);
        }

        // ===== ナビゲーション =====
        GameObject navGO = CreateUIObject("Navigation", contentGO.transform);
        RectTransform navRT = navGO.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0, 0);
        navRT.anchorMax = new Vector2(1, 0);
        navRT.pivot = new Vector2(0.5f, 0);
        navRT.anchoredPosition = Vector2.zero;
        navRT.sizeDelta = new Vector2(0, bottomArea);

        // 左ボタン（NeonMenuButton風）
        Button prevBtn = CreateNeonNavButton(navGO.transform, "PrevBtn", "PREV", "＜", font, true);
        // 右ボタン
        Button nextBtn = CreateNeonNavButton(navGO.transform, "NextBtn", "NEXT", "＞", font, false);

        // ドット
        Image[] dotImages = new Image[pageCount];
        float dotSize = 10f;
        float dotSpacing = 20f;
        float dotsWidth = pageCount * dotSpacing;

        GameObject dotsGO = CreateUIObject("Dots", navGO.transform);
        RectTransform dotsRT = dotsGO.GetComponent<RectTransform>();
        dotsRT.anchorMin = new Vector2(0.5f, 0.5f);
        dotsRT.anchorMax = new Vector2(0.5f, 0.5f);
        dotsRT.pivot = new Vector2(0.5f, 0.5f);
        dotsRT.anchoredPosition = Vector2.zero;
        dotsRT.sizeDelta = new Vector2(dotsWidth, dotSize);

        for (int i = 0; i < pageCount; i++)
        {
            GameObject dotGO = CreateUIObject("Dot_" + i, dotsGO.transform);
            RectTransform dotRT = dotGO.GetComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0, 0.5f);
            dotRT.anchorMax = new Vector2(0, 0.5f);
            dotRT.pivot = new Vector2(0.5f, 0.5f);
            dotRT.anchoredPosition = new Vector2(i * dotSpacing + dotSpacing * 0.5f, 0);
            dotRT.sizeDelta = new Vector2(dotSize, dotSize);

            Image dotImg = dotGO.AddComponent<Image>();
            dotImg.color = Color.white;
            dotImg.raycastTarget = false;
            dotImages[i] = dotImg;
        }

        // ===== コントローラー =====
        HowToPlayController ctrl = howToPlayPanel.gameObject.GetComponent<HowToPlayController>();
        if (ctrl == null)
            ctrl = howToPlayPanel.gameObject.AddComponent<HowToPlayController>();

        ctrl.Setup(pagesRT, pageRTs, dotImages, prevBtn, nextBtn, titleText, pageWidth, pageCount);
    }

    // ===== ページ生成（左テキスト / 右画像） =====

    RectTransform BuildPage(Transform parent, PageDef def, Sprite screenshot,
        Font font, int index, float width)
    {
        GameObject pageGO = CreateUIObject("Page_" + def.header, parent);
        RectTransform pageRT = pageGO.GetComponent<RectTransform>();
        pageRT.anchorMin = new Vector2(0, 0);
        pageRT.anchorMax = new Vector2(0, 1);
        pageRT.pivot = new Vector2(0, 0.5f);
        pageRT.anchoredPosition = new Vector2(index * width, 0);
        pageRT.sizeDelta = new Vector2(width, 0);

        // --- 右側: スクショ画像（幅42%） ---
        GameObject imgGO = CreateUIObject("Screenshot", pageGO.transform);
        RectTransform imgRT = imgGO.GetComponent<RectTransform>();
        imgRT.anchorMin = new Vector2(1f - imageRatio, 0);
        imgRT.anchorMax = new Vector2(1, 1);
        imgRT.offsetMin = new Vector2(4, 8);
        imgRT.offsetMax = new Vector2(-4, -8);

        Image imgComp = imgGO.AddComponent<Image>();
        if (screenshot != null)
        {
            imgComp.sprite = screenshot;
            imgComp.preserveAspect = true;
        }
        else
        {
            imgComp.color = new Color(1f, 1f, 1f, 0.05f);
        }
        imgComp.raycastTarget = false;

        // 画像に薄い枠
        Outline outline = imgGO.AddComponent<Outline>();
        outline.effectColor = new Color(neonRed.r, neonRed.g, neonRed.b, 0.2f);
        outline.effectDistance = new Vector2(1, -1);

        // --- 左側: テキストエリア（幅54%） ---

        // セクションヘッダー
        GameObject headerGO = CreateUIObject("HeaderBar", pageGO.transform);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1f - imageRatio - 0.02f, 1);
        headerRT.pivot = new Vector2(0, 1);
        headerRT.anchoredPosition = new Vector2(0, -20);
        headerRT.sizeDelta = new Vector2(0, 30);

        Image headerBg = headerGO.AddComponent<Image>();
        headerBg.color = new Color(neonRed.r, neonRed.g, neonRed.b, 0.12f);
        headerBg.raycastTarget = false;

        // 左アクセント
        GameObject accentGO = CreateUIObject("Accent", headerGO.transform);
        RectTransform accentRT = accentGO.GetComponent<RectTransform>();
        accentRT.anchorMin = new Vector2(0, 0);
        accentRT.anchorMax = new Vector2(0, 1);
        accentRT.pivot = new Vector2(0, 0.5f);
        accentRT.anchoredPosition = Vector2.zero;
        accentRT.sizeDelta = new Vector2(3, 0);
        Image accentImg = accentGO.AddComponent<Image>();
        accentImg.color = neonRed;
        accentImg.raycastTarget = false;

        // ヘッダーテキスト
        Text headerLabel = CreateText("HeaderLabel", headerGO.transform, "  " + def.header,
            font, headerFontSize, FontStyle.BoldAndItalic, neonRed);
        RectTransform headerLabelRT = headerLabel.rectTransform;
        headerLabelRT.anchorMin = Vector2.zero;
        headerLabelRT.anchorMax = Vector2.one;
        headerLabelRT.offsetMin = new Vector2(10, 0);
        headerLabelRT.offsetMax = Vector2.zero;
        headerLabel.alignment = TextAnchor.MiddleLeft;

        // 説明テキスト
        Text bodyText = CreateText("Body", pageGO.transform, def.body,
            font, bodyFontSize, FontStyle.Normal, new Color(1f, 1f, 1f, 0.9f));
        RectTransform bodyRT = bodyText.rectTransform;
        bodyRT.anchorMin = new Vector2(0, 0);
        bodyRT.anchorMax = new Vector2(1f - imageRatio - 0.02f, 1);
        bodyRT.pivot = new Vector2(0, 1);
        bodyRT.offsetMin = new Vector2(6, 0);
        bodyRT.offsetMax = new Vector2(0, -54);
        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bodyText.verticalOverflow = VerticalWrapMode.Overflow;
        bodyText.lineSpacing = bodyLineSpacing;
        bodyText.supportRichText = true;

        return pageRT;
    }

    // ===== NeonMenuButton風ナビボタン =====

    Button CreateNeonNavButton(Transform parent, string name, string label, string arrow,
        Font font, bool isLeft)
    {
        GameObject btnGO = CreateUIObject(name, parent);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();

        float btnWidth = 110f;
        float btnHeight = 36f;

        if (isLeft)
        {
            btnRT.anchorMin = new Vector2(0, 0.5f);
            btnRT.anchorMax = new Vector2(0, 0.5f);
            btnRT.pivot = new Vector2(0, 0.5f);
            btnRT.anchoredPosition = Vector2.zero;
        }
        else
        {
            btnRT.anchorMin = new Vector2(1, 0.5f);
            btnRT.anchorMax = new Vector2(1, 0.5f);
            btnRT.pivot = new Vector2(1, 0.5f);
            btnRT.anchoredPosition = Vector2.zero;
        }
        btnRT.sizeDelta = new Vector2(btnWidth, btnHeight);

        // 背景
        Image bgImg = btnGO.AddComponent<Image>();
        bgImg.color = new Color(1f, 1f, 1f, 0.04f);
        bgImg.raycastTarget = true;

        // 背景にSkew
        SkewRect skew = btnGO.AddComponent<SkewRect>();

        // 左ボーダー
        GameObject borderGO = CreateUIObject("LeftBorder", btnGO.transform);
        RectTransform borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0, 0);
        borderRT.anchorMax = new Vector2(0, 1);
        borderRT.pivot = new Vector2(0, 0.5f);
        borderRT.anchoredPosition = Vector2.zero;
        borderRT.sizeDelta = new Vector2(3, 0);
        Image borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(neonRed.r, neonRed.g, neonRed.b, 0.2f);
        borderImg.raycastTarget = false;
        borderGO.AddComponent<SkewRect>();

        // ラベル（矢印 + テキスト）
        string displayText = isLeft ? arrow + " " + label : label + " " + arrow;
        Text labelText = CreateText("Label", btnGO.transform, displayText, font, navFontSize,
            FontStyle.BoldAndItalic, new Color(1f, 1f, 1f, 0.55f));
        RectTransform labelRT = labelText.rectTransform;
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(12, 0);
        labelRT.offsetMax = new Vector2(-8, 0);
        labelText.alignment = isLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;

        // Button
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        btn.transition = Selectable.Transition.None;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        // NeonMenuButton風ホバー用コンポーネント
        NeonNavButton neon = btnGO.AddComponent<NeonNavButton>();
        neon.SetReferences(bgImg, borderImg, labelText, neonRed);

        return btn;
    }

    // ===== 赤ライン =====

    void CreateRedLine(Transform parent, float yPos)
    {
        GameObject lineGO = CreateUIObject("TitleLine", parent);
        RectTransform lineRT = lineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.05f, 1);
        lineRT.anchorMax = new Vector2(0.95f, 1);
        lineRT.pivot = new Vector2(0.5f, 1);
        lineRT.anchoredPosition = new Vector2(0, yPos);
        lineRT.sizeDelta = new Vector2(0, 2);
        Image lineImage = lineGO.AddComponent<Image>();
        lineImage.color = new Color(neonRed.r, neonRed.g, neonRed.b, 0.5f);
        lineImage.raycastTarget = false;
    }

    // ===== ユーティリティ =====

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

    Transform FindPanelBackground(Transform panelTransform)
    {
        for (int i = 0; i < panelTransform.childCount; i++)
        {
            Transform child = panelTransform.GetChild(i);
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                RectTransform rt = child as RectTransform;
                if (rt != null && rt.anchorMin == new Vector2(0.5f, 0.5f)
                              && rt.anchorMax == new Vector2(0.5f, 0.5f))
                    return child;
            }
        }
        return panelTransform;
    }
}