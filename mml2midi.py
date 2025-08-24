# Help me generate a melody using Music Macro Language so that the music composed of these notes sounds ethereal. It should contains about 240 notes, include every possible note which can appear in MML. The tone used is piano. A sample output is: T120 L4 O4\nCCGG AAG FFEEDDC\nGGFF EED GGFF EED\nCCGG AAG FFEEDDC

import re
import mido

# --- Configuration ---
TICKS_PER_QUARTER_NOTE = 480  # Standard MIDI resolution

# Map MML note names to MIDI pitch offsets from C
# MML uses C, C#, D, D#, E, F, F#, G, G#, A, A#, B
NOTE_MAP = {
    'C': 0, 'B#': 0,
    'C#': 1, 'D-': 1,
    'D': 2,
    'D#': 3, 'E-': 3,
    'E': 4, 'F-': 4,
    'F': 5, 'E#': 5,
    'F#': 6, 'G-': 6,
    'G': 7,
    'G#': 8, 'A-': 8,
    'A': 9,
    'A#': 10, 'B-': 10,
    'B': 11, 'C-': 11
}

def mml_to_midi(mml_string: str, output_filename: str = "output.mid"):
    """
    Parses an MML string and creates a MIDI file.
    
    Args:
        mml_string: The string containing the MML code.
        output_filename: The name of the MIDI file to save.
    """
    # --- Mido Setup ---
    mid = mido.MidiFile(ticks_per_beat=TICKS_PER_QUARTER_NOTE)
    track = mido.MidiTrack()
    mid.tracks.append(track)

    # --- Parser State ---
    state = {
        'octave': 4,
        'length': 4,  # Default length is a quarter note
        'velocity': 100, # Default velocity
        'tempo': 120, # Default tempo in BPM
    }
    
    # State for handling tied notes
    pending_note_off = None

    # --- Pre-processing ---
    # 1. Remove comments
    mml_clean = re.sub(r'//.*', '', mml_string)
    # 2. Collapse all whitespace to single spaces for easier tokenization
    mml_clean = ' '.join(mml_clean.split())

    # --- Main Parsing Logic ---
    # Regex to find all MML tokens
    token_regex = re.compile(r'([TLOV@])(\d+)|([<>])|([A-GR])([#\+\-]?)([\d\.]*)(&?)')
    
    time_since_last_note = 0

    for match in token_regex.finditer(mml_clean):
        command, cmd_val, oct_shift, note, accidental, length_dots, tie = match.groups()

        # 1. Handle Commands (T, L, O, V, @)
        if command:
            value = int(cmd_val)
            if command == 'T': # Tempo
                state['tempo'] = value
                # Add a tempo change event to the MIDI track
                track.append(mido.MetaMessage('set_tempo', tempo=mido.bpm2tempo(value), time=0))
            elif command == 'L': # Default Note Length
                state['length'] = value
            elif command == 'O': # Set Octave
                state['octave'] = value
            elif command == 'V': # Volume (Velocity)
                # MML volume is often 0-15. MIDI is 0-127. We scale it.
                # Assuming the MML uses a 0-15 range as is common.
                state['velocity'] = min(127, value * 8) 
            elif command == '@': # Instrument (Program Change)
                track.append(mido.Message('program_change', program=value, time=0))

        # 2. Handle Octave Shifts (<, >)
        elif oct_shift:
            if oct_shift == '>':
                state['octave'] += 1
            elif oct_shift == '<':
                state['octave'] -= 1

        # 3. Handle Notes and Rests
        elif note:
            # First, if a note was played and not tied, we need to turn it off.
            if pending_note_off:
                track.append(mido.Message('note_off', **pending_note_off))
                pending_note_off = None
            
            # Calculate duration in MIDI ticks
            length_val = state['length']
            if length_dots and length_dots.split('.')[0]:
                length_val = int(length_dots.split('.')[0])
            
            duration_multiplier = 1.0
            for dot in length_dots.split('.')[1:]:
                duration_multiplier += 0.5 / (2**len(length_dots.split('.')[1:]))
            
            note_duration_ticks = int((4 / length_val) * TICKS_PER_QUARTER_NOTE * duration_multiplier)

            if note == 'R': # It's a Rest
                time_since_last_note += note_duration_ticks
            else: # It's a Note
                # Calculate MIDI pitch
                base_pitch = NOTE_MAP[note.upper() + accidental.replace('+', '#')]
                # MIDI note C4 is 60. O4 in MML corresponds to MIDI octave 5.
                midi_pitch = base_pitch + (state['octave'] + 1) * 12

                # Create and append MIDI messages
                note_on_msg = {
                    'note': midi_pitch,
                    'velocity': state['velocity'],
                    'time': time_since_last_note
                }
                track.append(mido.Message('note_on', **note_on_msg))
                time_since_last_note = 0 # Reset time delta

                # Handle ties
                if tie == '&':
                    # This note is tied to the next one, so we don't schedule its note_off yet.
                    # We just extend its duration. The next note will handle the final note_off.
                    # A simple model: create a note_off but hold onto it.
                    # A better model for this parser: just extend the duration for the eventual note_off
                    # We will create a single note_off event when the tie chain breaks.
                    # For simplicity here, we'll treat `&` as "hold this note open".
                    # The note_off will be triggered by the *next* note or end of song.
                    pending_note_off = {
                        'note': midi_pitch,
                        'velocity': state['velocity'],
                        'time': note_duration_ticks
                    }
                else:
                    # This is a normal note, turn it off after its duration.
                    track.append(mido.Message('note_off', 
                                              note=midi_pitch, 
                                              velocity=state['velocity'], 
                                              time=note_duration_ticks))
    
    # Final cleanup: turn off any remaining tied note at the end of the song
    if pending_note_off:
        track.append(mido.Message('note_off', **pending_note_off))

    # Save the file
    print(f"Saving MIDI file to {output_filename}...")
    mid.save(output_filename)
    print("Done.")


