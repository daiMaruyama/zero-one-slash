using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// リザルトパネルのボタンをネオンレッドテーマ + ホバー演出でスタイルする
/// </summary>
[DefaultExecutionOrder(-5)]
public class ResultPanelBuilder : MonoBehaviour
{
    static readonly Color NeonRed = new Color(1f, 0.196f, 0.137f);

    void Awake()
    {
        foreach (var btn in GetComponentsInChildren<Button>(true))
            StyleButton(btn);
    }

    void StyleButton(Button button)
    {
        if (button == null) return;

        // フレーム
        var outline = button.GetComponent<Outline>();
        if (outline == null) outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(NeonRed.r, NeonRed.g, NeonRed.b, 0.6f);
        outline.effectDistance = new Vector2(3f, -3f);

        // ラベル：白太字 + ネオングロー
        var label = button.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            EnsureGlow(label.gameObject, NeonRed, 0.6f, 2);
        }

        // ホバー/プレス演出
        var nrb = button.gameObject.GetComponent<NeonResultButton>();
        if (nrb == null) nrb = button.gameObject.AddComponent<NeonResultButton>();
        nrb.Init(button.GetComponent<Image>());
    }

    void EnsureGlow(GameObject go, Color baseColor, float alpha, int strength)
    {
        Shadow shadow = null;
        Outline outline = null;

        foreach (var s in go.GetComponents<Shadow>())
        {
            if (s is Outline o) outline = o;
            else shadow = s;
        }

        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        shadow.effectDistance = new Vector2(strength, -strength);

        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.6f);
        outline.effectDistance = new Vector2(strength, -strength);
    }
}
