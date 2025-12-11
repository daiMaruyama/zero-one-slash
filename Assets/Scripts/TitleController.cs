using UnityEngine;
using DG.Tweening;
using UnityEngine.UI; // DOTween必須

public class TitleController : MonoBehaviour
{
    [Header("作成した黒帯(Image)をセット")]
    public RectTransform slashTop;    // 上の黒帯
    public RectTransform slashBottom; // 下の黒帯

    [Header("演出設定")]
    public RectTransform shakeTarget;
    public float enterDuration = 0.5f; // 突き刺さる速さ
    public float shakeDuration = 0.4f; // 揺れの長さ
    public float shakeStrength = 15f;  // 揺れの強さ
    public Text touchText;

    // 最終的な配置位置
    Vector2 endPosTop;
    Vector2 endPosBottom;

    void Start()
    {
        // 1. 今置いてある場所（ゴール地点）を記憶
        endPosTop = slashTop.anchoredPosition;
        endPosBottom = slashBottom.anchoredPosition;

        // 2. 画面外へ飛ばす（初期化）
        // 1920幅なので、±1800くらい外へ飛ばせば見えなくなる
        slashTop.anchoredPosition = new Vector2(-1800, endPosTop.y);
        slashBottom.anchoredPosition = new Vector2(1800, endPosBottom.y);

        if (touchText != null)
        {
            touchText.DOFade(0.0f, 1.0f).SetLoops(-1, LoopType.Yoyo);
        }
        PlayEntrance();
    }

    void PlayEntrance()
    {
        // 上の帯が入場（左から）
        slashTop.DOAnchorPos(endPosTop, enterDuration)
            .SetEase(Ease.OutExpo) // キレのある動き
            .SetDelay(0.2f);       // 少し溜める

        // 下の帯が入場（右から）＆ 完了時に衝撃！
        slashBottom.DOAnchorPos(endPosBottom, enterDuration)
            .SetEase(Ease.OutExpo)
            .SetDelay(0.4f) // 上の帯より少し遅らせる
            .OnComplete(() =>
            {
                // 突き刺さった瞬間に画面（Canvas全体、またはカメラ）を揺らす！
                ImpactShake();
            });
    }

    void ImpactShake()
    {
        // カメラではなく、指定したUI（shakeTarget）を揺らす！
        if (shakeTarget != null)
        {
            // Positionではなく AnchorPos を揺らすのがUIの鉄則
            // strength: 30 (ピクセル単位なので大きめに！)
            shakeTarget.DOShakeAnchorPos(0.4f, 30f, 20, 90, false, true);
        }
    }
}