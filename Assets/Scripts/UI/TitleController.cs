using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class TitleController : MonoBehaviour
{
    [Header("必須設定")]
    public string gameSceneName = "Main";

    [Header("タイトルUI")]
    public RectTransform slashTop;
    public RectTransform slashBottom;
    public Text versionText;

    [Header("音声")]
    public AudioClip seDecide;
    public AudioClip seSlam;

    [Header("演出調整")]
    public float slashAngle = 15f;
    public float closeSpeed = 0.3f;
    public float shakePower = 50f;

    [Header("ゲート設定")]
    public float gateOpenDistance = 1500f;
    public Color gateColor = new Color(0.03f, 0.01f, 0.05f);
    public Color neonTopColor = new Color(1f, 0.196f, 0.137f);
    public Color neonBottomColor = new Color(1f, 0.196f, 0.137f);

    RectTransform gateTop;
    RectTransform gateBottom;
    CanvasGroup flashPanel;
    RectTransform shakeTarget;

    GameObject transitionCanvasGO;
    AudioSource audioSource;

    Vector2 endPosTop, endPosBottom;
    bool isTransitioning = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (AudioManager.instance != null) audioSource.volume = AudioManager.instance.seVolume;

        GenerateStylishGates();

        if (versionText != null)
        {
            Color c = versionText.color; c.a = 0f; versionText.color = c;
        }

        if (slashTop != null && slashBottom != null)
        {
            endPosTop = slashTop.anchoredPosition;
            endPosBottom = slashBottom.anchoredPosition;

            slashTop.anchoredPosition = new Vector2(-2800, endPosTop.y);
            slashBottom.anchoredPosition = new Vector2(2800, endPosBottom.y);

            PlayEntrance();
        }
    }

    /// <summary>
    /// TitleUIManagerのGAME STARTボタンから呼ばれる
    /// </summary>
    public void StartGame()
    {
        if (isTransitioning) return;
        StartGateTransition();
    }

    void OnDestroy()
    {
        transform.DOKill();

        if (gateTop != null) gateTop.DOKill();
        if (gateBottom != null) gateBottom.DOKill();
        if (flashPanel != null) flashPanel.DOKill();
        if (versionText != null) versionText.DOKill();

        if (transitionCanvasGO != null) Destroy(transitionCanvasGO);
    }

    void PlayEntrance()
    {
        slashTop.DOAnchorPos(endPosTop, 0.5f).SetEase(Ease.OutExpo).SetDelay(0.2f);
        slashBottom.DOAnchorPos(endPosBottom, 0.5f).SetEase(Ease.OutExpo).SetDelay(0.4f)
            .OnComplete(FadeInUI);
    }

    void FadeInUI()
    {
        if (versionText != null)
        {
            versionText.DOFade(1f, 1.0f);
        }
    }

    void StartGateTransition()
    {
        if (gateTop == null || gateBottom == null) return;

        isTransitioning = true;

        if (seDecide != null) audioSource.PlayOneShot(seDecide);

        gateTop.DOKill();
        gateBottom.DOKill();

        Sequence seq = DOTween.Sequence().SetLink(gameObject);

        // ゲートクローズ
        seq.Append(gateTop.DOAnchorPos(Vector2.zero, closeSpeed).SetEase(Ease.InExpo));
        seq.Join(gateBottom.DOAnchorPos(Vector2.zero, closeSpeed).SetEase(Ease.InExpo));

        seq.OnComplete(() =>
        {
            if (seSlam != null) audioSource.PlayOneShot(seSlam);

            if (shakeTarget != null)
                shakeTarget.DOShakeAnchorPos(0.5f, shakePower, 20, 90, false, true);

            if (flashPanel != null)
            {
                flashPanel.alpha = 0.5f;
                flashPanel.DOFade(0f, 0.4f).SetLink(gameObject);
            }

            DOVirtual.DelayedCall(0.5f, () =>
            {
                DestroyDontDestroyGameManager();
                SceneManager.LoadScene(gameSceneName);
            }).SetLink(gameObject);
        });
    }

    void GenerateStylishGates()
    {
        GameObject exist = GameObject.Find("TransitionCanvas");
        if (exist != null) Destroy(exist);

        transitionCanvasGO = new GameObject("TransitionCanvas");

        Canvas transCanvas = transitionCanvasGO.AddComponent<Canvas>();
        transCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transCanvas.sortingOrder = 1000;

        CanvasScaler scaler = transitionCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject containerGO = new GameObject("ShakeContainer");
        containerGO.transform.SetParent(transitionCanvasGO.transform, false);

        shakeTarget = containerGO.AddComponent<RectTransform>();
        shakeTarget.anchorMin = Vector2.zero;
        shakeTarget.anchorMax = Vector2.one;
        shakeTarget.sizeDelta = Vector2.zero;
        shakeTarget.anchoredPosition = Vector2.zero;

        float width = 3500f;
        float height = 2000f;

        float rad = slashAngle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));

        // 上ゲート
        gateTop = CreateGate(shakeTarget, "GateTop", gateColor, width, height);
        gateTop.pivot = new Vector2(0.5f, 0f);
        gateTop.anchorMin = new Vector2(0.5f, 0.5f);
        gateTop.anchorMax = new Vector2(0.5f, 0.5f);
        gateTop.localRotation = Quaternion.Euler(0, 0, slashAngle);
        gateTop.anchoredPosition = dir * gateOpenDistance;
        CreateNeonGlow(gateTop, neonTopColor, new Vector2(0.5f, 0f));
        CreateScanLines(gateTop, height, 24);

        // 下ゲート
        gateBottom = CreateGate(shakeTarget, "GateBottom", gateColor, width, height);
        gateBottom.pivot = new Vector2(0.5f, 1f);
        gateBottom.anchorMin = new Vector2(0.5f, 0.5f);
        gateBottom.anchorMax = new Vector2(0.5f, 0.5f);
        gateBottom.localRotation = Quaternion.Euler(0, 0, slashAngle);
        gateBottom.anchoredPosition = -dir * gateOpenDistance;
        CreateNeonGlow(gateBottom, neonBottomColor, new Vector2(0.5f, 1f));
        CreateScanLines(gateBottom, height, 24);

        // フラッシュパネル（赤みがかった白）
        GameObject flashGO = new GameObject("FlashPanel");
        flashGO.transform.SetParent(transCanvas.transform, false);
        flashGO.transform.SetAsLastSibling();

        Image flashImg = flashGO.AddComponent<Image>();
        flashImg.color = new Color(1f, 0.85f, 0.8f);
        flashImg.raycastTarget = false;

        RectTransform flashRT = flashGO.GetComponent<RectTransform>();
        flashRT.anchorMin = Vector2.zero;
        flashRT.anchorMax = Vector2.one;
        flashRT.offsetMin = Vector2.zero;
        flashRT.offsetMax = Vector2.zero;

        flashPanel = flashGO.AddComponent<CanvasGroup>();
        flashPanel.alpha = 0f;
    }

    // === ゲート生成 ===

    RectTransform CreateGate(Transform parent, string name, Color col, float w, float h)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Image img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;

        RectTransform rt = go.GetComponent<RectTransform>();
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

    void DestroyDontDestroyGameManager()
    {
        foreach (var gm in FindObjectsOfType<GameManager>())
        {
            if (gm.gameObject.scene.name == "DontDestroyOnLoad")
                Destroy(gm.gameObject);
        }
    }
}
