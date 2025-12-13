using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // URPを使用していない場合は削除

public class GameEffectsManager : MonoBehaviour
{
    public static GameEffectsManager instance;

    [Header("UI参照")]
    public Image overlayRed;      // 画面全体を赤くするパネル
    // Text bustText は削除しました
    public RectTransform canvasRect; // 揺らす対象のUI親オブジェクト

    [Header("ポストプロセス (任意)")]
    public Volume globalVolume;   // URPのVolume
    ChromaticAberration chromatic;

    void Awake()
    {
        instance = this;
        // 色収差(Chromatic Aberration)の取得
        if (globalVolume != null && globalVolume.profile.TryGet(out ChromaticAberration ch))
        {
            chromatic = ch;
        }
    }

    // バースト演出（赤フラッシュと画面揺れのみ）
    public void PlayBustEffect()
    {
        // 1. 赤フラッシュ
        if (overlayRed != null)
        {
            overlayRed.color = new Color(1f, 0f, 0f, 0f);
            // 一瞬で赤くし、少し時間をかけて透明に戻す
            overlayRed.DOFade(0.6f, 0.05f).OnComplete(() =>
            {
                overlayRed.DOFade(0f, 0.5f);
            });
        }

        // 2. 画面揺れ
        if (canvasRect != null)
        {
            // UI全体を揺らす
            canvasRect.DOShakeAnchorPos(0.5f, 50f, 50, 90, false, true);
        }

        // テキスト演出のブロックは削除しました

        // 3. グリッチ表現（色収差）
        if (chromatic != null)
        {
            DOTween.To(() => chromatic.intensity.value, x => chromatic.intensity.value = x, 1f, 0.1f)
                .OnComplete(() =>
                {
                    DOTween.To(() => chromatic.intensity.value, x => chromatic.intensity.value = x, 0f, 0.5f);
                });
        }
    }

    // フィニッシュ演出（変更なし）
    public void PlayFinishEffect()
    {
        // 時間を遅くする
        Time.timeScale = 0.1f;

        // 視覚効果（色収差を強くする）
        if (chromatic != null)
        {
            chromatic.intensity.value = 1f;
            DOTween.To(() => chromatic.intensity.value, x => chromatic.intensity.value = x, 0f, 1.5f)
                .SetUpdate(true); // スロー中も動作させる
        }

        // 2秒後（実時間）に元に戻す予約
        DOVirtual.DelayedCall(2.0f, () =>
        {
            Time.timeScale = 1.0f;
        }).SetUpdate(true);
    }
}