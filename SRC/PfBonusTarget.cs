using Godot;
using System;

public partial class PfBonusTarget : Area2D
{
    [Export] AnimatedSprite2D exp_ani;
    bool enterred = false;
    public override void _Ready()
    {
        base._Ready();
        AreaEntered += OnAreaEntered;
        exp_ani.Play();
        enterred = false;
    }
    // public override void _Process(double delta)
    // {
    //     base._Process(delta);
    //     // 匀速旋转
    //     float rotationSpeed = 1.0f; // 每秒旋转1弧度，可根据需要调整
    //     Rotation += rotationSpeed * (float)delta;
    // }
    private void OnAreaEntered(Area2D area)
    {
        if (!IsInsideTree()) return;
        if (enterred) return;
        enterred = true;
        QueueFree();
        var scoreHint = GBank.Instance.ScoreHint.Instantiate<PfScoreHint>();
        scoreHint.Ctor(Position);
        GetTree().CurrentScene.AddChild(scoreHint);
        UI_ScoreWindow.Instance.AddScore(100);
        InGameNodeRoot.Instance.bonusTargetCount--;
    }
}
