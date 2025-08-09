using UnityEngine;
using UnityEngine.UI;

public class TargetPulse : MonoBehaviour
{
    public Image img;
    public float pulseScale = 1.15f;
    public float pulseTime = 0.12f;
    Coroutine co;

    void Awake()
    {
        if (!img)
            img = GetComponent<Image>();
    }

    public void Pulse()
    {
        if (co != null)
            StopCoroutine(co);
        co = StartCoroutine(CoPulse());
    }

    System.Collections.IEnumerator CoPulse()
    {
        var t = 0f;
        var from = Vector3.one;
        var to = Vector3.one * pulseScale;
        while (t < pulseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pulseTime);
            transform.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }
        t = 0f;
        while (t < pulseTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pulseTime);
            transform.localScale = Vector3.Lerp(to, from, k);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
