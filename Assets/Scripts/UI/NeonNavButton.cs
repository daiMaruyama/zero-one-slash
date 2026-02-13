using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// NeonMenuButton風のホバー/プレス演出（ナビボタン用の軽量版）
/// HowToPlayBuilder が自動アタッチする
/// </summary>
public class NeonNavButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    Image background;
    Image leftBorder;
    Text labelText;
    Color neonRed;

    RectTransform rt;
    Color normalBgColor;
    Color normalLabelColor;
    bool isHovered;
    Tween currentTween;

    const float AnimDuration = 0.1f;

    public void SetReferences(Image bg, Image border, Text label, Color red)
    {
        background = bg;
        leftBorder = border;
        labelText = label;
        neonRed = red;

        rt = GetComponent<RectTransform>();
        normalBgColor = bg != null ? bg.color : Color.clear;
        normalLabelColor = label != null ? label.color : Color.white;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        KillTween();

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        if (rt != null)
            seq.Append(rt.DOScale(1.04f, AnimDuration).SetEase(Ease.OutCubic));
        if (background != null)
            seq.Join(background.DOColor(new Color(neonRed.r, neonRed.g, neonRed.b, 0.12f), AnimDuration));
        if (leftBorder != null)
            seq.Join(leftBorder.DOColor(new Color(neonRed.r, neonRed.g, neonRed.b, 0.9f), AnimDuration));
        currentTween = seq;

        if (labelText != null) labelText.color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyNormal();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        KillTween();
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        if (rt != null)
            seq.Append(rt.DOScale(0.95f, 0.06f).SetEase(Ease.OutCubic));
        if (background != null)
            seq.Join(background.DOColor(new Color(neonRed.r, neonRed.g, neonRed.b, 0.22f), 0.06f));
        currentTween = seq;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isHovered)
            OnPointerEnter(eventData);
        else
            ApplyNormal();
    }

    /// <summary>
    /// SetActive(true) で復帰したときにホバー残りをリセット
    /// </summary>
    void OnEnable()
    {
        ResetInstant();
    }

    /// <summary>
    /// SetActive(false) で消えるときにホバー残りをリセット
    /// </summary>
    void OnDisable()
    {
        isHovered = false;
        ResetInstant();
    }

    void ApplyNormal()
    {
        KillTween();
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        if (rt != null)
            seq.Append(rt.DOScale(1f, AnimDuration).SetEase(Ease.OutCubic));
        if (background != null)
            seq.Join(background.DOColor(normalBgColor, AnimDuration));
        if (leftBorder != null)
            seq.Join(leftBorder.DOColor(new Color(neonRed.r, neonRed.g, neonRed.b, 0.2f), AnimDuration));
        currentTween = seq;

        if (labelText != null) labelText.color = normalLabelColor;
    }

    /// <summary>
    /// アニメーションなしで即座にノーマル状態に戻す
    /// </summary>
    void ResetInstant()
    {
        KillTween();
        if (rt != null) rt.localScale = Vector3.one;
        if (background != null) background.color = normalBgColor;
        if (leftBorder != null) leftBorder.color = new Color(neonRed.r, neonRed.g, neonRed.b, 0.2f);
        if (labelText != null) labelText.color = normalLabelColor;
    }

    void KillTween()
    {
        if (currentTween != null && currentTween.IsActive()) currentTween.Kill();
        currentTween = null;
    }

    void OnDestroy() => KillTween();
}
