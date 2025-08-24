using Godot;
using System;

public partial class PfExplodeMeteor : PfMeteor
{
    [Export] public Node2D shockwaveNode;
    [Export] public CollisionShape2D shockwaveCollider;
    [Export] public Sprite2D shockwaveSprite;
    [Export] public AnimationPlayer shockwaveAnimation;
    [Export] public Light2D shockwaveLight;

    private bool shockwaveActivated = false;
    [Export] float shockwaveDuration = 0.3f;
    [Export] float maxScale = 0.5f;
    [Export] float innerRingDelay = 0.05f;
    [Export] float maxRadiusTime = 0.25f;
    public override void Ctor(float soundTime, float spawnTime, Vector2 spawnPoint, Vector2 hitPoint, float pitchScale = 1f, float volumeScale = 1f)
    {
        base.Ctor(soundTime, spawnTime, spawnPoint, hitPoint, pitchScale, volumeScale);
        shockwaveNode.Visible = false;
        shockwaveLight.Visible = false;
    }
    public override void OnSoundTimeReached()
    {
        base.OnSoundTimeReached();
        // 激活冲击波
        shockwaveCollider.Disabled = false;
        shockwaveNode.Visible = true;
        shockwaveNode.Scale = Vector2.Zero;
        shockwaveLight.Visible = true;
    }

    public override void OnAfterHitUpdate(float gameTime)
    {
        float t = gameTime - m_hitTime;
        if (t >= shockwaveDuration)
        {
            shockwaveNode.Visible = false;
            shockwaveCollider.Disabled = true;
            shockwaveLight.Visible = false;
            return;
        }
        float scale;
        if (t <= maxRadiusTime)
            scale = t / maxRadiusTime * maxScale;
        else
            scale = maxScale;
        shockwaveNode.Scale = new Vector2(scale, scale);
        float innerElapsedTime = t - innerRingDelay;
        var material = shockwaveSprite.Material as ShaderMaterial;
        material.SetShaderParameter("progress", Mathf.InverseLerp(0, shockwaveDuration, innerElapsedTime));
    }
}
