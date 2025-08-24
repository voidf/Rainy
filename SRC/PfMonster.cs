using Godot;
using System;

public partial class PfMonster : Area2D
{
    [Export] float speed = 80f;
    [Export] Sprite2D sprite;
    float OnBeatScale = 1.9f;
    public MonsterCtorArg monsterCtorArg;
    public override void _Process(double delta)
    {
        base._Process(delta);
        // 让怪物在游戏进行时沿着MoveBar匀速移动
        if (InGameNodeRoot.Instance.ps_runningstate == GameRunningState.RUNNING)
        {
            // 获取MoveBar的位置
            var moveBar = MoveBar.Instance;
            // 匀速跟随MoveBar的X和Y坐标
            Vector2 targetPos = moveBar.Position;
            Vector2 currentPos = Position;
            float maxDelta = (float)delta * speed;

            Vector2 toTarget = targetPos - currentPos;
            float distance = toTarget.Length();

            if (distance <= maxDelta)
            {
                Position = targetPos;
            }
            else
            {
                Position = currentPos + toTarget.Normalized() * maxDelta;
            }

            // // 调整朝向，使头朝向MoveBar
            // Vector2 direction = moveBar.Position - Position;
            // if (direction.Length() > 0.01f)
            // {
            //     Rotation = Mathf.Atan2(direction.Y, direction.X);
            // }
        }
        Scale = new Vector2(
            Mathf.Pow(Scale.X, Mathf.Pow(0.6f, (float)delta)),
            Mathf.Pow(Scale.Y, Mathf.Pow(0.6f, (float)delta))
        );
    }
    public void OnBeatFX()
    {
        Scale = new Vector2(OnBeatScale, OnBeatScale);
    }
    public override void _Ready()
    {
        base._Ready();
        AreaEntered += OnAreaEntered;
        monsterCtorArg = GBank.Instance.GetEmojiTexture(InGameNodeRoot.Instance.ps_accumulatetime);
        sprite.Texture = monsterCtorArg.texture;
        InGameNodeRoot.Instance.aliveMonsters.Add(GetInstanceId(), this);
    }
    public void PlayDeadFX()
    {
        var particles = GBank.Instance.MonsterDeadParticles.Instantiate<GpuParticles2D>();
        particles.Position = Position;
        GetTree().CurrentScene.AddChild(particles);
        var scoreHint = GBank.Instance.ScoreHint.Instantiate<PfScoreHint>();
        scoreHint.Ctor(Position);
        GetTree().CurrentScene.AddChild(scoreHint);
        particles.Emitting = true;
    }
    private void OnAreaEntered(Area2D area)
    {
        if (!IsInsideTree()) return;
        PlayDeadFX();
        InGameNodeRoot.Instance.aliveMonsters.Remove(GetInstanceId());
        QueueFree();
        UI_ScoreWindow.Instance.AddScore(10);
    }
}
