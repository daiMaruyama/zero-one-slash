using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SegmentHighlighter : MonoBehaviour
{
    [Header("Flash/Ripple")]
    public float duration = 0.3f;

    Mesh _mesh;
    Material _mat;

    Color _targetColor;

    // Flash/Ripple 用
    bool _isRipple = false;
    float _rippleMaxRadius;
    float _rippleWidth;

    // Guide 用
    bool _isGuide = false;
    float _guideAlpha = 0.25f;
    float _guideFadeIn = 0.25f;

    // Pulse 用（Guideだけで使用）
    bool _pulseEnabled = false;
    float _pulseSpeed = 2.2f;
    float _pulseMinMul = 0.55f;
    float _pulseMaxMul = 1.15f;
    float _pulsePhase = 0f;

    Coroutine _guideRoutine;

    void Awake()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;

        _mat = new Material(Shader.Find("Sprites/Default"));
        GetComponent<MeshRenderer>().material = _mat;

        // デフォでそこそこ前に出す（必要なら外から SetSortingOrder で上書き）
        GetComponent<MeshRenderer>().sortingOrder = 20;
    }

    // -------------------------
    // Hit演出（静止画）
    // -------------------------
    public void FlashSegment(float inner, float outer, float centerAngle, float widthDeg, Color color)
    {
        _isGuide = false;

        _isRipple = false;
        _targetColor = color;

        UpdateMesh(inner, outer, centerAngle, widthDeg);

        StopGuideRoutineIfAny();
        StartCoroutine(AnimateFlashRoutine());
    }

    // -------------------------
    // Hit演出（波紋）
    // -------------------------
    public void RippleEffect(float maxRadius, Color color, float speedScale = 1.0f, float width = 0.5f)
    {
        _isGuide = false;

        _isRipple = true;
        _targetColor = color;
        _rippleMaxRadius = maxRadius;
        _rippleWidth = width;

        duration = 0.4f / Mathf.Max(0.01f, speedScale);

        StopGuideRoutineIfAny();
        StartCoroutine(AnimateFlashRoutine());
    }

    IEnumerator AnimateFlashRoutine()
    {
        float elapsed = 0f;
        float intensity = 4.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // フェードアウト（後半ほどスッと消える）
            float alpha = 1.0f - (t * t);

            Color c = _targetColor * intensity;
            c.a = alpha;
            _mat.color = c;

            if (_isRipple)
            {
                float currentOuter = Mathf.Lerp(0, _rippleMaxRadius, t);
                float currentInner = Mathf.Max(0, currentOuter - _rippleWidth);
                UpdateMesh(currentInner, currentOuter, 0, 360f);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // -------------------------
    // Guide表示（消さない）
    // -------------------------
    public void ShowGuide(float inner, float outer, float centerAngle, float widthDeg, Color color, float alpha, float fadeIn)
    {
        _isGuide = true;

        _isRipple = false;
        _targetColor = color;
        _guideAlpha = Mathf.Clamp01(alpha);
        _guideFadeIn = Mathf.Max(0.01f, fadeIn);

        UpdateMesh(inner, outer, centerAngle, widthDeg);

        StopGuideRoutineIfAny();
        _guideRoutine = StartCoroutine(GuideRoutine());
    }

    public void HideGuideAndDestroy(float fadeOut)
    {
        if (!_isGuide)
        {
            Destroy(gameObject);
            return;
        }

        StopGuideRoutineIfAny();
        _guideRoutine = StartCoroutine(HideRoutine(Mathf.Max(0.01f, fadeOut)));
    }

    IEnumerator GuideRoutine()
    {
        // フェードイン
        float t = 0f;
        while (t < _guideFadeIn)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / _guideFadeIn);

            float a = Mathf.Lerp(0f, _guideAlpha, p);
            ApplyGuideColor(a);

            yield return null;
        }

        // ここから維持（PulseがONなら脈動）
        while (true)
        {
            float a = _guideAlpha;

            if (_pulseEnabled)
            {
                // 0..1
                float s = (Mathf.Sin((Time.time * _pulseSpeed) + _pulsePhase) + 1f) * 0.5f;
                float mul = Mathf.Lerp(_pulseMinMul, _pulseMaxMul, s);
                a = Mathf.Clamp01(_guideAlpha * mul);
            }

            ApplyGuideColor(a);

            yield return null;
        }
    }

    IEnumerator HideRoutine(float fadeOut)
    {
        float startA = _mat.color.a;
        float t = 0f;

        while (t < fadeOut)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / fadeOut);

            float a = Mathf.Lerp(startA, 0f, p);
            ApplyGuideColor(a);

            yield return null;
        }

        Destroy(gameObject);
    }

    void ApplyGuideColor(float alpha)
    {
        Color c = _targetColor;
        c.a = alpha;
        _mat.color = c;
    }

    void StopGuideRoutineIfAny()
    {
        if (_guideRoutine != null)
        {
            StopCoroutine(_guideRoutine);
            _guideRoutine = null;
        }
    }

    // -------------------------
    // 外部から調整
    // -------------------------
    public void SetSortingOrder(int order)
    {
        var r = GetComponent<MeshRenderer>();
        if (r != null) r.sortingOrder = order;
    }

    // ガイドの「脈動」設定
    public void SetGuidePulse(bool enabled, float speed, float minMul, float maxMul, float phase = 0f)
    {
        _pulseEnabled = enabled;
        _pulseSpeed = Mathf.Max(0.01f, speed);
        _pulseMinMul = Mathf.Clamp(minMul, 0.05f, 2.0f);
        _pulseMaxMul = Mathf.Clamp(maxMul, 0.05f, 3.0f);
        _pulsePhase = phase;
    }

    // -------------------------
    // Mesh生成
    // -------------------------
    void UpdateMesh(float inner, float outer, float centerAngle, float widthDeg)
    {
        // 角度に応じて分割数を調整（360度なら滑らかに、狭ければ少なく）
        int segments = Mathf.Clamp((int)(widthDeg / 5f), 16, 72);

        int vertCount = segments * 2 + 2;
        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[segments * 6];

        float startAngle = centerAngle - (widthDeg * 0.5f);
        float angleStep = widthDeg / segments;
        int triIndex = 0;

        for (int i = 0; i <= segments; i++)
        {
            float angleRad = (startAngle + (angleStep * i)) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            vertices[i * 2] = new Vector3(cos * inner, sin * inner, 0);
            vertices[i * 2 + 1] = new Vector3(cos * outer, sin * outer, 0);

            if (i < segments)
            {
                int baseIndex = i * 2;

                triangles[triIndex++] = baseIndex;
                triangles[triIndex++] = baseIndex + 1;
                triangles[triIndex++] = baseIndex + 2;

                triangles[triIndex++] = baseIndex + 1;
                triangles[triIndex++] = baseIndex + 3;
                triangles[triIndex++] = baseIndex + 2;
            }
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
    }
}
