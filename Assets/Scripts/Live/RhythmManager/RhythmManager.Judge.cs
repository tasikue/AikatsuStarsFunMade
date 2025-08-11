using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * リズムマネージャー: スコア・判定
 */
public partial class RhythmManager
{
    private Queue<Note>[] laneQueues;

    // スコア関連
    private int score;
    private int combo;
    private int maxCombo;

    /* 判定・スコアの初期化 */
    void InitJudgeQueues(int laneCount)
    {
        // レーンの初期化
        laneQueues = new Queue<Note>[laneCount];
        for (int i = 0; i < laneCount; i++)
            laneQueues[i] = new Queue<Note>();

        // スコア関係の更新
        score = combo = maxCombo = 0;

        // UI の更新
        UpdateUI();
    }

    // レーンキューへ
    void EnqueueNote(Note n) => laneQueues[n.lane].Enqueue(n);

    /* ミスの判定 */
    void TickLateMiss()
    {
        double songTime = GetSongTime();
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

            double diff = songTime - head.time;

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
    }

    // キーボードでの判定
    void TickKeyboard()
    {
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

    /* タップでのヒット判定 */
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

    /* ヒット時の処理 */
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

    /* スコアの計算 */
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
}
