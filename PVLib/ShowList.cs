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
        public List<string> Shows = new();
        TimeSlot CurrentlyPlaying = new();
        public async void SendMedia(HttpListenerResponse client)
        {
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
                var info = new FileInfo(CurrentlyPlaying.Media);
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

        public string GetContent()
        {
            throw new NotImplementedException();
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
