using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class ShowList: ISchedule
    {
        public Channel_Type ScheduleType => Channel_Type.Binge_Like;
        public List<string> Shows = new();
        TimeSlot CurrentlyPlaying = new();
        string LastPLayed => Path.Combine(Directory.GetCurrentDirectory(), "Schedules", Name, "Last Played", $"LastPLayed.lsp");
        FileInfo info => new FileInfo(CurrentlyPlaying.Media);
        public string Name { get; set; }
        public async void SendMedia(HttpListenerResponse client)
        {
            if (File.Exists(LastPLayed)) {
                CurrentlyPlaying = SaveLoad<TimeSlot>.Load(LastPLayed);
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", Name, "Last Played"));
            }
            Random rnd = new Random();
            int shw = rnd.Next(Shows.Count);
            try
            {
                if (DateTime.Now > CurrentlyPlaying.EndTime)
                {
                    Show show = SaveLoad<Show>.Load(Shows[shw]);
                    CurrentlyPlaying = new TimeSlot(show.NextEpisode());
                    SaveLoad<Show>.Save(show, Shows[shw]);
                }
                SaveLoad<TimeSlot>.Save(CurrentlyPlaying, LastPLayed);
                Console.WriteLine(info.Name);
                var StreamBuffer = File.ReadAllBytes(CurrentlyPlaying.Media);
                client.ContentType = $"video/{info.Name.Replace(info.Extension, string.Empty)}";
                client.ContentLength64 = StreamBuffer.Length;
                client.SendChunked = true;
                await client.OutputStream.WriteAsync(StreamBuffer, 0, StreamBuffer.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            client.Close();
        }

        public string GetContent(int s, string ip, int prt)
        {
            return $@"<item id=""{s}"" parentID=""0"" restricted=""false"">
                        <dc:title>{Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem.videoProgram</upnp:class>
                        <res protocolInfo=""http-get:*:video/mp4:*"">http://{ip}:{prt}/live/{Name}.mp4</res>
                    </item>";
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
