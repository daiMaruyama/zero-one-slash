using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GlobalVolumeSwitcher : MonoBehaviour
{
    [SerializeField] Volume volume;
    [SerializeField] VolumeProfile titleProfile;
    [SerializeField] VolumeProfile inGameProfile;
    [SerializeField] string titleSceneName = "Title";
    [SerializeField] string inGameSceneName = "Main";

    void Awake()
    {
        if (volume == null) volume = GetComponent<Volume>();

        SceneManager.activeSceneChanged += OnSceneChanged;
        ApplyProfile(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    void OnSceneChanged(Scene prev, Scene next)
    {
        ApplyProfile(next.name);
    }

    void ApplyProfile(string sceneName)
    {
        if (volume == null) return;

        if (sceneName == titleSceneName && titleProfile != null)
            volume.sharedProfile = titleProfile;
        else if (sceneName == inGameSceneName && inGameProfile != null)
            volume.sharedProfile = inGameProfile;
    }
}
