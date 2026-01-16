using System.Collections.Generic;
using UnityEngine;

public class CheckoutAdvisor : MonoBehaviour
{
    public struct ThrowOption
    {
        public string areaCode;
        public int score;

        public ThrowOption(string code, int s)
        {
            areaCode = code;
            score = s;
        }
    }

    // ----------------------------
    // 1投フィニッシュ候補を返す
    // ----------------------------
    // doubleOutOnly = true なら「DとInnerBull(50)だけ」に絞る（ダブルアウト風）
    // false なら「S/D/T/Bull全部OK」（マスターアウト風）
    public List<string> GetOneDartFinishAreaCodes(int remaining, bool doubleOutOnly = false)
    {
        var list = new List<string>();

        if (remaining <= 0) return list;

        // Bull
        if (remaining == 50)
        {
            list.Add("Inner Bull");
            return list;
        }

        if (!doubleOutOnly && remaining == 25)
        {
            list.Add("Outer Bull");
            return list;
        }

        // Double
        if (remaining % 2 == 0)
        {
            int d = remaining / 2;
            if (d >= 1 && d <= 20) list.Add("D" + d);
        }

        if (!doubleOutOnly)
        {
            // Triple
            if (remaining % 3 == 0)
            {
                int t = remaining / 3;
                if (t >= 1 && t <= 20) list.Add("T" + t);
            }

            // Single
            if (remaining >= 1 && remaining <= 20)
            {
                list.Add("S" + remaining);
            }
        }

        return list;
    }

    // ----------------------------
    // ルート探索（必要なら使う用）
    // 「最後がマスターアウト(D/T/InnerBull)」を優先
    // 無理ならシングルフィニッシュも許可
    // ----------------------------
    public bool TryGetCheckoutRoute(int remaining, int dartsLeft, out List<ThrowOption> route)
    {
        route = new List<ThrowOption>();

        if (remaining <= 0) return false;
        if (dartsLeft <= 0) return false;

        var options = BuildPreferredOptions(remaining);

        // 1) マスターアウト優先（D/T/InnerBullで終わる）
        if (Search(remaining, dartsLeft, options, route, requireMasterOutFinish: true))
            return true;

        // 2) 無理なら妥協（最後がSでもOK）
        route.Clear();
        if (Search(remaining, dartsLeft, options, route, requireMasterOutFinish: false))
            return true;

        return false;
    }

    bool IsMasterOutFinish(string areaCode)
    {
        if (areaCode == "Inner Bull") return true;
        if (areaCode.StartsWith("D")) return true;
        if (areaCode.StartsWith("T")) return true;
        return false;
    }

    List<ThrowOption> BuildPreferredOptions(int remaining)
    {
        var list = new List<ThrowOption>();

        // それっぽく：まずトリプル狙い
        for (int i = 20; i >= 1; i--)
        {
            int t = i * 3;
            if (t <= remaining) list.Add(new ThrowOption("T" + i, t));
        }

        // 次にダブル
        for (int i = 20; i >= 1; i--)
        {
            int d = i * 2;
            if (d <= remaining) list.Add(new ThrowOption("D" + i, d));
        }

        // 最後にシングル
        for (int i = 20; i >= 1; i--)
        {
            if (i <= remaining) list.Add(new ThrowOption("S" + i, i));
        }

        // Bull
        list.Add(new ThrowOption("Inner Bull", 50));
        list.Add(new ThrowOption("Outer Bull", 25));

        return list;
    }

    bool Search(int remaining, int dartsLeft, List<ThrowOption> options, List<ThrowOption> route, bool requireMasterOutFinish)
    {
        if (remaining == 0) return true;
        if (dartsLeft == 0) return false;

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            if (opt.score > remaining) continue;

            // 最後の1投は exact で合わせる
            if (dartsLeft == 1)
            {
                if (opt.score != remaining) continue;

                if (requireMasterOutFinish && !IsMasterOutFinish(opt.areaCode))
                    continue;

                route.Add(opt);
                return true;
            }

            route.Add(opt);

            if (Search(remaining - opt.score, dartsLeft - 1, options, route, requireMasterOutFinish))
                return true;

            route.RemoveAt(route.Count - 1);
        }

        return false;
    }
}
