using Microsoft.Win32.SafeHandles;
using PVLib;
using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using static PVLib.Settings;

namespace PVLite
{
    internal class Program 
    {
        static TV_LikeChannel channel = null;
        static ISchedule schedule = null;
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Starting Server");
                try
                {
                    CurrentSettings = SaveLoad<Settings>.Load(FileSystem.SettingsFile);
                }
                catch
                {
                    CurrentSettings = Settings.Default;
                }
               
                Directory.SetCurrentDirectory(@"C:\Users\marko\Videos\Media\PVL");
                Set_Up();
                Task.Run(() => Server());
                Console.ReadLine();
            }
        }

        private static async Task Server()
        {
            TcpListener server = new(IPAddress.Any, CurrentSettings.Port);

            server.Start();
            while (true)
            {
                
                HandleClient(await server.AcceptTcpClientAsync());
            }
        }

        static async Task HandleClient(TcpClient client)
        {
            
            using NetworkStream ClientStream = client.GetStream();
            byte[] bytes = new byte[client.ReceiveBufferSize];
            int bytesRead;
            StringBuilder messageBuilder = new StringBuilder();
            while ((bytesRead = await ClientStream.ReadAsync(bytes, 0, bytes.Length)) != 0)
            {
                messageBuilder.Append(Encoding.ASCII.GetString(bytes, 0, bytesRead));

                string request = messageBuilder.ToString().Split("\n")[0].Split(' ')[1].Trim();
                Console.WriteLine(request);

                if (request == "/watch")
                {
                    await schedule.SendMedia(request, client.GetStream());
                    return;
                }
                else
                {
                    client.Close();
                }
            }
        }

        static void Set_Up()
        {
            var cn = new DirectoryInfo(Directory.GetCurrentDirectory()).Name;
            if (!File.Exists(FileSystem.ChannleChan(cn)))
            {
                channel = new TV_LikeChannel();
                channel.HomeDirectory= Path.Combine(FileSystem.Channels, cn);
                Directory.CreateDirectory(channel.ShowDirectory);
                Directory.CreateDirectory(channel.SeasonsDirectory);
                Directory.CreateDirectory(channel.ShortsDirectory);
                List<DirectoryInfo> dirs = new List<DirectoryInfo>(new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.GetDirectories());
                for (int i = 0; i < dirs.Count; i++)
                {
                    if (dirs[i].Name == "PVL")
                    {
                        dirs.RemoveAt(i);
                        break;
                    }
                }
                var PotShows = dirs.ToArray();
                for (int i = 0; i < PotShows.Length; i++)
                {
                    var Dtype = ContentDirectory.DDetector(PotShows[i]);
                    switch( Dtype )
                    {
                        case DirectoryType.Show:
                            Show show = new Show();
                            show.HomeDirectory = PotShows[i].FullName;
                            channel.AddShow(show);
                            break;
                        case DirectoryType.Movie:
                            MovieDirectory movieDirectory = new MovieDirectory();
                            movieDirectory.HomeDirectory = PotShows[i].FullName;
                            SaveLoad<MovieDirectory>.Save(movieDirectory, Path.Combine(channel.ShowDirectory, PotShows[i].Name + ".shw"));
                            break;
                    }
                }
            }
            else
            {
                channel = SaveLoad<TV_LikeChannel>.Load(FileSystem.ChannleChan(cn));
                Scan4New();
            }
            channel.CreateNewSchedule(DateTime.Now);
            var Da = DateTime.Now;
            var M = Da.Date.Month;
            var D = Da.Date.Day;
            var Y = Da.Date.Year;
            var scdpath = Path.Combine(FileSystem.ChanSchedules(channel.ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}");
            var sch = SaveLoad<Schedule>.Load(scdpath);
            sch.StartCycle();
            schedule = sch;
        }

        static void Scan4New()
        {
            
        }

    }
}
