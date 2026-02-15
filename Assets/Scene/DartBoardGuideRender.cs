using UnityEngine;

[ExecuteAlways]
public class DartBoardGuideRender : MonoBehaviour
{
    public float innerBull = 0.05f;
    public float outerBull = 0.09f;
    public float tripleInner = 0.35f;
    public float tripleOuter = 0.42f;
    public float doubleInner = 0.85f;
    public float doubleOuter = 0.92f;

    Material mat;

    void CreateMaterial()
    {
        if (mat != null) return;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;

        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    void OnRenderObject()
    {
        CreateMaterial();
        mat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);

        // 塗り
        DrawFilledRing(innerBull, outerBull, new Color(1f, 0.25f, 0.25f, 0.8f));
        DrawFilledRing(tripleInner, tripleOuter, new Color(1f, 0.9f, 0.2f, 0.6f));
        DrawFilledRing(doubleInner, doubleOuter, new Color(0.2f, 0.9f, 1f, 0.6f));

        // 外周ライン（白で締める）
        DrawOutline(innerBull, Color.white);
        DrawOutline(outerBull, Color.white);
        DrawOutline(tripleInner, Color.white);
        DrawOutline(tripleOuter, Color.white);
        DrawOutline(doubleInner, Color.white);
        DrawOutline(doubleOuter, Color.white);

        GL.PopMatrix();
    }

    void DrawFilledRing(float inner, float outer, Color color)
    {
        const int steps = 128;

        GL.Begin(GL.TRIANGLE_STRIP);
        GL.Color(color);

        for (int i = 0; i <= steps; i++)
        {
            float a = (i / (float)steps) * Mathf.PI * 2;
            Vector3 innerPos = transform.TransformPoint(
                new Vector3(Mathf.Cos(a) * inner, Mathf.Sin(a) * inner, 0));
            Vector3 outerPos = transform.TransformPoint(
                new Vector3(Mathf.Cos(a) * outer, Mathf.Sin(a) * outer, 0));

            GL.Vertex(innerPos);
            GL.Vertex(outerPos);
        }

        GL.End();
    }

    void DrawOutline(float radius, Color color)
    {
        const int steps = 128;

        GL.Begin(GL.LINE_STRIP);
        GL.Color(color);

        for (int i = 0; i <= steps; i++)
        {
            float a = (i / (float)steps) * Mathf.PI * 2;
            Vector3 pos = transform.TransformPoint(
                new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0));

            GL.Vertex(pos);
        }

        GL.End();
    }
}
