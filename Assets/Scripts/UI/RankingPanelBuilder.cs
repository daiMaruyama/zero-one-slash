using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ランキングパネルの中身を自動生成
/// タイトル + ヘッダー + 10行エントリーコンテナ + InfoText
/// RankingPanelController をセットアップする
/// </summary>
[DefaultExecutionOrder(-5)]
public class RankingPanelBuilder : MonoBehaviour
{
    [Header("必須設定")]
    [SerializeField] Font menuFont;
    [SerializeField] SettingsPanelAnimator rankingPanel;

    [Header("色")]
    [SerializeField] Color neonRed = new Color(1f, 0.196f, 0.137f);

    void Awake()
    {
        if (rankingPanel == null) return;
        Build();
    }

    void Build()
    {
        Transform panelTransform = rankingPanel.transform;
        Font font = menuFont != null ? menuFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // パネル背景（Image子オブジェクト）を探してその中に配置
        Transform contentParent = FindPanelBackground(panelTransform);

        // ===== コンテンツルート（パネル背景にストレッチ、余白付き） =====
        GameObject contentGO = CreateUIObject("RankingContent", contentParent);
        RectTransform contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(60, 40);
        contentRT.offsetMax = new Vector2(-60, -70);

        // ===== タイトル「RANKING」 =====
        Text titleText = CreateText("Title", contentGO.transform, "RANKING", font, 42, FontStyle.BoldAndItalic, Color.white);
        RectTransform titleRT = titleText.rectTransform;
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(0, 60);
        titleText.alignment = TextAnchor.MiddleCenter;

        // タイトル下の赤ライン
        GameObject lineGO = CreateUIObject("TitleLine", contentGO.transform);
        RectTransform lineRT = lineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.05f, 1);
        lineRT.anchorMax = new Vector2(0.95f, 1);
        lineRT.pivot = new Vector2(0.5f, 1);
        lineRT.anchoredPosition = new Vector2(0, -64);
        lineRT.sizeDelta = new Vector2(0, 2);
        Image lineImage = lineGO.AddComponent<Image>();
        lineImage.color = new Color(neonRed.r, neonRed.g, neonRed.b, 0.5f);
        lineImage.raycastTarget = false;

        // ===== ヘッダー行 =====
        GameObject headerGO = CreateUIObject("Header", contentGO.transform);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = new Vector2(0, -74);
        headerRT.sizeDelta = new Vector2(0, 36);

        Color headerColor = new Color(1f, 1f, 1f, 0.35f);

        Text hRank = CreateText("H_Rank", headerGO.transform, "#", font, 22, FontStyle.Bold, headerColor);
        SetAnchors(hRank.rectTransform, 0f, 0f, 0.12f, 1f);
        hRank.alignment = TextAnchor.MiddleCenter;

        Text hName = CreateText("H_Name", headerGO.transform, "NAME", font, 22, FontStyle.Bold, headerColor);
        SetAnchors(hName.rectTransform, 0.12f, 0f, 0.7f, 1f);
        hName.alignment = TextAnchor.MiddleLeft;

        Text hScore = CreateText("H_Score", headerGO.transform, "SCORE", font, 22, FontStyle.Bold, headerColor);
        SetAnchors(hScore.rectTransform, 0.7f, 0f, 0.95f, 1f);
        hScore.alignment = TextAnchor.MiddleRight;

        // ===== スクロールエリア（ヘッダー下〜パネル下部、マスクで切り抜き） =====
        GameObject scrollGO = CreateUIObject("ScrollArea", contentGO.transform);
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(0, 0);
        scrollRT.offsetMax = new Vector2(0, -115);

        Image scrollBg = scrollGO.AddComponent<Image>();
        scrollBg.color = Color.clear;

