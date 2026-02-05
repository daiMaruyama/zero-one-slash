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

    // 同じパネルを色違いで使う（Inspectorで調整できる）
    [Header("Failパネル色（Serializeで差し替え）")]
    [SerializeField] Color missPanelColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] Color noOutPanelColor = new Color(1f, 0.30f, 0.45f);

    [Header("Failパネル強さ（Serializeで差し替え）")]
    [SerializeField] float missPanelIntensity = 0.18f;
    [SerializeField] float noOutPanelIntensity = 0.22f;

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

    // MISS(OUT) 演出（BUSTより軽め）
    public void PlayMissEffect()
    {
        FlashPanel(missPanelColor, missPanelIntensity);

        if (canvasRect != null)
        {
            canvasRect.DOShakeAnchorPos(0.18f, 18f, 30, 90, false, true);
        }

        PlayGlitch(0.10f);
    }

    // NO OUT（足りない）演出（MISSより少し強め）
    public void PlayNoOutEffect()
    {
        FlashPanel(noOutPanelColor, noOutPanelIntensity);

        if (canvasRect != null)
        {
            canvasRect.DOShakeAnchorPos(0.25f, 24f, 35, 90, false, true);
        }

        PlayGlitch(0.12f);
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
