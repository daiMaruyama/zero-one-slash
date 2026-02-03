using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class FocusSlice : MonoBehaviour
{
    MeshFilter mf;
    MeshRenderer mr;
    Mesh mesh;

    MaterialPropertyBlock mpb;
    Color baseColor = Color.cyan;

    float currentAlpha = 0f;
    float targetAlpha = 0f;
    float fadeSpeed = 1.5f;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        mesh = new Mesh();
        mf.mesh = mesh;
        mpb = new MaterialPropertyBlock();
    }

    public void Init(Material mat, int order, float fade)
    {
        mr.sharedMaterial = mat;
        mr.sortingOrder = order;
        fadeSpeed = fade;
        Apply();
    }

    public void SetColor(Color c)
    {
        baseColor = c;
    }

    public void SetTargetAlpha(float a)
    {
        targetAlpha = Mathf.Clamp01(a);
    }

    void Update()
    {
        // Ç‰Ç¡Ç≠ÇËïÇÇ©Ç—è„Ç™ÇÈ
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, 1f - Mathf.Exp(-fadeSpeed * Time.deltaTime));
        Apply();
    }

    void Apply()
    {
        mr.GetPropertyBlock(mpb);
        Color c = baseColor;
        c.a = currentAlpha;
        mpb.SetColor("_Color", c);
        mr.SetPropertyBlock(mpb);
    }

    public void UpdateShape(float inner, float outer, float centerAngle, float widthDeg, float arcWidth)
    {
        BuildMesh(inner, outer, centerAngle, widthDeg);
    }

    void BuildMesh(float inner, float outer, float centerAngle, float widthDeg)
    {
        int segments = Mathf.Clamp((int)(widthDeg / 5f), 16, 64);

        int vertCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[segments * 6];

        float startAngle = centerAngle - (widthDeg / 2f);
        float angleStep = widthDeg / segments;

        int triIndex = 0;

        for (int i = 0; i <= segments; i++)
        {
            float angleRad = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);

            vertices[i * 2] = new Vector3(cos * inner, sin * inner, 0);
            vertices[i * 2 + 1] = new Vector3(cos * outer, sin * outer, 0);

            if (i < segments)
            {
                int b = i * 2;
                triangles[triIndex++] = b;
                triangles[triIndex++] = b + 1;
                triangles[triIndex++] = b + 2;

                triangles[triIndex++] = b + 1;
                triangles[triIndex++] = b + 3;
                triangles[triIndex++] = b + 2;
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }
}
