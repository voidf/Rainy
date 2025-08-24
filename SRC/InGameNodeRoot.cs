using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public enum GameRunningState : int
{
    PREPARE,
    STOP,
    RUNNING,
    PAUSE,
}

public partial class InGameNodeRoot : Node2D
{
    public const float GLOBAL_MIDI_TIMING_OFFSET = 1f;
    public Dictionary<ulong, PfMonster> aliveMonsters;
    public static InGameNodeRoot Instance;
    public RandomNumberGenerator ps_random;
    public List<PfMeteor> ps_meteorlist;
    public SortedList<float, PfMeteor> ps_meteorUpdQueue;
    public int ps_meteorUpdInQueuePtr;
    public float ps_gamestarttime = 0f;
    public double ps_accumulatetimephysical = 0;
    public double ps_accumulatetime = 0;
    public GameRunningState ps_runningstate = GameRunningState.STOP;

    // 成功窗口相关
    [Export] public WndSuccess wndSuccess;
    [Export] public WndFail wndFail;
    [Export] public WndDebug wndDebug;
    private bool successWindowShown = false;
    float maxHitTime = 0f;
    List<float> first30Note;
    List<float> last30Note;
    List<float> allNoteOnTime = new();

    PackedScene pfexp = (PackedScene)ResourceLoader.Load("res://PREFAB/PF_ExplodeMeteor.tscn");
    PackedScene pfcommon = (PackedScene)ResourceLoader.Load("res://PREFAB/PF_Meteor.tscn");
    PackedScene pfsplit = (PackedScene)ResourceLoader.Load("res://PREFAB/PF_SplitMeteor.tscn");
    PackedScene pfMonster = GD.Load<PackedScene>("res://PREFAB/PF_Monster.tscn");
    PackedScene pfBonusTarget = GD.Load<PackedScene>("res://PREFAB/PF_BonusTarget.tscn");

    [Export] int bonusTargetLimit = 3;

    public int bonusTargetCount = 0;


    public override void _Ready()
    {
        base._Ready();
        Instance = this;
        // 初始化日志系统
        Logger.Initialize("rainy_game.log");
        Logger.Info("游戏启动");
        LoadGame();
    }

    // public override void _PhysicsProcess(double delta)
    // {
    //     base._PhysicsProcess(delta);
    //     if (ps_runningstate != GameRunningState.RUNNING) return;
    //     ps_accumulatetimephysical += delta;
    // }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (ps_runningstate != GameRunningState.RUNNING) return;
        var before_accumulatetime = ps_accumulatetime;
        ps_accumulatetime += delta;
        float currentTime = (float)ps_accumulatetime;
        /*
        while (ps_meteorUpdInQueuePtr < ps_meteorlist.Count)
        {
            var meteor = ps_meteorlist[ps_meteorUpdInQueuePtr];
            if (meteor.m_spawnTime > currentTime)
                break;
            ps_meteorUpdQueue.Add(meteor.m_hitTime + 1f + meteor.GetInstanceId() * 1e-12f, meteor);
            ps_meteorUpdInQueuePtr++;
            // GD.Print($"Added meteor to queue... {Time.GetTicksMsec()} {ps_meteorUpdInQueuePtr} {ps_meteorUpdQueue.Count}");
        }

        while (ps_meteorUpdQueue.Count > 0)
        {
            var firstKey = ps_meteorUpdQueue.Keys[0];
            if (firstKey < currentTime)
            {
                ps_meteorUpdQueue.RemoveAt(0);
                // GD.Print($"Removed meteor from queue... {Time.GetTicksMsec()} {ps_meteorUpdQueue.Count}");
            }
            else
            {
                break;
            }
        }
        foreach (var meteor in ps_meteorUpdQueue.Values)
            meteor.ToGameTime(currentTime);
        */
        foreach (var meteor in ps_meteorlist)
            meteor.ToGameTime(currentTime);

