using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VLC_Debug
{
    public class VlcPlayer
    {
        private static int _waitForVlc = 500;//ms
        private static List<string> _vlcCommands = new List<string>
            { "pause", "cont", "next", "prev", "up", "down", "stop" };

        public static void RunPlayer(string musicDirectory)
        {
            Globals.VlcIsRunning = true;
            string playListFile;
            playListFile = GenVlcPlayList(musicDirectory);


            var musicVolume = 60;
            Core.Initialize();
            var libVlc = new LibVLC();
            var playList = new Media(libVlc, playListFile);

            playList.Parse(MediaParseOptions.ParseLocal);
            Thread.Sleep(_waitForVlc);

            // Create a shuffled playlist if random play is desired
            var shuffledPlaylist = playList.SubItems.OrderBy(s => Guid.NewGuid()).ToList();
            if (shuffledPlaylist.Count == 0)
            {
                Console.WriteLine("Playlist is empty");
                Globals.VlcIsRunning = false;
                return;
            }

            // Create a media player
            var mediaPlayer = new MediaPlayer(libVlc);
            mediaPlayer.Volume = musicVolume;
            //mediaPlayer.Media = shuffledPlaylist[0];
            Thread.Sleep(_waitForVlc);
            Globals.vaProxy.SessionState["player"] = mediaPlayer;
            mediaPlayer.Play(shuffledPlaylist[0]);
            Globals.vaProxy.SessionState["playList"] = shuffledPlaylist.Skip(1).ToList();
            mediaPlayer.EndReached += MediaPlayer_EndReached;
            //PlayNextSong();
        }

        private static void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            PlayNextSong();
        }

        private static void PlayNextSong()
        {
            MediaPlayer mediaPlayer = Globals.vaProxy.SessionState["player"];
            List<Media> playList = Globals.vaProxy.SessionState["playList"];

            if (playList.Count > 0)
            {
                var nextMedia = playList[0];
                playList.RemoveAt(0);
                //mediaPlayer.Media = nextMedia;
                var state = mediaPlayer.State;
                mediaPlayer.Play(nextMedia); //This is never performed after the event is triggered.
            }
            else
            {
                Console.WriteLine("Playlist ended.");
                mediaPlayer.Stop();
                mediaPlayer.Dispose();
                Globals.vaProxy.SessionState["player"] = null;
                Globals.vaProxy.SessionState["playList"] = null;
            }
        }


        public static void SendCommand(string command)
        {
            if (!Globals.VlcIsRunning) return;
            Dictionary<string, dynamic> sessionState = Globals.vaProxy.SessionState;
            if (!sessionState.ContainsKey("player") || !sessionState.ContainsKey("playList"))
            {
                Console.WriteLine("Unable. Player ia not running");
            }
            MediaPlayer mediaPlayer = Globals.vaProxy.SessionState["player"];
            List<Media> playList = Globals.vaProxy.SessionState["playList"];
            var volumeIncDec = 60;

            switch (command)
            {
                case "stop":
                    mediaPlayer.Stop();
                    break;

                case "pause":
                    mediaPlayer.Pause();
                    break;

                case "cont":
                    mediaPlayer.Play();
                    break;

                case "fastfwd":
                    Thread.Sleep(_waitForVlc);
                    var mediaLength = mediaPlayer.Length;
                    mediaPlayer.Time = mediaLength - 3000;
                    break;

                case "next":
                    var newSong = playList[0];
                    var newPlayList = playList.Skip(1).ToList();
                    Globals.vaProxy.SessionState["playList"] = newPlayList;
                    mediaPlayer.Stop();
                    Thread.Sleep(_waitForVlc);
                    mediaPlayer.Media = newSong;
                    Thread.Sleep(_waitForVlc);
                    mediaPlayer.Play();
                    break;

                case "prev":
                    break;

                case "up":
                    var newVolumeUp = mediaPlayer.Volume + volumeIncDec;
                    if (newVolumeUp > 100)
                    {
                        newVolumeUp = 100;
                    }
                    mediaPlayer.Volume = newVolumeUp;
                    break;

                case "down":
                    var newVolumeDn = mediaPlayer.Volume - volumeIncDec;
                    if (newVolumeDn < 0)
                    {
                        newVolumeDn = 100;
                    }
                    mediaPlayer.Volume = newVolumeDn;
                    break;
            }
        }

        public static void TestKeys()
        {
            foreach (var command in _vlcCommands)
            {
                Console.WriteLine(command);
                SendCommand(command);
                Thread.Sleep(2000);
            }
        }



        public static string GenVlcPlayList(string musicDirectory)
        {
            var playlistFileName = "my_playlist.xspf"; 
            var tempDir = Path.GetTempPath();
            var playlistFilePath = Path.Combine(tempDir, playlistFileName);
            var musicFiles = GetMusicFiles(musicDirectory);
            GeneratePlaylistFile(musicFiles, playlistFilePath);
            Console.WriteLine("Playlist file generated: " + playlistFilePath);
            return playlistFilePath;
        }

        static List<string> GetMusicFiles(string directory)
        {
            var musicFiles = new List<string>();
            // Collect all music files in the directory and its subfolders
            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file).ToLower();

                // Add the file to the musicFiles list if it has a valid music file extension
                if (IsMusicFile(extension))
                {
                    musicFiles.Add(file);
                }
            }
            return musicFiles;
        }

        static bool IsMusicFile(string extension)
        {
            string[] validExtensions = { ".mp3", ".wav", ".ogg", ".flac" };
            return validExtensions.Contains(extension);
        }

        static void GeneratePlaylistFile(List<string> musicFiles, string playlistFilePath)
        {
            using (var writer = new StreamWriter(playlistFilePath))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                writer.WriteLine("<playlist version=\"1\" xmlns=\"http://xspf.org/ns/0/\">");
                writer.WriteLine("  <trackList>");
                foreach (var file in musicFiles)
                {
                    //var fileName = Path.GetFileName(file);
                    var fileName = Uri.EscapeDataString(Path.GetFileName(file))
                        //.Replace("%", "%25")
                        .Replace(" ", "%20")
                        .Replace("'", "%27")
                        .Replace("(", "%28")
                        .Replace(")", "%29")
                        .Replace("[", "%5B")
                        .Replace("]", "%5D")
                        .Replace("&", "%26");
                    var filePath = "file:///" + file.Replace("\\", "/").Replace(Path.GetFileName(file), fileName);

                    writer.WriteLine("    <track>");
                    writer.WriteLine("      <location>" + filePath + "</location>");
                    writer.WriteLine("      <title>" + fileName + "</title>");
                    writer.WriteLine("    </track>");
                }
                writer.WriteLine("  </trackList>");
                writer.WriteLine("</playlist>");
            }
        }

        static string FindBestMatchSong(string directory, string songName)
        {
            var musicFiles = GetMusicFiles(directory);
            var bestMatchFilePath = string.Empty;
            var bestMatchScore = 0;

            foreach (var file in musicFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var matchScore = CalculateMatchScore(songName, fileName);
                if (matchScore > bestMatchScore)
                {
                    bestMatchScore = matchScore;
                    bestMatchFilePath = file;
                }
            }

            return bestMatchFilePath;
        }

        static int CalculateMatchScore(string searchQuery, string fileName)
        {
            var searchTerms = searchQuery.ToLower().Split(' ');
            var fileTerms = fileName.ToLower().Split(' ');
            var matchScore = 0;
            foreach (var searchTerm in searchTerms)
            {
                foreach (var fileTerm in fileTerms)
                {
                    if (fileTerm.StartsWith(searchTerm))
                    {
                        matchScore++;
                        break;
                    }
                }
            }
            return matchScore;
        }
    }
}
