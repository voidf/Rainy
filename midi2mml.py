# -*- coding: utf-8 -*-
import os
from fractions import Fraction
from collections import Counter
import mido

ALLOWED_DENOMS = [1, 2, 4, 8, 16, 32, 64]
MAX_DOTS = 2

def default_tolerance(tpqn: int) -> int:
    return max(1, tpqn // 48)

PC_TO_NAME_SHARP = {
    0: "C", 1: "C#", 2: "D", 3: "D#", 4: "E",
    5: "F", 6: "F#", 7: "G", 8: "G#", 9: "A", 10: "A#", 11: "B"
}

def tempo_events_initial_bpm(mid: mido.MidiFile, default_bpm: int = 120) -> int:
    for track in mid.tracks:
        t = 0
        for msg in track:
            t += msg.time
            if msg.type == "set_tempo" and t == 0:
                return round(mido.tempo2bpm(msg.tempo))
    return default_bpm

def ticks_of_length(tpqn: int, denom: int, dots: int) -> Fraction:
    base = Fraction(4, denom)
    dot_factor = sum(Fraction(1, 2**i) for i in range(dots + 1))
    return Fraction(tpqn) * base * dot_factor

def build_duration_table(tpqn: int):
    table = []
    for denom in ALLOWED_DENOMS:
        for dots in range(MAX_DOTS + 1):
            table.append((denom, dots, ticks_of_length(tpqn, denom, dots)))
    table.sort(key=lambda x: x[2], reverse=True)
    return table

def decompose_ticks_to_lengths(ticks: int, tpqn: int, tol: int):
    table = build_duration_table(tpqn)
    remaining = Fraction(ticks, 1)
    parts = []
    while remaining > tol:
        candidate = None
        for denom, dots, dur in table:
            if dur <= remaining + tol:
                candidate = (denom, dots, dur)
                break
        if candidate is None:
            break
        denom, dots, dur = candidate
        parts.append((denom, dots))
        remaining -= dur
    if remaining > tol // 2:
        denom, dots, _ = build_duration_table(tpqn)[-1]
        parts.append((denom, dots))
    return parts

def optimize_default_L(parts_list):
    from collections import Counter
    c = Counter()
    for parts in parts_list:
        for denom, _ in parts:
            c[denom] += 1
    return min(ALLOWED_DENOMS, key=lambda d: (-(c[d]), ALLOWED_DENOMS.index(d))) if c else 4

def mml_len_token(denom: int, dots: int, default_L: int) -> str:
    if denom == default_L and dots == 0:
        return ""
    return f"{denom}{'.' * dots}"

def note_name_and_octave(midi_note: int):
    pc = midi_note % 12
    return PC_TO_NAME_SHARP[pc], midi_note // 12 - 1

def extract_polyphonic_segments(track: mido.MidiTrack):
    """提取多音轨段，处理重叠音符"""
    t = 0
    active_notes = {}  # 当前活跃的音符 {note: (start_time, velocity)}
    all_segments = []  # 所有音符段
    program = None
    first_velocity = None
    track_name = None

    for msg in track:
        t += msg.time
        if msg.type == "track_name" and track_name is None:
            track_name = msg.name
        if msg.type == "program_change" and program is None:
            program = msg.program

        if msg.type == "note_on" and msg.velocity > 0:
            # 音符开始
            active_notes[msg.note] = (t, msg.velocity)
            if first_velocity is None:
                first_velocity = msg.velocity
        elif (msg.type == "note_off") or (msg.type == "note_on" and msg.velocity == 0):
            # 音符结束
            if msg.note in active_notes:
                start_time, velocity = active_notes[msg.note]
                all_segments.append((start_time, t, msg.note, velocity))
                del active_notes[msg.note]

    # 处理未结束的音符（如果有的话）
    for note, (start_time, velocity) in active_notes.items():
        all_segments.append((start_time, t, note, velocity))

    return all_segments, program, first_velocity, track_name

def split_overlapping_segments(segments):
    """将重叠的音符段分离到不同的轨道"""
    if not segments:
        return []
    
    # 按开始时间排序
    segments = sorted(segments, key=lambda x: x[0])
    
    # 分离重叠的音符
    tracks = []
    used_segments = set()
    
    while len(used_segments) < len(segments):
        current_track = []
        current_end_time = 0
        
        for i, (start, end, note, vel) in enumerate(segments):
            if i in used_segments:
                continue
                
            # 检查是否与当前轨道重叠
            if start >= current_end_time:
                current_track.append((start, end, note, vel))
                current_end_time = end
                used_segments.add(i)
        
        if current_track:
            tracks.append(current_track)
    
    return tracks

def extract_monophonic_segments(track: mido.MidiTrack):
    """保持向后兼容的原始单音轨提取函数"""
    t = 0
    current = None
    segments = []
    program = None
    first_velocity = None
    track_name = None

    for msg in track:
        t += msg.time
        if msg.type == "track_name" and track_name is None:
            track_name = msg.name
        if msg.type == "program_change" and program is None:
            program = msg.program

        if msg.type == "note_on" and msg.velocity > 0:
            if current is not None:
                segments.append((current["start"], t, current["note"], current["velocity"]))
            current = {"note": msg.note, "start": t, "velocity": msg.velocity}
            if first_velocity is None:
                first_velocity = msg.velocity
        elif (msg.type == "note_off") or (msg.type == "note_on" and msg.velocity == 0):
            if current is not None and msg.note == current["note"]:
                segments.append((current["start"], t, current["note"], current["velocity"]))
                current = None

    if current is not None:
        segments.append((current["start"], t, current["note"], current["velocity"]))

    return segments, program, first_velocity, track_name

def build_mml_for_track(mid: mido.MidiFile, track_index: int, segments, program=None, first_velocity=None, track_name=None) -> str:
    tpqn = mid.ticks_per_beat
    tol = default_tolerance(tpqn)
    bpm = tempo_events_initial_bpm(mid, 120)

    if not segments:
        header = f"T{bpm} L4"
        if program is not None:
            header += f" @{program}"
        return f"// Extracted by midi_to_mml (track {track_index}{' - ' + track_name if track_name else ''})\n" + header + "\n// (该轨无音符)\n"

    parts_list = []
    for start, end, _note, _vel in segments:
        parts_list.append(decompose_ticks_to_lengths(max(0, end - start), tpqn, tol))

    default_L = optimize_default_L(parts_list)

    tokens = [f"T{bpm}", f"L{default_L}"]
    if program is not None:
        tokens.append(f"@{program}")
    if first_velocity is not None:
        V = max(0, min(15, round(first_velocity / 8)))
        tokens.append(f"V{V}")

    segments.sort(key=lambda s: s[0])
    _, first_oct = note_name_and_octave(segments[0][2])
    tokens.append(f"O{first_oct}")
    cur_oct = first_oct
    cursor = 0

    def append_rest(gap_ticks: int):
        if gap_ticks <= 0:
            return
        for denom, dots in decompose_ticks_to_lengths(gap_ticks, tpqn, tol):
            tokens.append("R" + mml_len_token(denom, dots, default_L))

    def append_note(note: int, dur_ticks: int):
        nonlocal cur_oct
        name, octv = note_name_and_octave(note)
        diff = octv - cur_oct
        if diff == 0:
            pass
        elif abs(diff) <= 2:
            tokens.append((">" if diff > 0 else "<") * abs(diff))
        else:
            tokens.append(f"O{octv}")
        cur_oct = octv

        pieces = []
        for i, (denom, dots) in enumerate(decompose_ticks_to_lengths(dur_ticks, tpqn, tol)):
            ltok = mml_len_token(denom, dots, default_L)
            pieces.append((("&" if i > 0 else "") + f"{name}{ltok}"))
        tokens.append("".join(pieces))

    for start, end, note, _vel in segments:
        append_rest(start - cursor)
        append_note(note, max(0, end - start))
        cursor = end

    out_lines, line = [], []
    for t in tokens:
        line.append(t)
        if len(line) >= 16:
            out_lines.append(" ".join(line))
            line = []
    if line:
        out_lines.append(" ".join(line))

    header = f"// Extracted by midi_to_mml (track {track_index})\n"
    return header + "\n".join(out_lines) + "\n"

def convert_all_tracks(midi_file_path: str):
    mid = mido.MidiFile(midi_file_path)
    base = os.path.splitext(os.path.basename(midi_file_path))[0]
    outputs = []
    
    for i, tr in enumerate(mid.tracks):
        # 提取所有音符段（包括重叠的）
        all_segments, program, first_velocity, track_name = extract_polyphonic_segments(tr)
        
        if not all_segments:
            continue
            
        # 分离重叠的音符到不同轨道
        separated_tracks = split_overlapping_segments(all_segments)
        
        for sub_track_idx, track_segments in enumerate(separated_tracks):
            # 生成文件名
            if len(separated_tracks) == 1:
                # 只有一个轨道，使用原始命名
                out_path = f"{base}_track{i}.mml"
            else:
                # 多个轨道，使用 <轨道号>.<重叠轨道下标> 命名
                out_path = f"{base}_track{i}.{sub_track_idx}.mml"
            
            # 构建MML
            mml = build_mml_for_track(mid, i, track_segments, program, first_velocity, track_name)
            
            # 写入文件
            with open(out_path, "w", encoding="utf-8") as f:
                f.write(mml)
            outputs.append(out_path)
            
            print(f"导出轨道 {i}.{sub_track_idx}: {len(track_segments)} 个音符")
    
    return outputs

if __name__ == "__main__":
    import argparse
    ap = argparse.ArgumentParser()
    ap.add_argument("midi", help="输入 MIDI 文件路径")
    args = ap.parse_args()
    outs = convert_all_tracks(args.midi)
    if outs:
        print("已导出：")
        for p in outs:
            print(" -", p)
    else:
        print("未发现包含音符的轨道。")
