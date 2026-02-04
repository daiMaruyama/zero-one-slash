using UnityEngine;
using UnityEngine.UI;

public class SliderStyler : MonoBehaviour
{
    [Header("【画像再現】修正パラメータ")]
    // 画像の重厚感を出すため、線を太くする
    public float barHeight = 20f;             // 4 -> 20 (太く！)
    public Vector2 handleSize = new Vector2(25f, 55f); // 縦長のブロック形状に

    [Header("デザイン調整")]
    public float slantAngle = -20f;           // 斜めの角度
    public Color bgColor = new Color(0.1f, 0.1f, 0.1f, 1f); // 筐体の色
    public Color neonColor = new Color(0f, 1f, 1f, 1f);     // 発光色

    [ContextMenu("Fix Slider Style")]
    public void FixStyle()
    {
        Slider slider = GetComponent<Slider>();
        if (slider == null) return;

        // 1. Background (太く、どっしりと)
        Transform bgTrans = transform.Find("Background");
        if (bgTrans != null)
        {
            RectTransform bgRect = bgTrans.GetComponent<RectTransform>();
            Image bgImage = SetupImage(bgRect, bgColor);

            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, barHeight); // 高さ20
        }

        // 2. Fill Area (太く、光らせる)
        if (slider.fillRect != null)
        {
            RectTransform fillRect = slider.fillRect;
            RectTransform fillArea = fillRect.parent as RectTransform;

            if (fillArea != null)
            {
                fillArea.anchorMin = new Vector2(0f, 0.5f);
                fillArea.anchorMax = new Vector2(1f, 0.5f);
                fillArea.pivot = new Vector2(0.5f, 0.5f);
                fillArea.sizeDelta = new Vector2(0f, barHeight); // 高さ20
                fillArea.offsetMin = Vector2.zero;
                fillArea.offsetMax = Vector2.zero;
            }

            SetupImage(fillRect, neonColor);
        }

        // 3. Handle (縦長で、斜めに)
        if (slider.handleRect != null)
        {
            RectTransform handleRect = slider.handleRect;
            SetupImage(handleRect, Color.white); // 芯は白く

            // アンカー中央
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);

            // ここがサイズ感の肝
            handleRect.sizeDelta = handleSize; // 25 x 55

            // 斜めにする
            handleRect.localRotation = Quaternion.Euler(0, 0, slantAngle);
        }
    }

    Image SetupImage(RectTransform rt, Color c)
    {
        Image img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = null;
        img.color = c;
        return img;
    }
}