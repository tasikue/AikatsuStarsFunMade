using UnityEngine;

public class ButtonLane : MonoBehaviour
{
    public RhythmManager rhythm;
    public int lane; // 0..2
    public AudioSource audioSource; // 音を鳴らすためのAudioSource
    public AudioClip seClip; // 再生するSE

    public void OnClickLane()
    {
        // ノーツ判定
        if (rhythm != null)
            rhythm.TryHit(lane);

        // SE再生
        if (audioSource != null && seClip != null)
        {
            audioSource.PlayOneShot(seClip);
        }
    }
}
