using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI要素にCSS skewX相当の変形を適用する
/// Background/Borderに付けて、Textには付けない（JSXと同じ構造）
/// </summary>
public class SkewRect : BaseMeshEffect
{
    [SerializeField] float skewAngle = -8f;

    public float SkewAngle
    {
        get => skewAngle;
        set { skewAngle = value; graphic.SetVerticesDirty(); }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> verts = new List<UIVertex>();
        vh.GetUIVertexStream(verts);

        if (verts.Count == 0) return;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < verts.Count; i++)
        {
            if (verts[i].position.y < minY) minY = verts[i].position.y;
            if (verts[i].position.y > maxY) maxY = verts[i].position.y;
        }
        float centerY = (minY + maxY) * 0.5f;
        float tanA = Mathf.Tan(skewAngle * Mathf.Deg2Rad);

        for (int i = 0; i < verts.Count; i++)
        {
            UIVertex v = verts[i];
            v.position.x += tanA * (v.position.y - centerY);
            verts[i] = v;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(verts);
    }
}
