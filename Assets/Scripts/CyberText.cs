using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Text))]
public class CyberText : MonoBehaviour
{
    Text uiText;

    // 現在の状態を保存する変数
    int currentValue = -999999;
    string currentText = "";

    void Awake()
    {
        uiText = GetComponent<Text>();
    }

    // 数値更新
    public void SetValue(string prefix, int value)
    {
        if (uiText == null) return;

        // 値が変わっていなければ処理しない
        if (value == currentValue) return;

        currentValue = value;
        // テキストモードの状態をリセット（重要）
        currentText = "";

        string finalString = prefix + value.ToString();

        uiText.DOKill();

        // 色変化などの演出は削除し、動きのみ
        uiText.color = Color.white;

        // 0.15秒で素早く切り替える
        uiText.DOText(finalString, 0.15f, true, ScrambleMode.Numerals)
            .SetEase(Ease.OutQuad);
    }

    // 文字列更新（WINやBUSTなど）
    public void SetText(string text)
    {
        if (uiText == null) return;

        // 文字が変わっていなければ処理しない
        if (text == currentText) return;

        currentText = text;
        // 数値モードの状態をリセット（重要）
        currentValue = -999999;

        uiText.DOKill();
        uiText.color = Color.white;

        // 0.2秒で素早く切り替える
        uiText.DOText(text, 0.2f, true, ScrambleMode.Uppercase)
            .SetEase(Ease.OutQuad);
    }

    void OnDestroy()
    {
        if (uiText != null) uiText.DOKill();
    }
}