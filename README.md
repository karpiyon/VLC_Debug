# VLC_Debug
A simple test to try and create a VLC player in C#.
The purpose is to integrate it inside a plugin I create for VoiceAttack for a fight sim (DCS/BMS).
The user will be able to say "start play music" and the songs will start playing.

To achieve this I keep the MediaPLayer object globally so that I will be able to control it using, e.g., Play() or Pause() commands.

What I can’t achieve, so far, is continuous play. 
I am trying the add an event to the “mediaPlayer.EndReached” event but the player object does not keep playing.
 
It requires these nuget:
LibVLCSharp & VideoLAN.LibVLC.Windows
