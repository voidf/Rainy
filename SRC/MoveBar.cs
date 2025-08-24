using Godot;
using System;

public partial class MoveBar : Area2D
{
    public static MoveBar Instance;
    private bool isDragging = false;

    // 屏幕边界
    private float screenWidth;
    private float barWidth;
    float lastMouseX = 0;
    bool hasLastMouseX = false;
    [Export] public Sprite2D exp_sprite;

    // 拖动时切换贴图
    [Export] public Texture2D normalTexture;
    [Export] public Texture2D dragTexture;

    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        // 获取屏幕宽度
        screenWidth = GetViewport().GetVisibleRect().Size.X;

        // 获取方块的碰撞形状宽度
        var collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        if (collisionShape != null && collisionShape.Shape is RectangleShape2D rectShape)
        {
            barWidth = rectShape.Size.X;
        }
        else
        {
            // 如果没有碰撞形状，使用默认宽度
            barWidth = 50f;
        }
        exp_sprite.Texture = normalTexture;

        AreaEntered += OnAreaEntered;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (InGameNodeRoot.Instance.ps_runningstate != GameRunningState.RUNNING &&
            InGameNodeRoot.Instance.ps_runningstate != GameRunningState.PREPARE) return;
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    // 鼠标按下，开始拖动
                    isDragging = true;
                    hasLastMouseX = true;
                    lastMouseX = mouseButton.Position.X;
                    exp_sprite.Texture = dragTexture;
                    // GD.Print("Started dragging MoveBar");
                }
                else
                {
                    // 鼠标释放，停止拖动
                    // if (isDragging)
                    // {
                    // GD.Print("Stopped dragging MoveBar");
                    // }
                    exp_sprite.Texture = normalTexture;
                    isDragging = false;
                    hasLastMouseX = false;
                    lastMouseX = 0;
                }
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion && isDragging)
        {
            // 鼠标移动时更新位置
            var mousePos = GetGlobalMousePosition();

            // 直接使用鼠标的X坐标，但保持Y坐标不变
            // 计算本帧和上一帧鼠标X坐标的差分，并将其加到Position上
            if (!hasLastMouseX)
            {
                lastMouseX = mousePos.X;
                hasLastMouseX = true;
            }
            float deltaX = mousePos.X - lastMouseX;
            var newPosition = Position;
            newPosition.X += deltaX;
            lastMouseX = mousePos.X;

            // 限制X坐标在屏幕范围内
            float minX = barWidth / 2;
            float maxX = screenWidth - barWidth / 2;
            newPosition.X = Mathf.Clamp(newPosition.X, minX, maxX);

            Position = newPosition;
        }
    }

    private void OnAreaEntered(Area2D area)
    {
        InGameNodeRoot.Instance.ps_runningstate = GameRunningState.STOP;
        int currentScore = UI_ScoreWindow.Instance.score;
        var failWindow = GetNode<WndFail>("/root/Node2D/WndFail");
        if (area is PfMonster monster)
        {
            failWindow.ShowFailWindow(currentScore, monster.monsterCtorArg.slogan);
        }
        else if (area is PfSplitMeteor splitMeteor || area is PfSubMeteor subMeteor)
        {
            failWindow.ShowFailWindow(currentScore, "❄️:你成为了古希腊掌管26°C空调的神🥶");
        }
        else if (area is PfExplodeMeteor explodeMeteor || area is ShockwaveArea swa)
        {
            failWindow.ShowFailWindow(currentScore, "🔥:🥵能被🔥淋到，那很有生🔥了\n最近气候是不是有点异常");
        }
        else if (area is PfMeteor meteor)
        {
            failWindow.ShowFailWindow(currentScore, "💧:💧不会一直下，但...😅");
        }
        exp_sprite.Texture = GBank.Instance.fearTexture;
    }


}
