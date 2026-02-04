using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider seSlider;
    public Button closeButton; // 閉じるボタン
    public GameObject panelRoot; // パネル本体（表示/非表示用）

    // ゲーム中かどうか判定するために必要
    bool isGameScene = false;

    void Start()
    {
        // 自分がいるシーンの名前で判断
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        isGameScene = (sceneName == "GameScene");

        if (AudioManager.instance != null)
        {
            if (bgmSlider != null)
            {
                bgmSlider.value = AudioManager.instance.bgmVolume;
                bgmSlider.onValueChanged.AddListener(OnBgmChange);
            }
            if (seSlider != null)
            {
                seSlider.value = AudioManager.instance.seVolume;
                seSlider.onValueChanged.AddListener(OnSeChange);
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    // パネルを開く時に呼ぶ関数（ボタンなどから呼ぶ）
    public void OpenPanel()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        // ゲーム中なら時間を止める
        if (isGameScene)
        {
            Time.timeScale = 0f;
        }
    }

    // パネルを閉じる時に呼ぶ関数
    public void ClosePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        // ゲーム中なら時間を再開する
        if (isGameScene)
        {
            Time.timeScale = 1f;
        }
    }

    void OnBgmChange(float val)
    {
        if (AudioManager.instance) AudioManager.instance.SetBgmVolume(val);
    }

    void OnSeChange(float val)
    {
        if (AudioManager.instance) AudioManager.instance.SetSeVolume(val);
    }
}