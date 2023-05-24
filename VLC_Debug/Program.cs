using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VLC_Debug
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var myVaProxy = new vaProxy();
            Globals.vaProxy = myVaProxy;

            var musicDirectory = @"c:\My Doc\Google Drive\temp\music\";
            VlcPlayer.RunPlayer(musicDirectory);
            Console.ReadLine();//just to make sure the song is played 
            VlcPlayer.SendCommand("fastfwd");//speed it up to reach the end of the song
            Thread.Sleep(2000);
            VlcPlayer.SendCommand("next");//I still want to be able to congtol teh player externally
            VlcPlayer.SendCommand("next");

        }
    }
}
