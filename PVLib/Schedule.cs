using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace PVLib
{
    public class Schedule : ISchedule
    {
        public readonly List<TimeSlot> slots = new();
        public string Name {  get; set; }
        int CurrentSlot;
        byte[] StreamBuffer;
        public Channel_Type ScheduleType => Channel_Type.TV_Like;
        public TimeSlot Slot => slots[CurrentSlot];
        
        public TimeSpan ScheduleDuration
        {
            get
            {
                TimeSpan time = new TimeSpan();
                for (int i = 0; i < slots.Count; i++)
                {
                    time += slots[i].Duration;
                }
                return time;
            }
        }
        public FileInfo info => new FileInfo(Slot.Media);
        public async void SendMedia(HttpListenerResponse client)
        {
            try
            {
                client.ContentType = $"video/{info.Extension}";
                client.ContentLength64 = StreamBuffer.Length;
                client.SendChunked = true;
                await client.OutputStream.WriteAsync(StreamBuffer);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            client.Close();
        }

        public async void StartCycle()
        {
            var ct = DateTime.Now;
            double timeleft = 0;
            for (CurrentSlot = 0; CurrentSlot < slots.Count; CurrentSlot++)
            {
                if (Slot.Durring(ct))
                {
                    timeleft = (Slot.EndTime - ct).TotalMilliseconds;
                    break;
                }
            }
            StreamBuffer = File.ReadAllBytes(Slot.Media);
            UPNP.Update++;
            await Task.Delay(TimeSpan.FromMilliseconds(timeleft));
            while (CurrentSlot < slots.Count)
            {
                CurrentSlot++;
                UPNP.Update++;
                StreamBuffer = File.ReadAllBytes(Slot.Media);
                await Task.Delay((int)Slot.Duration.TotalMilliseconds);
            }
            if (DateTime.Now.Day == slots[^1].StartTime.Day)
            {
                Random random = new((int)DateTime.Now.Ticks);
                CurrentSlot = random.Next(0, slots.Count);
                StreamBuffer = File.ReadAllBytes(Slot.Media);
            }
        }

        public string GetContent(int index, string ip, int prt)
        {
            return $@"<item id=""{index}"" parentID=""0"" restricted=""false"">
                        <dc:title>{info.Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem.videoProgram</upnp:class>
                        <res protocolInfo=""http-get:*:video/mp4:*"">http://{ip}:{prt}/live/{Name}</res>
                    </item>";
        }

        public Schedule()
        {

        }
    }
}
