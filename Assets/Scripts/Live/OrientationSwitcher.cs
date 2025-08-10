using UnityEngine;
using UnityEngine.UI;

public class OrientationSwitcher : MonoBehaviour
{
    public GameObject layoutLandscape;
    public GameObject layoutPortrait;

    public CanvasScaler scaler; // CanvasのScaler
    public RhythmManager rhythm; // 共有のロジック

    // Landscape用の参照群
    [Header("Landscape")]
    public RectTransform playArea_L;
    public RenderTexture liveVideoRT_L; // 横画面用動画表示ターゲット
    public TargetPulse[] targets_L = new TargetPulse[3];

    // Portrait用の参照群
    [Header("Portrait")]
    public RectTransform playArea_P;
    public RenderTexture liveVideoRT_P; // 縦画面用動画表示ターゲット
    public TargetPulse[] targets_P = new TargetPulse[3];

    int lastW,
        lastH;

    void Start()
    {
        ApplyByAspect();
    }

    void Update()
    {
        // 画面サイズが変わったら判定（エディタでも切替）
        if (Screen.width != lastW || Screen.height != lastH)
            ApplyByAspect();
    }

    void ApplyByAspect()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        bool portrait = Screen.height >= Screen.width;

        layoutLandscape.SetActive(!portrait);
        layoutPortrait.SetActive(portrait);

        if (portrait)
        {
            if (scaler)
            {
                scaler.referenceResolution = new Vector2(720, 1280); // 縦基準
                scaler.matchWidthOrHeight = 1f;
            }
            // RhythmManager に縦レイアウトの参照を渡す
            rhythm.playArea = playArea_P;
            rhythm.targets = targets_P;
        }
        else
        {
            if (scaler)
            {
                scaler.referenceResolution = new Vector2(1280, 720); // 横基準
                scaler.matchWidthOrHeight = 0f;
            }
            // RhythmManager に横レイアウトの参照を渡す
            rhythm.playArea = playArea_L;
            rhythm.targets = targets_L;
        }

        // 例）VideoPlayerはRhythmManagerが持つ場合
        var vp = rhythm.videoPlayer;
        if (vp)
        {
            vp.targetTexture = portrait ? liveVideoRT_P : liveVideoRT_L;
        }

        // レーン幅が変わるので、必要なら内部キャッシュを更新
        rhythm.RefreshLayoutRuntime();
    }
}
