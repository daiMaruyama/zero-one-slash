using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// スライダーをネオン赤テーマにスタイリング
/// 太いトラック + ネオン赤フィル + パーセント表示
/// </summary>
public class SliderStyler : MonoBehaviour
{
    static readonly Color NEON_RED = new Color(1f, 0.22f, 0.14f, 1f);
    static readonly Color TRACK_BG = new Color(0.08f, 0.08f, 0.12f, 0.9f);
    static readonly Color HANDLE_COLOR = Color.white;

    const float TRACK_H = 20f;
    const float HANDLE_W = 10f;
    const float HANDLE_H = 32f;

    Text percentText;

    void Awake()
    {
        ApplyNeonStyle();
    }

    [ContextMenu("Apply Neon Style")]
    public void ApplyNeonStyle()
    {
        Slider slider = GetComponent<Slider>();
        if (slider == null) return;

        // 既存の子オブジェクト（前回生成分）を掃除
        CleanupGenerated(transform, "InnerBG");
        CleanupGenerated(transform, "Glow");
        CleanupGenerated(transform, "Border");
        CleanupGenerated(transform, "HandleGlow");

        // === 1. Background Track（暗い太めのバー） ===
        Transform bgTrans = transform.Find("Background");
        if (bgTrans != null)
        {
            RectTransform bgRect = bgTrans.GetComponent<RectTransform>();
            SetupImage(bgRect, TRACK_BG);
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, TRACK_H);
        }

        // === 2. Fill（ネオン赤、トラックと同じ太さ） ===
        if (slider.fillRect != null)
        {
            RectTransform fillRect = slider.fillRect;
            RectTransform fillArea = fillRect.parent as RectTransform;

            if (fillArea != null)
            {
                fillArea.anchorMin = new Vector2(0f, 0.5f);
                fillArea.anchorMax = new Vector2(1f, 0.5f);
                fillArea.pivot = new Vector2(0.5f, 0.5f);
                fillArea.anchoredPosition = Vector2.zero;
                fillArea.sizeDelta = new Vector2(0f, TRACK_H);
            }

            SetupImage(fillRect, NEON_RED);
        }

        // === 3. Handle（白い縦長の棒） ===
        if (slider.handleRect != null)
        {
            RectTransform handleRect = slider.handleRect;
            SetupImage(handleRect, HANDLE_COLOR);
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(HANDLE_W, HANDLE_H);
            handleRect.localRotation = Quaternion.identity;
        }

        // === 4. パーセント表示 ===
        Transform pctTrans = transform.Find("PercentText");
        if (pctTrans == null)
        {
            GameObject pctGO = new GameObject("PercentText");
            pctGO.transform.SetParent(transform, false);
            RectTransform pctRT = pctGO.AddComponent<RectTransform>();
            pctRT.anchorMin = new Vector2(1f, 0f);
            pctRT.anchorMax = new Vector2(1f, 1f);
            pctRT.pivot = new Vector2(0f, 0.5f);
            pctRT.anchoredPosition = new Vector2(12f, 0f);
            pctRT.sizeDelta = new Vector2(80f, 0f);

            percentText = pctGO.AddComponent<Text>();
            percentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            percentText.fontSize = 22;
            percentText.fontStyle = FontStyle.Bold;
            percentText.color = new Color(1f, 1f, 1f, 0.7f);
            percentText.alignment = TextAnchor.MiddleLeft;
            percentText.raycastTarget = false;
        }
        else
        {
            percentText = pctTrans.GetComponent<Text>();
        }

        slider.onValueChanged.AddListener(UpdatePercent);
        UpdatePercent(slider.value);
    }

    void UpdatePercent(float val)
    {
        if (percentText != null)
            percentText.text = Mathf.RoundToInt(val * 100f) + "%";
    }

    [ContextMenu("Fix Slider Style")]
    public void FixStyle() => ApplyNeonStyle();

    Image SetupImage(RectTransform rt, Color c)
    {
        Image img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = null;
        img.color = c;
        return img;
    }

    void CleanupGenerated(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child != root && child.name == name)
                Destroy(child.gameObject);
        }
    }

    void OnDestroy()
    {
        Slider slider = GetComponent<Slider>();
        if (slider != null) slider.onValueChanged.RemoveListener(UpdatePercent);
    }
}
