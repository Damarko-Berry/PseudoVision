using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class ShowList: ISchedule
    {
        public Channel_Type ScheduleType => Channel_Type.Binge_Like;
        public List<string> Shows;
        public async void SendMedia(HttpListenerResponse client)
        {
            Random rnd = new Random();
            int shw = rnd.Next(Shows.Count);
            try
            {
                Show show = SaveLoad<Show>.Load(Shows[shw]);
                var info = new FileInfo(show.NextEpisode());
                SaveLoad<Show>.Save(show, Shows[shw]);
                var StreamBuffer = File.ReadAllBytes(info.FullName);
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
