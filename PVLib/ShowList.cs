using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static System.Reflection.Metadata.BlobBuilder;

namespace PVLib
{
    public class ShowList: ISchedule
    {
        public Schedule_Type ScheduleType => Schedule_Type.Binge_Like;
        public List<string> Shows = new();
        TimeSlot CurrentlyPlaying = new();
        Playlist GetPlaylist
        {
            get
            {
                if (File.Exists(FileSystem.Archive(Name, DateTime.Now)))
                {
                    return new(FileSystem.Archive(Name, DateTime.Now));
                }
                Directory.CreateDirectory(FileSystem.ArchiveDirectory(Name));
                return new();
            }
            
        }
        string LastPLayed => Path.Combine(FileSystem.ChanSchedules(Name), "Last Played", $"LastPLayed.lsp");
        FileInfo info => new FileInfo(CurrentlyPlaying.Media);
        public string Name { get; set; }
        [XmlIgnore]
        public Dictionary<string, ISchedule> AllSchedules { get; set; }

        public async Task SendMedia(HttpListenerContext client)
        {
            if (File.Exists(LastPLayed)) {
                CurrentlyPlaying = SaveLoad<TimeSlot>.Load(LastPLayed);
            }
            else
            {
                UPNP.Update++;
                Directory.CreateDirectory(Path.Combine(FileSystem.ChanSchedules(Name), "Last Played"));
            }
            var P = GetPlaylist;
            Random rnd = new Random();
            int shw = rnd.Next(Shows.Count);
            if (DateTime.Now > CurrentlyPlaying.EndTime)
            {
                Show show = SaveLoad<Show>.Load(Shows[shw]);
                CurrentlyPlaying = new TimeSlot(show.NextEpisode());
                P.Add(CurrentlyPlaying);
                File.WriteAllText(FileSystem.Archive(Name, DateTime.Now), P.ToString());
                UPNP.Update++;
                SaveLoad<Show>.Save(show, Shows[shw]);
            }
            SaveLoad<TimeSlot>.Save(CurrentlyPlaying, LastPLayed);

            FileStream fs = new FileStream(CurrentlyPlaying.Media, FileMode.Open, FileAccess.Read);
            try
            {
                Console.WriteLine(info.Name);
                client.Response.ContentType = $"video/{info.Name.Replace(info.Extension, string.Empty)}";
                client.Response.ContentLength64 = fs.Length;
                await fs.CopyToAsync(client.Response.OutputStream);
            }
            catch (Exception ex)
            {
                fs.Close();
                Console.WriteLine(ex.ToString());
            }
            fs.Close();
            client.Response.Close();
        }

        public async Task SendMedia(string Request, NetworkStream stream)
        {
            if (File.Exists(LastPLayed))
            {
                CurrentlyPlaying = SaveLoad<TimeSlot>.Load(LastPLayed);
            }
            else
            {
                UPNP.Update++;
                Directory.CreateDirectory(Path.Combine(FileSystem.ChanSchedules(Name), "Last Played"));
            }
            var P = GetPlaylist;
            Random rnd = new Random();
            int shw = rnd.Next(Shows.Count);
            if (DateTime.Now > CurrentlyPlaying.EndTime)
            {
                Show show = SaveLoad<Show>.Load(Shows[shw]);
                CurrentlyPlaying = new TimeSlot(show.NextEpisode());
                P.Add(CurrentlyPlaying);
                File.WriteAllText(FileSystem.Archive(Name, DateTime.Now), P.ToString());
                SaveLoad<Show>.Save(show, Shows[shw]);
                UPNP.Update++;
            }
            SaveLoad<TimeSlot>.Save(CurrentlyPlaying, LastPLayed);

            byte[] fileBytes = await File.ReadAllBytesAsync(CurrentlyPlaying.Media);
            using StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
            writer.WriteLine("HTTP/1.1 200 OK"); writer.WriteLine("Content-Type: video/mp4");
            writer.WriteLine($"Content-Length: {fileBytes.Length}"); 
            writer.WriteLine(); 
            writer.Flush(); 
            await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
        }
        public string GetContent(int s, string ip, int prt)
        {
            return $@"<item id=""{s}"" parentID=""0"" restricted=""false"">
                        <dc:title>{Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem.videoItem</upnp:class>
                        <res protocolInfo=""http-get:*:video/{info.Extension}:*"" resolution=""1920x1080"">http://{ip}:{prt}/live/{Name}</res>
                    </item>";
        }
        public async Task StartCycle()
        {
            TimeSpan FiveMin= new(0, 5, 0);
        StartUp:
            await Task.Delay(TimeLeftInDay.Subtract(FiveMin));
            DateTime tmrw = DateTime.Now.AddDays(1);
            var chan = Channel.Load(FileSystem.ChanSchedules(Name));
            chan.CreateNewSchedule(tmrw);
            await Task.Delay(TimeLeftInDay);
            if (chan.ScheduleExists(tmrw))
            {
                var scdpath = Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{tmrw.Month}.{tmrw.Day}.{tmrw.Year}.{FileSystem.ScheduleEXT}");
                Shows = SaveLoad<ShowList>.Load(scdpath).Shows;
                goto StartUp;
            }
            AllSchedules.Remove(Name);
        }
        TimeSpan TimeLeftInDay
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
                TimeSpan timeLeft = endOfDay - now;
                return timeLeft;
            }
        }
        public ShowList() { }
        public ShowList(DirectoryInfo ShowDirectory)
        {
            for (int i = 0; i < ShowDirectory.GetFiles().Length; i++)
            {
                Shows.Add(ShowDirectory.GetFiles()[i].FullName);
            }
        }
    }
}