        scrollGO.AddComponent<RectMask2D>();

        ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // ===== エントリーコンテナ（ScrollRectのcontent） =====
        GameObject containerGO = CreateUIObject("EntryContainer", scrollGO.transform);
        RectTransform containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0, 1);
        containerRT.anchorMax = new Vector2(1, 1);
        containerRT.pivot = new Vector2(0.5f, 1);
        containerRT.anchoredPosition = Vector2.zero;
        containerRT.sizeDelta = new Vector2(0, 0);

        VerticalLayoutGroup vlg = containerGO.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 3f;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter csf = containerGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = containerRT;

        // ===== InfoText（LOADING / NO DATA 等） =====
        Text infoText = CreateText("InfoText", contentGO.transform, "", font, 28, FontStyle.Italic, new Color(1f, 1f, 1f, 0.5f));
        RectTransform infoRT = infoText.rectTransform;
        infoRT.anchorMin = new Vector2(0, 0.2f);
        infoRT.anchorMax = new Vector2(1, 0.7f);
        infoRT.offsetMin = Vector2.zero;
        infoRT.offsetMax = Vector2.zero;
        infoText.alignment = TextAnchor.MiddleCenter;

        // ===== エントリーテンプレート（panelTransform直下、非アクティブ） =====
        GameObject templateGO = CreateEntryTemplate(panelTransform, font);
        templateGO.SetActive(false);

        // ===== RankingPanelController セットアップ =====
        RankingPanelController controller = rankingPanel.GetComponentInChildren<RankingPanelController>(true);
        if (controller == null)
            controller = contentGO.AddComponent<RankingPanelController>();

        var type = typeof(RankingPanelController);
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        type.GetField("entryContainer", flags)?.SetValue(controller, containerGO.transform);
        type.GetField("entryPrefab", flags)?.SetValue(controller, templateGO);
        type.GetField("infoText", flags)?.SetValue(controller, infoText);
    }

    GameObject CreateEntryTemplate(Transform parent, Font font)
    {
        GameObject rowGO = CreateUIObject("EntryTemplate", parent);

        LayoutElement le = rowGO.AddComponent<LayoutElement>();
        le.preferredHeight = 44f;
        le.minHeight = 44f;

        // 背景
        Image rowBg = rowGO.AddComponent<Image>();
        rowBg.color = new Color(1f, 1f, 1f, 0.03f);
        rowBg.raycastTarget = false;

        // 順位テキスト
        Text rankText = CreateText("RankText", rowGO.transform, "1", font, 26, FontStyle.Bold, new Color(neonRed.r, neonRed.g, neonRed.b, 0.9f));
        SetAnchors(rankText.rectTransform, 0f, 0f, 0.12f, 1f);
        rankText.alignment = TextAnchor.MiddleCenter;

        // 名前テキスト
        Text nameText = CreateText("NameText", rowGO.transform, "Player", font, 24, FontStyle.Normal, new Color(1f, 1f, 1f, 0.9f));
        SetAnchors(nameText.rectTransform, 0.12f, 0f, 0.7f, 1f);
        nameText.alignment = TextAnchor.MiddleLeft;

        // スコアテキスト
        Text scoreText = CreateText("ScoreText", rowGO.transform, "0", font, 26, FontStyle.Bold, Color.white);
        SetAnchors(scoreText.rectTransform, 0.7f, 0f, 0.95f, 1f);
        scoreText.alignment = TextAnchor.MiddleRight;

        // RankingEntryRow コンポーネント
        RankingEntryRow row = rowGO.AddComponent<RankingEntryRow>();
        var type = typeof(RankingEntryRow);
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        type.GetField("rankText", flags)?.SetValue(row, rankText);
        type.GetField("nameText", flags)?.SetValue(row, nameText);
        type.GetField("scoreText", flags)?.SetValue(row, scoreText);

        return rowGO;
    }

    // ===== ユーティリティ =====

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

    void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// パネル内の背景Image（固定サイズのコンテンツ領域）を探す
    /// Window_Ranking自体は画面全体stretchなので、その中の
    /// 固定サイズImageを見つけてそこにコンテンツを配置する
    /// </summary>
    Transform FindPanelBackground(Transform panelTransform)
    {
        for (int i = 0; i < panelTransform.childCount; i++)
        {
            Transform child = panelTransform.GetChild(i);
            Image img = child.GetComponent<Image>();
            if (img != null)
            {
                RectTransform rt = child as RectTransform;
                if (rt != null && rt.anchorMin == new Vector2(0.5f, 0.5f) && rt.anchorMax == new Vector2(0.5f, 0.5f))
                    return child;
            }
        }
        return panelTransform;
    }
}
