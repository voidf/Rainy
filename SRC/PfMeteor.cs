using Godot;
using System;

public partial class PfMeteor : Area2D
{
    public float m_spawnTime;
    public float m_hitTime;
    public Vector2 m_spawnPoint;
    public Vector2 m_hitPoint;
    public float m_pitchScale;
    public float m_volumeScale;
    public bool p_soundTimeReached = false;
    public bool hasScored = false; // 防止重复计分
    public double p_onCatchTime = 0;
    public bool p_spawned = false;
    [Export] int grazeBonusScore = 1;
    [Export] float GRAZE_DISTANCE = 100f;

    // 公共属性用于访问私有字段
    [Export] public PointLight2D exp_light;
    [Export] public Sprite2D exp_sprite;
    [Export] public AnimatedSprite2D exp_anim;
    [Export] public GpuParticles2D exp_burstFX;
    [Export] public AudioStreamPlayer2D exp_se;
    [Export] public CollisionShape2D exp_collider;

    public virtual void Ctor(float soundTime, float spawnTime, Vector2 spawnPoint, Vector2 hitPoint, float pitchScale = 1f, float volumeScale = 1f)
    {
        m_spawnTime = spawnTime;
        m_hitTime = soundTime;
        m_spawnPoint = spawnPoint;
        m_hitPoint = hitPoint;
        Position = spawnPoint;
        m_pitchScale = pitchScale;
        m_volumeScale = volumeScale;
        Visible = false;
        hasScored = false;
        p_spawned = false;
        exp_collider.Disabled = true;
        if (exp_anim != null && IsInstanceValid(exp_anim))
            exp_anim.Visible = false;
        if (exp_sprite != null && IsInstanceValid(exp_sprite))
            exp_sprite.Visible = false;
        if (exp_light != null && IsInstanceValid(exp_light))
            exp_light.Visible = false;
    }

    // public virtual void OnCatch()
    // {
    //     p_onCatchTime = InGameNodeRoot.Instance.ps_accumulatetime;
    //     p_soundTimeReached = true;
    //     exp_se.SetPitchScale(m_pitchScale);
    //     exp_se.SetVolumeLinear(m_volumeScale);
    //     exp_se.Play();
    // }
    public virtual void OnSoundTimeReached()
    {
        if (exp_se != null && IsInstanceValid(exp_se))
        {
            exp_se.SetPitchScale(m_pitchScale);
            exp_se.SetVolumeLinear(m_volumeScale);
            exp_se.Play();
        }
        if (exp_burstFX != null && IsInstanceValid(exp_burstFX))
            exp_burstFX.Emitting = true;
        if (exp_collider != null && IsInstanceValid(exp_collider))
            exp_collider.Disabled = true;
        if (exp_anim != null && IsInstanceValid(exp_anim))
            exp_anim.Visible = false;
        if (exp_sprite != null && IsInstanceValid(exp_sprite))
            exp_sprite.Visible = false;
        var rgd = Mathf.Abs(m_hitPoint.X - MoveBar.Instance.Position.X);
        // GD.Print($"rgd {rgd} {GRAZE_DISTANCE}");
        // GD.Print($"Meteor type: {GetType().Name}, instance id: {GetInstanceId()} {rgd} {m_hitPoint}");
        if (rgd <= GRAZE_DISTANCE)
        {
            GD.Print($"Meteor type: {GetType().Name}, instance id: {GetInstanceId()} {rgd} {m_hitPoint}");
            // TODO 播表现
            var scoreHint = GBank.Instance.ScoreHint.Instantiate<PfScoreHint>();
            scoreHint.Ctor(Position);
            GetTree().CurrentScene.AddChild(scoreHint);
            UI_ScoreWindow.Instance.AddScore(grazeBonusScore);
        }
        if (exp_light != null && IsInstanceValid(exp_light))
            exp_light.Visible = false;
    }
    public virtual void OnSpawned()
    {
        m_spawnPoint = PfEmitterBar.Instance.Position + new Vector2(0, 150);
        var (leftX, rightX) = PfEmitterBar.Instance.GetRangeScreenBounds();
        m_hitPoint = new Vector2(
            InGameNodeRoot.Instance.ps_random.RandfRange(leftX, rightX),
            1080
        );
        if (exp_collider != null && IsInstanceValid(exp_collider))
            exp_collider.Disabled = false;
        if (exp_anim != null && IsInstanceValid(exp_anim))
        {
            exp_anim.Visible = true;
            exp_anim.Play();
        }
        if (exp_sprite != null && IsInstanceValid(exp_sprite))
            exp_sprite.Visible = true;
        if (exp_anim != null && IsInstanceValid(exp_anim))
            exp_anim.Visible = true;
        Visible = true;
        if (exp_light != null && IsInstanceValid(exp_light))
            exp_light.Visible = true;
    }
    public virtual void OnLifeUpdate(float gameTime)
    {
        float t = (float)((gameTime - m_spawnTime) / (m_hitTime - m_spawnTime));
        Position = m_spawnPoint.Lerp(m_hitPoint, t);
        // 调整exp_sprite的朝向，与前进方向一致
        if (exp_anim != null && IsInstanceValid(exp_anim))
            exp_anim.Rotation = (m_hitPoint - m_spawnPoint).Angle() - Mathf.Pi / 2;
        if (exp_sprite != null && IsInstanceValid(exp_sprite))
            exp_sprite.Rotation = (m_hitPoint - m_spawnPoint).Angle() - Mathf.Pi / 2;
    }
    public virtual void OnAfterHitUpdate(float gameTime) { }
    public virtual void ToGameTime(float gameTime)
    {
        if (gameTime < m_spawnTime) return;
        if (gameTime >= m_hitTime)
        {
            if (!p_soundTimeReached)
            {
                p_soundTimeReached = true;
                OnSoundTimeReached();
            }
            OnAfterHitUpdate(gameTime);
            return;
        }
        if (!p_spawned)
        {
            p_spawned = true;
            OnSpawned();
        }
        OnLifeUpdate(gameTime);
    }
}
