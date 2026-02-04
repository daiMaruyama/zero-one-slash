using System.Collections.Generic;
using UnityEngine;

public class FocusOverlayManager : MonoBehaviour
{
    [Header("Board Ref")]
    [SerializeField] DartsBoard board;

    [Header("見た目（静かに浮き上がる）")]
    [SerializeField] Color focusColor = new Color(0.3f, 1f, 1f, 0.22f);
    [SerializeField] float fadeSpeed = 1.5f; // 小さいほどゆっくり
    [SerializeField] float arcWidth = 14f;
    [SerializeField] int sortingOrder = 30;

    readonly Dictionary<string, FocusSlice> slices = new();
    Material mat;

    readonly int[] scoreMap = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    void Awake()
    {
        if (board == null) board = GetComponent<DartsBoard>();

        mat = new Material(Shader.Find("Sprites/Default"));
    }

    public void ClearFocus()
    {
        foreach (var kv in slices)
        {
            kv.Value.SetTargetAlpha(0f);
        }
    }

    public void SetFocusAreaCodes(List<string> areaCodes)
    {
        // 一旦全部消して、必要なものだけ点ける
        ClearFocus();

        if (board == null) return;
        if (areaCodes == null) return;

        for (int i = 0; i < areaCodes.Count; i++)
        {
            ApplyFocus(areaCodes[i]);
        }
    }

    void ApplyFocus(string areaCode)
    {
        if (string.IsNullOrEmpty(areaCode)) return;

        // Bullは波紋禁止 → 静かな円
        if (areaCode.Contains("Bull"))
        {
            var slice = GetOrCreate(areaCode);
            slice.UpdateShape(0f, board.outerBullRadius, 0f, 360f, arcWidth);
            slice.SetColor(focusColor);
            slice.SetTargetAlpha(focusColor.a);
            return;
        }

        if (areaCode.Length < 2) return;
        char ring = areaCode[0];

        if (!int.TryParse(areaCode.Substring(1), out int baseScore)) return;

        int index = FindIndex(baseScore);
        if (index < 0) return;

        float centerAngle = 90f - (index * 18f);

        if (ring == 'T')
        {
            var slice = GetOrCreate(areaCode);
            slice.UpdateShape(board.tripleInner, board.tripleOuter, centerAngle, arcWidth, arcWidth);
            slice.SetColor(focusColor);
            slice.SetTargetAlpha(focusColor.a);
            return;
        }

        if (ring == 'D')
        {
            var slice = GetOrCreate(areaCode);
            slice.UpdateShape(board.doubleInner, board.doubleOuter, centerAngle, arcWidth, arcWidth);
            slice.SetColor(focusColor);
            slice.SetTargetAlpha(focusColor.a);
            return;
        }

        // Single：ダブル・トリプル以外の帯（2箇所）
        if (ring == 'S')
        {
            // 内側シングル帯
            var innerKey = areaCode + "_INNER";
            var innerSlice = GetOrCreate(innerKey);
            innerSlice.UpdateShape(board.outerBullRadius, board.tripleInner, centerAngle, arcWidth, arcWidth);
            innerSlice.SetColor(focusColor);
            innerSlice.SetTargetAlpha(focusColor.a);

            // 外側シングル帯
            var outerKey = areaCode + "_OUTER";
            var outerSlice = GetOrCreate(outerKey);
            outerSlice.UpdateShape(board.tripleOuter, board.doubleInner, centerAngle, arcWidth, arcWidth);
            outerSlice.SetColor(focusColor);
            outerSlice.SetTargetAlpha(focusColor.a);
        }
    }

    int FindIndex(int baseScore)
    {
        for (int i = 0; i < scoreMap.Length; i++)
        {
            if (scoreMap[i] == baseScore) return i;
        }
        return -1;
    }

    FocusSlice GetOrCreate(string key)
    {
        if (slices.TryGetValue(key, out var existing)) return existing;

        GameObject go = new GameObject("Focus_" + key);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0, 0, -0.2f);

        var slice = go.AddComponent<FocusSlice>();
        slice.Init(mat, sortingOrder, fadeSpeed);

        slices[key] = slice;
        return slice;
    }
}
