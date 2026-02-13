using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 遊び方パネルのページ切り替え制御
/// タイトルテキストを「HOW TO PLAY  1/4」形式で更新
/// HowToPlayBuilder が自動セットアップする
/// </summary>
public class HowToPlayController : MonoBehaviour
{
    RectTransform[] pages;
    Image[] dots;
    Button prevButton;
    Button nextButton;
    Text titleText;
    RectTransform pagesContainer;

    int currentPage;
    int totalPages;
    bool isTweening;
    float pageWidth;

    [Header("アニメーション")]
    [SerializeField] float slideDuration = 0.25f;
    [SerializeField] Ease slideEase = Ease.OutCubic;

    Color dotActive = new Color(1f, 0.196f, 0.137f, 0.9f);
    Color dotInactive = new Color(1f, 1f, 1f, 0.2f);

    /// <summary>
    /// HowToPlayBuilder から呼ばれるセットアップ
    /// </summary>
    public void Setup(RectTransform container, RectTransform[] pageArray, Image[] dotArray,
        Button prev, Button next, Text title, float width, int count)
    {
        pagesContainer = container;
        pages = pageArray;
        dots = dotArray;
        prevButton = prev;
        nextButton = next;
        titleText = title;
        pageWidth = width;
        totalPages = count;
        currentPage = 0;

        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);

        UpdateView(false);
    }

    /// <summary>
    /// パネルが開かれるたびに1ページ目にリセット
    /// </summary>
    void OnEnable()
    {
        if (pages == null || pages.Length == 0) return;
        currentPage = 0;
        UpdateView(false);
    }

    void PrevPage()
    {
        if (isTweening || currentPage <= 0) return;
        currentPage--;
        UpdateView(true);
    }

    void NextPage()
    {
        if (isTweening || currentPage >= totalPages - 1) return;
        currentPage++;
        UpdateView(true);
    }

    void UpdateView(bool animate)
    {
        // ボタン表示
        if (prevButton != null) prevButton.gameObject.SetActive(currentPage > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(currentPage < totalPages - 1);

        // タイトル更新
        if (titleText != null)
            titleText.text = "HOW TO PLAY  " + (currentPage + 1) + "/" + totalPages;

        // ドット
        if (dots != null)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] != null)
                    dots[i].color = (i == currentPage) ? dotActive : dotInactive;
            }
        }

        // スライド
        float targetX = -currentPage * pageWidth;

        if (animate && pagesContainer != null)
        {
            isTweening = true;
            pagesContainer.DOAnchorPosX(targetX, slideDuration)
                .SetEase(slideEase)
                .SetUpdate(true)
                .OnComplete(() => isTweening = false);
        }
        else if (pagesContainer != null)
        {
            pagesContainer.anchoredPosition = new Vector2(targetX, 0);
        }
    }

    void OnDestroy()
    {
        if (prevButton != null) prevButton.onClick.RemoveListener(PrevPage);
        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
    }
}
