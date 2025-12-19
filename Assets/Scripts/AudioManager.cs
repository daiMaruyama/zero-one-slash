using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("音量設定（0 - 1）")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float seVolume = 0.5f;

    // スピーカーコンポーネント
    AudioSource bgmSource;
    AudioSource seSource; // 追加：SE用のスピーカー

    // 保存用の鍵
    const string KEY_BGM = "CFG_BGM_VOL";
    const string KEY_SE = "CFG_SE_VOL";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // BGM用のスピーカー
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;

            // SE用のスピーカーを取り付ける
            seSource = gameObject.AddComponent<AudioSource>();

            LoadVolume();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // 常に音量を反映させる
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }

        // 追加：SEの音量も反映
        if (seSource != null)
        {
            seSource.volume = seVolume;
        }
    }

    // 外部からBGM再生を依頼する関数
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // 追加：外部からSE再生を依頼する関数
    // これがないと GameManager から呼べません
    public void PlaySE(AudioClip clip)
    {
        if (clip == null) return;

        // PlayOneShotは音を重ねて鳴らせる
        seSource.PlayOneShot(clip);
    }

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
        bgmVolume = PlayerPrefs.GetFloat(KEY_BGM, 0.5f);
        seVolume = PlayerPrefs.GetFloat(KEY_SE, 0.5f);
    }
}