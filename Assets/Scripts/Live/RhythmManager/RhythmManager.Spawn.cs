using System.Collections.Generic;
using UnityEngine;

/*
 * リズムマネージャー: 動画・音楽関連
 */
public partial class RhythmManager
{
    private ChartData chart; // 譜面データ
    private Queue<Note>[] laneQueues = new Queue<Note>[3]; // ← 3レーン
    private int spawnIndex = 0;

    /* 譜面の読み込み */
    void LoadChartAndInit()
    {
        // json ファイルの読み込み
        TextAsset json = Resources.Load<TextAsset>("Charts/sample_chart");
        if (json == null)
        {
            Debug.LogError(
                "[Rhythm] sample_chart.json が見つかりません（Assets/Resources/Charts/ に配置・拡張子.json）"
            );
            enabled = false; // コンポーネントの停止
            return;
        }

        // 譜面の読み込み
        chart = JsonUtility.FromJson<ChartData>(json.text);
        if (chart == null || chart.notes == null || chart.notes.Count == 0)
        {
            Debug.LogError("[Rhythm] 譜面の中身が空です（notes を確認）");
            enabled = false; // コンポーネントの停止
            return;
        }

        // 時間順に並び替え
        chart.notes.Sort((a, b) => a.time.CompareTo(b.time));
        Debug.Log($"[Rhythm] 読み込みOK: notes={chart.notes.Count}, offset={chart.offset}");

        // レーンの初期化
        for (int i = 0; i < laneQueues.Length; i++)
            laneQueues[i] = new Queue<Note>();
    }

    /* スポーン生成 */
    void TickSpawn()
    {
        double songTime = GetSongTime();
        // 生成ループ(既存)
        while (chart != null && spawnIndex < chart.notes.Count)
        {
            var n = chart.notes[spawnIndex];
            double spawnAt = n.time - noteTravelTime - chart.offset;
            if (songTime >= spawnAt)
            {
                SpawnNote(n);
                spawnIndex++;
            }
            else
                break;
        }
    }

    /* ノーツの処理 */
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
        int laneCount = laneQueues.Length; // 3
        float x = xStart + (n.lane + 0.5f) * (laneWidth / laneCount);

        float sy = (spawnY != 0f) ? spawnY : rect.height * 0.5f - 20f;
        float hy = (hitY != 0f) ? hitY : -rect.height * 0.5f + 200f;

        rt.anchoredPosition = new Vector2(x, sy);

        // Note情報を付与
        var note = go.GetComponent<Note>();
        if (note == null)
            note = go.AddComponent<Note>();
        note.lane = n.lane;
        note.time = n.time;
        note.Init(this);

        // レーンキューへ
        laneQueues[n.lane].Enqueue(note);

        // 落下開始：note側でStartCoroutine（Destroyと同時に止まる）
        note.StartCoroutine(CoMove(rt, new Vector2(x, hy), noteTravelTime));
    }

    /* 指定座標までアニメーション移動させるコルーチン */
    System.Collections.IEnumerator CoMove(RectTransform rt, Vector2 target, float duration)
    {
        if (!rt)
            yield break;

        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            // ここで毎フレーム生存確認
            if (!rt)
                yield break;

            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            rt.anchoredPosition = Vector2.Lerp(start, target, k);
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
    }
}
