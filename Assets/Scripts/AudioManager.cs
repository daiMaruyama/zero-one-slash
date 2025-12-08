using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("音量設定（0 - 1）")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float seVolume = 0.5f;

    // 保存用の鍵（キー）
    const string KEY_BGM = "CFG_BGM_VOL";
    const string KEY_SE = "CFG_SE_VOL";

    void Awake()
    {
        // シングルトン化
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadVolume(); // 起動時に保存データを読み込む
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // スライダーから呼ばれる関数
    public void SetBgmVolume(float volume)
    {
        bgmVolume = volume;
        PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSeVolume(float volume)
    {
        seVolume = volume;
        PlayerPrefs.SetFloat(KEY_SE, seVolume);
        PlayerPrefs.Save();
    }

    void LoadVolume()
    {
        // データがあれば読み込む、なければデフォルト(0.5)
        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM, 0.5f);
        seVolume = PlayerPrefs.GetFloat(KEY_SE, 0.5f);
    }
}