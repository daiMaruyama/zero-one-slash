using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// リザルト画面ボタン用のホバー/プレス演出
/// NeonNavButton の軽量版
/// </summary>
public class NeonResultButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    static readonly Color NeonRed = new Color(1f, 0.196f, 0.137f);
    const float Dur = 0.1f;

    Image bg;
    RectTransform rt;
    Tween tween;
    bool isHovered;

    Color normalBg  = new Color(1f, 0.196f, 0.137f, 0.18f);
    Color hoverBg   = new Color(1f, 0.196f, 0.137f, 0.45f);
    Color pressBg   = new Color(1f, 0.196f, 0.137f, 0.65f);

    public void Init(Image background)
    {
        bg = background;
        rt = transform as RectTransform;
        if (bg != null) bg.color = normalBg;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        isHovered = true;
        Animate(1.05f, hoverBg);
    }

    public void OnPointerExit(PointerEventData e)
    {
        isHovered = false;
        Animate(1f, normalBg);
    }

    public void OnPointerDown(PointerEventData e)
    {
        Animate(0.95f, pressBg);
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (isHovered) Animate(1.05f, hoverBg);
        else           Animate(1f, normalBg);
    }

    void Animate(float scale, Color col)
    {
        Kill();
        var seq = DOTween.Sequence().SetLink(gameObject);
        if (rt != null) seq.Append(rt.DOScale(scale, Dur).SetEase(Ease.OutCubic));
        if (bg != null) seq.Join(bg.DOColor(col, Dur));
        tween = seq;
    }

    void OnEnable()
    {
        Kill();
        isHovered = false;
        if (rt != null) rt.localScale = Vector3.one;
        if (bg != null) bg.color = normalBg;
    }

    void Kill()
    {
        if (tween != null && tween.IsActive()) tween.Kill();
        tween = null;
    }

    void OnDestroy() => Kill();
}
