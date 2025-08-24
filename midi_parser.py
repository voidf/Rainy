"""
MIDI文件解析器，将MIDI文件转换为简单的音符数据格式
"""

import mido
import json
import sys
import os

def parse_midi_file(midi_file_path):
    """
    解析MIDI文件，提取所有音符信息
    
    返回格式：
    {
        "tempo": 120,
        "ticks_per_beat": 480,
        "tracks": [
            {
                "name": "Track 1",
                "notes": [
                    {
                        "start_time": 0.0,      # 开始时间（秒）
                        "end_time": 0.5,        # 结束时间（秒）
                        "note": 60,             # MIDI音符（C4）
                        "velocity": 100,        # 音量
                        "duration": 0.5         # 持续时间（秒）
                    }
                ]
            }
        ]
    }
    """
    
    try:
        mid = mido.MidiFile(midi_file_path)
    except Exception as e:
        print(f"Error opening MIDI file: {e}")
        return None
    
    # 获取初始速度
    initial_tempo = 120  # 默认120 BPM
    for track in mid.tracks:
        t = 0
        for msg in track:
            t += msg.time
            if msg.type == "set_tempo" and t == 0:
                initial_tempo = round(mido.tempo2bpm(msg.tempo))
                break
    
    result = {
        "tempo": initial_tempo,
        "ticks_per_beat": mid.ticks_per_beat,
        "tracks": []
    }
    
    # 处理每个轨道
    for track_index, track in enumerate(mid.tracks):
        track_data = {
            "name": f"Track {track_index}",
            "notes": []
        }
        
        # 获取轨道名称
        for msg in track:
            if msg.type == "track_name":
                track_data["name"] = msg.name
                break
        
        # 解析音符
        t = 0  # 当前时间（ticks）
        tempo = initial_tempo
        active_notes = {}  # 当前活跃的音符 {note: (start_tick, velocity)}
        
        for msg in track:
            t += msg.time
            
            # 处理速度变化
            if msg.type == "set_tempo":
                tempo = round(mido.tempo2bpm(msg.tempo))
            
            # 处理音符开始
            elif msg.type == "note_on" and msg.velocity > 0:
                active_notes[msg.note] = (t, msg.velocity)
            
            # 处理音符结束
            elif (msg.type == "note_off") or (msg.type == "note_on" and msg.velocity == 0):
                if msg.note in active_notes:
                    start_tick, velocity = active_notes[msg.note]
                    end_tick = t
                    
                    # 转换为秒
                    start_time = mido.tick2second(start_tick, mid.ticks_per_beat, tempo)
                    end_time = mido.tick2second(end_tick, mid.ticks_per_beat, tempo)
                    duration = end_time - start_time
                    
                    note_data = {
                        "start_time": round(start_time, 3),
                        "end_time": round(end_time, 3),
                        "note": msg.note,
                        "velocity": velocity,
                        "duration": round(duration, 3)
                    }
                    
                    track_data["notes"].append(note_data)
                    del active_notes[msg.note]
        
        # 处理未结束的音符
        for note, (start_tick, velocity) in active_notes.items():
            start_time = mido.tick2second(start_tick, mid.ticks_per_beat, tempo)
            end_time = mido.tick2second(t, mid.ticks_per_beat, tempo)
            duration = end_time - start_time
            
            note_data = {
                "start_time": round(start_time, 3),
                "end_time": round(end_time, 3),
                "note": note,
                "velocity": velocity,
                "duration": round(duration, 3)
            }
            
            track_data["notes"].append(note_data)
        
        # 按开始时间排序
        track_data["notes"].sort(key=lambda x: x["start_time"])
        
        if track_data["notes"]:
            result["tracks"].append(track_data)
    
    return result

def midi_to_json(midi_file_path, output_file_path=None):
    """
    将MIDI文件转换为JSON格式
    """
    data = parse_midi_file(midi_file_path)
    if data is None:
        return None
    
    if output_file_path is None:
        base_name = os.path.splitext(os.path.basename(midi_file_path))[0]
        output_file_path = f"{base_name}_notes.json"
    
    with open(output_file_path, 'w', encoding='utf-8') as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
    
    print(f"MIDI解析完成，输出到: {output_file_path}")
    print(f"总轨道数: {len(data['tracks'])}")
    total_notes = sum(len(track['notes']) for track in data['tracks'])
    print(f"总音符数: {total_notes}")
    
    return output_file_path

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python midi_parser.py <midi_file> [output_json_file]")
        sys.exit(1)
    
    midi_file = sys.argv[1]
    output_file = sys.argv[2] if len(sys.argv) > 2 else None
    
    midi_to_json(midi_file, output_file)
