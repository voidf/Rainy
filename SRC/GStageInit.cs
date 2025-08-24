using Godot;
using System;
using System.Diagnostics;

public partial class GStageInit : Node
{
    public static GStageInit Instance;
    public override void _Ready()
    {
        // base._Ready();
        // p_random.Seed = 114514;
        Instance = this;
    }
}
