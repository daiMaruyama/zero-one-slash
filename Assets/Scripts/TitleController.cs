using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class TitleController : MonoBehaviour
{
    [Header("必須設定")]
    public string gameSceneName = "GameScene";

    [Header("UI制御")]
    public GameObject settingsWindow;

    [Header("タイトルUI")]
    public RectTransform slashTop;
    public RectTransform slashBottom;
    public Text touchText;
    public Text versionText;

    [Header("音声")]
    public AudioClip seDecide;
    public AudioClip seSlam;

    [Header("演出調整")]
    public float slashAngle = 15f;
    public float closeSpeed = 0.35f;
    public float shakePower = 50f;

    // 自動生成変数
    RectTransform gateTop;
    RectTransform gateBottom;
    CanvasGroup flashPanel;
    RectTransform shakeTarget;

    AudioSource audioSource;
    Vector2 endPosTop, endPosBottom;
    bool isTransitioning = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        if (AudioManager.instance != null) audioSource.volume = AudioManager.instance.seVolume;

        GenerateStylishGates();

        // テキストを透明状態で初期化
        if (touchText != null)
        {
            Color c = touchText.color;
            c.a = 0f;
            touchText.color = c;
        }
        if (versionText != null)
        {
            Color c = versionText.color;
            c.a = 0f;
            versionText.color = c;
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

    void Update()
    {
        // すでに遷移中なら何もしない
        if (isTransitioning) return;

        // 設定ウィンドウが開いているなら何もしない
        if (settingsWindow != null && settingsWindow.activeSelf)
        {
            return;
        }

        // クリック（タップ）されたら
        if (Input.GetMouseButtonDown(0))
        {
            // もしクリックした場所にUI（ボタンなど）があったら、ここで中断！
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // UIじゃなければ、ゲーム開始演出へ
            StartGateTransition();
        }
    }

    void OnDestroy()
    {
        transform.DOKill();
        if (gateTop != null) gateTop.DOKill();
        if (gateBottom != null) gateBottom.DOKill();
        if (flashPanel != null) flashPanel.DOKill();
        if (touchText != null) touchText.DOKill();
        if (versionText != null) versionText.DOKill();
    }

    void PlayEntrance()
    {
        // ロゴ入場
        slashTop.DOAnchorPos(endPosTop, 0.5f).SetEase(Ease.OutExpo).SetDelay(0.2f);
        slashBottom.DOAnchorPos(endPosBottom, 0.5f).SetEase(Ease.OutExpo).SetDelay(0.4f)
            .OnComplete(() =>
            {
                FadeInUI();
            });
    }

    void FadeInUI()
    {
        // テキストフェードイン
        if (touchText != null)
        {
            touchText.DOFade(1f, 1.0f).OnComplete(() =>
            {
                touchText.DOFade(0f, 1.5f).SetLoops(-1, LoopType.Yoyo);
            });
        }
        if (versionText != null)
        {
            versionText.DOFade(1f, 1.0f);
        }
    }

    void StartGateTransition()
    {
        isTransitioning = true;
        if (seDecide != null) audioSource.PlayOneShot(seDecide);

        if (touchText != null)
        {
            touchText.DOKill();
            touchText.color = new Color(touchText.color.r, touchText.color.g, touchText.color.b, 1f);
            touchText.transform.DOScale(1.2f, 0.05f).SetLoops(2, LoopType.Yoyo);
        }

        float overlap = 50f;

        gateTop.DOAnchorPosY(-overlap, closeSpeed).SetEase(Ease.InExpo);

        gateBottom.DOAnchorPosY(overlap, closeSpeed).SetEase(Ease.InExpo)
            .OnComplete(() =>
            {
                if (seSlam != null) audioSource.PlayOneShot(seSlam);

                if (shakeTarget != null)
                    shakeTarget.DOShakeAnchorPos(0.5f, shakePower, 20, 90, false, true);

                if (flashPanel != null)
                {
                    flashPanel.alpha = 1f;
                    flashPanel.DOFade(0f, 0.5f);
                }

                DOVirtual.DelayedCall(0.5f, () => SceneManager.LoadScene(gameSceneName));
            });
    }

    void GenerateStylishGates()
    {
        GameObject canvasGO = new GameObject("TransitionCanvas");
        Canvas transCanvas = canvasGO.AddComponent<Canvas>();
        transCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transCanvas.sortingOrder = 999;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject containerGO = new GameObject("ShakeContainer");
        containerGO.transform.SetParent(canvasGO.transform, false);
        shakeTarget = containerGO.AddComponent<RectTransform>();
        shakeTarget.anchorMin = Vector2.zero; shakeTarget.anchorMax = Vector2.one;
        shakeTarget.sizeDelta = Vector2.zero;

        float width = 3500f;
        float height = 2000f;

        gateTop = CreateGate(shakeTarget, "Auto_GateTop", Color.black, width, height);
        gateTop.pivot = new Vector2(0.5f, 0f);
        gateTop.anchorMin = new Vector2(0.5f, 0.5f);
        gateTop.anchorMax = new Vector2(0.5f, 0.5f);
        gateTop.anchoredPosition = new Vector2(0, 1500);
        gateTop.localRotation = Quaternion.Euler(0, 0, slashAngle);
        CreateNeonLine(gateTop, Color.cyan, new Vector2(0.5f, 0f));

        gateBottom = CreateGate(shakeTarget, "Auto_GateBottom", Color.black, width, height);
        gateBottom.pivot = new Vector2(0.5f, 1f);
        gateBottom.anchorMin = new Vector2(0.5f, 0.5f);
        gateBottom.anchorMax = new Vector2(0.5f, 0.5f);
        gateBottom.anchoredPosition = new Vector2(0, -1500);
        gateBottom.localRotation = Quaternion.Euler(0, 0, slashAngle);
        CreateNeonLine(gateBottom, Color.magenta, new Vector2(0.5f, 1f));

        GameObject flashGO = new GameObject("Auto_FlashPanel");
        flashGO.transform.SetParent(transCanvas.transform, false);
        Image img = flashGO.AddComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
        RectTransform rt = flashGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        flashPanel = flashGO.AddComponent<CanvasGroup>();
        flashPanel.alpha = 0f;
    }

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

    void CreateNeonLine(Transform parent, Color col, Vector2 pivot)
    {
        GameObject go = new GameObject("NeonLine");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = col;
        RectTransform rt = go.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0, pivot.y);
        rt.anchorMax = new Vector2(1, pivot.y);
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 15);
    }
}