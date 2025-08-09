using UnityEngine;
using UnityEngine.UI;

public class Note : MonoBehaviour
{
    public int lane; // 0〜3
    public float time; // 判定時刻(秒)（chart.notes[i].time）

    [Header("Debug Colors")]
    public Color baseColor = Color.white; // ふだんの色
    public Color nearColor = new Color(1f, 0.85f, 0f, 1f); // Great以下での色（例：黄）
    public Color parfectColor = new Color(1f, 0f, 0f, 1f);

    private Image img;
    private RhythmManager rhythm;

    public void Init(RhythmManager r)
    {
        rhythm = r;
    }

    void Awake()
    {
        img = GetComponent<Image>();
        if (!img)
            return;
        // レーンカラー例
        Color[] laneColors =
        {
            new Color(1f, 0.5f, 0.8f, 1f), // lane0 ピンク
            new Color(1f, 0.9f, 0.2f, 1f), // lane1 黄
            new Color(0.4f, 0.7f, 1f, 1f), // lane2 青
        };
        img.color = lane < laneColors.Length ? laneColors[lane] : Color.white;
    }

    void Update()
    {
        if (rhythm == null || img == null)
            return;

        // 現在時刻との差（絶対値）を計算
        double now = rhythm.GetSongTime();
        float diff = Mathf.Abs((float)(now - time));

        // Great以下（= Perfect/Great の範囲）だけ別色
        if (diff <= rhythm.perfect)
            img.color = parfectColor;
        else if (diff <= rhythm.great)
            img.color = nearColor;
        else
            img.color = baseColor;
    }
}
