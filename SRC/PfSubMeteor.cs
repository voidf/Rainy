using Godot;
using System;

public partial class PfSubMeteor : PfMeteor
{
    const float g = 2000f;
    public Vector2 m_initVelocity;
    public void PreCtor()
    {
        exp_sprite.Visible = false;
        Visible = false;
        exp_collider.Disabled = true;
    }
    public void Ctor(Vector2 spawnPoint, Vector2 initVelocity, float spawnTime)
    {
        m_spawnPoint = spawnPoint;
        m_initVelocity = initVelocity;
        m_spawnTime = spawnTime;
        // initVelocity.Y 为负数，表示向上抛出
        float t1 = -initVelocity.Y / g;
        float dy = 1080 - spawnPoint.Y;
        float t2 = MathF.Sqrt(2 * dy / g + t1 * t1);
        float tsum = t1 + t2;
        m_hitTime = tsum + m_spawnTime;
        m_hitPoint = new Vector2(
            m_spawnPoint.X + m_initVelocity.X * tsum,
            m_spawnPoint.Y + tsum * m_initVelocity.Y + g * tsum * tsum / 2
        );
    }
    public override void OnSpawned()
    {
        if (exp_collider != null && IsInstanceValid(exp_collider))
            exp_collider.Disabled = false;
        if (exp_sprite != null && IsInstanceValid(exp_sprite))
            exp_sprite.Visible = true;
        Visible = true;
    }
    public override void OnLifeUpdate(float gameTime)
    {
        base.OnLifeUpdate(gameTime);
        float t = gameTime - m_spawnTime;
        Position = new Vector2(
            m_spawnPoint.X + m_initVelocity.X * t,
            m_spawnPoint.Y + t * m_initVelocity.Y + g * t * t / 2
        );
    }
}
