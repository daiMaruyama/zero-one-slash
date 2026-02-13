using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の4メニューボタンを自動生成（2x2グリッド）
/// GAME START / HOW TO PLAY / RANKING / SETTING
/// </summary>
[DefaultExecutionOrder(-10)]
public class TitleMenuBuilder : MonoBehaviour
{
    [Header("必須設定")]
    [SerializeField] Font menuFont;
    [SerializeField] TitleUIManager titleUIManager;

    [Header("レイアウト（2x2グリッド）")]
    [SerializeField] float gridWidth = 860f;
    [SerializeField] float gridHeight = 260f;
    [SerializeField] float spacingX = 20f;
    [SerializeField] float spacingY = 14f;

    [Header("色")]
    [SerializeField] Color neonRed = new Color(1f, 0.196f, 0.137f);

    struct MenuDef
    {
        public string label;
        public string jp;
        public bool primary;
    }

    static readonly MenuDef[] MENU_ITEMS = new MenuDef[]
    {
        new MenuDef { label = "GAME START",   jp = "ゲームスタート", primary = true },
        new MenuDef { label = "HOW TO PLAY",  jp = "あそびかた",     primary = false },
        new MenuDef { label = "RANKING",      jp = "ランキング",     primary = false },
        new MenuDef { label = "SETTING",      jp = "せってい",       primary = false },
    };

    void Awake()
    {
        Build();
    }

    void Build()
    {
        // ===== コンテナ（画面下部中央） =====
        GameObject containerGO = new GameObject("MenuContainer");
        containerGO.transform.SetParent(transform, false);

        RectTransform containerRT = containerGO.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0);
        containerRT.anchorMax = new Vector2(0.5f, 0);
        containerRT.pivot = new Vector2(0.5f, 0);
        containerRT.anchoredPosition = new Vector2(0, 50f);
        containerRT.sizeDelta = new Vector2(gridWidth, gridHeight);

        CanvasGroup containerCG = containerGO.AddComponent<CanvasGroup>();

        // ===== 2x2 グリッド配置 =====
        float cellW = (gridWidth - spacingX) / 2f;
        float cellH = (gridHeight - spacingY) / 2f;

        Vector2[] positions = new Vector2[]
        {
            new Vector2(0, cellH + spacingY),               // 左上
            new Vector2(cellW + spacingX, cellH + spacingY), // 右上
            new Vector2(0, 0),                                // 左下
            new Vector2(cellW + spacingX, 0),                 // 右下
        };

        Button[] buttons = new Button[4];
        NeonMenuButton[] neonButtons = new NeonMenuButton[4];

        for (int i = 0; i < MENU_ITEMS.Length; i++)
        {
            GameObject btnGO = CreateButton(containerGO.transform, MENU_ITEMS[i], i, positions[i], cellW, cellH);
            buttons[i] = btnGO.GetComponent<Button>();
            neonButtons[i] = btnGO.GetComponent<NeonMenuButton>();
        }

