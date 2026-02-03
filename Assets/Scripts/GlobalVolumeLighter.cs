using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlobalVolumeLighter : MonoBehaviour
{
    [Header("Hue Cycle (デフォルトから1周)")]
    [SerializeField] bool _useUnscaledTime = true;
    [SerializeField] float _cycleSeconds = 60f;

    [Header("SVの扱い")]
    [SerializeField] bool _useBaseSaturationValue = true;
    [SerializeField, Range(0f, 1f)] float _saturation = 0.85f;
    [SerializeField, Range(0f, 2f)] float _value = 1.0f;

    [Header("Organic (わずかなゆらぎ)")]
    [SerializeField, Range(0f, 0.2f)] float _noiseStrength = 0.04f;
    [SerializeField] float _noiseSpeed = 0.08f;
    [SerializeField] float _noiseFadeInSeconds = 2.0f;

    [Header("Smoothing (ヌルっと)")]
    [SerializeField] float _smooth = 6.0f;

    [Header("Debug")]
    [SerializeField] bool _debugLog = false;

    Volume _volume;
    Bloom _bloom;

    Color _baseTint;
    Color _currentTint;

    float _phase;     // 0..1（0がデフォ）
    float _elapsed;
    float _noiseSeed;

    Coroutine _initCo;
    bool _ready;

    void Awake()
    {
        _volume = GetComponent<Volume>();
        _noiseSeed = Random.value * 1000f;
    }

    void OnEnable()
    {
        if (_initCo != null) StopCoroutine(_initCo);
        _initCo = StartCoroutine(InitAfterOneFrame());
    }

    void OnDisable()
    {
        _ready = false;

        if (_initCo != null)
        {
            StopCoroutine(_initCo);
            _initCo = null;
        }

        if (_bloom != null)
            _bloom.tint.value = _baseTint;
    }

    IEnumerator InitAfterOneFrame()
    {
        _ready = false;

        // 他のAwake/Startで Volume/Profile が切り替わるのを待つ
        yield return null;

        if (_volume == null)
            yield break;

        // 「デフォ」は今シーンで設定してる sharedProfile の値を採用（ここ重要）
        VolumeProfile src = _volume.sharedProfile != null ? _volume.sharedProfile : _volume.profile;
        if (src == null)
            yield break;

        if (!src.TryGet(out Bloom srcBloom) || srcBloom == null)
        {
            Debug.LogError("ProfileにBloomが無い。Title用ProfileにBloom Overrideを追加して。");
            yield break;
        }

        _baseTint = srcBloom.tint.value;
        _currentTint = _baseTint;

        // 以降の変更はランタイム用インスタンスにだけ反映（アセット汚染防止）
        _volume.profile = Instantiate(src);

        if (!_volume.profile.TryGet(out _bloom) || _bloom == null)
        {
            Debug.LogError("Instantiate後のprofileからBloomが取れない。");
            yield break;
        }

        // ここで必ずデフォ値を1回セットしてから開始
        _bloom.tint.value = _baseTint;

        _phase = 0f;
        _elapsed = 0f;
        _ready = true;

        if (_debugLog)
            Debug.Log($"BaseTint = {_baseTint}");
    }

    void Update()
    {
        if (!_ready || _bloom == null) return;

        float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (dt <= 0f) return;

        // このフレームで使うphase/elapsedは“進める前”を使う（初回にズレない）
        float phase = _phase;
        float elapsed = _elapsed;

        float cycle = Mathf.Max(0.01f, _cycleSeconds);
        _phase = Mathf.Repeat(_phase + dt / cycle, 1f);
        _elapsed += dt;

        float noiseFade = (_noiseFadeInSeconds <= 0f) ? 1f : Mathf.Clamp01(elapsed / _noiseFadeInSeconds);

        float n = Mathf.PerlinNoise(_noiseSeed, Time.time * _noiseSpeed);
        float noise = (n - 0.5f) * 2f * _noiseStrength * noiseFade;

        float deltaHue = phase + noise; // 0→1で1周

        Color target = HueRotatePreserveHDR(
            _baseTint,
            deltaHue,
            _useBaseSaturationValue,
            _saturation,
            _value
        );

        float k = 1f - Mathf.Exp(-_smooth * dt);
        _currentTint = Color.Lerp(_currentTint, target, k);

        _bloom.tint.value = _currentTint;
    }

    static Color HueRotatePreserveHDR(Color baseTint, float deltaHue, bool useBaseSV, float satOverride, float valOverride)
    {
        float a = baseTint.a;

        // HDR保持：最大成分で正規化→HSV→戻してから再スケール
        float max = Mathf.Max(baseTint.r, Mathf.Max(baseTint.g, baseTint.b));
        if (max <= 0f) return baseTint;

        Color n = baseTint;
        if (max > 1f)
        {
            n.r /= max; n.g /= max; n.b /= max;
        }

        Color.RGBToHSV(n, out float h, out float s, out float v);

        h = Mathf.Repeat(h + deltaHue, 1f);

        if (!useBaseSV)
        {
            s = satOverride;
            v = valOverride;
        }

        Color rgb = Color.HSVToRGB(h, s, v);

        if (max > 1f)
        {
            rgb.r *= max; rgb.g *= max; rgb.b *= max;
        }

        rgb.a = a;
        return rgb;
    }
}
