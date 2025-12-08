using UnityEngine;
using DG.Tweening; // DOTween必須

public class ShaderStarter : MonoBehaviour
{
    [Header("待機する時間")]
    [SerializeField] float delaySeconds = 3.0f;

    [Header("最終的なスクロール速度")]
    [SerializeField] float targetSpeed = 0.2f;

    [Header("加速にかける時間")]
    [SerializeField] float duration = 2.0f;

    [Header("対象の背景オブジェクト")]
    [SerializeField] MeshRenderer targetRenderer;

    void Start()
    {
        // 念のため最初は速度0にする
        if (targetRenderer != null)
        {
            targetRenderer.material.SetFloat("_ScrollSpeed", 0f);
        }

        // 待機してから加速開始
        StartCoroutine(StartRoutine());
    }

    System.Collections.IEnumerator StartRoutine()
    {
        yield return new WaitForSeconds(delaySeconds);

        if (targetRenderer != null)
        {
            // DOTweenで数値をアニメーションさせる
            // "_ScrollSpeed" という名前のプロパティを、0 から targetSpeed まで変化させる
            targetRenderer.material.DOFloat(targetSpeed, "_ScrollSpeed", duration)
                .SetEase(Ease.InOutQuad); // 滑らかに加速
        }
    }
}