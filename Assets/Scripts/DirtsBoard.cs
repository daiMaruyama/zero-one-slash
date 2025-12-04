using UnityEngine;
using UnityEngine.EventSystems;

public class DartsBoard : MonoBehaviour
{
    readonly int[] scoreMap = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    [Header("中心からの距離設定")]
    public float bullRadius = 0.5f;
    public float outerBullRadius = 1.0f;
    public float tripleInner = 3.0f;
    public float tripleOuter = 3.5f;
    public float doubleInner = 5.5f;
    public float doubleOuter = 6.0f;

    [Header("アウト判定の設定")]
    public float missRadius = 8.0f;

    [Header("演出用")]
    public Card cardPrefab;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI越しのタップ防止
            if (EventSystem.current.IsPointerOverGameObject()) return;
            if (Input.touchCount > 0)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId)) return;
            }

            // 連射防止の確認
            // GameManagerを探して、投げていい状態か聞く
            GameManager gm = FindObjectOfType<GameManager>();

            // GMがいない、または「投げちゃダメ(CanThrowがfalse)」なら無視
            if (gm != null && !gm.CanThrow) return;

            Vector2 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 center = transform.position;

            ThrowCard(tapPos, center);
        }
    }

    void ThrowCard(Vector2 targetPos, Vector2 center)
    {
        if (cardPrefab != null)
        {
            // 画面の中央下から発射
            Vector3 startPos = new Vector3(0, -6, -5);

            Card card = Instantiate(cardPrefab, startPos, Quaternion.identity);

            // 発射！第3引数が「刺さった時に実行する中身」
            card.Fire(startPos, targetPos, () => {

                // === 刺さった瞬間の処理 ===

                // 1. ヒットストップ（ここで使用！）
                // ここでHitStopManagerを呼び出し、時間を一瞬止める
                if (HitStopManager.instance) HitStopManager.instance.StopFrame(0.05f);

                // 2. 判定とスコア計算
                CalculateScore(targetPos, center);
            });
        }
        else
        {
            // Prefab設定がない場合の予備動作
            CalculateScore(targetPos, center);
        }
    }

    void CalculateScore(Vector2 tapPos, Vector2 center)
    {
        float distance = Vector2.Distance(tapPos, center);

        if (distance > missRadius) return;

        // アウト判定
        if (distance > doubleOuter)
        {
            GameManager gmTemp = FindObjectOfType<GameManager>();
            if (gmTemp != null) gmTemp.ProcessHit("OUT", 0, tapPos);
            return;
        }

        string typePrefix = "S";
        int multiplier = 1;
        bool isBull = false;
        string bullName = "";
        int bullScore = 0;

        if (distance < bullRadius)
        {
            isBull = true;
            bullName = "Inner Bull";
            bullScore = 50;
        }
        else if (distance < outerBullRadius)
        {
            isBull = true;
            bullName = "Outer Bull";
            bullScore = 25;
        }
        else if (distance >= tripleInner && distance <= tripleOuter)
        {
            typePrefix = "T";
            multiplier = 3;
        }
        else if (distance >= doubleInner && distance <= doubleOuter)
        {
            typePrefix = "D";
            multiplier = 2;
        }

        Vector2 dir = tapPos - center;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        float correctedAngle = 90 - angle;
        if (correctedAngle < 0) correctedAngle += 360;
        correctedAngle += 9;
        if (correctedAngle >= 360) correctedAngle -= 360;

        int index = (int)(correctedAngle / 18);
        int baseScore = scoreMap[index];

        string areaCode = "";
        int finalScore = 0;

        if (isBull)
        {
            areaCode = bullName;
            finalScore = bullScore;
        }
        else
        {
            areaCode = typePrefix + baseScore;
            finalScore = baseScore * multiplier;
        }

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.ProcessHit(areaCode, finalScore, tapPos);
        }
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