using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class BloomManager : MonoBehaviour
{
    public static BloomManager instance;

    [Header("3ポイント（Master Out）の設定")]
    public Color masterColor = Color.cyan;
    public float masterIntensity = 5.0f;

    [Header("1ポイント（Single Out）の設定")]
    public Color singleColor = Color.yellow;
    public float singleIntensity = 2.0f;

    [Header("基本設定")]
    public float decaySpeed = 2.0f;

    // URP用の変数
    Volume volume;
    Bloom bloom;
    float defaultIntensity;
    Color defaultColor;

    void Awake()
    {
        instance = this;
        volume = GetComponent<Volume>(); // ★ここが違う

        // VolumeからBloomの設定を引っ張り出す
        if (volume != null && volume.profile.TryGet(out bloom)) // ★ここも違う
        {
            defaultIntensity = bloom.intensity.value;
            defaultColor = bloom.tint.value; // ★ColorじゃなくてTint
        }
    }

    public void FlashBloom(int points)
    {
        if (bloom == null) return;

        Color targetColor = (points >= 3) ? masterColor : singleColor;
        float targetIntensity = (points >= 3) ? masterIntensity : singleIntensity;

        StopAllCoroutines();
        StartCoroutine(FlashRoutine(targetColor, targetIntensity));
    }

    IEnumerator FlashRoutine(Color targetColor, float targetIntensity)
    {
        // 瞬間的に変える
        bloom.tint.value = targetColor;
        bloom.intensity.value = targetIntensity;

        float currentInt = targetIntensity;

        while (currentInt > defaultIntensity)
        {
            currentInt -= Time.deltaTime * decaySpeed;
            bloom.intensity.value = currentInt;

            float t = (currentInt - defaultIntensity) / (targetIntensity - defaultIntensity);
            bloom.tint.value = Color.Lerp(defaultColor, targetColor, t);

            yield return null;
        }

        bloom.intensity.value = defaultIntensity;
        bloom.tint.value = defaultColor;
    }
}