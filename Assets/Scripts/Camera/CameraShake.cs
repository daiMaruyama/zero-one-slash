using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // シングルトン化
    public static CameraShake instance;

    Vector3 originalPos;

    void Awake()
    {
        instance = this;
        originalPos = transform.localPosition;
    }

    // duration: 揺れる時間（秒）, magnitude: 揺れの強さ
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(DoShake(duration, magnitude));
    }

    IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // ランダムに位置をずらす
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 元の位置に戻す
        transform.localPosition = originalPos;
    }
}