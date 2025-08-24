using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Debug窗口，用于在屏幕上显示Logger输出
/// </summary>
public partial class WndDebug : CanvasLayer
{
    [Export] private RichTextLabel logDisplay;
    [Export] private Button toggleButton;
    [Export] private Button clearButton;
    [Export] private Button copyButton;

    private List<string> logBuffer = new List<string>();
    private int maxLogLines = 1000; // 最大显示行数

    public override void _Ready()
    {
        base._Ready();

        // 获取UI元素引用
        logDisplay = GetNode<RichTextLabel>("Panel/VBoxContainer/LogDisplay");
        toggleButton = GetNode<Button>("Panel/VBoxContainer/ButtonContainer/ToggleButton");
        clearButton = GetNode<Button>("Panel/VBoxContainer/ButtonContainer/ClearButton");
        copyButton = GetNode<Button>("Panel/VBoxContainer/ButtonContainer/CopyButton");

        // 连接按钮信号
        if (toggleButton != null)
            toggleButton.Pressed += Hide;
        if (clearButton != null)
            clearButton.Pressed += OnClearPressed;
        if (copyButton != null)
            copyButton.Pressed += OnCopyPressed;

        // 注册到Logger系统
        LoggerDebugOutput.RegisterDebugWindow(this);

        GD.Print("Debug window initialized");
    }

    /// <summary>
    /// 添加日志条目到显示
    /// </summary>
    /// <param name="logEntry">日志条目</param>
    public void AddLogEntry(string logEntry)
    {
        // 添加到缓冲区
        logBuffer.Add(logEntry);

        // 限制缓冲区大小
        if (logBuffer.Count > maxLogLines)
        {
            logBuffer.RemoveAt(0);
        }

        UpdateDisplay();
    }

    /// <summary>
    /// 更新显示内容
    /// </summary>
    private void UpdateDisplay()
    {
        if (logDisplay == null) return;

        // 清空显示
        logDisplay.Clear();

        // 添加所有日志条目
        foreach (string entry in logBuffer)
        {
            // 根据日志级别设置颜色
            string coloredEntry = ColorizeLogEntry(entry);
            logDisplay.AppendText(coloredEntry + "\n");
        }

        // 滚动到底部
        logDisplay.ScrollToLine(logBuffer.Count);
    }

    /// <summary>
    /// 为日志条目添加颜色
    /// </summary>
    /// <param name="entry">日志条目</param>
    /// <returns>带颜色的日志条目</returns>
    private string ColorizeLogEntry(string entry)
    {
        if (entry.Contains("[ERROR]"))
        {
            return $"[color=red]{entry}[/color]";
        }
        else if (entry.Contains("[WARN]"))
        {
            return $"[color=orange]{entry}[/color]";
        }
        else if (entry.Contains("[DEBUG]"))
        {
            return $"[color=gray]{entry}[/color]";
        }
        else if (entry.Contains("[INFO]"))
        {
            return $"[color=white]{entry}[/color]";
        }

        return entry;
    }


    /// <summary>
    /// 清空日志显示
    /// </summary>
    private void OnClearPressed()
    {
        logBuffer.Clear();
        if (logDisplay != null)
        {
            logDisplay.Clear();
        }

        GD.Print("Debug window cleared");
    }

    /// <summary>
    /// 复制日志到剪贴板
    /// </summary>
    private void OnCopyPressed()
    {
        string allLogs = string.Join("\n", logBuffer);
        DisplayServer.ClipboardSet(allLogs);

        GD.Print("Logs copied to clipboard");
    }

    /// <summary>
    /// 设置最大显示行数
    /// </summary>
    /// <param name="maxLines">最大行数</param>
    public void SetMaxLines(int maxLines)
    {
        maxLogLines = maxLines;

        // 如果当前缓冲区超过新的限制，移除多余的条目
        while (logBuffer.Count > maxLogLines)
        {
            logBuffer.RemoveAt(0);
        }

        UpdateDisplay();
    }
}
