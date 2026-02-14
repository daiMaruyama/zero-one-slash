using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

/// <summary>
/// NewRecordパネルにネオンレッドの統一スタイルを適用するランタイムビルダー
/// HowToPlayBuilder / RankingPanelBuilder と同パターン
/// [DefaultExecutionOrder(-5)] で NewRecordPanelController.Awake() より先に実行
///
/// 使い方:
///   NewRecordPanelController と同じ GameObject に AddComponent するだけ
///   （Inspector で targetController を手動設定しても可）
/// </summary>
[DefaultExecutionOrder(-5)]
public class NewRecordPanelBuilder : MonoBehaviour
{
    [SerializeField] NewRecordPanelController targetController;

    // ネオンレッドテーマ（他パネルと統一）
    static readonly Color NeonRed   = new Color(1f, 0.196f, 0.137f);
    static readonly Color DarkBg    = new Color(0.03f, 0.01f, 0.05f, 0.92f);

    void Awake()
    {
        Apply();
    }

    void Apply()
    {
        if (targetController == null)
            targetController = GetComponentInChildren<NewRecordPanelController>(true);
        if (targetController == null)
        {
            Debug.LogWarning("[NewRecordPanelBuilder] NewRecordPanelController not found.");
            return;
        }

        // Reflection で Controller の SerializeField を取得
        var type  = typeof(NewRecordPanelController);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;

        var panelRoot    = type.GetField("panelRoot",    flags)?.GetValue(targetController) as RectTransform;
        var titleText    = type.GetField("titleText",    flags)?.GetValue(targetController) as Text;
        var scoreText    = type.GetField("scoreText",    flags)?.GetValue(targetController) as Text;
        var statusText   = type.GetField("statusText",   flags)?.GetValue(targetController) as Text;
        var nameInput    = type.GetField("nameInput",    flags)?.GetValue(targetController) as InputField;
        var submitButton = type.GetField("submitButton", flags)?.GetValue(targetController) as Button;
        var skipButton   = type.GetField("skipButton",   flags)?.GetValue(targetController) as Button;

        // === Panel Background & Frame ===
        if (panelRoot != null)
        {
            // 背景色
            var bg = panelRoot.GetComponent<Image>();
            if (bg != null) bg.color = DarkBg;

            // フレーム Outline
            var outline = panelRoot.GetComponent<Outline>();
            if (outline == null) outline = panelRoot.gameObject.AddComponent<Outline>();
            outline.effectColor    = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.85f);
            outline.effectDistance = new Vector2(6f, -6f);

            // 上端ネオングローライン（TitleController / GameStarter と同演出）
            CreateNeonGlow(panelRoot, NeonRed, new Vector2(0.5f, 1f));

            // 下端ネオングローライン
            CreateNeonGlow(panelRoot, NeonRed, new Vector2(0.5f, 0f));

            // スキャンライン（サイバー感）
            float panelH = panelRoot.sizeDelta.y;
            if (panelH > 0f)
                CreateScanLines(panelRoot, panelH, 16);

            // アクセント区切り線（タイトル下）
            CreateAccentLine(panelRoot, 110f);
        }

        // === Title "NEW RECORD!!" ===
        if (titleText != null)
        {
            titleText.color     = NeonRed;
            titleText.fontStyle = FontStyle.Bold;
            EnsureGlow(titleText.gameObject, NeonRed, 0.6f, 2);
        }

        // === Score ===
        if (scoreText != null)
        {
            scoreText.color     = Color.white;
            scoreText.fontStyle = FontStyle.Bold;
            EnsureGlow(scoreText.gameObject, NeonRed, 0.55f, 3);
        }

        // === Status / Message ===
        if (statusText != null)
        {
            statusText.color     = new Color(1f, 1f, 1f, 0.75f);
            statusText.fontStyle = FontStyle.Bold;
        }

        // === InputField ===
        if (nameInput != null)
        {
            // 背景
            var inputBg = nameInput.GetComponent<Image>();
            if (inputBg != null)
                inputBg.color = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.15f);

