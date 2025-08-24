"""
MIDI文件处理脚本
将MIDI文件转换为JSON格式，供Godot C#脚本使用
"""

import mido
import json
import sys
import os
from midi_parser import parse_midi_file

def process_midi_to_json(midi_file_path, output_dir="."):
    """
    处理MIDI文件并生成JSON格式的音符数据
    
    Args:
        midi_file_path: MIDI文件路径
        output_dir: 输出目录
    
    Returns:
        生成的JSON文件路径列表
    """
    
    print(f"Processing MIDI file: {midi_file_path}")
    
    # 解析MIDI文件
    midi_data = parse_midi_file(midi_file_path)
    if midi_data is None:
        print("Failed to parse MIDI file")
        return []
    
    # 生成输出文件名
    base_name = os.path.splitext(os.path.basename(midi_file_path))[0]
    output_file = os.path.join(output_dir, f"{base_name}_notes.json")
    
    # 保存JSON文件
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(midi_data, f, indent=2, ensure_ascii=False)
    
    print(f"Generated JSON file: {output_file}")
    print(f"Total tracks: {len(midi_data['tracks'])}")
    total_notes = sum(len(track['notes']) for track in midi_data['tracks'])
    print(f"Total notes: {total_notes}")
    
    return [output_file]

def create_sample_midi():
    """
    创建一个示例MIDI文件用于测试
    """
    mid = mido.MidiFile(ticks_per_beat=480)
    track = mido.MidiTrack()
    mid.tracks.append(track)
    
    # 添加轨道名称
    track.append(mido.MetaMessage('track_name', name='Sample Melody', time=0))
    
    # 添加速度
    track.append(mido.MetaMessage('set_tempo', tempo=mido.bpm2tempo(120), time=0))
    
    # 添加乐器（钢琴）
    track.append(mido.Message('program_change', program=0, time=0))
    
    # 创建简单的旋律：小星星
    notes = [
        (60, 480),  # C4 - 1拍
        (60, 480),  # C4 - 1拍
        (67, 480),  # G4 - 1拍
        (67, 480),  # G4 - 1拍
        (69, 480),  # A4 - 1拍
        (69, 480),  # A4 - 1拍
        (67, 960),  # G4 - 2拍
        (65, 480),  # F4 - 1拍
        (65, 480),  # F4 - 1拍
        (64, 480),  # E4 - 1拍
        (64, 480),  # E4 - 1拍
        (62, 480),  # D4 - 1拍
        (62, 480),  # D4 - 1拍
        (60, 960),  # C4 - 2拍
    ]
    
    for note, duration in notes:
        # 音符开始
        track.append(mido.Message('note_on', note=note, velocity=100, time=0))
        # 音符结束
        track.append(mido.Message('note_off', note=note, velocity=0, time=duration))
    
    return mid

def main():
    if len(sys.argv) == 1:
        print("Usage: python process_midi.py <midi_file> [output_dir]")
        print("Or: python process_midi.py --create-sample")
        return
    
    if len(sys.argv) == 2 and sys.argv[1] == "--create-sample":
        print("Creating sample MIDI file...")
        sample_midi = create_sample_midi()
        sample_file = "sample_melody.mid"
        sample_midi.save(sample_file)
        print(f"Created sample MIDI file: {sample_file}")
        
        # 处理示例文件
        process_midi_to_json(sample_file)
        return
    
    if len(sys.argv) < 2:
        print("Usage: python process_midi.py <midi_file> [output_dir]")
        print("Or: python process_midi.py --create-sample")
        return
    
    midi_file = sys.argv[1]
    output_dir = sys.argv[2] if len(sys.argv) > 2 else "."
    
    if not os.path.exists(midi_file):
        print(f"MIDI file not found: {midi_file}")
        return
    
    process_midi_to_json(midi_file, output_dir)

if __name__ == "__main__":
    main()
