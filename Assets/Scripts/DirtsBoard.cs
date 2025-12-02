using UnityEngine;

public class DartsBoard : MonoBehaviour
{
    // 並び順（上から時計回り）
    // 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5
    readonly int[] scoreMap = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    // 距離のしきい値（Unityのインスペクターで画像に合わせて調整する）
    [Header("中心からの距離設定")]
    public float bullRadius = 0.5f;       // インブルの半径
    public float outerBullRadius = 1.0f;  // アウターブルの半径
    public float tripleInner = 3.0f;      // トリプルの内側
    public float tripleOuter = 3.5f;      // トリプルの外側
    public float doubleInner = 5.5f;      // ダブルの内側
    public float doubleOuter = 6.0f;      // ダブルの外側（これより外はアウト）

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 tapPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // ボードの中心位置
            Vector2 center = transform.position;

            // 1. 判定処理を実行
            CalculateScore(tapPos, center);
        }
    }

    void CalculateScore(Vector2 tapPos, Vector2 center)
    {
        // 中心からの距離を計算
        float distance = Vector2.Distance(tapPos, center);

        // 中心からの角度を計算（Atan2は -180〜180度を返す）
        Vector2 dir = tapPos - center;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // --- 倍率判定 ---
        int multiplier = 1;
        string typeName = "Single";

        if (distance > doubleOuter) { Debug.Log("OUT"); return; }

        if (distance < bullRadius)
        {
            Debug.Log("Inner Bull (50)"); return;
        }
        else if (distance < outerBullRadius)
        {
            Debug.Log("Outer Bull (25)"); return;
        }
        else if (distance >= tripleInner && distance <= tripleOuter)
        {
            multiplier = 3; typeName = "Triple";
        }
        else if (distance >= doubleInner && distance <= doubleOuter)
        {
            multiplier = 2; typeName = "Double";
        }

        // --- 数字判定 ---
        // 角度を補正（真上を20に）
        // Atan2は右(3時方向)が0度なので、90度(12時方向)に合わせるためにずらす
        float correctedAngle = 90 - angle;
        if (correctedAngle < 0) correctedAngle += 360;

        // 20等分（1エリア18度）だが、20の中心が真上なので、半分の9度ずらして計算
        correctedAngle += 9;
        if (correctedAngle >= 360) correctedAngle -= 360;

        // 配列のインデックス（何番目の数字か）を計算
        int index = (int)(correctedAngle / 18);
        int baseScore = scoreMap[index];

        // 結果発表
        int finalScore = baseScore * multiplier;

        Debug.Log($"判定: {typeName} {baseScore} = {finalScore}点");

        // ここで正解判定（例：残り32のときに D16 かどうか）を行う
    }

    // デバッグ用
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