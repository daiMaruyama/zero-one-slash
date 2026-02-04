using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Color Settings")]
    // 初期値を空っぽにしておき、Awakeで現在の色を取得するスタイルに変更
    public Color normalBgColor;
    public Color hoverBgColor = new Color(1f, 0f, 1f, 1f); // ホバー時の色は指定通り

    public Color normalTextColor;
    public Color hoverTextColor = Color.black;

    [Header("Animation Settings")]
    public float scaleMultiplier = 1.1f;
    public float duration = 0.2f;

    private Image bgImage;
    private Text btnText;
    private Vector3 originalScale;

    void Awake()
    {
        bgImage = GetComponent<Image>();
        btnText = GetComponentInChildren<Text>();
        originalScale = transform.localScale;

        // 勝手に色を上書きせず、「今設定されている色」を通常色として覚える
        if (bgImage) normalBgColor = bgImage.color;
        if (btnText) normalTextColor = btnText.color;
    }

    void OnEnable()
    {
        // 有効化されたら「覚えている通常色」に戻す
        ResetToNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(originalScale * scaleMultiplier, duration).SetEase(Ease.OutQuad);
        if (bgImage) bgImage.DOColor(hoverBgColor, duration);
        if (btnText) btnText.DOColor(hoverTextColor, duration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetToNormal(true);
    }

    void OnDisable()
    {
        transform.DOKill();
        if (bgImage) bgImage.DOKill();
        if (btnText) btnText.DOKill();
        ResetToNormal(false);
    }

    void ResetToNormal(bool animate = false)
    {
        if (animate)
        {
            transform.DOScale(originalScale, duration).SetEase(Ease.OutQuad);
            // 覚えておいた元の色（透明なら透明）に戻る
            if (bgImage) bgImage.DOColor(normalBgColor, duration);
            if (btnText) btnText.DOColor(normalTextColor, duration);
        }
        else
        {
            transform.localScale = originalScale;
            if (bgImage) bgImage.color = normalBgColor;
            if (btnText) btnText.color = normalTextColor;
        }
    }
}