        // ===== TitleUIManagerにセット =====
        if (titleUIManager != null)
        {
            titleUIManager.SetupMenu(
                containerCG,
                buttons[0], buttons[1], buttons[2], buttons[3],
                neonButtons
            );
        }
    }

    GameObject CreateButton(Transform parent, MenuDef def, int index, Vector2 pos, float width, float height)
    {
        // ===== ルート =====
        GameObject btnGO = new GameObject("Btn_" + def.label.Replace(" ", ""));
        btnGO.transform.SetParent(parent, false);

        RectTransform btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0, 0);
        btnRT.anchorMax = new Vector2(0, 0);
        btnRT.pivot = new Vector2(0, 0);
        btnRT.anchoredPosition = pos;
        btnRT.sizeDelta = new Vector2(width, height);

        // Image first so Button can find it as targetGraphic
        Image raycastImage = btnGO.AddComponent<Image>();
        raycastImage.color = Color.clear;

        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = raycastImage;
        btn.transition = Selectable.Transition.None;
        var nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;

        CanvasGroup cg = btnGO.AddComponent<CanvasGroup>();
        NeonMenuButton neon = btnGO.AddComponent<NeonMenuButton>();

        // ===== 背景（ダーク半透明） =====
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(btnGO.transform, false);
        StretchFill(bgGO);

        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.04f);
        bgImage.raycastTarget = false;
        bgGO.AddComponent<SkewRect>();

        // ===== 左ボーダー（赤ネオン） =====
        GameObject borderGO = new GameObject("LeftBorder");
        borderGO.transform.SetParent(btnGO.transform, false);

        RectTransform borderRT = borderGO.AddComponent<RectTransform>();
        borderRT.anchorMin = new Vector2(0, 0);
        borderRT.anchorMax = new Vector2(0, 1);
        borderRT.pivot = new Vector2(0, 0.5f);
        borderRT.anchoredPosition = Vector2.zero;
        borderRT.sizeDelta = new Vector2(3f, 0);

        Image borderImage = borderGO.AddComponent<Image>();
        borderImage.color = new Color(neonRed.r, neonRed.g, neonRed.b, def.primary ? 0.9f : 0.2f);
        borderImage.raycastTarget = false;
        borderGO.AddComponent<SkewRect>();

        // ===== テキストグループ =====
        GameObject textGroupGO = new GameObject("TextGroup");
        textGroupGO.transform.SetParent(btnGO.transform, false);

        RectTransform textGroupRT = textGroupGO.AddComponent<RectTransform>();
        textGroupRT.anchorMin = Vector2.zero;
        textGroupRT.anchorMax = Vector2.one;
        textGroupRT.offsetMin = new Vector2(20, 6);
        textGroupRT.offsetMax = new Vector2(-10, -4);

        // ===== ラベル（全ボタン同じサイズ） =====
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(textGroupGO.transform, false);

        RectTransform labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.25f);
        labelRT.anchorMax = new Vector2(1, 1);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        Text labelText = labelGO.AddComponent<Text>();
        labelText.text = def.label;
        labelText.font = menuFont != null ? menuFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 26;
        labelText.fontStyle = FontStyle.BoldAndItalic;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        labelText.color = def.primary ? Color.white : new Color(1, 1, 1, 0.55f);
        labelText.raycastTarget = false;

        // ===== サブテキスト（全ボタン表示） =====
        GameObject subGO = new GameObject("Sub");
        subGO.transform.SetParent(textGroupGO.transform, false);

        RectTransform subRT = subGO.AddComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 0);
        subRT.anchorMax = new Vector2(1, 0.3f);
        subRT.offsetMin = Vector2.zero;
        subRT.offsetMax = Vector2.zero;

        Text subText = subGO.AddComponent<Text>();
        subText.text = def.jp;
        subText.font = menuFont != null ? menuFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = 13;
        subText.alignment = TextAnchor.UpperLeft;
        subText.horizontalOverflow = HorizontalWrapMode.Overflow;
        subText.verticalOverflow = VerticalWrapMode.Overflow;
        subText.color = def.primary
            ? new Color(1f, 0.706f, 0.627f, 0.35f)
            : new Color(1f, 0.706f, 0.627f, 0.15f);
        subText.raycastTarget = false;

        // ===== NeonMenuButton設定 =====
        SetNeonFields(neon, def.primary, index, bgImage, borderImage, labelText, subText);
        neon.Initialize();

        return btnGO;
    }

    void StretchFill(GameObject go)
    {
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void SetNeonFields(NeonMenuButton neon, bool primary, int index,
        Image bg, Image border, Text label, Text sub)
    {
        var type = typeof(NeonMenuButton);
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        type.GetField("isPrimary", flags)?.SetValue(neon, primary);
        type.GetField("entranceIndex", flags)?.SetValue(neon, index);
        type.GetField("background", flags)?.SetValue(neon, bg);
        type.GetField("leftBorder", flags)?.SetValue(neon, border);
        type.GetField("labelText", flags)?.SetValue(neon, label);
        type.GetField("subText", flags)?.SetValue(neon, sub);
        type.GetField("neonRed", flags)?.SetValue(neon, neonRed);
    }
}