            // Outline
            var inputOl = nameInput.GetComponent<Outline>();
            if (inputOl == null) inputOl = nameInput.gameObject.AddComponent<Outline>();
            inputOl.effectColor    = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.55f);
            inputOl.effectDistance = new Vector2(3f, -3f);

            // テキスト色
            if (nameInput.textComponent != null)
                nameInput.textComponent.color = Color.white;

            // プレースホルダー色
            var placeholder = nameInput.placeholder as Text;
            if (placeholder != null)
                placeholder.color = new Color(1f, 1f, 1f, 0.45f);
        }

        // === Buttons ===
        StyleButton(submitButton, new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.25f));
        StyleButton(skipButton,   new Color(1f, 1f, 1f, 0.08f));
    }

    // -------------------------------------------------------
    // ボタンスタイル
    // -------------------------------------------------------

    void StyleButton(Button button, Color bgColor)
    {
        if (button == null) return;

        var img = button.GetComponent<Image>();
        if (img != null) img.color = bgColor;

        var outline = button.GetComponent<Outline>();
        if (outline == null) outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor    = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.5f);
        outline.effectDistance = new Vector2(3f, -3f);

        var label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color     = Color.white;
            label.fontStyle = FontStyle.Bold;
            EnsureGlow(label.gameObject, NeonRed, 0.5f, 2);
        }
    }

    // -------------------------------------------------------
    // グロー（Shadow + Outline）
    // 既存コンポーネントがあれば色を上書き、なければ追加
    // -------------------------------------------------------

    void EnsureGlow(GameObject go, Color baseColor, float alpha, int strength)
    {
        Shadow shadow   = null;
        Outline outline = null;

        foreach (var s in go.GetComponents<Shadow>())
        {
            if (s is Outline o)
                outline = o;
            else
                shadow = s;
        }

        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor    = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        shadow.effectDistance = new Vector2(strength, -strength);

        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor    = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.75f);
        outline.effectDistance = new Vector2(strength, -strength);
    }

    // -------------------------------------------------------
    // ネオングロー（3層ライン — TitleController / GameStarter と同演出）
    // -------------------------------------------------------

    void CreateNeonGlow(Transform parent, Color col, Vector2 pivot)
    {
        string id = "NeonGlow_" + (pivot.y > 0.5f ? "Top" : "Bot");
        if (parent.Find(id) != null) return;

        var container = new GameObject(id);
        container.transform.SetParent(parent, false);
        var rt = container.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CreateLine(container.transform, "Outer", new Color(col.r, col.g, col.b, 0.12f), pivot, 48f);
        CreateLine(container.transform, "Mid",   new Color(col.r, col.g, col.b, 0.4f),  pivot, 10f);
        CreateLine(container.transform, "Core",  new Color(1f, 0.9f, 0.85f, 0.95f),     pivot, 2.5f);
    }

    void CreateLine(Transform parent, string name, Color col, Vector2 pivot, float thickness)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, pivot.y);
        rt.anchorMax = new Vector2(1, pivot.y);
        rt.pivot     = pivot;
        rt.sizeDelta = new Vector2(0, thickness);
        rt.anchoredPosition = Vector2.zero;
    }

    // -------------------------------------------------------
    // スキャンライン（サイバー感の横線）
    // -------------------------------------------------------

    void CreateScanLines(Transform parent, float totalHeight, int count)
    {
        if (parent.Find("ScanLines_NR") != null) return;

        var container = new GameObject("ScanLines_NR");
        container.transform.SetParent(parent, false);
        var crt = container.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        float spacing = totalHeight / (count + 1);

        for (int i = 0; i < count; i++)
        {
            var line = new GameObject("SL");
            line.transform.SetParent(container.transform, false);

            var img = line.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.015f);
            img.raycastTarget = false;

            var rt = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -totalHeight * 0.5f + spacing * (i + 1));
        }
    }

    // -------------------------------------------------------
    // アクセント区切り線（赤いセパレーター）
    // -------------------------------------------------------

    void CreateAccentLine(Transform parent, float yFromTop)
    {
        if (parent.Find("AccentLine_NR") != null) return;

        var go = new GameObject("AccentLine_NR");
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.5f);
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 1f);
        rt.anchorMax = new Vector2(0.95f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0, 2);
        rt.anchoredPosition = new Vector2(0, -yFromTop);
    }

}
