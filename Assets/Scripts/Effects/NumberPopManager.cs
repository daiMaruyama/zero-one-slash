using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class NumberPopManager : MonoBehaviour
{
    [Header("¶¬Ý’è")]
    [SerializeField] NumberHighlight numberPrefab;
    [SerializeField] float radius = 2.8f;

    [Header("FÝ’è")]
    [SerializeField] Color singleColor = Color.white;
    [SerializeField] Color doubleColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] Color tripleColor = new Color(0.4f, 1f, 0.4f);

    [SerializeField] NumberHighlight[] numberObjects = new NumberHighlight[20];
    readonly int[] scoreMap = { 20, 1, 18, 4, 13, 6, 10, 15, 2, 17, 3, 19, 7, 16, 8, 11, 14, 9, 12, 5 };

    [ContextMenu("”Žš‚ðŽ©“®¶¬‚µ‚Ä”z’u")]
    public void GenerateAndAlign()
    {
        if (numberPrefab == null) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        numberObjects = new NumberHighlight[20];

        for (int i = 0; i < 20; i++)
        {
            int baseScore = scoreMap[i];
            NumberHighlight instance;
#if UNITY_EDITOR
            instance = (NumberHighlight)PrefabUtility.InstantiatePrefab(numberPrefab, transform);
#else
            instance = Instantiate(numberPrefab, transform);
#endif
            instance.name = $"Number_{baseScore}";

            float angleDeg = 90f - (i * 18f);
            float angleRad = angleDeg * Mathf.Deg2Rad;
            instance.transform.localPosition = new Vector3(Mathf.Cos(angleRad) * radius, Mathf.Sin(angleRad) * radius, -0.2f);

            instance.Init(baseScore);
            numberObjects[i] = instance;
        }
    }

    public void NotifyHit(int baseScore, int actualScore)
    {
        if (baseScore <= 0) return;
        int index = System.Array.IndexOf(scoreMap, baseScore);
        if (index < 0 || index >= numberObjects.Length || numberObjects[index] == null) return;

        int multiplier = actualScore / baseScore;
        Color color = (multiplier == 3) ? tripleColor : (multiplier == 2) ? doubleColor : singleColor;

        numberObjects[index].PlayPop(actualScore, color);
    }
}