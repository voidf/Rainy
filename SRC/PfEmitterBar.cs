using Godot;
using System;

public partial class PfEmitterBar : Node2D
{
    public static PfEmitterBar Instance;
    [Export] Sprite2D exp_emitter;
    [Export] Sprite2D exp_range;
    [Export] float MOVESPEED = 10f;
    float pressedTime = 0;
    float idleTime = 0;
    float rangeOriginScaleX;
    private float screenWidth;
    float scatterScale = 1f;
    private float barWidth = 0f;
    float maxScatterScale = 0f;
    [Export] float HoldAccMaxTime = .5f;
    [Export] float AccMaxRate = 3f;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        screenWidth = GetViewport().GetVisibleRect().Size.X;
        barWidth = exp_emitter.Texture.GetWidth() * exp_emitter.Scale.X;
        rangeOriginScaleX = exp_range.Scale.X;
        maxScatterScale = screenWidth * 2 / (exp_range.Texture.GetWidth() * rangeOriginScaleX);
    }

    public override void _Process(double delta)
    {
        float m = 0;
        if (Input.IsActionPressed("MovA"))
            m -= 1;
        if (Input.IsActionPressed("MovD"))
            m += 1;
        var ignr = InGameNodeRoot.Instance;
        if (m != 0)
        {
            pressedTime += (float)delta;
            idleTime = 0;
            float f1 = Mathf.InverseLerp(
                0f, HoldAccMaxTime, Mathf.Min(pressedTime, HoldAccMaxTime)
            );

            float f2 = Mathf.Lerp(1f, 1f + AccMaxRate, f1);
            // GD.Print($"f1:{f1} f2:{f2}");

            float newX = Position.X + (m * f2 * MOVESPEED);

            float minX = barWidth / 2;
            float maxX = screenWidth - barWidth / 2;
            newX = Mathf.Clamp(newX, minX, maxX);

            if (ignr.ps_runningstate == GameRunningState.RUNNING || ignr.ps_runningstate == GameRunningState.PREPARE)
                Position = new Vector2(newX, Position.Y);
            if (ignr.ps_runningstate == GameRunningState.RUNNING)
                // 每秒衰减到原来的0.1次方，每帧按delta比例衰减
                scatterScale = Mathf.Pow(scatterScale, Mathf.Pow(0.1f, (float)delta));
        }
        else
        {
            if (ignr.ps_runningstate == GameRunningState.RUNNING)
                // 使每秒增长到4倍，增长因子为4^(delta)
                scatterScale *= Mathf.Pow(8f, (float)delta);
            idleTime += (float)delta;
            pressedTime = 0;
        }
        scatterScale = Mathf.Clamp(scatterScale, 1f, maxScatterScale);
        exp_range.Scale = new Vector2(rangeOriginScaleX * scatterScale, exp_range.Scale.Y);
    }

    public (float leftX, float rightX) GetRangeScreenBounds()
    {
        Vector2 rangeWorldPos = exp_range.GlobalPosition;
        float textureWidth = exp_range.Texture.GetWidth() * exp_range.Scale.X;
        float leftX = rangeWorldPos.X - textureWidth / 2;
        float rightX = rangeWorldPos.X + textureWidth / 2;

        // 限制范围不超过屏幕
        float minX = 0f;
        float maxX = screenWidth;
        leftX = Mathf.Max(leftX, minX);
        rightX = Mathf.Min(rightX, maxX);

        return (leftX, rightX);
    }
}

