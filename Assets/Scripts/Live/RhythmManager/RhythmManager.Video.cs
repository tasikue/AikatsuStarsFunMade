using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public partial class RhythmManager
{
    // 変数
    private double dspStartTime; // 曲と動画の開始時間
    private double fallbackStartTime; // クリップが無い時の保険
    private bool _videoSyncActive = false; // 映像同期フラグ
    private bool _videoBooting = false; // 再生を実行したかのフラグ

    /* 動画と音楽の初期準備まとめ */
    void setVideoAndAudio()
    {
        // 再生開始時刻
        dspStartTime = AudioSettings.dspTime + audioDelay;

        // ① 再生準備（AudioClipが無い場合はフォールバック）
        PrepareVideo();
        // ② 曲の予約再生
        ScheduleAudio();
        // ③ 動画も同時にスタートするコルーチン
        StartCoroutine(CoStartVideoAtDSP(dspStartTime));
    }

    /* 動画の準備開始 */
    void PrepareVideo()
    {
        if (videoPlayer)
            videoPlayer.Prepare();
    }

    /* 曲を予約再生 */
    void ScheduleAudio()
    {
        // BGMを高精度に予約再生
        if (audioSource && audioSource.clip)
        {
            // BGM を dspStartTime 秒後に再生
            audioSource.PlayScheduled(dspStartTime);
            Debug.Log("[Rhythm] Audio scheduled start at " + dspStartTime.ToString("F3"));
        }
        else
        {
            // AudioClip 未設定時(よくわかってない)
            fallbackStartTime = Time.time + audioDelay;
            Debug.LogWarning("[Rhythm] AudioClip 未設定。Time.time ベースで進行します（暫定）");
        }
    }

    /*
     * Unity の VideoPlayer と Audio DSP 時間 を同期させて再生するためのコルーチン
     * ※※ 待機時間が長いと開始数秒が繰り返されるバグあり。よくわからない。
     */
    IEnumerator CoStartVideoAtDSP(double dspTime)
    {
        if (!videoPlayer)
            yield break;

        // 外部時間参照モード
        videoPlayer.timeReference = VideoTimeReference.ExternalTime;

        // 動画が準備できるまで待機
        while (!videoPlayer.isPrepared)
            yield return null;

        // 再生前に一時的にplayする必要があるらしい
        if (!_videoBooting)
        {
            videoPlayer.time = 0.0; // 念のため先頭へ
            videoPlayer.Play(); // External Timeでも必要
            // 待機中は固定時間を供給（進ませない）
            videoPlayer.externalReferenceTime = 0.0;

            _videoBooting = true;
        }

        // DSP開始まで待機
        while (AudioSettings.dspTime < dspTime)
            yield return null;

        // 同期開始：以後はDSPに追従させる
        _videoSyncActive = true;
    }

    /* 外部時間で動画を同期駆動 */
    void videoSync()
    {
        if (_videoSyncActive && videoPlayer)
        {
            Debug.Log("Video Sync");
            double t = AudioSettings.dspTime - dspStartTime;
            videoPlayer.externalReferenceTime = Math.Max(0.0, t);
        }
    }

    // 曲の時間を取得する便利ヘルパー（HitJudgeから参照する場合）
    public double GetSongTime()
    {
        if (seSource != null && seSource.clip != null)
        {
            double t = AudioSettings.dspTime - dspStartTime;
            return Math.Max(0.0, t);
        }
        else
        {
            return Math.Max(0.0, Time.time - fallbackStartTime);
        }
    }

    // 開始時間の取得
    public double GetStartDsp()
    {
        return dspStartTime;
    }
}
