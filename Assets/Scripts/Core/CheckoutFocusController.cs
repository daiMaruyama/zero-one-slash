using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheckoutFocusController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameManager gameManager;
    [SerializeField] CheckoutAdvisor advisor;
    [SerializeField] FocusOverlayManager focusOverlay;

    [Header("WorldSpace Guide Text")]
    [SerializeField] Text worldGuideText; // ←WorldSpace CanvasのText

    [Header("条件")]
    [SerializeField] int focusThreshold = 60;

    List<CheckoutAdvisor.ThrowOption> route = new();
    List<string> focusCodes = new();

    int lastRemaining = -999;
    int lastThrows = -999;

    void Start()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (advisor == null) advisor = FindObjectOfType<CheckoutAdvisor>();
        if (focusOverlay == null) focusOverlay = FindObjectOfType<FocusOverlayManager>();
    }

    void Update()
    {
        if (gameManager == null || advisor == null || focusOverlay == null) return;

        if (!gameManager.CanThrow)
        {
            Clear();
            return;
        }

        int remaining = gameManager.RemainingScore;
        int throwsLeft = gameManager.ThrowsLeft;

        if (remaining > focusThreshold)
        {
            Clear();
            return;
        }

        // 同じ状態なら更新しない（無駄なチラつき防止）
        if (remaining == lastRemaining && throwsLeft == lastThrows) return;

        lastRemaining = remaining;
        lastThrows = throwsLeft;

        if (advisor.TryGetCheckoutRoute(remaining, throwsLeft, out route))
        {
            // ルート内の“全部”をフォーカス対象にする（重複除去）
            focusCodes.Clear();
            for (int i = 0; i < route.Count; i++)
            {
                string code = route[i].areaCode;
                if (!focusCodes.Contains(code)) focusCodes.Add(code);
            }

            focusOverlay.SetFocusAreaCodes(focusCodes);

            // ガイドもWorldSpaceに表示
            if (worldGuideText != null)
            {
                worldGuideText.text = BuildGuideText(remaining, route);
            }
        }
        else
        {
            Clear();
        }
    }

    void Clear()
    {
        focusOverlay.ClearFocus();
        if (worldGuideText != null) worldGuideText.text = "";
        lastRemaining = -999;
        lastThrows = -999;
    }

    string BuildGuideText(int remaining, List<CheckoutAdvisor.ThrowOption> route)
    {
        int temp = remaining;
        string s = "CHECKOUT\n";

        for (int i = 0; i < route.Count; i++)
        {
            temp -= route[i].score;
            s += $"{route[i].areaCode} → {temp}\n";
        }

        return s.TrimEnd();
    }
}
