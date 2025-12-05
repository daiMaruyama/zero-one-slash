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
    public Color innerBullHighlightColor = new Color(1f, 0.0f, 0.2f);
    public Color outerBullHighlightColor = new Color(1f, 0.3f, 0.0f);

    [Header("ハイライト調整")]
    [Range(10f, 18f)] public float highlightArcWidth = 16.0f; // 18度より少し狭くしてワイヤー被りを防ぐ
    [Range(0f, 1f)] public float dimmerIntensity = 0.5f;

    private GameObject dimmerObject;

    // ヒット情報構造体
    private struct HitResult
    {
        public bool isValid;
        public bool isOut;
        public string areaCode;
        public int score;

        // 演出用
        public bool shouldHighlight;
        public bool isRipple;       // 波紋モードか？
        public bool isDoubleRipple; // インナーブル用

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
            // Inner Bull (波紋 x2)
            res.areaCode = "Inner Bull";
            res.score = 50;
            res.shouldHighlight = true;
            res.isRipple = true;
            res.isDoubleRipple = true;
            // 波紋なので画面端まで広げる
            res.hlOuter = doubleOuter;
            res.hlColor = innerBullHighlightColor;
        }
        else if (distance < outerBullRadius)
        {
            // Outer Bull (波紋 x1)
            res.areaCode = "Outer Bull";
            res.score = 25;
            res.shouldHighlight = true;
            res.isRipple = true;
            res.isDoubleRipple = false;
            // 波紋なので画面端まで広げる
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

            // 厳密な半径を使用
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

            // 厳密な半径を使用
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
        if (dimmerIntensity > 0)
        {
            StartCoroutine(FlashDimmer());
        }

        if (res.isRipple)
        {
            // 波紋モード
            float rippleRadius = res.hlOuter; // doubleOuterが入っている

            if (res.isDoubleRipple)
            {
                // インナーブル：2連波紋
                StartCoroutine(SpawnDoubleRipple(rippleRadius));
            }
            else
            {
                // アウターブル：単発波紋
                float w = rippleRadius * 0.2f;
                CreateHighlighter().RippleEffect(rippleRadius, res.hlColor, 1.0f, w);
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

    IEnumerator SpawnDoubleRipple(float maxRadius)
    {
        float w = maxRadius * 0.2f;
        CreateHighlighter().RippleEffect(maxRadius, innerBullHighlightColor, 1.2f, w);
        yield return new WaitForSeconds(0.15f);
        CreateHighlighter().RippleEffect(maxRadius, innerBullHighlightColor, 1.5f, w);
    }

    IEnumerator FlashDimmer()
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
        dimMat.color = new Color(0, 0, 0, dimmerIntensity);

        float duration = 0.3f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float a = Mathf.Lerp(dimmerIntensity, 0.0f, t);
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
}