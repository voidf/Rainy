using Godot;
using System;

public partial class PfScoreHint : Sprite2D
{
    private Vector2 startPos;
    private Vector2 targetPos = new Vector2(1743, 42);
    private float duration = 1.0f;
    private float elapsed = 0.0f;
    private bool started = false;

    public void Ctor(Vector2 pos) {
        startPos = pos;
        started = true;
    }

    public override void _Process(double delta)
    {
        if (!started)
            return;

        elapsed += (float)delta;
        float t = Mathf.Clamp(elapsed / duration, 0f, 1f);

        // Ease out: fast at start, slow at end (using cubic ease out)
        float easeT = 1 - Mathf.Pow(1 - t, 3);

        Position = startPos.Lerp(targetPos, easeT);

        if (t >= 1.0f)
        {
            QueueFree();
        }
    }
}
