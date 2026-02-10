using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TimePopupText : MonoBehaviour
{
    [Header("éQè∆")]
    [SerializeField] Text label;

    [Header("ââèo")]
    [SerializeField] float riseY = 40f;
    [SerializeField] float duration = 0.55f;
    [SerializeField] float startScale = 0.9f;
    [SerializeField] float peakScale = 1.15f;

    RectTransform _rt;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (label == null) label = GetComponentInChildren<Text>();
    }

    public void Play(string message)
    {
        if (label != null) label.text = message;

        // ä˘ë∂TweenÇ™écÇ¡ÇƒÇƒÇ‡éñåÃÇÁÇ»Ç¢ÇÊÇ§Ç…
        transform.DOKill();
        if (_rt != null) _rt.DOKill();

        // èâä˙èÛë‘
        transform.localScale = Vector3.one * startScale;

        // è„Ç…è„Ç™ÇÈ
        Sequence seq = DOTween.Sequence().SetUpdate(true); // timeScale = 0Ç≈Ç‡ìÆÇ≠
        if (_rt != null)
        {
            Vector2 from = _rt.anchoredPosition;
            Vector2 to = from + new Vector2(0f, riseY);
            seq.Join(_rt.DOAnchorPos(to, duration).SetEase(Ease.OutCubic));
        }

        seq.Join(transform.DOScale(peakScale, duration * 0.35f).SetEase(Ease.OutBack));
        seq.Join(transform.DOScale(1.0f, duration * 0.65f).SetEase(Ease.OutCubic).SetDelay(duration * 0.35f));

        seq.OnComplete(() => Destroy(gameObject));
    }
}
