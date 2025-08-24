using Godot;
using System;
using System.Collections.Generic;

public partial class PfSplitMeteor : PfMeteor
{
    public List<PfSubMeteor> subMeteors;
    PackedScene pf = (PackedScene)ResourceLoader.Load("res://PREFAB/PF_SubMeteor.tscn");
    public override void Ctor(float soundTime, float spawnTime, Vector2 spawnPoint, Vector2 hitPoint, float pitchScale = 1, float volumeScale = 1)
    {
        base.Ctor(soundTime, spawnTime, spawnPoint, hitPoint, pitchScale, volumeScale);

        subMeteors = new();
        var ignr = InGameNodeRoot.Instance;
        int subMeteorCnt = ignr.ps_random.RandiRange(2, 3);
        for (int i = 0; i < subMeteorCnt; ++i)
        {
            var ins = pf.Instantiate<Area2D>();
            var subMeteor = ins as PfSubMeteor;
            subMeteor.PreCtor();
            subMeteor.m_spawnTime = m_hitTime + 1f; // HACK 防止OnSoundTimeReached晚于OnSpawned被调用
            ignr.AddChild(ins);
            ignr.ps_meteorlist.Add(subMeteor);
            subMeteors.Add(subMeteor);
        }
        exp_sprite.Visible = false;
        exp_collider.Disabled = true;
        exp_burstFX.QueueFree();
    }
    public override void OnSoundTimeReached()
    {
        base.OnSoundTimeReached();
        var ignr = InGameNodeRoot.Instance;
        foreach (var subMeteor in subMeteors)
        {
            float mag = ignr.ps_random.RandfRange(800f, 1600f); // 速度模长
            float direction = ignr.ps_random.RandfRange(MathF.PI * 0.25f, MathF.PI * 0.75f); // 速度角度，0°为水平向x轴增长方向，90°为y轴减少方向，180°为x周减少方向
            subMeteor.Ctor(
                m_hitPoint,
                new Vector2(mag * MathF.Cos(direction), -mag * MathF.Sin(direction)),
                m_hitTime
            );
        }
    }
}
