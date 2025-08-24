using System;
using System.IO;
using Godot;

/// <summary>
/// 简单的日志记录器，支持同时输出到控制台和文件
/// </summary>
public static class Logger
{
    private static string logFilePath;
    private static bool isInitialized = false;
    private static readonly object lockObject = new object();

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    /// <param name="fileName">日志文件名，默认为"game.log"</param>
    public static void Initialize(string fileName = "Rainy.log")
    {
        if (isInitialized) return;

        try
        {
            // 获取用户数据目录
            string userDataDir = OS.GetUserDataDir();
            logFilePath = Path.Combine(userDataDir, fileName);

            // 确保目录存在
            string logDir = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // 写入日志头
            string header = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] === 游戏日志开始 ===";
            File.WriteAllText(logFilePath, header + System.Environment.NewLine);

            isInitialized = true;
            GD.Print($"Logger initialized. Log file: {logFilePath}");

            LoggerDebugOutput.OutputToDebugWindows($"Logger initialized. Log file: {logFilePath}"); 
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to initialize logger: {ex.Message}");
        }
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    /// <param name="message">日志消息</param>
    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    /// <param name="message">日志消息</param>
    public static void Warning(string message)
    {
        WriteLog("WARN", message);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    /// <param name="message">日志消息</param>
    public static void Error(string message)
    {
        WriteLog("ERROR", message);
    }

    /// <summary>
    /// 记录调试日志
    /// </summary>
    /// <param name="message">日志消息</param>
    public static void Debug(string message)
    {
        WriteLog("DEBUG", message);
    }

    /// <summary>
    /// 写入日志到文件和控制台
    /// </summary>
    /// <param name="level">日志级别</param>
    /// <param name="message">日志消息</param>
    private static void WriteLog(string level, string message)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logEntry = $"[{timestamp}] [{level}] {message}";

        // 输出到控制台
        switch (level)
        {
            case "ERROR":
                GD.PrintErr(logEntry);
                break;
            case "WARN":
                GD.Print(logEntry); // Godot没有专门的警告输出，使用普通输出
                break;
            default:
                GD.Print(logEntry);
                break;
        }
        
        // 输出到Debug窗口
        LoggerDebugOutput.OutputToDebugWindows(logEntry);

        // 写入文件
        try
        {
            lock (lockObject)
            {
                File.AppendAllText(logFilePath, logEntry + System.Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to write to log file: {ex.Message}");
        }
    }

    /// <summary>
    /// 清理日志文件（保留最近的行数）
    /// </summary>
    /// <param name="maxLines">保留的最大行数，默认1000行</param>
    public static void Cleanup(int maxLines = 1000)
    {
        if (!isInitialized || string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
            return;

        try
        {
            lock (lockObject)
            {
                string[] lines = File.ReadAllLines(logFilePath);
                if (lines.Length > maxLines)
                {
                    string[] recentLines = new string[maxLines];
                    Array.Copy(lines, lines.Length - maxLines, recentLines, 0, maxLines);
                    File.WriteAllLines(logFilePath, recentLines);
                    Info($"Log file cleaned up. Kept {maxLines} most recent lines.");
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Failed to cleanup log file: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    /// <returns>日志文件完整路径</returns>
    public static string GetLogFilePath()
    {
        return logFilePath;
    }

    /// <summary>
    /// 获取日志文件大小（字节）
    /// </summary>
    /// <returns>文件大小，如果文件不存在返回0</returns>
    public static long GetLogFileSize()
    {
        if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
            return 0;

        try
        {
            var fileInfo = new FileInfo(logFilePath);
            return fileInfo.Length;
        }
        catch
        {
            return 0;
        }
    }
}