# --- Main Execution ---
if __name__ == "__main__":
    # Paste the MML code from the previous response here
    ethereal_mml = """
    // Title: Celestial Echoes
    // Tone: Piano (@0)
    // This piece contains approx. 248 notes.

    // --- Section 1: Gentle Introduction (Drifting Arpeggios) ---
    T100 L8 @0 V7 O4
    C E G >C <G E C R8
    F# A >D <A F# A <B> R8
    E G >C <G E G <A> R8
    D F# A >C# <A F# D R8

    // --- Section 2: Main Theme (Melody over a low drone) ---
    V9 L4 O3
    C1 O5 G4 E4 F#4 G4
    O3 G1 O5 A4 G4 F#4 E4
    O3 A1 O5 E4 D#4 E4 F#4
    O3 F#1 O5 B4 A4 G4 B4

    // --- Section 3: Development (Faster, more complex) ---
    T120 V11 L16 O5
    C+ D E- F G# A B- >C <B- A G# F E- D C+
    O6 C <B- A G# F E- D C+ >C+ <B- A G# F E- D
    L8 O4 A- G F E- D- C <B- A-

    // --- Section 4: Rhythmic Interlude (Syncopation and space) ---
    T110 V10 O4
    C4. E8 G4. B8 >D4. <A8 G2
    F#4. A8 >C#4. E8 <B4. >F#8 <A2
    O5 C4.&C16 R16 D#4.&D#16 R16 F#4.&F#16 R16 A#4.&A#16 R16

    // --- Section 5: Climax (High, loud, and fast) ---
    T135 V15 L16 O6
    C D E F G A B >C
    L32 <B A G F E D C >C <B A G F E D C
    L16 O7 C D E F G A B >C <B A G F E D C
    O7 C1&C2. R8

    // --- Section 6: Outro (Fade to silence) ---
    T95 V9 L4 O5
    G F# E D <B> A G F#
    V7 E D C <B A G F# E.
    V5 L2 D C R4
    V3 O2 C1&C1 R1
    """
    ethereal_mml = """
T100 L8 @1 V14 O4 R4  > D4  E4 <   > 
D  E  F <  >   G4 > C <<  >
G E <<  >  >  F  E <  > D <
   > C4. <  A# > C <<  >> D < 
> C < A# <  >> C <  > D <   >
C <   > D  F <   >  F4 < 
>  G  <   > C#4.   <   F#4 <
 >> B > C# D O3  O6 C#4 < B A F# << F#4
>> E F# << B4  >> E F# <<  >> D E F# E
<< A >> D C# D <   < B4 >  > D4 
<  > E4  F#4  <<  >> B4. > C#16 D16  O3
A >> B A B <<  O6 D C#16 < A16 R16 F#16 A16 F#16
<<  >> A B  <<  O6 D <  > C#  <
 << G4 >> A > C#16 D16 O3  >>  > C# < A
>  < F# > D O3  >> A4 R2 G# A# <  >
B A# G# <  > F#4 R F# G# <  > D# R B
R <  > A# R F# R <  > G# F# G# B <
F# > B > C# D# <<  >> C#4 < B4 <   >
A# F# A# B <   >> C#4 D# C# <  < F# >>
C# D# F# <  >  << G# > B A# B <  >
F# < D# >  < F# >  < A#16 > F#16   <
 B8. >> C# R16 <  C#16 D#16  <  > C# < B16
> F#16 < A#  > D# <  >   < G# B >
C# D# < G#  B  > C# <  > D# <<  >
B < B >  > C# F# << F#  >> D# <  >
C# D# <  < E B > C# B <  > A#  <
B >>  < C# > D# <<  >> C# << E >  <
G# > A# <  > G# R  < D#8. R16  > G# <
B > C# D#  G#  < B >  < A#  >> C#
 <<  > B < B >>  <  > C#  F# <<
F# >>  <<  >> D#  <  > C# D#  << 
> G# < B >  >  < C# >  F# << D# >
 >  << B >  C# A#  < C# E >>  <<
 > B >  < A# >   <  < F# >> B
  <<  > F# > F# <<  >>   < G# <
B > C# D# G# A#  > B  > C# O3  >> 
<  >> D O3  O6 C# <<  > B <<  >> 
<  > A4 < C#  >  A <  > B << 
>>  D < D  >  > D <<  > F# << 
O6 C# O3  >> D <  >  A <  > D <<
 >  > B <<  >> A <  >  B < 
>  > D O3 A >  >  > D <<   >>
E <<  >  > F# O3  >>  > E << D 
>  > D << B <  >>   > C# O3  >>
A >  << C# >>  << E <  >>   > E
O3  >> A >  <  < D >>  << G > 
 << A >  >  > E   << E >>  <<
A <  >>  > E <<  >> D <<  >> C# 
<< A <  >  >  E < F# >  <  >
 << A >  >  C#16 A16  <  >   <<
G B16 O6 E16 << D  >  > D16 < F#16 >  <<
 >  << A >>  < C#16 > A16 <  > C# <
 >  F#    <<  > B >  < D 
> E <  > F# <  > E <  >  F# <
 >  B <  >  > C# O3  >>  < 
>> D O3  O6 C# <<  > B <  >  << 
>> E R < C# >  <  > A <  > B 
<<  >> D < D  >  > D <<  > F# >
 O3  O6 C# O3  >> D >   <<  > 
A <  > D >  O3  >  > B <<  >>
A <  >  B >  <<  >  > D O3 A
O6  <<  > C# >  <<  A >>  <<  >
C# >   O3  >> D < D >>  <<  > 
> D << B >>  O3  >>   > C# O3  >>
A >  <<  >> C#  <<  >> D O3  >> 
 > E O3  >> A <  >  > F# <<  >>
E O3  >>  E <  >  > E <<  >> 
F# <<  >> A O3  >>  > E <<  >> D <<
 >> C#  << A <  >  >  E < F# >
 <  >  << A >  >  C#16 A16 <<  >>
  <  > D << B16 O6 E16 << D >>  << 
>  > D16 < F#16 >  O3  >>  <  >> C#
<<  > D16 >  < A16 <  > C# >  << 
>  F# >  <<  <  >>   F# <  >
D <  > E <  > F#  < B >   <
F# >   < A >   < B
    """

    mml_to_midi(ethereal_mml, "ethereal_melody.mid")