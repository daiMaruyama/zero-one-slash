using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GameEffectsManager : MonoBehaviour
{
    public static GameEffectsManager instance;

    [Header("UI参照")]
    public Image overlayRed;
    public RectTransform canvasRect;

    [Header("ポストプロセス (任意)")]
    public Volume globalVolume;
    ChromaticAberration chromatic;

    void Awake()
    {
        instance = this;
        if (globalVolume != null && globalVolume.profile.TryGet(out ChromaticAberration ch))
        {
            chromatic = ch;
        }
    }

    // バースト演出
    public void PlayBustEffect()
    {
        // 赤フラッシュ
        FlashPanel(Color.red, 0.3f); // 赤は少し強め

        // 画面揺れ
        if (canvasRect != null)
        {
            canvasRect.DOShakeAnchorPos(0.5f, 50f, 50, 90, false, true);
        }

        // グリッチ表現 (短く)
        PlayGlitch(0.2f);
    }

    // 勝利演出
    public void PlayFinishEffect()
    {
        // 白フラッシュ
        FlashPanel(Color.white, 0.05f);

        // 時間を遅くする
        Time.timeScale = 0.1f;

        // グリッチ表現 (ここも短く 0.5秒 で切る)
        PlayGlitch(0.5f);

        DOVirtual.DelayedCall(2.0f, () =>
        {
            Time.timeScale = 1.0f;
        }).SetUpdate(true);
    }

    // パネルを光らせる共通処理 (強さを引数で指定)
    void FlashPanel(Color color, float intensity)
    {
        if (overlayRed != null)
        {
            overlayRed.color = new Color(color.r, color.g, color.b, 0f);

            // 指定した強さ(intensity)まで光らせる
            overlayRed.DOFade(intensity, 0.05f).SetUpdate(true).OnComplete(() =>
            {
                overlayRed.DOFade(0f, 0.5f).SetUpdate(true);
            });
        }
    }

    // グリッチ共通処理
    void PlayGlitch(float duration)
    {
        if (chromatic != null)
        {
            chromatic.intensity.value = 1f;
            DOTween.To(() => chromatic.intensity.value, x => chromatic.intensity.value = x, 0f, duration)
                .SetUpdate(true);
        }
    }
}