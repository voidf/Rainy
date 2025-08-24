using Godot;
using System;

public partial class GEntry : Control
{
    [Export] Button startButton;
    [Export] Button exitButton;
    [Export] Button tutButton;
    [Export] Control tutWindow;
    [Export] TextureRect bowRect;

    private Vector2 bowInitialPosition;
    private float bowShakeTime = 0f;
    private float bowShakeIntensity = 3f; // 扰动强度
    private float bowShakeSpeed = 8f; // 扰动速度
    RandomNumberGenerator rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        base._Ready();

        // 连接按钮信号
        startButton.Pressed += OnStartButtonPressed;
        exitButton.Pressed += OnExitButtonPressed;
        tutButton.Pressed += OnTutButtonPressed;

        tutWindow.Visible = false;
        tutWindow.ProcessMode = ProcessModeEnum.Disabled; // 禁用处理模式

        bowInitialPosition = bowRect.Position;

        GD.Print("Main menu loaded successfully");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        float shakeX = rng.RandfRange(-1, 1) * bowShakeIntensity;
        float shakeY = rng.RandfRange(-1, 1) * bowShakeIntensity;

        // 应用扰动到初始位置
        Vector2 shakeOffset = new Vector2(shakeX, shakeY);
        bowRect.Position = bowInitialPosition + shakeOffset;
    }

    private void OnStartButtonPressed()
    {
        GD.Print("Start game button pressed");

        // 加载游戏场景
        GetTree().ChangeSceneToFile("res://TSCN/ingameStage.tscn");
    }

    private void OnExitButtonPressed()
    {
        GD.Print("Exit game button pressed");

        // 退出游戏
        GetTree().Quit();
    }

    private void OnTutButtonPressed()
    {
        GD.Print("Tutorial button pressed");

        // 显示教程窗口
        if (tutWindow != null)
        {
            tutWindow.Visible = true;
            tutWindow.ProcessMode = ProcessModeEnum.Inherit; // 恢复处理模式
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // 如果教程窗口可见，点击鼠标关闭它
        if (tutWindow != null && tutWindow.Visible && @event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                tutWindow.Visible = false;
                tutWindow.ProcessMode = ProcessModeEnum.Disabled; // 禁用处理模式
            }
        }
    }
}
