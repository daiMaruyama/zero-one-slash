using UnityEngine;
using DG.Tweening;

public class SettingsPanelAnimator : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] RectTransform panelRoot;

    [Header("Open")]
    [SerializeField] float openDuration = 0.18f;
    [SerializeField] Ease openEase = Ease.OutBack;
    [SerializeField] float openScaleFrom = 0.92f;

    [Header("Close")]
    [SerializeField] float closeDuration = 0.06f;
    [SerializeField] Ease closeEase = Ease.InQuad;
    [SerializeField] float closeScaleTo = 0.95f;

    Tween _tween;

    void Reset()
    {
        canvasGroup = GetComponentInChildren<CanvasGroup>();
        panelRoot = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (panelRoot == null) panelRoot = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>();

        // 初期状態は非表示にしておく（必要ならInspectorでOFFにしてもOK）
        HideInstant();
    }

    public void Open()
    {
        gameObject.SetActive(true);

        KillTween();

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (panelRoot != null) panelRoot.localScale = Vector3.one * openScaleFrom;

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        if (canvasGroup != null) seq.Join(canvasGroup.DOFade(1f, openDuration).SetEase(Ease.OutQuad));
        if (panelRoot != null) seq.Join(panelRoot.DOScale(1f, openDuration).SetEase(openEase));

        _tween = seq;
    }

    public void Close()
    {
        KillTween();

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(true);

        if (canvasGroup != null) seq.Join(canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.OutQuad));
        if (panelRoot != null) seq.Join(panelRoot.DOScale(closeScaleTo, closeDuration).SetEase(closeEase));

        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });

        _tween = seq;
    }

    public void HideInstant()
    {
        KillTween();

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (panelRoot != null) panelRoot.localScale = Vector3.one;
        gameObject.SetActive(false);
    }

    void KillTween()
    {
        if (_tween != null && _tween.IsActive()) _tween.Kill();
        _tween = null;
    }
}
