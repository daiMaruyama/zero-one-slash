using UnityEngine;
using UnityEngine.EventSystems; // 追加：UI越しのタップ防止に必要

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
    public float missRadius = 8.0f; // ★追加：これより外は反応しない、内側ならOUT

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 追加：UI（リザルト画面など）の上をクリックしていたら無視する
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // スマホ対応（タッチ操作の場合）
            if (Input.touchCount > 0)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.touches[0].fingerId))
                {
                    return;
                }
            }

            Vector2 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 center = transform.position;

            CalculateScore(tapPos, center);
        }
    }

    void CalculateScore(Vector2 tapPos, Vector2 center)
    {
        float distance = Vector2.Distance(tapPos, center);

        // 1. 完全な範囲外（タップ無効）
        if (distance > missRadius)
        {
            return; // 何も起きない
        }

        // 2. アウト判定（ダブルの外側 〜 ミス範囲の内側）
        if (distance > doubleOuter)
        {
            Debug.Log("OUT判定！投げたことになります");

            // GameManagerに「OUT」と「0点」を送る
            GameManager gmTemp = FindObjectOfType<GameManager>();
            if (gmTemp != null)
            {
                gmTemp.ProcessHit("OUT", 0, tapPos);
            }
            return;
        }

        // 3. ボード内の判定（既存ロジック）
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

        Debug.Log($"判定: {areaCode} ({finalScore}点)");

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.ProcessHit(areaCode, finalScore, tapPos);
        }
    }

    // ギズモも更新
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

        // アウトエリアを表示（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, missRadius);
    }
}