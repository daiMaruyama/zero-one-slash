using UnityEngine;

public class DartsBoard : MonoBehaviour
{
    // 並び順（上から時計回り）
    readonly int[] scoreMap = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    [Header("中心からの距離設定")]
    public float bullRadius = 0.5f;       // インブル
    public float outerBullRadius = 1.0f;  // アウターブル
    public float tripleInner = 3.0f;      // トリプル内側
    public float tripleOuter = 3.5f;      // トリプル外側
    public float doubleInner = 5.5f;      // ダブル内側
    public float doubleOuter = 6.0f;      // ダブル外側

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 center = transform.position;

            // 判定実行
            CalculateScore(tapPos, center);
        }
    }

    void CalculateScore(Vector2 tapPos, Vector2 center)
    {
        float distance = Vector2.Distance(tapPos, center);

        // 1. アウト判定
        if (distance > doubleOuter)
        {
            Debug.Log("OUT");
            return;
        }

        // 2. エリアの種類（S, D, T, Bull）と倍率を判定
        string typePrefix = "S";
        int multiplier = 1; // 倍率
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

        // 3. 数字（1〜20）を判定
        Vector2 dir = tapPos - center;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 角度補正（時計回りに直す）
        float correctedAngle = 90 - angle;
        if (correctedAngle < 0) correctedAngle += 360;
        correctedAngle += 9;
        if (correctedAngle >= 360) correctedAngle -= 360;

        int index = (int)(correctedAngle / 18);
        int baseScore = scoreMap[index];

        // 4. エリアコードと最終スコアの生成
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

        // ★GameManagerに「エリア名」と「点数」の両方を報告
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            gm.ProcessHit(areaCode, finalScore, tapPos);
        }
    }

    // デバッグ用表示
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
    }
}