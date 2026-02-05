using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HitPopupText : MonoBehaviour
{
    [SerializeField] Text text;
    [SerializeField] float lifeTime = 0.8f;

    public void Setup(string areaCode, int score)
    {
        if (text != null)
        {
            // 表示例：T20 / 60 とか
            text.text = $"{areaCode}\n{score}";
        }

        transform.localScale = Vector3.one * 0.6f;

        // Phoenixっぽい「浮いて、デカくなって、消える」
        transform.DOScale(1.2f, 0.15f).SetEase(Ease.OutBack);
        transform.DOMoveY(transform.position.y + 0.4f, lifeTime).SetEase(Ease.OutQuad);

        if (text != null)
        {
            text.DOFade(0f, lifeTime).SetEase(Ease.InQuad);
        }

        Destroy(gameObject, lifeTime + 0.05f);
    }
}
