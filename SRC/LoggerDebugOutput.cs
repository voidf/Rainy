using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Logger调试输出管理器，连接Logger系统和DebugWindow
/// </summary>
public static class LoggerDebugOutput
{
    private static List<WndDebug> debugWindows = new List<WndDebug>();
    private static bool isInitialized = false;
    
    /// <summary>
    /// 注册Debug窗口
    /// </summary>
    /// <param name="debugWindow">Debug窗口实例</param>
    public static void RegisterDebugWindow(WndDebug debugWindow)
    {
        if (debugWindow != null && !debugWindows.Contains(debugWindow))
        {
            debugWindows.Add(debugWindow);
            GD.Print($"Debug window registered. Total windows: {debugWindows.Count}");
        }
    }

    
    /// <summary>
    /// 输出日志到所有注册的Debug窗口
    /// </summary>
    /// <param name="logEntry">日志条目</param>
    public static void OutputToDebugWindows(string logEntry)
    {
        // 清理无效的窗口引用
        debugWindows.RemoveAll(window => window == null || !GodotObject.IsInstanceValid(window));
        
        // 输出到所有有效的Debug窗口
        foreach (var window in debugWindows)
        {
            if (window != null && GodotObject.IsInstanceValid(window))
            {
                window.AddLogEntry(logEntry);
            }
        }
    }
    

}
