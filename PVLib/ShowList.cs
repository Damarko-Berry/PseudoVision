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
        public async Task SendMedia(HttpListenerContext client)
        {
            if (File.Exists(LastPLayed)) {
                CurrentlyPlaying = SaveLoad<TimeSlot>.Load(LastPLayed);
            }
            else
            {
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

        public string GetContent(int s, string ip, int prt)
        {
            return $@"<item id=""{s}"" parentID=""0"" restricted=""false"">
                        <dc:title>{Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem.videoProgram</upnp:class>
                        <res protocolInfo=""http-get:*:video/{info.Extension}:*"">http://{ip}:{prt}/live/{Name}.mp4</res>
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
