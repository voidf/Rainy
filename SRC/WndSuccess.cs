using Godot;
using System;

public partial class WndSuccess : CanvasLayer
{
    [Export]
    private Label LabelSeed;

    [Export]
    public Label LabelScore;
    [Export]
    public Label LabelText;

    [Export]
    public Button ButtonMainMenu;

    [Export]
    public Button ButtonRetry;

    [Export]
    public Button ButtonQuit;

    [Export]
    public Label LabelContinueCount;

    public override void _Ready()
    {
        base._Ready();
        ButtonMainMenu.Pressed += OnMainMenuPressed;
        ButtonRetry.Pressed += OnRetryPressed;
        ButtonQuit.Pressed += OnQuitPressed;

        // 初始时隐藏窗口
        Hide();
    }

    public void ShowSuccessWindow(uint seed, int score, int continueCount)
    {
        var ignr = InGameNodeRoot.Instance;
        ignr.ps_runningstate = GameRunningState.STOP;
        LabelSeed.Text = $"🎲随机种子: {seed}";
        LabelScore.Text = $"🎵总分数: {score}";
        LabelContinueCount.Text = $"💀续关次数: {continueCount}";
        if (continueCount == 0)
        {
            LabelText.Text = "你🈚敌了";
        }
        else
        {
            LabelText.Text = "看来你离😇还有点距离，但万幸的是，离👱已经很远了";
        }
        // 显示窗口
        Show();
        Logger.Info($"显示成功窗口 - 种子: {seed}, 分数: {score}");
    }

    private void OnMainMenuPressed()
    {
        Logger.Info("用户点击返回主菜单");
        GetTree().ChangeSceneToFile("res://TSCN/GEntry.tscn");
    }

    private void OnRetryPressed()
    {
        Logger.Info("用户点击重试");
        GetTree().ReloadCurrentScene();
    }

    private void OnQuitPressed()
    {
        Logger.Info("用户点击退出游戏");
        GetTree().Quit();
    }
}
