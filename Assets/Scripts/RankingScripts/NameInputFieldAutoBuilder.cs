using UnityEngine;
using UnityEngine.UI;

public class NameInputFieldAutoBuilder : MonoBehaviour
{
    [Header("生成先（ResultPanelの中の好きな親）")]
    [SerializeField] RectTransform parent;

    [Header("見た目を合わせたい既存Text（フォント/サイズのコピー元）")]
    [SerializeField] Text styleSourceText;

    [Header("生成後の参照（自動で入る）")]
    [SerializeField] InputField generatedInputField;

    [Header("サイズ/配置")]
    [SerializeField] Vector2 size = new Vector2(900, 140);
    [SerializeField] Vector2 anchoredPos = new Vector2(0, -220); // ResultPanel内で調整

    [Header("文字設定")]
    [SerializeField] string placeholderText = "ENTER NAME";
    [SerializeField] int characterLimit = 12;

    [Header("ネオンカラー")]
    [SerializeField] Color backColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] Color frameColor = new Color(1f, 0.2f, 0.6f, 0.85f);
    [SerializeField] Color glowColor = new Color(1f, 0.2f, 0.6f, 0.18f);

    [Header("枠の太さ/光り")]
    [SerializeField] Vector2 outlineDistance = new Vector2(6, -6);
    [SerializeField] Vector2 glowOutlineDistance = new Vector2(10, -10);

    [Header("内側余白")]
    [SerializeField] Vector2 padding = new Vector2(40, 18);

    [ContextMenu("Generate Name Input UI")]
    public void Generate()
    {
        if (parent == null)
        {
            Debug.LogError("[NameInputAutoBuilder] parent が未設定です");
            return;
        }
        if (styleSourceText == null)
        {
            Debug.LogError("[NameInputAutoBuilder] styleSourceText が未設定です（既存Textを1つ入れて）");
            return;
        }

        // 既にあるなら消して作り直し
        Transform old = parent.Find("NameInputGroup(Auto)");
        if (old != null) DestroyImmediate(old.gameObject);

        // Root
        GameObject rootGO = CreateUIObject("NameInputGroup(Auto)", parent);
        RectTransform rootRT = rootGO.GetComponent<RectTransform>();
        rootRT.sizeDelta = size;
        rootRT.anchoredPosition = anchoredPos;

        // 背景
        Image back = rootGO.AddComponent<Image>();
        back.color = backColor;
        back.raycastTarget = true;

        // Glow（外側の薄い光）
        GameObject glowGO = CreateUIObject("Glow", rootRT);
        RectTransform glowRT = glowGO.GetComponent<RectTransform>();
        Stretch(glowRT, Vector2.zero, Vector2.zero);
        glowRT.localScale = Vector3.one * 1.04f;

        Image glowImg = glowGO.AddComponent<Image>();
        glowImg.color = glowColor;
        glowImg.raycastTarget = false;

        Outline glowOutline = glowGO.AddComponent<Outline>();
        glowOutline.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, Mathf.Clamp01(glowColor.a * 2.2f));
        glowOutline.effectDistance = glowOutlineDistance;

        // Frame（枠線）
        GameObject frameGO = CreateUIObject("Frame", rootRT);
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        Stretch(frameRT, Vector2.zero, Vector2.zero);

        Image frameImg = frameGO.AddComponent<Image>();
        frameImg.color = new Color(frameColor.r, frameColor.g, frameColor.b, 0.12f);
        frameImg.raycastTarget = false;

        Outline frameOutline = frameGO.AddComponent<Outline>();
        frameOutline.effectColor = frameColor;
        frameOutline.effectDistance = outlineDistance;

        Shadow frameShadow = frameGO.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(frameColor.r, frameColor.g, frameColor.b, 0.35f);
        frameShadow.effectDistance = new Vector2(0, 0);

        // InputField（本体）
        GameObject fieldGO = CreateUIObject("InputField", rootRT);
        RectTransform fieldRT = fieldGO.GetComponent<RectTransform>();
        Stretch(fieldRT, new Vector2(padding.x, padding.y), new Vector2(-padding.x, -padding.y));

        Image fieldImage = fieldGO.AddComponent<Image>();
        fieldImage.color = new Color(0, 0, 0, 0); // 背景は透明（枠は別で出してる）
        fieldImage.raycastTarget = true;

        InputField input = fieldGO.AddComponent<InputField>();
        input.transition = Selectable.Transition.ColorTint;
        input.characterLimit = characterLimit;
        input.lineType = InputField.LineType.SingleLine;
        input.contentType = InputField.ContentType.Standard;
        input.selectionColor = new Color(1f, 0.2f, 0.6f, 0.25f);
        input.caretColor = new Color(1f, 0.2f, 0.6f, 1f);
        input.caretWidth = 2;

        // Text（入力文字）
        GameObject textGO = CreateUIObject("Text", fieldRT);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        Stretch(textRT, Vector2.zero, Vector2.zero);

        Text text = textGO.AddComponent<Text>();
        CopyTextStyle(styleSourceText, text);
        text.text = "";
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = new Color(1f, 0.85f, 0.95f, 1f);

        // Placeholder
        GameObject phGO = CreateUIObject("Placeholder", fieldRT);
        RectTransform phRT = phGO.GetComponent<RectTransform>();
        Stretch(phRT, Vector2.zero, Vector2.zero);

        Text placeholder = phGO.AddComponent<Text>();
        CopyTextStyle(styleSourceText, placeholder);
        placeholder.text = placeholderText;
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.color = new Color(1f, 0.6f, 0.85f, 0.35f);

        input.textComponent = text;
        input.placeholder = placeholder;

        // 参照保存
        generatedInputField = input;

        // クリックできるように最前面に（念のため）
        rootRT.SetAsLastSibling();

        Debug.Log("[NameInputAutoBuilder] NameInputGroup を生成しました");
    }

    public InputField GetInputField()
    {
        return generatedInputField;
    }

    // ---------- Utility ----------

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(RectTransform rt, Vector2 minOffset, Vector2 maxOffset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = minOffset;
        rt.offsetMax = maxOffset;
    }

    static void CopyTextStyle(Text src, Text dst)
    {
        dst.font = src.font;
        dst.fontSize = src.fontSize;
        dst.fontStyle = src.fontStyle;
        dst.lineSpacing = src.lineSpacing;
        dst.supportRichText = false;
        dst.resizeTextForBestFit = false;
        dst.material = src.material; // 見た目の差が減る
    }
}
