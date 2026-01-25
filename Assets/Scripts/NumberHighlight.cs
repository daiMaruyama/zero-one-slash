using System.Collections;
using TMPro;
using UnityEngine;

public class NumberHighlight : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] TMP_Text scoreText;     // UIでも3DでもOK
    [SerializeField] GameObject background;  // 黒Circle（元数字を隠す蓋）

    [Header("普段の表示（デフォルト）")]
    [SerializeField] Color defaultColor = Color.white;
    [SerializeField, Range(0f, 1f)] float defaultAlpha = 1f;

    [Header("演出設定")]
    [SerializeField] float animDuration = 0.35f;
    [SerializeField] float displayTime = 1.2f;
    [SerializeField] float fadeOutTime = 0.25f;
    [SerializeField] float scaleAmount = 1.5f;

    [Header("浮かび上がり感（任意）")]
    [SerializeField] bool useFloatOffset = true;
    [SerializeField] Vector3 floatLocalOffset = new Vector3(0f, 0f, -0.05f);

    [Header("フェードアウト時の縮み（小さすぎると違和感）")]
    [SerializeField] float fadeScaleTo = 0.95f;

    Vector3 _textBaseScale;
    Vector3 _textBaseLocalPos;
    int _originalNumber;
    Coroutine _animationCoroutine;

    void Awake()
    {
        // 黒Circleは「元画像の数字を隠す」目的なので常時ON
        if (background != null) background.SetActive(true);

        CacheBaseTransform();

        // Initが走らないケースでも0事故らないよう復元
        ResolveOriginalNumberIfNeeded();

        // 普段の数字を表示しておく（ここが大事）
        ApplyDefaultVisuals();
    }

    void CacheBaseTransform()
    {
        if (scoreText == null) return;

        Transform t = scoreText.transform;
        _textBaseScale = t.localScale;
        _textBaseLocalPos = t.localPosition;
    }

    void ResolveOriginalNumberIfNeeded()
    {
        if (_originalNumber > 0) return;

        // ① 名前が "Number_20" 形式ならそこから復元
        const string prefix = "Number_";
        if (name.StartsWith(prefix))
        {
            string s = name.Substring(prefix.Length);
            if (int.TryParse(s, out int n) && n > 0)
            {
                _originalNumber = n;
                return;
            }
        }

        // ② すでにテキストが入ってるならそこから復元
        if (scoreText != null && int.TryParse(scoreText.text, out int fromText) && fromText > 0)
        {
            _originalNumber = fromText;
            return;
        }

        // ③ 最後の保険
        _originalNumber = 20;
    }

    void ApplyDefaultVisuals()
    {
        if (scoreText == null) return;

        scoreText.gameObject.SetActive(true);
        scoreText.text = _originalNumber.ToString();
        scoreText.color = WithAlpha(defaultColor, defaultAlpha);
        scoreText.transform.localScale = _textBaseScale;
        scoreText.transform.localPosition = _textBaseLocalPos;
    }

    public void Init(int number)
    {
        _originalNumber = number;

        if (background != null) background.SetActive(true);

        if (scoreText != null)
        {
            CacheBaseTransform();
            ApplyDefaultVisuals();
        }
    }

    public void PlayPop(int score, Color targetColor)
    {
        gameObject.SetActive(true);

        if (!gameObject.activeInHierarchy)
        {
            // 親がOFFで演出できない場合の保険
            if (scoreText != null)
            {
                scoreText.text = score.ToString();
                scoreText.color = targetColor;
            }
            return;
        }

        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;

            // 途中中断しても普段表示に戻す
            ApplyDefaultVisuals();
        }

        _animationCoroutine = StartCoroutine(PopRoutine(score, targetColor));
    }

    IEnumerator PopRoutine(int score, Color targetColor)
    {
        if (background != null) background.SetActive(true);

        if (scoreText == null)
        {
            _animationCoroutine = null;
            yield break;
        }

        CacheBaseTransform();

        scoreText.gameObject.SetActive(true);

        // 初期化（当たったスコアを表示して浮かび上がり開始）
        scoreText.text = score.ToString();
        scoreText.color = WithAlpha(targetColor, 0f);
        scoreText.transform.localScale = _textBaseScale * 0.9f;
        scoreText.transform.localPosition = _textBaseLocalPos;

        Vector3 fromPos = _textBaseLocalPos;
        Vector3 toPos = useFloatOffset ? (_textBaseLocalPos + floatLocalOffset) : _textBaseLocalPos;

        // ===== 出現（弾む＋浮く＋フェードイン） =====
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / animDuration);

            float alpha = SmoothStep01(t01);
            float elastic = ElasticOut01(t01);

            float scaleMul = 1.0f + (scaleAmount - 1.0f) * elastic;
            scoreText.transform.localScale = _textBaseScale * scaleMul;
            scoreText.transform.localPosition = Vector3.LerpUnclamped(fromPos, toPos, elastic);
            scoreText.color = WithAlpha(targetColor, alpha);

            yield return null;
        }

        scoreText.transform.localScale = _textBaseScale;
        scoreText.transform.localPosition = toPos;
        scoreText.color = WithAlpha(targetColor, 1f);

        // ===== 表示 =====
        yield return new WaitForSeconds(displayTime);

        // ===== 消える（自然に元の数字へ戻す） =====
        elapsed = 0f;
        Color startColor = scoreText.color;
        Color endColor = WithAlpha(defaultColor, defaultAlpha);

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / fadeOutTime);
            float ease = SmoothStep01(t01);

            // “透明に消す” じゃなく “普段の見え方に戻る” ほうが違和感少ない
            scoreText.color = Color.Lerp(startColor, endColor, ease);

            scoreText.transform.localScale = Vector3.Lerp(_textBaseScale, _textBaseScale * fadeScaleTo, ease);
            scoreText.transform.localPosition = Vector3.Lerp(toPos, _textBaseLocalPos, ease);

            yield return null;
        }

        // 最後に普段表示へ完全復帰
        ApplyDefaultVisuals();

        _animationCoroutine = null;
    }

    static Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }

    static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    static float ElasticOut01(float t)
    {
        t = Mathf.Clamp01(t);
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;

        return Mathf.Sin(-13f * (t + 1f) * Mathf.PI * 0.5f) * Mathf.Pow(2f, -10f * t) + 1f;
    }
}
