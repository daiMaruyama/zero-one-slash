using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SegmentHighlighter : MonoBehaviour
{
    public float duration = 0.3f;

    Mesh _mesh;
    Material _mat;
    MeshRenderer _renderer;

    Color _targetColor;

    bool _isRipple = false;
    float _rippleMaxRadius;
    float _rippleWidth;

    bool _isGuide = false;
    Coroutine _co;

    void Awake()
    {
        _mesh = new Mesh();
        _mesh.name = "SegmentHighlighterMesh";
        _mesh.MarkDynamic();

        GetComponent<MeshFilter>().mesh = _mesh;

        _renderer = GetComponent<MeshRenderer>();

        Shader shader = FindBestShader();
        _mat = new Material(shader);

        // 描画を確実にするため白テクスチャを噛ませる（Sprites系の無描画事故防止）
        if (_mat.HasProperty("_MainTex"))
        {
            _mat.SetTexture("_MainTex", Texture2D.whiteTexture);
        }

        _mat.color = Color.clear;
        _renderer.material = _mat;

        // とにかく前に出す
        _renderer.sortingOrder = 300;
    }

    Shader FindBestShader()
    {
        // URP 2D → Built-in の順で探す
        Shader s;

        s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (s != null) return s;

        s = Shader.Find("Sprites/Default");
        if (s != null) return s;

        // 最後の保険
        s = Shader.Find("Unlit/Transparent");
        if (s != null) return s;

        return Shader.Find("Standard");
    }

    public void SetSortingOrder(int order)
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        _renderer.sortingOrder = order;
    }

    public void SetSorting(string layerName, int order)
    {
        if (_renderer == null) _renderer = GetComponent<MeshRenderer>();
        if (!string.IsNullOrEmpty(layerName))
        {
            _renderer.sortingLayerName = layerName;
        }
        _renderer.sortingOrder = order;
    }

    // ヒット用（静止帯）
    public void FlashSegment(float inner, float outer, float centerAngle, float widthDeg, Color color)
    {
        _isGuide = false;
        _isRipple = false;
        _targetColor = color;

        UpdateMesh(inner, outer, centerAngle, widthDeg);
        StartCo(HitAnimateRoutine());
    }

    // ヒット用（波紋）
    public void RippleEffect(float maxRadius, Color color, float speedScale = 1.0f, float width = 0.5f)
    {
        _isGuide = false;
        _isRipple = true;

        _targetColor = color;
        _rippleMaxRadius = maxRadius;
        _rippleWidth = width;

        duration = 0.4f / Mathf.Max(0.01f, speedScale);
        StartCo(HitAnimateRoutine());
    }

    // ガイド用（ふわっと表示して維持）
    public void ShowGuide(float inner, float outer, float centerAngle, float widthDeg, Color color, float alpha, float fadeIn)
    {
        _isGuide = true;
        _isRipple = false;

        UpdateMesh(inner, outer, centerAngle, widthDeg);

        Color c = color;
        c.a = alpha;
        _targetColor = c;

        StartCo(GuideFadeInRoutine(fadeIn));
    }

    public void HideGuideAndDestroy(float fadeOut = 0.12f)
    {
        if (!_isGuide)
        {
            Destroy(gameObject);
            return;
        }

        StartCo(GuideFadeOutRoutine(fadeOut));
    }

    void StartCo(IEnumerator routine)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(routine);
    }

    IEnumerator GuideFadeInRoutine(float fadeIn)
    {
        float elapsed = 0f;
        float targetA = _targetColor.a;

        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Mathf.Max(0.001f, fadeIn);
            float a = Mathf.Lerp(0f, targetA, t);

            _mat.color = new Color(_targetColor.r, _targetColor.g, _targetColor.b, a);
            yield return null;
        }

        _mat.color = _targetColor;
    }

    IEnumerator GuideFadeOutRoutine(float fadeOut)
    {
        float elapsed = 0f;
        Color start = _mat.color;

        while (elapsed < fadeOut)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Mathf.Max(0.001f, fadeOut);
            float a = Mathf.Lerp(start.a, 0f, t);

            _mat.color = new Color(start.r, start.g, start.b, a);
            yield return null;
        }

        Destroy(gameObject);
    }

    IEnumerator HitAnimateRoutine()
    {
        float elapsed = 0f;
        float intensity = 4.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float alpha = 1.0f - Mathf.Pow(t, 2);

            Color c = _targetColor * intensity;
            c.a = alpha;
            _mat.color = c;

            if (_isRipple)
            {
                float currentOuter = Mathf.Lerp(0f, _rippleMaxRadius, t);
                float currentInner = Mathf.Max(0f, currentOuter - _rippleWidth);
                UpdateMesh(currentInner, currentOuter, 0f, 360f);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void UpdateMesh(float inner, float outer, float centerAngle, float widthDeg)
    {
        // 安定した分割
        int segments = Mathf.Clamp((int)(widthDeg / 4f), 24, 96);

        int vertCount = segments * 2 + 2;
        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[segments * 6];

        float startAngle = centerAngle - (widthDeg / 2f);
        float angleStep = widthDeg / segments;

        int triIndex = 0;

        for (int i = 0; i <= segments; i++)
        {
            float angleRad = (startAngle + (angleStep * i)) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            vertices[i * 2] = new Vector3(cos * inner, sin * inner, 0f);
            vertices[i * 2 + 1] = new Vector3(cos * outer, sin * outer, 0f);

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

        _mesh.RecalculateBounds();
        _mesh.RecalculateNormals();
    }
}
