using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // マウスイベント用
using DG.Tweening; // DOTween用

public class ButtonController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Color Settings")]
    public Color normalBgColor = new Color(0, 0, 0, 0.6f); // 通常時の背景（半透明黒）
    public Color hoverBgColor = new Color(1f, 0f, 1f, 1f); // ホバー時の背景（ビビッドピンク）

    public Color normalTextColor = Color.white;            // 通常時の文字（白）
    public Color hoverTextColor = Color.black;             // ホバー時の文字（黒）

    [Header("Animation Settings")]
    public float scaleMultiplier = 1.1f; // どんくらい大きくなるか
    public float duration = 0.2f;        // アニメーション時間

    private Image bgImage;
    private Text btnText;
    private Vector3 originalScale;

    void Start()
    {
        bgImage = GetComponent<Image>();
        btnText = GetComponentInChildren<Text>(); // 子にあるTextを探す
        originalScale = transform.localScale;

        // 初期カラーの適用
        if (bgImage) bgImage.color = normalBgColor;
        if (btnText) btnText.color = normalTextColor;
    }

    // マウスが乗った時
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 拡大
        transform.DOScale(originalScale * scaleMultiplier, duration).SetEase(Ease.OutQuad);
        // 色変更
        if (bgImage) bgImage.DOColor(hoverBgColor, duration);
        if (btnText) btnText.DOColor(hoverTextColor, duration);
    }

    // マウスが離れた時
    public void OnPointerExit(PointerEventData eventData)
    {
        // サイズ戻す
        transform.DOScale(originalScale, duration).SetEase(Ease.OutQuad);
        // 色戻す
        if (bgImage) bgImage.DOColor(normalBgColor, duration);
        if (btnText) btnText.DOColor(normalTextColor, duration);
    }
}