        // 检查是否需要显示成功窗口
        CheckForSuccessWindow();
        // if (Input.IsActionPressed("MovS"))
            // ShowSuccessWindow();
        // 处理last30Note（从右边缘生成怪物）
        while (last30Note.Count > 0 && currentTime >= last30Note[^1])
        {
            float spawnTime = last30Note[last30Note.Count - 1];
            last30Note.RemoveAt(last30Note.Count - 1);

            var monster = pfMonster.Instantiate<Area2D>();
            // 屏幕右边缘
            var viewport = GetViewport();
            var screenSize = viewport.GetVisibleRect().Size;
            float y = (float)ps_random.RandfRange(200, screenSize.Y - 300);
            float x = screenSize.X + 20;
            monster.Position = new Vector2(x, y);
            AddChild(monster);
        }

        // 处理first30Note（从左边缘生成怪物）
        while (first30Note.Count > 0 && currentTime >= first30Note[^1])
        {
            float spawnTime = first30Note[first30Note.Count - 1];
            first30Note.RemoveAt(first30Note.Count - 1);

            var monster = pfMonster.Instantiate<Area2D>();
            // 屏幕左边缘
            var viewport = GetViewport();
            var screenSize = viewport.GetVisibleRect().Size;
            float y = (float)ps_random.RandfRange(200, screenSize.Y - 300);
            float x = -20;
            monster.Position = new Vector2(x, y);
            AddChild(monster);
        }

        while (allNoteOnTime.Count > 0 && currentTime >= allNoteOnTime[^1])
        {
            allNoteOnTime.RemoveAt(allNoteOnTime.Count - 1);
            foreach (var m in GetAllAliveMonsters())
                m.OnBeatFX();
        }

