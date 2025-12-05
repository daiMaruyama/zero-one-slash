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

    // ズームイン（DOTween）
    public void ZoomIn(Vector3 targetPos, float targetSize, float duration)
    {
        // 以前のズームをキャンセル（連打対策）
        transform.DOKill();
        cam.DOKill();

        // ターゲットのZ座標はカメラの元のZに合わせる
        Vector3 finalPos = new Vector3(targetPos.x, targetPos.y, defaultPosition.z);

        cam.DOOrthoSize(targetSize, duration).SetEase(Ease.OutExpo);
        transform.DOMove(finalPos, duration).SetEase(Ease.OutExpo);
    }

    // 元に戻す
    public void ResetCamera(float duration)
    {
        // 戻る時は少し優しく (OutQuad)
        cam.DOOrthoSize(defaultSize, duration).SetEase(Ease.OutQuad);
        transform.DOMove(defaultPosition, duration).SetEase(Ease.OutQuad);
    }
}