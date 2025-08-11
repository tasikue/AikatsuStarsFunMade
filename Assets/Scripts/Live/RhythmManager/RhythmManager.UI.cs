/*
 * リズムマネージャー: スコア・判定
 */
public partial class RhythmManager
{
    void UpdateUI()
    {
        if (scoreText)
            scoreText.text = $"Score: {score}";
        if (comboText)
            comboText.text = combo > 0 ? $"Combo: {combo}" : "";
    }

    // 縦横切替後に再計算が必要なとき用のフック（今は空でOK）
    public void RefreshLayoutRuntime()
    {
        // もし内部で PlayArea のサイズをキャッシュしているなら、ここで再計算する処理を書く
        // 例）laneWidth = playArea.rect.width; など
    }
}
