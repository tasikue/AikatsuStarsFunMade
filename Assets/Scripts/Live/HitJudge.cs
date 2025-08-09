using UnityEngine;
using UnityEngine.EventSystems;

public class HitJudge : MonoBehaviour, IPointerDownHandler
{
    public RhythmManager rhythm;
    public void OnPointerDown(PointerEventData eventData)
    {
        double now = AudioSettings.dspTime - rhythm.GetStartDsp();
        Debug.Log($"Tap at {now:F3}s");
    }
}