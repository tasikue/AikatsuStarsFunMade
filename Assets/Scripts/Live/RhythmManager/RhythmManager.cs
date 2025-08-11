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

    // スコア関連
    private int score = 0;
    private int combo = 0;
    private int maxCombo = 0;

    void Start()
    {
        LoadChartAndInit(); // 譜面読み込み
        SetVideoAndAudio(); // 曲と動画のセット

        UpdateUI();
    }

    void Update()
    {
        TickVideoSync(); // ビデオ同期
        TickSpawn(); // スポーン更新

        // 遅延Miss処理：先頭が判定窓を超えたらMiss
        for (int lane = 0; lane < laneQueues.Length; lane++)
        {
            // 先に null 化された要素を掃除
            while (laneQueues[lane].Count > 0 && laneQueues[lane].Peek() == null)
                laneQueues[lane].Dequeue();

            if (laneQueues[lane].Count == 0)
                continue;

            var head = laneQueues[lane].Peek();
            if (head == null)
            {
                laneQueues[lane].Dequeue();
                continue;
            } // 念のため

            double diff = GetSongTime() - head.time;

            if (diff > bad) // 遅すぎ → Miss
            {
                // 先にキューから外す → それからDestroy
                laneQueues[lane].Dequeue();
                var go = head.gameObject;
                if (go)
                    Destroy(go);

                RegisterJudge("Miss", 0, resetCombo: true);
            }
        }

        // キーボードでの判定
        if (Keyboard.current != null) // 新Input Systemの場合
        {
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                TryHit(0);
                PlayLaneSE(0);
            }
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                TryHit(1);
                PlayLaneSE(1);
            }
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                TryHit(2);
                PlayLaneSE(2);
            }
        }
    }

    // ユーザーのタップから呼ぶ判定
    public void TryHit(int lane)
    {
        if (lane < 0 || lane >= laneQueues.Length)
            return;

        // null掃除
        while (laneQueues[lane].Count > 0 && laneQueues[lane].Peek() == null)
            laneQueues[lane].Dequeue();

        if (laneQueues[lane].Count == 0)
        {
            RegisterJudge("Miss", 0, resetCombo: true);
            return;
        }

        var head = laneQueues[lane].Peek();
        if (head == null)
        {
            laneQueues[lane].Dequeue();
            RegisterJudge("Miss", 0, resetCombo: true);
            return;
        }

        double t = GetSongTime();
        double diff = Mathf.Abs((float)(t - head.time));

        if (diff <= perfect)
            Hit(head, lane, "Perfect", 1000);
        else if (diff <= great)
            Hit(head, lane, "Great", 700);
        else if (diff <= good)
            Hit(head, lane, "Good", 400);
        else if (diff <= bad)
            Hit(head, lane, "Bad", 100, resetCombo: true);
        else
            RegisterJudge("Miss", 0, resetCombo: true);
    }

    void Hit(Note head, int lane, string label, int add, bool resetCombo = false)
    {
        laneQueues[lane].Dequeue();
        var go = head ? head.gameObject : null;
        if (go)
            Destroy(go);

        if (lane >= 0 && lane < targets.Length && targets[lane])
            targets[lane].Pulse(); // ← 追加
        RegisterJudge(label, add, resetCombo);
    }

    void RegisterJudge(string label, int add, bool resetCombo = false)
    {
        score += add;
        combo = resetCombo ? 0 : combo + 1;
        maxCombo = Mathf.Max(maxCombo, combo);
        if (judgeText)
            judgeText.text = label;
        if (judgeFX)
            judgeFX.Show(label); // ← 追加

        UpdateUI();
    }

    void UpdateUI()
    {
        if (scoreText)
            scoreText.text = $"Score: {score}";
        if (comboText)
            comboText.text = combo > 0 ? $"Combo: {combo}" : "";
    }

    public void PlayLaneSE(int lane)
    {
        if (seSource != null && lane < seClips.Length && seClips[lane] != null)
        {
            seSource.PlayOneShot(seClips[lane]);
        }
    }

    // 縦横切替後に再計算が必要なとき用のフック（今は空でOK）
    public void RefreshLayoutRuntime()
    {
        // もし内部で PlayArea のサイズをキャッシュしているなら、ここで再計算する処理を書く
        // 例）laneWidth = playArea.rect.width; など
    }
}
