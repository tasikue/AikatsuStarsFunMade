using System;
using System.Collections.Generic;
using UnityEngine;

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
    }

    void Update()
    {
        // 経過時刻
        double songTime;
        if (audioSource != null && audioSource.clip != null)
        {
            songTime = Math.Max(0, AudioSettings.dspTime - dspStartTime);
        }
        else
        {
            songTime = Math.Max(0, Time.time - fallbackStartTime);
        }

        // 生成タイミング（判定時刻 - 落下時間 - オフセット）
        while (chart != null && spawnIndex < chart.notes.Count)
        {
            var n = chart.notes[spawnIndex];
            double spawnAt = n.time - noteTravelTime - chart.offset;
            if (songTime >= spawnAt)
            {
                SpawnNote(n);
                spawnIndex++;
                if (spawnIndex == 1) Debug.Log($"[Rhythm] 1個目スポーン: t={songTime:F3}s (target {n.time:F3})");
            }
            else break;
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

        // レーンをXに割り当て（0〜3の4レーン想定）
        float laneWidth = 600f; // 任意：プレイエリア幅
        float xStart = -laneWidth/2f;
        float x = xStart + (n.lane + 0.5f) * (laneWidth / 4f);

        rt.anchoredPosition = new Vector2(x, spawnY);

        // 判定ラインまで移動
        float duration = noteTravelTime;
        StartCoroutine(CoMove(rt, new Vector2(x, hitY), noteTravelTime));
    }

   System.Collections.IEnumerator CoMove(RectTransform rt, Vector2 target, float duration)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            rt.anchoredPosition = Vector2.Lerp(start, target, k);
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
        Destroy(rt.gameObject);
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
