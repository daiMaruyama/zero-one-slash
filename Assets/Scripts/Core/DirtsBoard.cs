using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class DartsBoard : MonoBehaviour
{
    readonly int[] scoreMap = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    [Header("エリア半径設定")]
    public float bullRadius = 0.5f;
    public float outerBullRadius = 1.0f;
    public float tripleInner = 3.0f;
    public float tripleOuter = 3.5f;
    public float doubleInner = 5.5f;
    public float doubleOuter = 6.0f;
    public float missRadius = 8.0f;

    [Header("演出設定")]
    public Card cardPrefab;

    [Header("ハイライト色（ヒット時演出）")]
    public Color highlightColor = Color.yellow;
    public Color innerBullHighlightColor = new Color(1f, 0.0f, 0.2f);
    public Color outerBullHighlightColor = new Color(1f, 0.3f, 0.0f);

    [Header("ハイライト調整（ヒット時演出）")]
    [Range(10f, 18f)] public float highlightArcWidth = 16.0f;
    [Range(0f, 1f)] public float dimmerIntensity = 0.5f;
    [Range(0f, 1f)] public float heavyDimmerIntensity = 0.75f;

    [Header("ガイド（1投で上がれる場所だけ浮かぶ）")]
    [SerializeField] bool enableAnswerGuide = true;
    [SerializeField] int guideThreshold = 60;

    [Header("ガイド色（上がり方で変える）")]
    [SerializeField] Color guideColorSingle = new Color(0.3f, 1f, 1f, 1f);
    [SerializeField] Color guideColorPower = new Color(1f, 0.2f, 1f, 1f);
    [SerializeField] Color guideColorBull = new Color(1f, 0.6f, 0.1f, 1f);

    [Header("ガイド表示調整")]
    [SerializeField] float guideAlphaSingle = 0.22f;
    [SerializeField] float guideAlphaPower = 0.45f;
    [SerializeField] float guideAlphaBull = 0.35f;

    [SerializeField] float guideFadeIn = 0.35f;
    [SerializeField] float guideArcWidth = 14f;
    [SerializeField] int guideSortingOrder = 18;

    [Tooltip("ヒット演出と同じ平面に出す（ズレ根絶）。基本このままでOK")]
    [SerializeField] float guideZOffsetFromBoard = -2.0f;

    [Header("ガイドを分かりやすくする（点滅/脈動）")]
    [SerializeField] bool guidePulseEnabled = true;
    [SerializeField] float guidePulseSpeed = 2.2f;        // 大きいほど速く点滅
    [SerializeField] float guidePulseMinMul = 0.55f;       // 最低の明るさ倍率
    [SerializeField] float guidePulseMaxMul = 1.15f;       // 最大の明るさ倍率
    [SerializeField] float guidePulsePhaseJitter = 0.35f;  // ばらけさせる（同時に点滅しない）
    
    [Header("ガイド表示調整")]
    [SerializeField] float guideAppearanceDelay = 1.5f; // 何秒待つか
    float _guideTimer = 0f;                            // 経過時間用

    GameManager _gm;
    CheckoutAdvisor _advisor;

    int _lastRemaining = -999;
    int _lastThrows = -999;

    readonly List<SegmentHighlighter> _guideHls = new();

    GameObject dimmerObject;
    public NumberPopManager popManager;

    struct HitResult
    {
        public bool isValid;
        public bool isOut;
        public string areaCode;
        public int score;

        public bool shouldHighlight;
        public bool isRipple;
        public bool isInnerBull;
        public int baseScore;

        public float hlInner;
        public float hlOuter;
        public float hlCenterAngle;
        public float hlArcWidth;
        public Color hlColor;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }

        UpdateAnswerGuide();
    }

    void HandleInput()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId)) return;
        }

        _gm = FindInSceneGameManager();
        if (_gm != null && !_gm.CanThrow) return;

        if (Camera.main == null) return;

        Vector2 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        ThrowCard(tapPos);
    }

    void ThrowCard(Vector2 targetPos)
    {
        if (cardPrefab == null)
        {
            OnCardHit(targetPos);
            return;
        }

        Vector3 startPos = new Vector3(0, -6, -5);
        Card card = Instantiate(cardPrefab, startPos, Quaternion.identity);

        card.Fire(startPos, targetPos, () => OnCardHit(targetPos));
    }

    void OnCardHit(Vector2 hitPos)
    {
        if (HitStopManager.instance) HitStopManager.instance.StopFrame(0.05f);

        HitResult result = CalculateHitResult(hitPos);
        if (!result.isValid) return;

        // NumberPopManager への通知
        // ブル以外の「数字部分」に当たった時だけ数字演出を出す
        if (popManager != null && !result.isOut && !result.isRipple)
        {
            popManager.NotifyHit(result.baseScore, result.score);
        }

        if (result.shouldHighlight)
        {
            SpawnHighlight(result);
        }

        _gm = FindInSceneGameManager();
        if (_gm != null)
        {
            _gm.ProcessHit(result.areaCode, result.score, hitPos);
        }
    }

    HitResult CalculateHitResult(Vector2 tapPos)
    {
        HitResult res = new HitResult();

        Vector2 center = transform.position;
        float distance = Vector2.Distance(tapPos, center);

        if (distance > missRadius)
        {
            res.isValid = false;
            return res;
        }

        res.isValid = true;

        if (distance > doubleOuter)
        {
            res.isOut = true;
            res.areaCode = "OUT";
            res.score = 0;
            return res;
        }

        Vector2 dir = tapPos - center;
        float angleRad = Mathf.Atan2(dir.y, dir.x);
        float angleDeg = angleRad * Mathf.Rad2Deg;

        float correctedAngle = 90 - angleDeg;
        if (correctedAngle < 0) correctedAngle += 360;
        correctedAngle += 9;
        if (correctedAngle >= 360) correctedAngle -= 360;

        int index = (int)(correctedAngle / 18);
        int baseScore = scoreMap[index];
        res.baseScore = baseScore;

        if (distance < bullRadius)
        {
            res.areaCode = "Inner Bull";
            res.score = 50;
            res.shouldHighlight = true;
            res.isRipple = true;
            res.isInnerBull = true;

            res.hlOuter = doubleOuter;
            res.hlColor = innerBullHighlightColor;
        }
        else if (distance < outerBullRadius)
        {
            res.areaCode = "Outer Bull";
            res.score = 25;
            res.shouldHighlight = true;
            res.isRipple = true;
            res.isInnerBull = false;

            res.hlOuter = doubleOuter;
            res.hlColor = outerBullHighlightColor;
        }
        else if (distance >= tripleInner && distance <= tripleOuter)
        {
            res.areaCode = "T" + baseScore;
            res.score = baseScore * 3;
            res.shouldHighlight = true;
            res.isRipple = false;

            res.hlInner = tripleInner;
            res.hlOuter = tripleOuter;
            res.hlCenterAngle = 90f - (index * 18f);
            res.hlArcWidth = highlightArcWidth;
            res.hlColor = highlightColor;
        }
        else if (distance >= doubleInner && distance <= doubleOuter)
        {
            res.areaCode = "D" + baseScore;
            res.score = baseScore * 2;
            res.shouldHighlight = true;
            res.isRipple = false;

            res.hlInner = doubleInner;
            res.hlOuter = doubleOuter;
            res.hlCenterAngle = 90f - (index * 18f);
            res.hlArcWidth = highlightArcWidth;
            res.hlColor = highlightColor;
        }
        else
        {
            res.areaCode = "S" + baseScore;
            res.score = baseScore;
            res.shouldHighlight = false;
        }

        return res;
    }

    void SpawnHighlight(HitResult res)
    {
        float targetIntensity = res.isInnerBull ? heavyDimmerIntensity : dimmerIntensity;

        if (targetIntensity > 0)
        {
            StartCoroutine(FlashDimmer(targetIntensity));
        }

        if (res.isRipple)
        {
            float rippleRadius = res.hlOuter;

            if (res.isInnerBull)
            {
                StartCoroutine(SpawnHeavyBullEffect(rippleRadius));
            }
            else
            {
                float w = rippleRadius * 0.2f;
                CreateHighlighter().RippleEffect(rippleRadius, res.hlColor, 0.8f, w);
            }
        }
        else
        {
            CreateHighlighter().FlashSegment(
                res.hlInner,
                res.hlOuter,
                res.hlCenterAngle,
                res.hlArcWidth,
                res.hlColor
            );
        }
    }

    SegmentHighlighter CreateHighlighter()
    {
        GameObject hlObj = new GameObject("Highlight");
        hlObj.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - 2.0f);

        var hl = hlObj.AddComponent<SegmentHighlighter>();
        return hl;
    }

    IEnumerator SpawnHeavyBullEffect(float maxRadius)
    {
        CreateHighlighter().FlashSegment(0, outerBullRadius, 0, 360, Color.white);

        float heavyWidth = maxRadius * 0.4f;
        CreateHighlighter().RippleEffect(maxRadius, innerBullHighlightColor, 0.6f, heavyWidth);

        yield return new WaitForSeconds(0.12f);

        CreateHighlighter().RippleEffect(maxRadius, innerBullHighlightColor, 1.0f, heavyWidth);
    }

    IEnumerator FlashDimmer(float intensity)
    {
        if (dimmerObject == null)
        {
            dimmerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(dimmerObject.GetComponent<Collider>());
            dimmerObject.name = "BoardDimmer";
            dimmerObject.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - 1.0f);
            dimmerObject.transform.localScale = new Vector3(200, 200, 1);

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0, 0, 0, 0);
            dimmerObject.GetComponent<MeshRenderer>().material = mat;
            dimmerObject.GetComponent<MeshRenderer>().sortingOrder = 10;
        }

        Material dimMat = dimmerObject.GetComponent<MeshRenderer>().material;
        dimMat.color = new Color(0, 0, 0, intensity);

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float a = Mathf.Lerp(intensity, 0.0f, t * t);
            dimMat.color = new Color(0, 0, 0, a);
            yield return null;
        }

        dimMat.color = Color.clear;
    }

    // =========================
    // ガイド表示（1投で上がれる場所だけ）
    // =========================
    void UpdateAnswerGuide()
    {
        if (!enableAnswerGuide)
        {
            ClearGuideHighlights(true);
            return;
        }

        if (_gm == null) _gm = FindInSceneGameManager();
        if (_advisor == null) _advisor = FindInSceneAdvisor();

        if (_gm == null || _advisor == null)
        {
            ClearGuideHighlights(true);
            return;
        }

        // 投げられない状態（リザルト中など）は即消去
        if (!_gm.CanThrow)
        {
            ClearGuideHighlights(false);
            _guideTimer = 0; // タイマーもリセット
            return;
        }

        int remaining = _gm.RemainingScore;
        int throwsLeft = _gm.ThrowsLeft;

        // スコアが変わった（投げた）瞬間にタイマーをリセットしてガイドを消す
        if (remaining != _lastRemaining || throwsLeft != _lastThrows)
        {
            ClearGuideHighlights(false); // 一旦消す
            _lastRemaining = remaining;
            _lastThrows = throwsLeft;
            _guideTimer = 0f; // タイマーリセット
            return;
        }

        // ガイド表示対象外のスコアなら何もしない
        if (remaining > guideThreshold)
        {
            ClearGuideHighlights(true);
            return;
        }

        // 指定秒数経過するまでカウントアップして、足りなければ return
        if (_guideTimer < guideAppearanceDelay)
        {
            _guideTimer += Time.deltaTime;
            return;
        }

        // すでにガイドが出ているなら、これ以降の処理（生成）は不要
        if (_guideHls.Count > 0) return;

        // --- ここからガイド生成 ---
        var finishes = _advisor.GetOneDartFinishAreaCodes(remaining, doubleOutOnly: false);
        if (finishes == null || finishes.Count == 0)
        {
            ClearGuideHighlights(true);
            return;
        }

        ShowGuideHighlights(finishes);
    }

    void ClearGuideHighlights(bool resetCache)
    {
        for (int i = 0; i < _guideHls.Count; i++)
        {
            if (_guideHls[i] != null)
                _guideHls[i].HideGuideAndDestroy(0.12f);
        }
        _guideHls.Clear();

        if (resetCache)
        {
            _lastRemaining = -999;
            _lastThrows = -999;
        }
    }

    void ShowGuideHighlights(List<string> codes)
    {
        ClearGuideHighlights(false);

        for (int i = 0; i < codes.Count; i++)
        {
            CreateGuideForCode(codes[i]);
        }
    }

    Color GetGuideColorByAreaCode(string areaCode)
    {
        if (areaCode == "Inner Bull" || areaCode == "Outer Bull")
            return guideColorBull;

        if (areaCode.StartsWith("D") || areaCode.StartsWith("T"))
            return guideColorPower;

        return guideColorSingle;
    }

    float GetGuideAlphaByAreaCode(string areaCode)
    {
        if (areaCode == "Inner Bull" || areaCode == "Outer Bull")
            return guideAlphaBull;

        if (areaCode.StartsWith("D") || areaCode.StartsWith("T"))
            return guideAlphaPower;

        return guideAlphaSingle;
    }

    void ApplyGuidePulse(SegmentHighlighter hl)
    {
        if (hl == null) return;

        // ばらけさせて「全員同時点滅」を防ぐ
        float phase = Random.Range(-guidePulsePhaseJitter, guidePulsePhaseJitter);

        hl.SetGuidePulse(
            guidePulseEnabled,
            guidePulseSpeed,
            guidePulseMinMul,
            guidePulseMaxMul,
            phase
        );
    }

    void CreateGuideForCode(string areaCode)
    {
        Color col = GetGuideColorByAreaCode(areaCode);
        float a = GetGuideAlphaByAreaCode(areaCode);

        // Bull
        if (areaCode == "Inner Bull")
        {
            var hl = CreateGuideHighlighter();
            hl.ShowGuide(0f, bullRadius, 0f, 360f, col, a, guideFadeIn);
            ApplyGuidePulse(hl);
            return;
        }

        if (areaCode == "Outer Bull")
        {
            var hl = CreateGuideHighlighter();
            hl.ShowGuide(bullRadius, outerBullRadius, 0f, 360f, col, a, guideFadeIn);
            ApplyGuidePulse(hl);
            return;
        }

        // D/T/S
        if (areaCode.Length < 2) return;

        char ring = areaCode[0];
        if (!int.TryParse(areaCode.Substring(1), out int baseScore)) return;

        int index = System.Array.IndexOf(scoreMap, baseScore);
        if (index < 0) return;

        float centerAngle = 90f - (index * 18f);

        if (ring == 'T')
        {
            var hl = CreateGuideHighlighter();
            hl.ShowGuide(tripleInner, tripleOuter, centerAngle, guideArcWidth, col, a, guideFadeIn);
            ApplyGuidePulse(hl);
            return;
        }

        if (ring == 'D')
        {
            var hl = CreateGuideHighlighter();
            hl.ShowGuide(doubleInner, doubleOuter, centerAngle, guideArcWidth, col, a, guideFadeIn);
            ApplyGuidePulse(hl);
            return;
        }

        if (ring == 'S')
        {
            var hl1 = CreateGuideHighlighter();
            hl1.ShowGuide(outerBullRadius, tripleInner, centerAngle, guideArcWidth, col, a, guideFadeIn);
            ApplyGuidePulse(hl1);

            var hl2 = CreateGuideHighlighter();
            hl2.ShowGuide(tripleOuter, doubleInner, centerAngle, guideArcWidth, col, a, guideFadeIn);
            ApplyGuidePulse(hl2);
        }
    }

    SegmentHighlighter CreateGuideHighlighter()
    {
        GameObject hlObj = new GameObject("AnswerGuide");

        hlObj.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            transform.position.z + guideZOffsetFromBoard
        );

        var hl = hlObj.AddComponent<SegmentHighlighter>();
        hl.SetSortingOrder(guideSortingOrder);

        _guideHls.Add(hl);
        return hl;
    }

    // =========================
    // DDOL誤取得対策：同じSceneから拾う
    // =========================
    GameManager FindInSceneGameManager()
    {
        Scene s = gameObject.scene;
        var list = FindObjectsOfType<GameManager>(true);

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null && list[i].gameObject.scene == s)
                return list[i];
        }

        return FindObjectOfType<GameManager>();
    }

    CheckoutAdvisor FindInSceneAdvisor()
    {
        Scene s = gameObject.scene;
        var list = FindObjectsOfType<CheckoutAdvisor>(true);

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null && list[i].gameObject.scene == s)
                return list[i];
        }

        return FindObjectOfType<CheckoutAdvisor>();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, bullRadius);
        Gizmos.DrawWireSphere(transform.position, outerBullRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, tripleInner);
        Gizmos.DrawWireSphere(transform.position, tripleOuter);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, doubleInner);
        Gizmos.DrawWireSphere(transform.position, doubleOuter);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, missRadius);
    }
}
