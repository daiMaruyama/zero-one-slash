using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class GameStarter : MonoBehaviour
{
    [Header("ビジュアル設定")]
    public Font customFont;
    public Color gateColor = new Color(0.03f, 0.01f, 0.05f);
    public Color neonColor = new Color(1f, 0.196f, 0.137f);
    public float slashAngle = 15f;

    [Header("位置・サイズ設定")]
    public Vector2 textOffset = Vector2.zero;
    public float textSizeScale = 1.0f;

    [Header("カウントダウン設定")]
    public int countdownFrom = 3;
    public float countStep = 0.75f;
    public float goStep = 0.3f;
    public float goHold = 0.1f;

    [Header("ゲートオープン距離")]
    public float gateOpenDistance = 1500f;

    // 後方互換（Inspectorで設定済みの場合に使われる）
    [HideInInspector] public Color neonTopColor = new Color(1f, 0.196f, 0.137f);
    [HideInInspector] public Color neonBottomColor = new Color(1f, 0.196f, 0.137f);

    Canvas _canvas;
    RectTransform _gateTop;
    RectTransform _gateBottom;
    CanvasGroup _flashGroup;
    CanvasGroup _pulseGroup;
    Text _announceText;
    CanvasGroup _announceGroup;
    Outline _announceOutline;

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
        Color useNeon = neonColor.a > 0.01f ? neonColor : neonTopColor;

        for (int n = from; n >= 1; n--)
        {
            int num = n;

            seq.AppendCallback(() =>
            {
                if (_announceText == null) return;

                _announceText.text = num.ToString();
                _announceText.color = Color.white;

                if (_announceOutline != null)
                    _announceOutline.effectColor = new Color(useNeon.r, useNeon.g, useNeon.b, 0.7f);

                _announceText.transform.localScale = Vector3.one * 2.2f * textSizeScale;
                _announceGroup.alpha = 1f;

                // カウントごとの赤パルス
                if (_pulseGroup != null)
                {
                    _pulseGroup.alpha = 0.3f;
                    _pulseGroup.DOFade(0f, 0.3f).SetEase(Ease.OutSine).SetLink(gameObject);
                }
            });

            seq.Append(_announceText.transform
                .DOScale(0.9f * textSizeScale, countStep)
                .SetEase(Ease.OutExpo));

            seq.Join(_announceGroup
                .DOFade(0f, countStep * 0.85f)
                .SetDelay(countStep * 0.15f)
                .SetEase(Ease.InQuad));
        }
    }

    void AppendGo(Sequence seq, Action onGoTiming)
    {
        Color useNeon = neonColor.a > 0.01f ? neonColor : neonTopColor;

        seq.AppendCallback(() =>
        {
            if (_announceText != null)
            {
                _announceText.text = "GO!!";
                _announceText.color = new Color(1f, 0.95f, 0.92f);

                if (_announceOutline != null)
                    _announceOutline.effectColor = new Color(useNeon.r, useNeon.g, useNeon.b, 0.9f);

                _announceText.transform.localScale = Vector3.one * 1.8f * textSizeScale;
                _announceGroup.alpha = 1f;
            }

            onGoTiming?.Invoke();

            // フラッシュ（赤みがかった白）
            if (_flashGroup != null)
            {
                _flashGroup.alpha = 0.8f;
                _flashGroup.DOFade(0f, 0.4f).SetEase(Ease.OutSine).SetLink(gameObject);
            }

            // パルス
            if (_pulseGroup != null)
            {
                _pulseGroup.alpha = 0.5f;
                _pulseGroup.DOFade(0f, 0.4f).SetEase(Ease.OutSine).SetLink(gameObject);
            }
        });

        seq.Append(_announceText.transform
            .DOScale(5f * textSizeScale, goStep)
            .SetEase(Ease.OutExpo));

        seq.Join(_announceGroup
            .DOFade(0f, goStep * 0.6f)
            .SetDelay(goStep * 0.25f));

        if (goHold > 0f) seq.AppendInterval(goHold);
    }

    void AppendOpenGates(Sequence seq)
    {
        float duration = 0.35f;

        if (_gateTop != null)
            seq.Join(_gateTop.DOAnchorPos(_gateTopOpenPos, duration).SetEase(Ease.OutExpo));

        if (_gateBottom != null)
            seq.Join(_gateBottom.DOAnchorPos(_gateBottomOpenPos, duration).SetEase(Ease.OutExpo));
    }

    void EnsureUi()
    {
        if (_canvas != null) return;

        Color useNeon = neonColor.a > 0.01f ? neonColor : neonTopColor;

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

        // 上ゲート
        _gateTop = CreatePanel(canvasObj.transform, "GateTop", width, height, gateColor);
        _gateTop.pivot = new Vector2(0.5f, 0f);
        _gateTop.anchorMin = new Vector2(0.5f, 0.5f);
        _gateTop.anchorMax = new Vector2(0.5f, 0.5f);
        _gateTop.anchoredPosition = Vector2.zero;
        _gateTop.localRotation = Quaternion.Euler(0, 0, slashAngle);
        CreateNeonGlow(_gateTop, useNeon, new Vector2(0.5f, 0f));
        CreateScanLines(_gateTop, height, 24);

        // 下ゲート
        _gateBottom = CreatePanel(canvasObj.transform, "GateBottom", width, height, gateColor);
        _gateBottom.pivot = new Vector2(0.5f, 1f);
        _gateBottom.anchorMin = new Vector2(0.5f, 0.5f);
        _gateBottom.anchorMax = new Vector2(0.5f, 0.5f);
        _gateBottom.anchoredPosition = Vector2.zero;
        _gateBottom.localRotation = Quaternion.Euler(0, 0, slashAngle);
        CreateNeonGlow(_gateBottom, useNeon, new Vector2(0.5f, 1f));
        CreateScanLines(_gateBottom, height, 24);

        float rad = slashAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
        _gateTopOpenPos = dir * gateOpenDistance;
        _gateBottomOpenPos = -dir * gateOpenDistance;

        // パルスオーバーレイ（赤いパルス演出用）
        GameObject pulseObj = new GameObject("PulseOverlay");
        pulseObj.transform.SetParent(canvasObj.transform, false);
        Image pulseImg = pulseObj.AddComponent<Image>();
        pulseImg.color = new Color(useNeon.r, useNeon.g, useNeon.b, 1f);
        pulseImg.raycastTarget = false;
        _pulseGroup = pulseObj.AddComponent<CanvasGroup>();
        _pulseGroup.alpha = 0f;
        RectTransform pulseRT = pulseObj.GetComponent<RectTransform>();
        pulseRT.anchorMin = Vector2.zero;
        pulseRT.anchorMax = Vector2.one;
        pulseRT.offsetMin = Vector2.zero;
        pulseRT.offsetMax = Vector2.zero;

        // カウントダウンテキスト
        GameObject txtObj = new GameObject("AnnounceText");
        txtObj.transform.SetParent(canvasObj.transform, false);

        _announceText = txtObj.AddComponent<Text>();
        _announceText.font = useFont;
        _announceText.fontSize = 180;
        _announceText.fontStyle = FontStyle.Bold;
        _announceText.alignment = TextAnchor.MiddleCenter;
        _announceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _announceText.verticalOverflow = VerticalWrapMode.Overflow;
        _announceText.raycastTarget = false;

        _announceGroup = txtObj.AddComponent<CanvasGroup>();

        // ネオングロー（Outline + Shadow）
        _announceOutline = txtObj.AddComponent<Outline>();
        _announceOutline.effectColor = new Color(useNeon.r, useNeon.g, useNeon.b, 0.7f);
        _announceOutline.effectDistance = new Vector2(3, -3);

        Shadow shadow1 = txtObj.AddComponent<Shadow>();
        shadow1.effectColor = new Color(useNeon.r, useNeon.g, useNeon.b, 0.4f);
        shadow1.effectDistance = new Vector2(6, -6);

        Shadow shadow2 = txtObj.AddComponent<Shadow>();
        shadow2.effectColor = new Color(0, 0, 0, 0.6f);
        shadow2.effectDistance = new Vector2(4, -4);

        RectTransform txtRect = _announceText.rectTransform;
        txtRect.anchorMin = new Vector2(0.5f, 0.5f);
        txtRect.anchorMax = new Vector2(0.5f, 0.5f);
        txtRect.sizeDelta = new Vector2(1000, 400);
        txtRect.anchoredPosition = textOffset;
        txtRect.localRotation = Quaternion.Euler(0, 0, slashAngle);

        // フラッシュパネル（赤みがかった白）
        GameObject flashObj = new GameObject("FlashPanel");
        flashObj.transform.SetParent(canvasObj.transform, false);
        flashObj.transform.SetAsLastSibling();

        Image flashImage = flashObj.AddComponent<Image>();
        flashImage.color = new Color(1f, 0.85f, 0.8f);
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
        if (_pulseGroup != null) _pulseGroup.alpha = 0f;

        if (_gateTop != null) _gateTop.anchoredPosition = Vector2.zero;
        if (_gateBottom != null) _gateBottom.anchoredPosition = Vector2.zero;
    }

    // === ユーティリティ ===

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

    /// <summary>
    /// ネオングロー（3層の線で光のにじみを表現）
    /// </summary>
    void CreateNeonGlow(Transform parent, Color col, Vector2 pivot)
    {
        // 外側グロー（太く薄い）
        CreateLine(parent, "NeonGlow_Outer", new Color(col.r, col.g, col.b, 0.12f), pivot, 48f);
        // 中間グロー
        CreateLine(parent, "NeonGlow_Mid", new Color(col.r, col.g, col.b, 0.4f), pivot, 10f);
        // コアライン（細く明るい白に近い色）
        CreateLine(parent, "NeonCore", new Color(1f, 0.9f, 0.85f, 0.95f), pivot, 2.5f);
    }

    void CreateLine(Transform parent, string name, Color col, Vector2 pivot, float thickness)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, pivot.y);
        rt.anchorMax = new Vector2(1, pivot.y);
        rt.pivot = pivot;
        rt.sizeDelta = new Vector2(0, thickness);
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// スキャンライン（サイバー感の横線）
    /// </summary>
    void CreateScanLines(Transform parent, float totalHeight, int count)
    {
        float spacing = totalHeight / (count + 1);

        for (int i = 0; i < count; i++)
        {
            GameObject line = new GameObject("ScanLine");
            line.transform.SetParent(parent, false);

            Image img = line.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.015f);
            img.raycastTarget = false;

            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(0, -totalHeight * 0.5f + spacing * (i + 1));
        }
    }

    void OnDestroy()
    {
        _seq?.Kill();
        if (_canvas != null) Destroy(_canvas.gameObject);
    }
}
