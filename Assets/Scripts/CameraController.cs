using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    Camera cam;
    float defaultSize;
    Vector3 defaultPosition;

    void Awake()
    {
        instance = this;
        cam = GetComponent<Camera>();
        defaultSize = cam.orthographicSize;
        defaultPosition = transform.position;
    }

    // ズーム実行（ターゲットの場所へ、指定したサイズまで寄る）
    public void ZoomIn(Vector3 targetPos, float targetSize, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ZoomRoutine(targetPos, targetSize, duration));
    }

    // 元に戻す
    public void ResetCamera(float duration)
    {
        StopAllCoroutines();
        StartCoroutine(ZoomRoutine(defaultPosition, defaultSize, duration));
    }

    IEnumerator ZoomRoutine(Vector3 targetPos, float targetSize, float duration)
    {
        float startSize = cam.orthographicSize;
        Vector3 startPos = transform.position;

        // ターゲットのZ座標はカメラの元のZ座標に合わせる（2Dなので）
        Vector3 finalPos = new Vector3(targetPos.x, targetPos.y, defaultPosition.z);

        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // イージング（滑らかに動く計算）
            // t = t * t * (3f - 2f * t); // SmoothStep

            cam.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            transform.position = Vector3.Lerp(startPos, finalPos, t);

            yield return null;
        }

        cam.orthographicSize = targetSize;
        transform.position = finalPos;
    }
}