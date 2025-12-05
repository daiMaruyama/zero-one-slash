using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SegmentHighlighter : MonoBehaviour
{
    public float duration = 0.3f;

    private Mesh mesh;
    private Material mat;
    private Color targetColor;

    // モード管理用
    private bool isRipple = false;
    private float rippleMaxRadius;
    private float rippleWidth;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mat = new Material(Shader.Find("Sprites/Default"));
        GetComponent<MeshRenderer>().material = mat;
        GetComponent<MeshRenderer>().sortingOrder = 20;
    }

    // 静止画モード（ダブル・トリプル用）
    public void FlashSegment(float inner, float outer, float centerAngle, float widthDeg, Color color)
    {
        isRipple = false;
        targetColor = color;
        // 形を一度だけ生成
        UpdateMesh(inner, outer, centerAngle, widthDeg);
        StartCoroutine(AnimateRoutine());
    }

    // 波紋モード（ブル用）
    public void RippleEffect(float maxRadius, Color color, float speedScale = 1.0f, float width = 0.5f)
    {
        isRipple = true;
        targetColor = color;
        rippleMaxRadius = maxRadius;
        rippleWidth = width;

        duration = 0.4f / speedScale;
        StartCoroutine(AnimateRoutine());
    }

    IEnumerator AnimateRoutine()
    {
        float elapsed = 0;
        float intensity = 4.0f; // 発光強度

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // フェードアウト処理
            float alpha = 1.0f - Mathf.Pow(t, 2);
            Color c = targetColor * intensity;
            c.a = alpha;
            mat.color = c;

            // 波紋の場合は毎フレーム形を更新して広げる
            if (isRipple)
            {
                float currentOuter = Mathf.Lerp(0, rippleMaxRadius, t);
                float currentInner = Mathf.Max(0, currentOuter - rippleWidth);
                UpdateMesh(currentInner, currentOuter, 0, 360f);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void UpdateMesh(float inner, float outer, float centerAngle, float widthDeg)
    {
        // 角度に応じて分割数を調整（360度なら滑らかに、狭ければ少なく）
        int segments = Mathf.Clamp((int)(widthDeg / 5f), 16, 64);

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

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }
}