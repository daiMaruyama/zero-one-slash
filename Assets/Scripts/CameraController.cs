using UnityEngine;
using DG.Tweening;

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

    // ズームイン（移動しない版）
    public void ZoomIn(Vector3 targetPos, float targetSize, float duration)
    {
        // 以前のズームをキャンセル
        transform.DOKill();
        cam.DOKill();

        // カメラは動かさない（ズームだけ）
        cam.DOOrthoSize(targetSize, duration).SetEase(Ease.OutExpo);
    }

    // 元に戻す
    public void ResetCamera(float duration)
    {
        transform.DOKill();
        cam.DOKill();

        cam.DOOrthoSize(defaultSize, duration).SetEase(Ease.OutQuad);

        // 念のため位置も確実に戻す
        transform.position = defaultPosition;
    }
}
