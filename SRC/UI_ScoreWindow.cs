using Godot;
using System;

public partial class UI_ScoreWindow : CanvasLayer
{
    public static UI_ScoreWindow Instance;
    // 分数系统
    public int score = 0;
    private Label scoreLabel;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        // 获取分数标签
        scoreLabel = GetNode<Label>("Panel/ScoreLabel");
        UpdateScoreDisplay();
    }

    public void AddScore(int scoreAdd = 1)
    {
        score += scoreAdd;
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        scoreLabel.Text = $"{score}";
    }

    public void UpdateScore(int score)
    {
        this.score = score;
        UpdateScoreDisplay();
    }

}
