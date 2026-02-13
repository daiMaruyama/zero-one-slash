using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// パネルにタイトルテキスト＋赤ラインを自動追加する汎用ビルダー
/// SETTING / HOW TO PLAY パネルで使用
/// （RANKINGは RankingPanelBuilder が独自に生成済み）
/// </summary>
[DefaultExecutionOrder(-5)]
public class PanelHeaderBuilder : MonoBehaviour
{
    [Header("必須設定")]
    [SerializeField] string titleLabel = "TITLE";
    [SerializeField] Font menuFont;
    [SerializeField] SettingsPanelAnimator targetPanel;

    [Header("色")]
    [SerializeField] Color neonRed = new Color(1f, 0.196f, 0.137f);

    void Awake()
    {
        if (targetPanel == null) return;
        Build();
    }

    void Build()
    {
        Transform panelTransform = targetPanel.transform;
        Font font = menuFont != null ? menuFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // パネル背景（固定サイズのImage子オブジェクト）を探す
        Transform contentParent = FindPanelBackground(panelTransform);

        // ===== ヘッダーエリア（RankingPanelBuilderのcontentGOと同じ余白） =====
        GameObject headerGO = CreateUIObject("HeaderArea", contentParent);
        RectTransform headerRT = headerGO.GetComponent<RectTransform>();
        headerRT.anchorMin = Vector2.zero;
        headerRT.anchorMax = Vector2.one;
        headerRT.offsetMin = new Vector2(60, 40);
        headerRT.offsetMax = new Vector2(-60, -70);

        // ===== タイトルテキスト（RankingPanelBuilderと完全一致） =====
        GameObject titleGO = CreateUIObject("PanelTitle", headerGO.transform);
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.pivot = new Vector2(0.5f, 1);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(0, 60);

        Text titleText = titleGO.AddComponent<Text>();
        titleText.text = titleLabel;
        titleText.font = font;
        titleText.fontSize = 42;
        titleText.fontStyle = FontStyle.BoldAndItalic;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.raycastTarget = false;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;

        // ===== タイトル下の赤ライン（RankingPanelBuilderと完全一致） =====
        GameObject lineGO = CreateUIObject("TitleLine", headerGO.transform);
        RectTransform lineRT = lineGO.GetComponent<RectTransform>();
        lineRT.anchorMin = new Vector2(0.05f, 1);
        lineRT.anchorMax = new Vector2(0.95f, 1);
        lineRT.pivot = new Vector2(0.5f, 1);
        lineRT.anchoredPosition = new Vector2(0, -64);
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

    /// <summary>
    /// パネル内の背景Image（固定サイズのコンテンツ領域）を探す
    /// Window_XXX自体は画面全体stretchなので、その中の
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
