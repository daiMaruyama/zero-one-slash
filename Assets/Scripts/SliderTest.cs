using UnityEngine;
using UnityEngine.UI;

public class SliderTest : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] AudioClip tickSe;

    [Header("–Â‚ç‚µ‚·‚¬–hŽ~")]
    [SerializeField] float minInterval = 0.05f;
    [SerializeField] float minDelta = 0.02f;

    float _lastTime = -999f;
    float _lastValue = -999f;

    void Reset()
    {
        slider = GetComponent<Slider>();
    }

    void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (slider != null)
        {
            _lastValue = slider.value;
            slider.onValueChanged.AddListener(OnValueChanged);
        }
    }

    void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(OnValueChanged);
        }
    }

    void OnValueChanged(float value)
    {
        if (tickSe == null) return;

        float delta = Mathf.Abs(value - _lastValue);
        if (delta < minDelta) return;

        float now = Time.unscaledTime;
        if (now - _lastTime < minInterval) return;

        _lastTime = now;
        _lastValue = value;

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySE(tickSe);
        }
        else
        {
            // AudioManager‚ª–³‚¢Žž‚Ì•ÛŒ¯
            AudioSource.PlayClipAtPoint(tickSe, Vector3.zero);
        }
    }
}
