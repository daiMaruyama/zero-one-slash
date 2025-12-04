using UnityEngine;
using System.Collections;

public class HitStopManager : MonoBehaviour
{
    public static HitStopManager instance;
    bool isStopped = false;

    void Awake()
    {
        instance = this;
    }

    // w’è‚µ‚½•b”‚¾‚¯ŠÔ‚ğ~‚ß‚é
    public void StopFrame(float duration)
    {
        if (isStopped) return;
        StartCoroutine(StopRoutine(duration));
    }

    IEnumerator StopRoutine(float duration)
    {
        isStopped = true;

        // ŠÔ’â~
        Time.timeScale = 0.0f;

        // Œ»ÀŠÔ‚Å‘Ò‹@
        yield return new WaitForSecondsRealtime(duration);

        // ÄŠJ
        Time.timeScale = 1.0f;

        isStopped = false;
    }
}