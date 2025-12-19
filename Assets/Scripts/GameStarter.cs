using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class GameStarter : MonoBehaviour
{
    [Header("ビジュアル設定")]
    public Font customFont;
    public Color gateColor = Color.black;
    public Color neonColor = Color.cyan;
    public float slashAngle = 15f;

    // 位置・サイズ調整用
    public Vector2 textOffset = Vector2.zero;
    public float textSizeScale = 1.0f;

    // 内部変数
    Canvas canvas;
    RectTransform gateTop;
    RectTransform gateBottom;
    Image flashPanel;
    Text announceText;

    Vector2 topEndPos;
    Vector2 bottomEndPos;

    public static GameStarter instance;

    void Awake()
    {
        instance = this;
    }

    public void Play(Action onGoTiming, Action onComplete)
    {
        SetupStylishUI();

        Sequence seq = DOTween.Sequence();

        // --- READY ---
        seq.AppendCallback(() =>
        {
            if (announceText != null)
            {
                announceText.text = "READY";
                announceText.color = Color.white;
                announceText.transform.localScale = Vector3.one * textSizeScale;
            }
        });

        if (announceText != null)
        {
            seq.Append(announceText.transform.DOScale(1.1f * textSizeScale, 0.3f).SetLoops(3, LoopType.Yoyo).SetEase(Ease.InOutQuad));
        }

        // --- GO!! & FLASH ---
        seq.AppendCallback(() =>
        {
            if (announceText != null)
            {
                announceText.text = "GO!!";
                announceText.color = neonColor;
                announceText.transform.localScale = Vector3.one * 1.5f * textSizeScale;
            }

            onGoTiming?.Invoke();

            if (flashPanel != null)
            {
                flashPanel.color = new Color(1, 1, 1, 1);
                flashPanel.DOFade(0f, 0.5f).SetEase(Ease.OutSine);
            }
        });

        if (announceText != null)
        {
            seq.Append(announceText.transform.DOScale(5f * textSizeScale, 0.4f).SetEase(Ease.OutExpo));
            seq.Join(announceText.DOFade(0f, 0.2f).SetDelay(0.1f));
        }

        // --- スラッシュオープン ---
        float duration = 0.4f;
        if (gateTop != null) seq.Join(gateTop.DOAnchorPos(topEndPos, duration).SetEase(Ease.OutExpo));
        if (gateBottom != null) seq.Join(gateBottom.DOAnchorPos(bottomEndPos, duration).SetEase(Ease.OutExpo));

        seq.OnComplete(() =>
        {
            onComplete?.Invoke();
            if (canvas != null) canvas.gameObject.SetActive(false);
        });
    }

    void SetupStylishUI()
    {
        GameObject canvasObj = new GameObject("StylishStartCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // フォント取得ロジック（安全策）
        Font useFont = customFont;
        if (useFont == null)
        {
            useFont = Font.CreateDynamicFontFromOSFont("Arial", 50);
            if (useFont == null)
            {
                string[] fonts = Font.GetOSInstalledFontNames();
                if (fonts.Length > 0) useFont = Font.CreateDynamicFontFromOSFont(fonts[0], 50);
            }
        }

        float width = 3500f;
        float height = 2000f;

        // Top Gate
        gateTop = CreatePanel("GateTop", width, height, gateColor);
        gateTop.SetParent(canvasObj.transform, false);
        gateTop.pivot = new Vector2(0.5f, 0f);
        gateTop.anchorMin = new Vector2(0.5f, 0.5f);
        gateTop.anchorMax = new Vector2(0.5f, 0.5f);
        gateTop.anchoredPosition = Vector2.zero;
        gateTop.localRotation = Quaternion.Euler(0, 0, slashAngle);

        CreateNeonLine(gateTop, neonColor, new Vector2(0.5f, 0f));

        // Bottom Gate
        gateBottom = CreatePanel("GateBottom", width, height, gateColor);
        gateBottom.SetParent(canvasObj.transform, false);
        gateBottom.pivot = new Vector2(0.5f, 1f);
        gateBottom.anchorMin = new Vector2(0.5f, 0.5f);
        gateBottom.anchorMax = new Vector2(0.5f, 0.5f);
        gateBottom.anchoredPosition = Vector2.zero;
        gateBottom.localRotation = Quaternion.Euler(0, 0, slashAngle);

        CreateNeonLine(gateBottom, neonColor, new Vector2(0.5f, 1f));

        float moveDistance = 1500f;
        float rad = slashAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));

        topEndPos = direction * moveDistance;
        bottomEndPos = -direction * moveDistance;

        // --- テキスト ---
        GameObject txtObj = new GameObject("AnnounceText");
        txtObj.transform.SetParent(canvasObj.transform, false);
        announceText = txtObj.AddComponent<Text>();
        announceText.font = useFont;
        announceText.fontSize = 150;
        announceText.fontStyle = FontStyle.Italic;
        announceText.alignment = TextAnchor.MiddleCenter;
        announceText.horizontalOverflow = HorizontalWrapMode.Overflow;
        announceText.verticalOverflow = VerticalWrapMode.Overflow;
        announceText.raycastTarget = false;

        Shadow shadow = txtObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(5, -5);

        RectTransform txtRect = announceText.rectTransform;
        txtRect.anchorMin = new Vector2(0.5f, 0.5f);
        txtRect.anchorMax = new Vector2(0.5f, 0.5f);
        txtRect.sizeDelta = new Vector2(1000, 400);
        txtRect.anchoredPosition = textOffset;
        txtRect.localRotation = Quaternion.Euler(0, 0, slashAngle);

        // --- フラッシュパネル ---
        GameObject flashObj = new GameObject("FlashPanel");
        flashObj.transform.SetParent(canvasObj.transform, false);
        flashObj.transform.SetAsLastSibling();

        flashPanel = flashObj.AddComponent<Image>();
        flashPanel.color = new Color(1, 1, 1, 0);
        flashPanel.raycastTarget = false;

        RectTransform flashRect = flashPanel.rectTransform;
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
    }

    RectTransform CreatePanel(string name, float w, float h, Color col)
    {
        GameObject obj = new GameObject(name);
        Image img = obj.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = true;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    void CreateNeonLine(Transform parent, Color col, Vector2 pivot)
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
}