using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

[Serializable]
public class NoteData { public float time; public int lane; }

[Serializable]
public class ChartData {
    public string song;
    public float bpm;
    public float offset;
    public List<NoteData> notes;
}

public class RhythmManager : MonoBehaviour
{
    [Header("Refs")]
    public AudioSource audioSource;
    public RectTransform playArea;     // Canvas内 ノーツを流す親
    public GameObject notePrefab;

    [Header("Scroll")]
    public float spawnY = 400f;        // 出現位置Y（上）
    public float hitY = -200f;         // 判定ラインY（下）
    public float noteTravelTime = 1.0f;// 出現→判定ラインまでの所要時間(秒)

    private ChartData chart;
    private int spawnIndex = 0;
    private double dspStartTime;
    private float fallbackStartTime; // クリップが無い時の保険
    private Queue<Note>[] laneQueues = new Queue<Note>[4];

    // 判定ウィンドウ（秒）
[Header("Judge Windows (sec)")]
public float perfect = 0.050f;
public float great   = 0.090f;
public float good    = 0.140f;
public float bad     = 0.200f; // これを超えたらMiss

// スコアUI（Text でも TextMeshProUGUIでもOK）
[Header("UI")]
public TextMeshProUGUI scoreText;
public TextMeshProUGUI comboText;
public TextMeshProUGUI judgeText;

// スコア関連
private int score = 0;
private int combo = 0;
private int maxCombo = 0;

    public double GetStartDsp()
{
    return dspStartTime;
}

    void Start()
    {
        // 譜面読み込み
        TextAsset json = Resources.Load<TextAsset>("Charts/sample_chart");
         if (json == null)
        {
            Debug.LogError("[Rhythm] sample_chart.json が見つかりません（Assets/Resources/Charts/ に配置・拡張子.json）");
            enabled = false;
            return;
        }

        chart = JsonUtility.FromJson<ChartData>(json.text);
        if (chart == null || chart.notes == null || chart.notes.Count == 0)
        {
            Debug.LogError("[Rhythm] 譜面の中身が空です（notes を確認）");
            enabled = false;
            return;
        }

        chart.notes.Sort((a,b) => a.time.CompareTo(b.time));
        Debug.Log($"[Rhythm] 読み込みOK: notes={chart.notes.Count}, offset={chart.offset}");

        // 再生準備（AudioClipが無い場合はフォールバック）
        double startDelay = 0.2;
        if (audioSource != null && audioSource.clip != null)
        {
            dspStartTime = AudioSettings.dspTime + startDelay;
            audioSource.PlayScheduled(dspStartTime);
            Debug.Log("[Rhythm] Audio scheduled start at " + dspStartTime.ToString("F3"));
        }
        else
        {
            fallbackStartTime = Time.time + (float)startDelay;
            Debug.LogWarning("[Rhythm] AudioClip 未設定。Time.time ベースで進行します（暫定）");
        }

        for (int i = 0; i < laneQueues.Length; i++) laneQueues[i] = new Queue<Note>();
    UpdateUI();
    }

    void Update()
    {
        double songTime = GetSongTime();

    // 生成ループ(既存)
    while (chart != null && spawnIndex < chart.notes.Count)
    {
        var n = chart.notes[spawnIndex];
        double spawnAt = n.time - noteTravelTime - chart.offset;
        if (songTime >= spawnAt) { SpawnNote(n); spawnIndex++; }
        else break;
    }

    // 遅延Miss処理：先頭が判定窓を超えたらMiss
   for (int lane = 0; lane < laneQueues.Length; lane++)
{
    // 先に null 化された要素を掃除
    while (laneQueues[lane].Count > 0 && laneQueues[lane].Peek() == null)
        laneQueues[lane].Dequeue();

    if (laneQueues[lane].Count == 0) continue;

    var head = laneQueues[lane].Peek();
    if (head == null) { laneQueues[lane].Dequeue(); continue; } // 念のため

    double diff = songTime - head.time;

    if (diff > bad) // 遅すぎ → Miss
    {
        // 先にキューから外す → それからDestroy
        laneQueues[lane].Dequeue();
        var go = head.gameObject;
        if (go) Destroy(go);

        RegisterJudge("Miss", 0, resetCombo: true);
    }
}

    }

    void SpawnNote(NoteData n)
    {
        var go = Instantiate(notePrefab, playArea);
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("[Rhythm] NotePrefab が UI ではありません（RectTransform が必要）");
            return;
        }

        var rect = playArea.rect;
    float laneWidth = rect.width;
    float xStart = -laneWidth * 0.5f;
    float x = xStart + (n.lane + 0.5f) * (laneWidth / 4f);

    float sy = (spawnY != 0f) ? spawnY : rect.height * 0.5f - 20f;
    float hy = (hitY   != 0f) ? hitY   : -rect.height * 0.5f + 200f;

    rt.anchoredPosition = new Vector2(x, sy);

    // Note情報を付与
    var note = go.GetComponent<Note>();
    if (note == null) note = go.AddComponent<Note>();
    note.lane = n.lane;
    note.time = n.time;
    note.Init(this);    

    // レーンキューへ
    laneQueues[n.lane].Enqueue(note);

   // 落下開始：note側でStartCoroutine（Destroyと同時に止まる）
note.StartCoroutine(CoMove(rt, new Vector2(x, hy), noteTravelTime));

    
    }

   System.Collections.IEnumerator CoMove(RectTransform rt, Vector2 target, float duration)
    {
         if (!rt) yield break;

        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            // ここで毎フレーム生存確認
        if (!rt) yield break;
        
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            rt.anchoredPosition = Vector2.Lerp(start, target, k);
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
    }

    // ユーザーのタップから呼ぶ判定
public void TryHit(int lane)
{
    if (lane < 0 || lane >= laneQueues.Length) return;

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

    if      (diff <= perfect) Hit(head, lane, "Perfect", 1000);
    else if (diff <= great  ) Hit(head, lane, "Great",    700);
    else if (diff <= good   ) Hit(head, lane, "Good",     400);
    else if (diff <= bad    ) Hit(head, lane, "Bad",      100, resetCombo:true);
    else                      RegisterJudge("Miss",        0,   resetCombo:true);
}

void Hit(Note head, int lane, string label, int add, bool resetCombo = false)
{
    // 先にDequeue
    laneQueues[lane].Dequeue();
    // それからDestroy（すでに消えていても安全）
    var go = head ? head.gameObject : null;
    if (go) Destroy(go);

    RegisterJudge(label, add, resetCombo);
}


void RegisterJudge(string label, int add, bool resetCombo = false)
{
    score += add;
    if (resetCombo) combo = 0;
    else combo++;

    maxCombo = Mathf.Max(maxCombo, combo);
    if (judgeText) judgeText.text = label;
    UpdateUI();
}

void UpdateUI()
{
    if (scoreText) scoreText.text = $"Score: {score}";
    if (comboText) comboText.text = combo > 0 ? $"Combo: {combo}" : "";
}

// 便利ヘルパー（HitJudgeから参照する場合）
    public double GetSongTime()
    {
        if (audioSource != null && audioSource.clip != null)
            return Math.Max(0, AudioSettings.dspTime - dspStartTime);
        else
            return Math.Max(0, Time.time - fallbackStartTime);
    }
}
