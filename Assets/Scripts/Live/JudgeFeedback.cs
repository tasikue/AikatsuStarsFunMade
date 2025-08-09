using TMPro;
using UnityEngine;

public class JudgeFeedback : MonoBehaviour
{
    public TextMeshProUGUI text;
    public float scaleFrom = 1.2f;
    public float scaleTo = 1.0f;
    public float fadeTime = 0.35f;

    Coroutine co;

    void Awake()
    {
        if (!text)
            text = GetComponent<TextMeshProUGUI>();
    }

    public void Show(string label)
    {
        if (co != null)
            StopCoroutine(co);
        co = StartCoroutine(CoShow(label));
    }

    System.Collections.IEnumerator CoShow(string label)
    {
        if (!text)
            yield break;
        text.text = label;
        var t = 0f;
        var col = text.color;
        col.a = 1f;
        text.color = col;
        transform.localScale = Vector3.one * scaleFrom;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            transform.localScale = Vector3.Lerp(Vector3.one * scaleFrom, Vector3.one * scaleTo, k);
            col.a = 1f - k;
            text.color = col;
            yield return null;
        }
        col.a = 0f;
        text.color = col;
    }
}
