using Godot;
using System;

public struct MonsterCtorArg
{
	public Texture2D texture;
	public string slogan;

    public MonsterCtorArg(Texture2D t, string s)
    {
        this.texture = t;
        slogan = s;
    }

}

public partial class GBank : Node2D
{
	public static GBank Instance;
	public override void _Ready()
	{
		base._Ready();
		Instance = this;
	}

	public PackedScene ScoreHint = GD.Load<PackedScene>("res://PREFAB/PF_ScoreHint.tscn");
	public PackedScene MonsterDeadParticles = GD.Load<PackedScene>("res://PREFAB/PF_MonsterHitFX.tscn");
	public Texture2D crystalBallTexture = GD.Load<Texture2D>("res://ART/emoji/crystal_ball.png");
	public Texture2D shitTexture = GD.Load<Texture2D>("res://ART/emoji/hankey.png");
	public Texture2D deerTexture = GD.Load<Texture2D>("res://ART/emoji/deer.png");
	public Texture2D ghostTexture = GD.Load<Texture2D>("res://ART/emoji/ghost.png");
	public Texture2D catTexture = GD.Load<Texture2D>("res://ART/emoji/cat.png");
	public Texture2D fearTexture = GD.Load<Texture2D>("res://ART/emoji/fearful.png");
	public MonsterCtorArg GetEmojiTexture(double accumulatetime)
	{
		if(accumulatetime < 36.8 + InGameNodeRoot.GLOBAL_MIDI_TIMING_OFFSET) {
			return new MonsterCtorArg(crystalBallTexture, "🔮:你对随机刷新的紫色石墩子犯了错");
		}
		if(accumulatetime < 51.69 + InGameNodeRoot.GLOBAL_MIDI_TIMING_OFFSET) {
			return new MonsterCtorArg(catTexture, "🐱:😯哈 😃 基 😉 米👐南北绿豆♡\n被哈气了喵");
		}
		if(accumulatetime < 66.34 + InGameNodeRoot.GLOBAL_MIDI_TIMING_OFFSET) {
			return new MonsterCtorArg(shitTexture, "💩:我很少会用一个emoji来形容代码\n直到我用上了claude");
		}
		if(accumulatetime < 81.11 + InGameNodeRoot.GLOBAL_MIDI_TIMING_OFFSET) {
			return new MonsterCtorArg(ghostTexture, "👻:2天2小时的精致睡眠让你看到了不该看到的东西");
		}
		return new MonsterCtorArg(ghostTexture, "👻:2天2小时的精致睡眠让你看到了不该看到的东西");
	}
}