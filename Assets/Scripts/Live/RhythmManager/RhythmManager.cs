using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

[Serializable]
public class NoteData
{
    public float time;
    public int lane;
}

[Serializable]
public class ChartData
{
    public string song;
    public float bpm;
    public float offset;
    public List<NoteData> notes;
}

public partial class RhythmManager : MonoBehaviour
{
    [Header("Refs")]
    public GameObject notePrefab;
    public RectTransform playArea; // Canvas内 ノーツを流す親
    public TargetPulse[] targets = new TargetPulse[3]; // Inspectorで Target0~2 を割当
    public JudgeFeedback judgeFX;

    [Header("Movie")]
    public VideoPlayer videoPlayer;

    [Header("Audio")]
    public AudioSource audioSource;
    public double audioDelay;

    [Header("Sound")]
    public AudioSource seSource; // 効果音再生用
    public AudioClip[] seClips = new AudioClip[3]; // レーンごとのSE

    [Header("Scroll")]
    public float spawnY = 400f; // 出現位置Y（上）
    public float hitY = -200f; // 判定ラインY（下）
    public float noteTravelTime = 1.0f; // 出現→判定ラインまでの所要時間(秒)

    // 判定ウィンドウ（秒）
    [Header("Judge Windows (sec)")]
    public float perfect = 0.050f;
    public float great = 0.090f;
    public float good = 0.140f;
    public float bad = 0.200f; // これを超えたらMiss

    // スコアUI（Text でも TextMeshProUGUIでもOK）
    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI judgeText;

    void Start()
    {
        LoadChartAndInit(); // 譜面読み込み
        SetVideoAndAudio(); // 曲と動画のセット
        InitJudgeQueues(3); // 判定・スコアの初期化
    }

    void Update()
    {
        TickVideoSync(); // ビデオ同期
        TickSpawn(); // スポーン更新
        TickLateMiss(); // 判定：ミス処理
        TickKeyboard(); // キーボードの判定
    }
}
