using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class GameStarter : MonoBehaviour
{
    [Header("ビジュアル設定")]
    public Font customFont;
    public Color gateColor = Color.black;

    // ここを上下別にする
    public Color neonTopColor = Color.cyan;
    public Color neonBottomColor = Color.magenta;

    public float slashAngle = 15f;

    [Header("位置・サイズ調整")]
    public Vector2 textOffset = Vector2.zero;
    public float textSizeScale = 1.0f;

    [Header("カウントダウン設定")]
    public int countdownFrom = 3;
    public float countStep = 0.9f;
    public float goStep = 0.35f;
    public float goHold = 0.1f;

    [Header("ゲート開き距離（Titleと揃える）")]
    public float gateOpenDistance = 1500f;

    Canvas _canvas;
    RectTransform _gateTop;
    RectTransform _gateBottom;
    CanvasGroup _flashGroup;
    Text _announceText;
    CanvasGroup _announceGroup;

    Vector2 _gateTopOpenPos;
    Vector2 _gateBottomOpenPos;

    Sequence _seq;

    public void Play(Action onGoTiming, Action onComplete)
    {
        EnsureUi();

        _canvas.gameObject.SetActive(true);
        ResetUiClosed();

        _seq?.Kill();
        _seq = DOTween.Sequence().SetLink(gameObject);

        AppendCountdown(_seq, Mathf.Max(1, countdownFrom));
        AppendGo(_seq, onGoTiming);
        AppendOpenGates(_seq);

        _seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        });
    }

    void AppendCountdown(Sequence seq, int from)
    {
        for (int n = from; n >= 1; n--)
        {
            int num = n;

            seq.AppendCallback(() =>
            {
                if (_announceText == null) return;

                _announceText.text = num.ToString();
                _announceText.color = Color.white;

                _announceText.transform.localScale = Vector3.one * 1.9f * textSizeScale;
                _announceGroup.alpha = 1f;
            });

            seq.Append(_announceText.transform
                .DOScale(1.0f * textSizeScale, countStep)
                .SetEase(Ease.OutBack));

            seq.Join(_announceGroup
                .DOFade(0f, countStep)
                .SetEase(Ease.InQuad));
        }
    }

    void AppendGo(Sequence seq, Action onGoTiming)
    {
        seq.AppendCallback(() =>
        {
            if (_announceText != null)
            {
                _announceText.text = "GO!!";
                // GOは上のネオン色に寄せる（見た目が締まる）
                _announceText.color = neonTopColor;

                _announceText.transform.localScale = Vector3.one * 1.6f * textSizeScale;
                _announceGroup.alpha = 1f;
            }

            onGoTiming?.Invoke();

            if (_flashGroup != null)
            {
                _flashGroup.alpha = 1f;
                _flashGroup.DOFade(0f, 0.45f).SetEase(Ease.OutSine).SetLink(gameObject);
            }
        });

        seq.Append(_announceText.transform
            .DOScale(4.6f * textSizeScale, goStep)
            .SetEase(Ease.OutExpo));

        seq.Join(_announceGroup
            .DOFade(0f, 0.18f)
            .SetDelay(0.08f));

        if (goHold > 0f) seq.AppendInterval(goHold);
    }

    void AppendOpenGates(Sequence seq)
    {
        float duration = 0.4f;

        if (_gateTop != null)
            seq.Join(_gateTop.DOAnchorPos(_gateTopOpenPos, duration).SetEase(Ease.OutExpo));

        if (_gateBottom != null)
            seq.Join(_gateBottom.DOAnchorPos(_gateBottomOpenPos, duration).SetEase(Ease.OutExpo));
    }

    void EnsureUi()
    {
        if (_canvas != null) return;

        GameObject canvasObj = new GameObject("StylishStartCanvas");
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Font useFont = ResolveFont(customFont);

        float width = 3500f;
        float height = 2000f;

        _gateTop = CreatePanel(canvasObj.transform, "GateTop", width, height, gateColor);
        _gateTop.pivot = new Vector2(0.5f, 0f);
        _gateTop.anchorMin = new Vector2(0.5f, 0.5f);
        _gateTop.anchorMax = new Vector2(0.5f, 0.5f);
        _gateTop.anchoredPosition = Vector2.zero;
        _gateTop.localRotation = Quaternion.Euler(0, 0, slashAngle);
        CreateNeonLine(_gateTop, neonTopColor, new Vector2(0.5f, 0f));

        _gateBottom = CreatePanel(canvasObj.transform, "GateBottom", width, height, gateColor);
        _gateBottom.pivot = new Vector2(0.5f, 1f);
        _gateBottom.anchorMin = new Vector2(0.5f, 0.5f);
        _gateBottom.anchorMax = new Vector2(0.5f, 0.5f);
        _gateBottom.anchoredPosition = Vector2.zero;
        _gateBottom.localRotation = Quaternion.Euler(0, 0, slashAngle);
        CreateNeonLine(_gateBottom, neonBottomColor, new Vector2(0.5f, 1f));

        float rad = slashAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        _gateTopOpenPos = dir * gateOpenDistance;
        _gateBottomOpenPos = -dir * gateOpenDistance;

        GameObject txtObj = new GameObject("AnnounceText");
        txtObj.transform.SetParent(canvasObj.transform, false);

        _announceText = txtObj.AddComponent<Text>();
        _announceText.font = useFont;
        _announceText.fontSize = 150;
        _announceText.fontStyle = FontStyle.Italic;
        _announceText.alignment = TextAnchor.MiddleCenter;
        _announceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _announceText.verticalOverflow = VerticalWrapMode.Overflow;
        _announceText.raycastTarget = false;

        _announceGroup = txtObj.AddComponent<CanvasGroup>();

        Shadow shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(5, -5);

        RectTransform txtRect = _announceText.rectTransform;
        txtRect.anchorMin = new Vector2(0.5f, 0.5f);
        txtRect.anchorMax = new Vector2(0.5f, 0.5f);
        txtRect.sizeDelta = new Vector2(1000, 400);
        txtRect.anchoredPosition = textOffset;
        txtRect.localRotation = Quaternion.Euler(0, 0, slashAngle);

        GameObject flashObj = new GameObject("FlashPanel");
        flashObj.transform.SetParent(canvasObj.transform, false);
        flashObj.transform.SetAsLastSibling();

        Image flashImage = flashObj.AddComponent<Image>();
        flashImage.color = Color.white;
        flashImage.raycastTarget = false;

        _flashGroup = flashObj.AddComponent<CanvasGroup>();
        _flashGroup.alpha = 0f;

        RectTransform flashRect = flashObj.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
    }

    void ResetUiClosed()
    {
        if (_announceText != null)
        {
            _announceText.text = string.Empty;
            _announceText.color = Color.white;
            _announceText.transform.localScale = Vector3.one * textSizeScale;
        }

        if (_announceGroup != null) _announceGroup.alpha = 0f;
        if (_flashGroup != null) _flashGroup.alpha = 0f;

        if (_gateTop != null) _gateTop.anchoredPosition = Vector2.zero;
        if (_gateBottom != null) _gateBottom.anchoredPosition = Vector2.zero;
    }

    static Font ResolveFont(Font preferred)
    {
        if (preferred != null) return preferred;

        Font font = Font.CreateDynamicFontFromOSFont("Arial", 50);
        if (font != null) return font;

        string[] fonts = Font.GetOSInstalledFontNames();
        if (fonts != null && fonts.Length > 0)
            return Font.CreateDynamicFontFromOSFont(fonts[0], 50);

        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }

    static RectTransform CreatePanel(Transform parent, string name, float w, float h, Color col)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    static void CreateNeonLine(Transform parent, Color col, Vector2 pivot)
    {
        GameObject line = new GameObject("NeonLine");
        line.transform.SetParent(parent, false);

        Image img = line.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;

        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, pivot.y);
        rt.anchorMax = new Vector2(1, pivot.y);
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(0, 15);
        rt.anchoredPosition = Vector2.zero;
    }

    void OnDestroy()
    {
        _seq?.Kill();
        if (_canvas != null) Destroy(_canvas.gameObject);
    }
}
