using UnityEngine;

public class ButtonLane : MonoBehaviour
{
    public RhythmManager rhythm;
    public int lane; // 0..2

    // UnityEventのOnClickから呼ぶ
    public void OnClickLane()
    {
        if (rhythm != null) rhythm.TryHit(lane);
    }
}
