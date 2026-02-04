using UnityEngine;
using System.Collections;

public class Card : MonoBehaviour
{
    [SerializeField] float flightTime = 0.15f; // 到達までの時間
    [SerializeField] float startScale = 1.0f;  // 発射時の倍率（1.0ならPrefabそのまま）
    [SerializeField] float endScale = 0.5f;    // 刺さった時の倍率（0.5なら半分）

    Vector3 targetPos;
    Vector3 startPos;  // 追加：発射地点を覚えておく
    Vector3 baseScale; // 追加：元の形（X:0.2, Y:0.4等）を覚えておく

    System.Action onHitCallback;

    void Awake()
    {
        // Prefab等の元のスケールを記憶しておく
        baseScale = transform.localScale;
    }

    public void Fire(Vector3 start, Vector3 end, System.Action onHit)
    {
        startPos = start; // 開始地点を記憶
        transform.position = start;
        targetPos = end;
        onHitCallback = onHit;

        // 進行方向を向く
        Vector3 dir = (end - start).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);

        StartCoroutine(FlyRoutine());
    }

    IEnumerator FlyRoutine()
    {
        float elapsed = 0;

        while (elapsed < flightTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flightTime;

            // 移動：開始地点(startPos)から目的地(targetPos)へ
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            // スケール：元の形(baseScale) に 倍率(Lerp) を掛ける
            // これなら縦長の比率が崩れません
            float scaleMultiplier = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = baseScale * scaleMultiplier;

            yield return null;
        }

        // 到達
        transform.position = targetPos;
        transform.localScale = baseScale * endScale;

        if (onHitCallback != null) onHitCallback();

        Destroy(gameObject, 0.5f);
    }
}