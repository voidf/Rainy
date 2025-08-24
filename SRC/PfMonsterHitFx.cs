using Godot;
using System;

public partial class PfMonsterHitFx : GpuParticles2D
{
    public override void _Ready()
    {
        base._Ready();
        // 在粒子lifetime过完后自动销毁
        float totalLifetime = (float)(Lifetime + (ProcessMaterial as ParticleProcessMaterial).LifetimeRandomness);
        // 保险起见多加一点时间
        float delay = totalLifetime + 0.2f;
        GetTree().CreateTimer(delay).Timeout += QueueFree;
    }
}