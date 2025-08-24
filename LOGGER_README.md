# Logger 使用说明

这个项目现在使用了一个简单的Logger类来替代所有的`GD.Print`调用，支持同时输出到控制台和日志文件。

## 功能特性

### 📝 **日志级别**
- **Info**: 一般信息日志
- **Warning**: 警告日志
- **Error**: 错误日志
- **Debug**: 调试日志

### 💾 **文件输出**
- 自动创建日志文件在用户数据目录
- 支持日志文件大小管理
- 线程安全的文件写入

### 🕒 **时间戳**
- 每条日志都包含精确的时间戳
- 格式：`[2024-01-01 12:00:00.123] [INFO] 消息内容`

## 使用方法

### 1. 初始化Logger

```csharp
// 在游戏启动时初始化
Logger.Initialize("game.log");
```

### 2. 记录不同级别的日志

```csharp
Logger.Info("游戏启动成功");
Logger.Warning("配置文件未找到，使用默认设置");
Logger.Error("无法加载资源文件");
Logger.Debug("当前帧率: 60 FPS");
```

### 3. 日志文件管理

```csharp
// 获取日志文件路径
string logPath = Logger.GetLogFilePath();

// 获取日志文件大小
long fileSize = Logger.GetLogFileSize();

// 清理日志文件（保留最近1000行）
Logger.Cleanup(1000);
```

## 日志文件位置

### Windows
```
%APPDATA%/Godot/app_userdata/[项目名]/game.log
```

### Android
```
/storage/emulated/0/Android/data/[包名]/files/game.log
```

### 其他平台
```
用户数据目录/game.log
```

## 日志格式示例

```
[2024-01-01 12:00:00.000] === 游戏日志开始 ===
[2024-01-01 12:00:00.123] [INFO] 游戏启动
[2024-01-01 12:00:00.456] [INFO] Loading MIDI file: res://MIDI/牧羊人的眼泪.mid
[2024-01-01 12:00:00.789] [INFO] MIDI Info: Format=1, Tracks=16, TicksPerQuarterNote=96
[2024-01-01 12:00:01.012] [INFO] Extracted tempo: 100 BPM (microsecondsPerQuarterNote: 600000)
[2024-01-01 12:00:01.345] [INFO] Processing track 0: Piano with 156 events
[2024-01-01 12:00:01.678] [INFO] MIDI parsing completed: created 78 meteors from 16 tracks
```

## 性能考虑

### ✅ **优点**
- 线程安全的文件写入
- 自动文件大小管理
- 支持日志级别过滤

### ⚠️ **注意事项**
- 频繁的日志写入可能影响性能
- 建议在发布版本中减少Debug日志
- 定期清理日志文件避免占用过多磁盘空间

## 迁移指南

### 从GD.Print迁移

**之前:**
```csharp
GD.Print("游戏启动");
GD.PrintErr("错误信息");
```

**之后:**
```csharp
Logger.Info("游戏启动");
Logger.Error("错误信息");
```

### 批量替换建议

可以使用以下正则表达式进行批量替换：

1. `GD\.Print\(` → `Logger.Info(`
2. `GD\.PrintErr\(` → `Logger.Error(`

## 调试技巧

### 1. 查看实时日志
```bash
# Windows (PowerShell)
Get-Content "game.log" -Wait

# Linux/Mac
tail -f game.log
```

### 2. 过滤特定级别的日志
```bash
# 只查看错误日志
grep "ERROR" game.log

# 只查看今天的日志
grep "$(date +%Y-%m-%d)" game.log
```

### 3. 日志文件分析
```bash
# 统计各级别日志数量
grep -o "\[.*\]" game.log | sort | uniq -c

# 查找特定关键词
grep -i "midi" game.log
```

## 故障排除

### 常见问题

1. **日志文件无法创建**
   - 检查用户数据目录权限
   - 确保磁盘空间充足

2. **日志文件过大**
   - 定期调用`Logger.Cleanup()`
   - 减少Debug日志输出

3. **性能问题**
   - 在发布版本中禁用Debug日志
   - 考虑异步日志写入

## 扩展功能

### 自定义日志格式
可以修改`WriteLog`方法来支持自定义格式：

```csharp
private static void WriteLog(string level, string message)
{
    // 自定义格式
    string logEntry = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
    // ...
}
```

### 日志级别过滤
可以添加日志级别过滤功能：

```csharp
public static LogLevel MinimumLevel = LogLevel.Info;

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
}
```