        if (Mathf.Floor(before_accumulatetime / 10f) != Mathf.Floor(currentTime / 10f))
        {
            if (bonusTargetCount < bonusTargetLimit)
            {
                var bonusTarget = pfBonusTarget.Instantiate<Area2D>();
                var viewport = GetViewport();
                var screenSize = viewport.GetVisibleRect().Size;
                float y = (float)ps_random.RandfRange(screenSize.Y * 0.2f, screenSize.Y * 0.8f);
                float x = (float)ps_random.RandfRange(screenSize.X * 0.1f, screenSize.X * 0.9f);
                bonusTarget.Position = new Vector2(x, y);
                AddChild(bonusTarget);
                bonusTargetCount++;
            }
        }
    }
    public void LoadGame()
    {
        ps_runningstate = GameRunningState.PREPARE;
        ps_random = new();
        ps_random.SetSeed(Time.GetTicksMsec());
        if (ps_meteorlist == null)
            ps_meteorlist = new();
        if (ps_meteorUpdQueue == null)
            ps_meteorUpdQueue = new();
        foreach (var meteor in ps_meteorlist)
            if (IsInstanceValid(meteor))
                meteor.Free();
        ps_meteorUpdQueue.Clear();
        ps_meteorlist.Clear();

        wndSuccess.Hide();
        wndFail.Hide();
        // ParseMML(@"T120 L4 O4 CCGG");
        //         ParseMML(@"T120 L4 O4
        // CCGG AAG FFEEDDC
        // GGFF EED GGFF EED
        // CCGG AAG FFEEDDC
        // O5 C4 O4 G4 F4 E4 D4 C4
        // O5 C4 O4 G4 F4 E4 D4 C4
        // O5 C4 O4 G4 F4 E4 D4 C4
        // O5 C4 O4 G4 F4 E4 D4 C4"
        //         );
        allNoteOnTime.Clear();
        LoadSpecifyMidi();

        // 为 allNoteOnTime 去重
        allNoteOnTime = allNoteOnTime.Distinct().ToList();
        allNoteOnTime.Sort((a, b) => b.CompareTo(a));

        // // 为 allNoteOnTime 洗牌
        // var rng = new Random();
        // var shuffledNoteOnTime = allNoteOnTime.OrderBy(x => rng.Next()).ToList();

        // int count = shuffledNoteOnTime.Count;
        // int takeCount = (int)(count * 0.3f);

        // // 取前30%和后30%
        // first30Note = shuffledNoteOnTime.Take(takeCount).ToList();
        // last30Note = shuffledNoteOnTime.Skip(count - takeCount).Take(takeCount).ToList();

        // // 排序
        first30Note.Sort((a, b) => b.CompareTo(a));
        last30Note.Sort((a, b) => b.CompareTo(a));

        // GD.Print($"first30: {string.Join(", ", first30)}");
        // GD.Print($"last30: {string.Join(", ", last30)}");

        // GD.Print($"Sorting meteors... {Time.GetTicksMsec()} {ps_meteorlist.Count}");
        ps_meteorlist.Sort(
            (a, b) => a.m_spawnTime.CompareTo(b.m_spawnTime)
        );
        // GD.Print($"Sorted meteors... {Time.GetTicksMsec()} {ps_meteorlist.Count}");
        ps_meteorUpdInQueuePtr = 0;
        foreach (var meteor in ps_meteorlist)
        {
            if (meteor.m_hitTime > maxHitTime)
            {
                maxHitTime = meteor.m_hitTime;
            }
        }
        GD.Print($"maxHitTime: {maxHitTime}");

        // 根据平台加载MIDI文件
        // LoadMidiFilesByPlatform();
        aliveMonsters = new();
        GetTree().CreateTimer(1.9f).Connect("timeout", new Callable(this, nameof(StartGame))); // 协程
    }


    void LoadSpecifyMidi()
    {
        var noteTimeSet = ParseMidi("res://MIDI/bell_001.mid", "res://AUDIO/八音盒.ogg", pfcommon);
        var noteTimeSet2 = ParseMidi("res://MIDI/bell_002.mid", "res://AUDIO/八音盒.ogg", pfcommon);
        var noteTimeSet3 = ParseMidi("res://MIDI/bell_003.mid", "res://AUDIO/八音盒.ogg", pfcommon);
        var noteTimeSet4 = ParseMidi("res://MIDI/key_001.mid", "res://AUDIO/八音盒.ogg", pfsplit);
        var noteTimeSet5 = ParseMidi("res://MIDI/drum_001.mid", "res://AUDIO/八音盒.ogg", pfexp);
        var noteTimeSet6 = ParseMidi("res://MIDI/drum_002.mid", "res://AUDIO/TeslaPipeC5.ogg", pfexp);
        var l = noteTimeSet.ToList().OrderBy(x => ps_random.Randf()).ToList();

        first30Note = l.Take(l.Count / 2).ToList();
        last30Note = l.Skip(l.Count / 2).ToList();
    }
    public void StartGame()
    {
        ps_runningstate = GameRunningState.RUNNING;
        ps_accumulatetimephysical = 0;
        ps_accumulatetime = 0;
        ps_gamestarttime = Time.GetTicksMsec();
        if (wndDebug != null)
            wndDebug.Show();
    }

    public List<float> ParseMidi(string midiFilePath, string oggFilePath, PackedScene pf)
    {
        List<float> noteOnTimeSet = new();
        // 直接读取文件字节内容，不用转为绝对路径
        byte[] midiBytes = FileAccess.GetFileAsBytes(midiFilePath);
        if (midiBytes == null || midiBytes.Length == 0)
        {
            Logger.Error($"Failed to read MIDI file bytes: {midiFilePath}");
            return noteOnTimeSet;
        }
        var midiFile = new MidiParser.MidiFile(midiBytes);
        // 将midiFilePath转为绝对路径
        // if (!System.IO.Path.IsPathRooted(midiFilePath))
        // {
        //     // Godot的res://路径转为绝对路径
        //     var absPath = ProjectSettings.GlobalizePath(midiFilePath);
        //     midiFilePath = absPath;
        // }
        // 使用MidiFile类解析MIDI文件
        // var midiFile = new MidiParser.MidiFile(midiFilePath);

        Logger.Info($"MIDI Info: Format={midiFile.Format}, Tracks={midiFile.TracksCount}, TicksPerQuarterNote={midiFile.TicksPerQuarterNote}");

        // 获取初始速度（默认120 BPM）
        int tempo = 120;
        float microsecondsPerQuarterNote = 500000; // 默认值，对应120 BPM

        // 尝试从MIDI文件中直接读取速度信息
        var tempoInfo = ExtractTempoFromMidiFile(midiFilePath);
        if (tempoInfo.HasValue)
        {
            microsecondsPerQuarterNote = tempoInfo.Value;
            tempo = (int)(60000000.0 / microsecondsPerQuarterNote);
            Logger.Info($"Extracted tempo: {tempo} BPM (microsecondsPerQuarterNote: {microsecondsPerQuarterNote})");
        }
        else
        {
            Logger.Info($"Using default tempo: {tempo} BPM");
        }

        // 计算每tick的秒数
        float secondsPerTick = microsecondsPerQuarterNote / 1000000.0f / midiFile.TicksPerQuarterNote;


        // 处理每个轨道
        for (int trackIndex = 0; trackIndex < midiFile.TracksCount; trackIndex++)
        {
            var track = midiFile.Tracks[trackIndex];

            // 获取轨道名称
            string trackName = $"Track {trackIndex}";
            foreach (var textEvent in track.TextEvents)
            {
                if (textEvent.TextEventType == MidiParser.TextEventType.TrackName)
                {
                    trackName = textEvent.Value;
                    break;
                }
            }

            Logger.Info($"Processing track {trackIndex}: {trackName} with {track.MidiEvents.Count} events");

            // 解析音符事件
            var activeNotes = new Dictionary<int, (int startTime, int velocity)>();

            foreach (var midiEvent in track.MidiEvents)
            {
                if (midiEvent.MidiEventType == MidiParser.MidiEventType.NoteOn && midiEvent.Velocity > 0)
                {
                    // 音符开始
                    activeNotes[midiEvent.Note] = (midiEvent.Time, midiEvent.Velocity);
                }
                else if (midiEvent.MidiEventType == MidiParser.MidiEventType.NoteOff ||
                         (midiEvent.MidiEventType == MidiParser.MidiEventType.NoteOn && midiEvent.Velocity == 0))
                {
                    // 音符结束
                    if (activeNotes.ContainsKey(midiEvent.Note))
                    {
                        var (startTime, velocity) = activeNotes[midiEvent.Note];

                        var endTime = midiEvent.Time;

                        // 转换为秒
                        float startTimeSeconds = startTime * secondsPerTick + GLOBAL_MIDI_TIMING_OFFSET;
                        float endTimeSeconds = endTime * secondsPerTick + GLOBAL_MIDI_TIMING_OFFSET;
                        float duration = endTimeSeconds - startTimeSeconds;

                        // 计算音调比例（相对于C5，MIDI音高72）
                        float pitchScale = (float)Math.Pow(2, (midiEvent.Note - 72) / 12.0);

                        // 计算音量比例（从MIDI velocity转换，范围0-127）
                        float volumeScale = velocity / 127.0f;
                        allNoteOnTime.Add(startTimeSeconds);
                        noteOnTimeSet.Add(startTimeSeconds);

                        // 创建陨石
                        CreateMeteor(startTimeSeconds, pitchScale, volumeScale, $"track={trackIndex}, note={midiEvent.Note}, duration={duration:F2}s, velocity={velocity}", oggFilePath, pf);

                        activeNotes.Remove(midiEvent.Note);
                    }
                }
            }

            // 处理未结束的音符（如果有的话）
            foreach (var kvp in activeNotes)
            {

                var note = kvp.Key;
                var (startTime, velocity) = kvp.Value;

                float startTimeSeconds = startTime * secondsPerTick + GLOBAL_MIDI_TIMING_OFFSET;
                allNoteOnTime.Add(startTimeSeconds);
                noteOnTimeSet.Add(startTimeSeconds);
                float pitchScale = (float)Math.Pow(2, (note - 72) / 12.0);

                // 计算音量比例（从MIDI velocity转换，范围0-127）
                float volumeScale = velocity / 127.0f;

                CreateMeteor(startTimeSeconds, pitchScale, volumeScale, $"track={trackIndex}, note={note}, velocity={velocity} (unfinished)", oggFilePath, pf);
            }
        }

        Logger.Info($"MIDI parsing completed: created {ps_meteorlist.Count} meteors from {midiFile.TracksCount} tracks");
        return noteOnTimeSet;
    }

    // 支持手动指定BPM，如果未指定则自动从MIDI文件读取
    [Export] public float ManualBPM = 0; // 可以在编辑器或代码中手动指定BPM

    private float? ExtractTempoFromMidiFile(string midiFilePath)
    {
        // 如果手动指定了BPM，则直接返回
        if (ManualBPM > 0)
        {
            Logger.Info($"Using manually specified BPM: {ManualBPM}");
            // MIDI标准：BPM = 60_000_000 / 微秒每四分音符
            // 反推微秒每四分音符
            float bpm = ManualBPM;
            float microsecondsPerQuarterNote = 60000000f / bpm;
            return microsecondsPerQuarterNote;
        }

        Logger.Debug($"Extracting tempo from MIDI file: {midiFilePath}");
        // 直接读取MIDI文件来解析速度信息
        var file = FileAccess.Open(midiFilePath, FileAccess.ModeFlags.Read);
        if (file == null) return null;

        var data = file.GetBuffer((long)file.GetLength());
        file.Close();

        // 查找速度事件 (FF 51 03 xx xx xx)
        for (int i = 0; i < data.Length - 4; i++)
        {
            if (data[i] == 0xFF && data[i + 1] == 0x51 && data[i + 2] == 0x03)
            {
                // 读取3字节的微秒每四分音符
                int microsecondsPerQuarterNote = (data[i + 3] << 16) | (data[i + 4] << 8) | data[i + 5];
                Logger.Debug($"Raw tempo data: {data[i + 3]:X2} {data[i + 4]:X2} {data[i + 5]:X2} = {microsecondsPerQuarterNote}");
                return microsecondsPerQuarterNote;
            }
        }
        return null;
    }
    /*
        public void ParseMML(string mml, string oggFilePath, PackedScene pf)
        {
            // 解析状态
            int octave = 4;
            int length = 4;  // 默认长度是四分音符
            int velocity = 100; // 默认音量
            int tempo = 120; // 默认速度 BPM

            // 音符映射表
            Dictionary<string, int> noteMap = new Dictionary<string, int>
            {
                {"C", 0}, {"B#", 0},
                {"C#", 1}, {"D-", 1},
                {"D", 2},
                {"D#", 3}, {"E-", 3},
                {"E", 4}, {"F-", 4},
                {"F", 5}, {"E#", 5},
                {"F#", 6}, {"G-", 6},
                {"G", 7},
                {"G#", 8}, {"A-", 8},
                {"A", 9},
                {"A#", 10}, {"B-", 10},
                {"B", 11}, {"C-", 11}
            };

            // 预处理：移除注释和清理空白
            string mmlClean = Regex.Replace(mml, @"//.*", "");
            mmlClean = string.Join(" ", mmlClean.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            // 正则表达式匹配MML标记
            string tokenPattern = @"([TLOV@])(\d+)|([<>])|([A-GR])([#\+\-]?)([\d\.]*)(&?)";
            Regex tokenRegex = new Regex(tokenPattern);

            float currentTime = 0f; // 当前时间（秒）
            float ticksPerQuarterNote = 480f; // MIDI标准分辨率
            float secondsPerTick = 60f / (tempo * ticksPerQuarterNote); // 每tick的秒数

            foreach (Match match in tokenRegex.Matches(mmlClean))
            {
                string command = match.Groups[1].Value;
                string cmdVal = match.Groups[2].Value;
                string octShift = match.Groups[3].Value;
                string note = match.Groups[4].Value;
                string accidental = match.Groups[5].Value;
                string lengthDots = match.Groups[6].Value;
                string tie = match.Groups[7].Value;

                // 处理命令 (T, L, O, V, @)
                if (!string.IsNullOrEmpty(command))
                {
                    int value = int.Parse(cmdVal);
                    switch (command)
                    {
                        case "T": // 速度
                            tempo = value;
                            secondsPerTick = 60f / (tempo * ticksPerQuarterNote);
                            break;
                        case "L": // 默认音符长度
                            length = value;
                            break;
                        case "O": // 设置八度
                            octave = value;
                            break;
                        case "V": // 音量
                            velocity = Math.Min(127, value * 8);
                            break;
                        case "@": // 乐器
                            // 这里可以设置乐器，暂时忽略
                            break;
                    }
                }
                // 处理八度移位 (<, >)
                else if (!string.IsNullOrEmpty(octShift))
                {
                    if (octShift == ">")
                        octave++;
                    else if (octShift == "<")
                        octave--;
                }
                // 处理音符和休止符
                else if (!string.IsNullOrEmpty(note))
                {
                    // 计算持续时间（MIDI ticks）
                    int lengthVal = length;
                    if (!string.IsNullOrEmpty(lengthDots))
                    {
                        string[] lengthParts = lengthDots.Split('.');
                        if (lengthParts[0].Length > 0)
                        {
                            lengthVal = int.Parse(lengthParts[0]);
                        }
                    }

                    float durationMultiplier = 1.0f;
                    string[] dots = lengthDots.Split('.');
                    if (dots.Length > 1)
                    {
                        for (int i = 1; i < dots.Length; i++)
                        {
                            durationMultiplier += 0.5f / (float)Math.Pow(2, dots.Length - 1);
                        }
                    }

                    float noteDurationTicks = (4f / lengthVal) * ticksPerQuarterNote * durationMultiplier;
                    float noteDurationSeconds = noteDurationTicks * secondsPerTick;

                    if (note == "R") // 休止符
                    {
                        currentTime += noteDurationSeconds;
                    }
                    else // 音符
                    {
                        // 计算MIDI音高
                        string noteKey = note.ToUpper() + accidental.Replace("+", "#");
                        if (noteMap.ContainsKey(noteKey))
                        {
                            int basePitch = noteMap[noteKey];
                            // MIDI音符C4是60，MML的O4对应MIDI八度5
                            int midiPitch = basePitch + (octave + 1) * 12;

                            // 计算音调比例（相对于C5，MIDI音高72）
                            float pitchScale = (float)Math.Pow(2, (midiPitch - 72) / 12.0);

                            // MML使用最大音量
                            float volumeScale = 1.0f;

                            // 创建陨石
                            CreateMeteor(currentTime, pitchScale, volumeScale, $"note={noteKey}, pitch={midiPitch}", oggFilePath, pf);
                        }

                        // 如果不是连音，更新时间
                        if (string.IsNullOrEmpty(tie))
                        {
                            currentTime += noteDurationSeconds;
                        }
                    }
                }
            }

            Logger.Info($"Parsed MML: created {ps_meteorlist.Count} meteors, total duration: {currentTime:F2}s");
        }
    */

    private void CreateMeteor(float hitTime, float pitchScale, float volumeScale, string debugInfo, string oggFilePath, PackedScene pf)
    {
        // PackedScene pf = (PackedScene)ResourceLoader.Load("res://PREFAB/PF_SoundOnlyMeteor.tscn");
        // PackedScene pf = (PackedScene)ResourceLoader.Load("res://PREFAB/PF_Meteor.tscn");

        // 生成随机位置
        float spawnX = ps_random.RandfRange(0, 1920);
        float hitX = ps_random.RandfRange(0, 1920);

        float spawnTime = hitTime - 1f; // 提前1秒生成

        var ins = pf.Instantiate<Area2D>();
        var meteor = ins as PfMeteor;

        meteor.Ctor(hitTime, spawnTime,
            new Vector2(spawnX, 0),
            new Vector2(hitX, 1080),
            pitchScale,
            volumeScale
        );

        var audioStream = ResourceLoader.Load<AudioStreamOggVorbis>(oggFilePath);
        // 设置meteor的音频流
        if (meteor.exp_se != null)
        {
            meteor.exp_se.Stream = audioStream;
        }


        AddChild(ins);
        ps_meteorlist.Add(meteor);

        // GD.Print($"Created meteor: {debugInfo}, time={hitTime:F2}s, pitchScale={pitchScale:F2}");
    }

    /// <summary>
    /// 获取当前平台可用的MIDI文件列表
    /// </summary>
    /// <returns>MIDI文件路径列表</returns>
    public List<string> GetAvailableMidiFiles()
    {
        List<string> midiFiles = new List<string>();
        string platform = OS.GetName().ToLower();

        if (platform == "android")
        {
            string externalPath = OS.GetUserDataDir();
            string midiFolder = System.IO.Path.Combine(externalPath, "midi");

            var dir = DirAccess.Open(midiFolder);
            if (dir != null)
            {
                dir.ListDirBegin();
                string fileName = dir.GetNext();

                while (!string.IsNullOrEmpty(fileName))
                {
                    if (fileName.ToLower().EndsWith(".mid") && !dir.CurrentIsDir())
                    {
                        string fullPath = System.IO.Path.Combine(midiFolder, fileName);
                        midiFiles.Add(fullPath);
                    }
                    fileName = dir.GetNext();
                }

                dir.ListDirEnd();
            }
        }
        else if (platform == "windows")
        {
            string executablePath = OS.GetExecutablePath();
            string executableDir = System.IO.Path.GetDirectoryName(executablePath);

            var dir = DirAccess.Open(executableDir);
            dir.ListDirBegin();
            string fileName = dir.GetNext();

            while (!string.IsNullOrEmpty(fileName))
            {
                if (fileName.ToLower().EndsWith(".mid") && !dir.CurrentIsDir())
                {
                    string fullPath = System.IO.Path.Combine(executableDir, fileName);
                    midiFiles.Add(fullPath);
                }
                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
        }

        return midiFiles;
    }

    /// <summary>
    /// 检查是否需要显示成功窗口
    /// </summary>
    private void CheckForSuccessWindow()
    {
        if (wndSuccess.Visible)
            return;

        // 如果游戏没有运行，不检查
        if (ps_runningstate != GameRunningState.RUNNING)
            return;

        // 如果meteor列表为空，不检查
        if (ps_meteorlist.Count == 0)
            return;

        // 如果当前时间超过了最后一个meteor的hitTime + 2秒，显示成功窗口
        if (ps_accumulatetime > maxHitTime + 2.0)
        {
            ShowSuccessWindow();
        }
    }

    /// <summary>
    /// 显示成功窗口
    /// </summary>
    private void ShowSuccessWindow()
    {
        if (wndSuccess.Visible)
            return;

        wndSuccess.ShowSuccessWindow((uint)ps_random.Seed, UI_ScoreWindow.Instance.score, WndFail.Instance.continueCount);

        Logger.Info($"游戏通关！种子: {ps_random.Seed}, 分数: {UI_ScoreWindow.Instance.score}");
    }



    /// <summary>
    /// 获取场景中所有存活的Monster
    /// </summary>
    /// <returns>存活的Monster列表</returns>
    public List<PfMonster> GetAllAliveMonsters()
    {
        return aliveMonsters.Values.ToList();
        // List<PfMonster> aliveMonsters = new List<PfMonster>();

        // // 遍历场景中所有节点，查找存活的Monster
        // GetAllMonstersRecursive(this, aliveMonsters);

        // return aliveMonsters;
    }

    // /// <summary>
    // /// 递归获取所有存活的Monster（辅助方法）
    // /// </summary>
    // /// <param name="node">当前节点</param>
    // /// <param name="monsters">Monster列表</param>
    // private void GetAllMonstersRecursive(Node node, List<PfMonster> monsters)
    // {
    //     // 检查当前节点是否为存活的Monster
    //     if (node is PfMonster monster && IsInstanceValid(monster) && monster.IsInsideTree())
    //     {
    //         monsters.Add(monster);
    //     }

    //     // 递归检查所有子节点
    //     foreach (Node child in node.GetChildren())
    //     {
    //         GetAllMonstersRecursive(child, monsters);
    //     }
    // }

    // /// <summary>
    // /// 获取场景中存活Monster的数量
    // /// </summary>
    // /// <returns>存活Monster的数量</returns>
    // public int GetAliveMonsterCount()
    // {
    //     return GetAllAliveMonsters().Count;
    // }

    // /// <summary>
    // /// 检查是否有存活的Monster
    // /// </summary>
    // /// <returns>如果有存活Monster返回true，否则返回false</returns>
    // public bool HasAliveMonsters()
    // {
    //     return GetAliveMonsterCount() > 0;
    // }
}
