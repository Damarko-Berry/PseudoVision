using Microsoft.Win32.SafeHandles;
using PVLib;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.Metadata;
using static PVLib.Settings;

namespace PVLite
{
    internal class Program
    {
        static TV_LikeChannel channel = null;
        
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

        static async void HandleClient(TcpClient client)
        {
            
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(stream);
            using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            // Read the HTTP request
            string request = await reader.ReadToEndAsync();
            Console.WriteLine("Received request: " + request);

            // Skip the remaining request headers
            while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) { }

            // Create a simple HTTP response
            string response = "HTTP/1.1 200 OK\r\n" +
                                "Content-Type: text/plain\r\n" +
                                "Content-Length: 13\r\n" +
                                "\r\n" +
                                "Hello, World!";

            // Write the HTTP response to the client
            await writer.WriteAsync(response);
            Console.WriteLine("Response sent.");

            client.Close();
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
                channel.rotation.ShowList = new();
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
        }

        static void Scan4New()
        {
            
        }

    }
}
