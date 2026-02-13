using UnityEngine;
using DG.Tweening;

public class SettingsPanelAnimator : MonoBehaviour
{
    [Header("�Q��")]
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

    [Header("����")]
    [SerializeField] bool useUnscaledTime = true;

    Tween _tween;

    void Awake()
    {
        if (panelRoot == null) panelRoot = transform as RectTransform;
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        HideInstant();
    }

    public void Open()
    {
        // SetActive(true) may trigger Awake() which calls HideInstant() -> SetActive(false)
        // Awake only runs once, so calling SetActive(true) again is safe
        gameObject.SetActive(true);
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        KillTween();

        float from = Mathf.Max(0.01f, openScaleFrom);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (panelRoot != null)
        {
            // �����ŋ��������iscale 0 �Œ���E���j
            panelRoot.localScale = Vector3.one * from;
        }

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(useUnscaledTime);

        if (panelRoot != null) seq.Join(panelRoot.DOScale(1f, openDuration).SetEase(openEase));

        _tween = seq;
    }

    public void Close()
    {
        KillTween();

        float to = Mathf.Max(0.01f, closeScaleTo);

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Sequence seq = DOTween.Sequence();
        seq.SetUpdate(useUnscaledTime);

        if (panelRoot != null) seq.Join(panelRoot.DOScale(to, closeDuration).SetEase(closeEase));

        seq.OnComplete(() =>
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        });

        _tween = seq;
    }

    public void HideInstant()
    {
        KillTween();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
        {
            panelRoot.localScale = Vector3.one; // 0�ɂ��Ȃ��i���̖h�~�j
        }

        gameObject.SetActive(false);
    }

    void KillTween()
    {
        if (_tween != null && _tween.IsActive()) _tween.Kill();
        _tween = null;
    }
}
