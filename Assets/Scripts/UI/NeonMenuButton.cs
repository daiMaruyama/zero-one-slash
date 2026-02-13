using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// ネオンメニューボタン
/// ホバーで赤ハイライト + 1クリックで動作
/// skew(-8deg)はBG/Borderのみ（TextGroupには付けない）
/// </summary>
public class NeonMenuButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("設定")]
    [SerializeField] bool isPrimary;
    [SerializeField] int entranceIndex;

    [Header("参照")]
    [SerializeField] Image background;
    [SerializeField] Image leftBorder;
    [SerializeField] Text labelText;
    [SerializeField] Text subText;

    [Header("色")]
    [SerializeField] Color neonRed = new Color(1f, 0.196f, 0.137f);

    [Header("アニメーション")]
    [SerializeField] float animDuration = 0.1f;

    RectTransform rt;
    CanvasGroup cg;
    Vector2 originalPos;
    Color normalBgColor;
    bool isHovered;
    bool isPressed;
    Tween currentTween;

    bool initialized;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Initialize()
    {
        if (rt == null) rt = GetComponent<RectTransform>();
        if (cg == null) cg = GetComponent<CanvasGroup>();
        originalPos = rt.anchoredPosition;
        if (background != null) normalBgColor = background.color;
        ApplyNormalState();
        initialized = true;
    }

    // ===== 入場アニメーション =====

    public void PlayEntrance()
    {
        if (!initialized) Initialize();
        cg.alpha = 0f;
        rt.anchoredPosition = originalPos + new Vector2(-18f, 0f);
        rt.localScale = Vector3.one * 0.97f;

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.SetDelay(0.6f + entranceIndex * 0.08f);
        seq.Append(cg.DOFade(1f, 0.5f).SetEase(Ease.OutCubic));
        seq.Join(rt.DOAnchorPos(originalPos, 0.5f).SetEase(Ease.OutCubic));
        seq.Join(rt.DOScale(1f, 0.5f).SetEase(Ease.OutCubic));
    }

    // ===== ポインターイベント =====

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyHoverState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        ApplyNormalState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        ApplyPressState();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        if (isHovered)
            ApplyHoverState();
        else
            ApplyNormalState();
    }

    // ===== 状態適用 =====

    void ApplyHoverState()
    {
        KillTween();
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(rt.DOScale(1.02f, animDuration).SetEase(Ease.OutCubic));

        if (background != null)
            seq.Join(background.DOColor(HoverBgColor(), animDuration));
        if (leftBorder != null)
            seq.Join(leftBorder.DOColor(WithAlpha(neonRed, 0.9f), animDuration));

        currentTween = seq;

        if (labelText != null) labelText.color = Color.white;
        if (subText != null) subText.color = new Color(1f, 0.706f, 0.627f, 0.4f);
    }

    void ApplyPressState()
    {
        KillTween();
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(rt.DOScale(0.97f, 0.06f).SetEase(Ease.OutCubic));

        if (background != null)
            seq.Join(background.DOColor(PressedBgColor(), 0.06f));

        currentTween = seq;
    }

    void ApplyNormalState()
    {
        if (rt == null) return;
        KillTween();

        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(rt.DOScale(1f, animDuration).SetEase(Ease.OutCubic));

        if (background != null)
            seq.Join(background.DOColor(normalBgColor, animDuration));
        if (leftBorder != null)
            seq.Join(leftBorder.DOColor(WithAlpha(neonRed, isPrimary ? 0.9f : 0.2f), animDuration));

        currentTween = seq;

        if (labelText != null)
            labelText.color = isPrimary ? Color.white : new Color(1, 1, 1, 0.55f);
        if (subText != null)
            subText.color = isPrimary ? new Color(1f, 0.706f, 0.627f, 0.35f) : new Color(1f, 0.706f, 0.627f, 0.15f);
    }

    /// <summary>
    /// パネルが閉じた時にTitleUIManagerから呼ぶ
    /// </summary>
    public void ResetState()
    {
        isHovered = false;
        isPressed = false;
        if (!initialized) Initialize();

        rt.localScale = Vector3.one;
        rt.anchoredPosition = originalPos;
        if (background != null) background.color = normalBgColor;
        if (leftBorder != null) leftBorder.color = WithAlpha(neonRed, isPrimary ? 0.9f : 0.2f);
        if (labelText != null) labelText.color = isPrimary ? Color.white : new Color(1, 1, 1, 0.55f);
        if (subText != null) subText.color = isPrimary ? new Color(1f, 0.706f, 0.627f, 0.35f) : new Color(1f, 0.706f, 0.627f, 0.15f);
    }

    // ===== ユーティリティ =====

    Color HoverBgColor() => new Color(neonRed.r, neonRed.g, neonRed.b, 0.12f);
    Color PressedBgColor() => new Color(neonRed.r, neonRed.g, neonRed.b, 0.22f);
    Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    void KillTween()
    {
        if (currentTween != null && currentTween.IsActive()) currentTween.Kill();
        currentTween = null;
    }

    void OnDestroy()
    {
        KillTween();
    }
}
