using Godot;
using System;

public partial class PfSoundOnlyMeteor : PfMeteor
{
    public override void ToGameTime(float gameTime)
    {
        if (gameTime >= m_hitTime)
        {
            if (!p_soundTimeReached)
            {
                p_soundTimeReached = true;
                exp_se.SetPitchScale(m_pitchScale);
                exp_se.SetVolumeLinear(m_volumeScale);
                exp_se.Play();
            }
            return;
        }
    }
}
