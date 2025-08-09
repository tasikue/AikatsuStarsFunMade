using UnityEngine;
using UnityEngine.EventSystems;

public class HitJudge : MonoBehaviour, IPointerDownHandler
{
    public RhythmManager rhythm;
    public int lane = 0; // 0〜3をInspectorで設定

    public void OnPointerDown(PointerEventData eventData)
    {
        if (rhythm != null) rhythm.TryHit(lane);
    }
}
