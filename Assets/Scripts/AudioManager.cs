using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("音量設定（0 - 1）")]
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float seVolume = 0.5f;

    // 実際に音を鳴らすスピーカーコンポーネント
    private AudioSource bgmSource;

    // 保存用の鍵
    const string KEY_BGM = "CFG_BGM_VOL";
    const string KEY_SE = "CFG_SE_VOL";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // BGM用のスピーカーを自動で取り付ける
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;

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
    }

    // ★重要：外部からBGM再生を依頼する関数
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        // もし「今流れている曲」と「これから流す曲」が同じなら、何もしない
        // これにより、シーン遷移しても曲が途切れない！
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            return;
        }

        // 違う曲なら再生する
        bgmSource.clip = clip;
        bgmSource.Play();
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