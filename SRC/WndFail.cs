using Godot;
using System;

public partial class WndFail : CanvasLayer
{
    public static WndFail Instance;
    [Export] private Button returnToMenuButton;
    [Export] private Button retryButton;
    [Export] private Button continueButton;
    [Export] private Label scoreLabel;
    [Export] public Label sloganLabel;
    public int continueCount = 0;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        GD.Print("Fail window _Ready");

        // 连接按钮信号
        returnToMenuButton.Pressed += OnReturnToMenuPressed;
        retryButton.Pressed += OnRetryPressed;
        continueButton.Pressed += OnContinuePressed;
        // 初始时隐藏窗口
        Visible = false;

        GD.Print("Fail window initialized");
    }
    private void OnContinuePressed()
    {
        continueCount++;
        Visible = false;
        InGameNodeRoot.Instance.GetAllAliveMonsters().ForEach(x =>
        {
            InGameNodeRoot.Instance.aliveMonsters.Remove(x.GetInstanceId());
            x.PlayDeadFX();
            x.Free();
        });
        // 处理ps_meteorlist中已到达的元素，并将其从列表中移除
        var toRemove = new System.Collections.Generic.List<PfMeteor>();
        foreach (var x in InGameNodeRoot.Instance.ps_meteorlist)
        {
            if (x.m_spawnTime <= InGameNodeRoot.Instance.ps_accumulatetime)
            {
                x.Free();
                toRemove.Add(x);
            }
        }
        MoveBar.Instance.exp_sprite.Texture = MoveBar.Instance.normalTexture;

        foreach (var x in toRemove)
        {
            InGameNodeRoot.Instance.ps_meteorlist.Remove(x);
        }
        UI_ScoreWindow.Instance.UpdateScore(0);
        InGameNodeRoot.Instance.ps_runningstate = GameRunningState.RUNNING;
    }
    public void ShowFailWindow(int finalScore, string slogan)
    {
        scoreLabel.Text = $"🎵 最终得分: {finalScore}";
        sloganLabel.Text = slogan;
        // 显示窗口
        Visible = true;
    }

    /// <summary>
    /// 返回主菜单按钮点击事件
    /// </summary>
    private void OnReturnToMenuPressed()
    {
        GD.Print("Return to menu button pressed");

        // InGameNodeRoot.Instance.LoadGame();
        // 返回主菜单
        GetTree().ChangeSceneToFile("res://TSCN/GEntry.tscn");
    }

    /// <summary>
    /// 重试按钮点击事件
    /// </summary>
    private void OnRetryPressed()
    {
        GD.Print("Retry button pressed");
        GetTree().ReloadCurrentScene();
        // 重新加载游戏场景
        // GetTree().ChangeSceneToFile("res://TSCN/ingameStage.tscn");
    }
}
