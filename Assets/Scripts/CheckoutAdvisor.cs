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

    static readonly List<ThrowOption> AllOptions = BuildAllOptions();

    static List<ThrowOption> BuildAllOptions()
    {
        var list = new List<ThrowOption>();

        // Bull
        list.Add(new ThrowOption("Inner Bull", 50));
        list.Add(new ThrowOption("Outer Bull", 25));

        // Singles / Doubles / Triples
        for (int i = 1; i <= 20; i++)
        {
            list.Add(new ThrowOption("T" + i, i * 3));
            list.Add(new ThrowOption("D" + i, i * 2));
            list.Add(new ThrowOption("S" + i, i));
        }

        return list;
    }

    /// <summary>
    /// remaining を dartsLeft 投以内で 0 にできるルートを返す。
    /// まず「最後がマスターアウト(D/T/InnerBull)」のルートを優先。
    /// 無理なら「最後シングルでもOK」のルートで妥協。
    /// </summary>
    public bool TryGetCheckoutRoute(int remaining, int dartsLeft, out List<ThrowOption> route)
    {
        route = new List<ThrowOption>();
        if (remaining <= 0) return false;
        if (dartsLeft <= 0) return false;

        var options = GetPreferredOptions(remaining);

        // 1) まずマスターアウトで上がるルートを探す
        if (Search(remaining, dartsLeft, options, route, requireMasterOutFinish: true))
            return true;

        // 2) 無理なら妥協（最後がシングルでもOK）
        route.Clear();
        if (Search(remaining, dartsLeft, options, route, requireMasterOutFinish: false))
            return true;

        return false;
    }

    // マスターアウトの「最後の1投」判定
    bool IsMasterOutFinish(string areaCode)
    {
        // 一般的には「D/T/InnerBull(50)」がフィニッシュ扱いでそれっぽい
        if (areaCode == "Inner Bull") return true;
        if (areaCode.StartsWith("D")) return true;
        if (areaCode.StartsWith("T")) return true;
        return false;
    }

    // 「それっぽく見える」順（大きいのから）
    List<ThrowOption> GetPreferredOptions(int remaining)
    {
        var list = new List<ThrowOption>();

        // Triple → Double → Single
        for (int i = 20; i >= 1; i--)
        {
            int t = i * 3;
            if (t <= remaining) list.Add(new ThrowOption("T" + i, t));
        }

        for (int i = 20; i >= 1; i--)
        {
            int d = i * 2;
            if (d <= remaining) list.Add(new ThrowOption("D" + i, d));
        }

        for (int i = 20; i >= 1; i--)
        {
            if (i <= remaining) list.Add(new ThrowOption("S" + i, i));
        }

        // Bullは最後（うるさくなりがちだから）
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

            // 「最後の1投」なら制約をかける
            if (dartsLeft == 1)
            {
                if (opt.score != remaining) continue;

                // マスターアウト優先モードなら、最後の1投はD/T/InnerBullのみ許可
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

    public List<string> GetOneDartFinishAreaCodes(int remaining, bool masterOutOnly = false)
    {
        var list = new List<string>();

        if (remaining <= 0) return list;

        // Bull
        if (!masterOutOnly)
        {
            if (remaining == 25) list.Add("Outer Bull");
        }
        if (remaining == 50) list.Add("Inner Bull");

        // 1〜20のS/D/T
        for (int i = 1; i <= 20; i++)
        {
            // Single
            if (!masterOutOnly && i == remaining) list.Add("S" + i);

            // Double
            if (i * 2 == remaining) list.Add("D" + i);

            // Triple
            if (!masterOutOnly && i * 3 == remaining) list.Add("T" + i);
        }

        return list;
    }
}
