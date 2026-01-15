using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

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

    [Header("ハイライト色")]
    public Color highlightColor = Color.yellow;
    public Color innerBullHighlightColor = new Color(1f, 0.0f, 0.2f); // 赤系
    public Color outerBullHighlightColor = new Color(1f, 0.3f, 0.0f); // オレンジ系

    [Header("ハイライト調整")]
    [Range(10f, 18f)] public float highlightArcWidth = 16.0f;
    [Range(0f, 1f)] public float dimmerIntensity = 0.5f;
    [Range(0f, 1f)] public float heavyDimmerIntensity = 0.75f; // インブル用の強い暗転

    [Header("ガイド（アンサー部分だけ浮かぶ）")]
    [SerializeField] bool enableAnswerGuide = true;
    [SerializeField] int guideThreshold = 60;
    [SerializeField] Color guideColor = new Color(0.3f, 1f, 1f, 1f);
    [SerializeField] float guideAlpha = 0.22f;
    [SerializeField] float guideFadeIn = 0.35f;
    [SerializeField] float guideArcWidth = 14f;
    [SerializeField] int guideSortingOrder = 12;

    GameManager _gm;
    CheckoutAdvisor _advisor;

    int _lastRemaining = -999;
    int _lastThrows = -999;

    readonly System.Collections.Generic.List<SegmentHighlighter> _guideHls = new();
    readonly System.Collections.Generic.List<string> _guideCodes = new();


    GameObject dimmerObject;

    // ヒット情報構造体
    struct HitResult
    {
        public bool isValid;
        public bool isOut;
        public string areaCode;
        public int score;

        // 演出用
        public bool shouldHighlight;
        public bool isRipple;        // 波紋モードか？
        public bool isInnerBull;     // インブル判定用に追加

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
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId)) return;

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null && !gm.CanThrow) return;

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

        if (result.shouldHighlight)
        {
            SpawnHighlight(result);
        }

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.ProcessHit(result.areaCode, result.score, hitPos);
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

        if (distance < bullRadius)
        {
            // Inner Bull (重い演出)
            res.areaCode = "Inner Bull";
            res.score = 50;
            res.shouldHighlight = true;
            res.isRipple = true;
            res.isInnerBull = true;

            // 修正: 半径をダブルアウターまでに変更
            res.hlOuter = doubleOuter;
            res.hlColor = innerBullHighlightColor;
        }
        else if (distance < outerBullRadius)
        {
            // Outer Bull (通常波紋)
            res.areaCode = "Outer Bull";
            res.score = 25;
            res.shouldHighlight = true;
            res.isRipple = true;
            res.isInnerBull = false;

            // 修正: 半径をダブルアウターまでに変更
            res.hlOuter = doubleOuter;
            res.hlColor = outerBullHighlightColor;
        }
        else if (distance >= tripleInner && distance <= tripleOuter)
        {
            // Triple (静止画)
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
            // Double (静止画)
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
            // Single
            res.areaCode = "S" + baseScore;
            res.score = baseScore;
            res.shouldHighlight = false;
        }

        return res;
    }

    void SpawnHighlight(HitResult res)
    {
        // インブルかどうかで暗転の強さを変える
        float targetIntensity = res.isInnerBull ? heavyDimmerIntensity : dimmerIntensity;

        if (targetIntensity > 0)
        {
            StartCoroutine(FlashDimmer(targetIntensity));
        }

        if (res.isRipple)
        {
            // 波紋モード
            float rippleRadius = res.hlOuter; // doubleOuterが入っている

            if (res.isInnerBull)
            {
                // インブル：重厚な演出
                StartCoroutine(SpawnHeavyBullEffect(rippleRadius));
            }
            else
            {
                // アウターブル：単発波紋
                float w = rippleRadius * 0.2f;
                CreateHighlighter().RippleEffect(rippleRadius, res.hlColor, 0.8f, w);
            }
        }
        else
        {
            // 静止画モード
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
        return hlObj.AddComponent<SegmentHighlighter>();
    }

    // ダーツライブ風の重いインブル演出
    IEnumerator SpawnHeavyBullEffect(float maxRadius)
    {
        // 1. 着弾瞬間の白い閃光 (インパクト)
        // アウターブル半径までの一瞬の白フラッシュ
        CreateHighlighter().FlashSegment(0, outerBullRadius, 0, 360, Color.white);

        // 2. 1発目の重い波紋
        // 幅を太く(0.4倍)、速度は少し遅くして重量感を出す
        float heavyWidth = maxRadius * 0.4f;
        CreateHighlighter().RippleEffect(maxRadius, innerBullHighlightColor, 0.6f, heavyWidth);

        // 少し溜める (連射間隔を調整)
        yield return new WaitForSeconds(0.12f);

        // 3. 2発目の余韻波紋
        // 1発目より少しゆっくり広がる
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
            // 減衰カーブを少し緩やかにして暗さを少し維持する
            float a = Mathf.Lerp(intensity, 0.0f, t * t);
            dimMat.color = new Color(0, 0, 0, a);
            yield return null;
        }
        dimMat.color = Color.clear;
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
    public void HighlightAreaForFocus(string areaCode)
    {
        // Bull系
        if (areaCode.Contains("Bull"))
        {
            var hl = CreateHighlighter();
            hl.RippleEffect(doubleOuter, areaCode == "Inner Bull" ? innerBullHighlightColor : outerBullHighlightColor, 0.6f, doubleOuter * 0.25f);
            return;
        }

        // S/D/T の解析
        if (areaCode.Length < 2) return;

        char ring = areaCode[0];
        if (!int.TryParse(areaCode.Substring(1), out int baseScore)) return;

        // baseScore → indexに変換（scoreMapに従う）
        int index = -1;
        for (int i = 0; i < scoreMap.Length; i++)
        {
            if (scoreMap[i] == baseScore)
            {
                index = i;
                break;
            }
        }
        if (index < 0) return;

        float inner = 0f;
        float outer = 0f;

        if (ring == 'T')
        {
            inner = tripleInner;
            outer = tripleOuter;
        }
        else if (ring == 'D')
        {
            inner = doubleInner;
            outer = doubleOuter;
        }
        else
        {
            // シングル：見せたいなら外周の帯に寄せる（好みで）
            inner = outerBullRadius;
            outer = tripleInner;
        }

        float centerAngle = 90f - (index * 18f);

        CreateHighlighter().FlashSegment(
            inner,
            outer,
            centerAngle,
            highlightArcWidth,
            highlightColor
        );
    }
    void UpdateAnswerGuide()
    {
        if (!enableAnswerGuide) return;

        if (_gm == null) _gm = FindObjectOfType<GameManager>();
        if (_advisor == null) _advisor = FindObjectOfType<CheckoutAdvisor>();

        if (_gm == null || _advisor == null)
        {
            ClearGuideHighlights();
            return;
        }

        // 投げられない瞬間は消す
        if (!_gm.CanThrow)
        {
            ClearGuideHighlights();
            return;
        }

        int remaining = _gm.RemainingScore;
        int throwsLeft = _gm.ThrowsLeft;

        // 60点以下だけ
        if (remaining > guideThreshold)
        {
            ClearGuideHighlights();
            return;
        }

        // 状態変わってないなら更新しない
        if (remaining == _lastRemaining && throwsLeft == _lastThrows) return;
        _lastRemaining = remaining;
        _lastThrows = throwsLeft;

        // 1投で上がれる場所だけ光らせる
        var finishes = _advisor.GetOneDartFinishAreaCodes(remaining, masterOutOnly: false);

        if (finishes.Count == 0)
        {
            // 1投で上がれないなら光らない
            ClearGuideHighlights();
            return;
        }

        // 上がれる場合は「その1投で上がれる場所だけ」静かに浮かぶ
        ShowGuideHighlights(finishes);
    }

    void ClearGuideHighlights()
    {
        for (int i = 0; i < _guideHls.Count; i++)
        {
            if (_guideHls[i] != null)
                _guideHls[i].HideGuideAndDestroy(0.12f);
        }
        _guideHls.Clear();
    }

    void ShowGuideHighlights(System.Collections.Generic.List<string> codes)
    {
        ClearGuideHighlights();

        for (int i = 0; i < codes.Count; i++)
        {
            CreateGuideForCode(codes[i]);
        }
    }

    void CreateGuideForCode(string areaCode)
    {
        // Bull
        if (areaCode == "Inner Bull")
        {
            var hl = CreateGuideHighlighter();
            hl.ShowGuide(0f, bullRadius, 0f, 360f, guideColor, guideAlpha, guideFadeIn);
            return;
        }
        if (areaCode == "Outer Bull")
        {
            var hl = CreateGuideHighlighter();
            hl.ShowGuide(bullRadius, outerBullRadius, 0f, 360f, guideColor, guideAlpha, guideFadeIn);
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
            hl.ShowGuide(tripleInner, tripleOuter, centerAngle, guideArcWidth, guideColor, guideAlpha, guideFadeIn);
            return;
        }

        if (ring == 'D')
        {
            var hl = CreateGuideHighlighter();
            hl.ShowGuide(doubleInner, doubleOuter, centerAngle, guideArcWidth, guideColor, guideAlpha, guideFadeIn);
            return;
        }

        if (ring == 'S')
        {
            // Singleは「ダブル/トリプル以外」2本の帯
            var hl1 = CreateGuideHighlighter();
            hl1.ShowGuide(outerBullRadius, tripleInner, centerAngle, guideArcWidth, guideColor, guideAlpha, guideFadeIn);

            var hl2 = CreateGuideHighlighter();
            hl2.ShowGuide(tripleOuter, doubleInner, centerAngle, guideArcWidth, guideColor, guideAlpha, guideFadeIn);
        }
    }

    SegmentHighlighter CreateGuideHighlighter()
    {
        GameObject hlObj = new GameObject("AnswerGuide");
        hlObj.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z - 0.25f);

        var hl = hlObj.AddComponent<SegmentHighlighter>();
        hl.SetSortingOrder(guideSortingOrder);

        _guideHls.Add(hl);
        return hl;
    }
}