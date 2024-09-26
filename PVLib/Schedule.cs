using System.Net;


namespace PVLib
{
    public class Schedule : ISchedule
    {
        public readonly List<TimeSlot> slots = new();
        public string Name { get; set; }
        int CurrentSlot;
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
        public async Task SendMedia(HttpListenerResponse client)
        {
            FileStream fs = new (Slot.Media, FileMode.Open, FileAccess.Read);
            try
            {
                client.ContentType = $"video/{info.Extension}";
                client.ContentLength64 = fs.Length;
                await fs.CopyToAsync(client.OutputStream);
            }
            catch (Exception ex)
            {
                fs.Close();
                Console.WriteLine(ex.ToString());
            }
            client.Close();
            fs.Close();
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

            UPNP.Update++;
            await Task.Delay(TimeSpan.FromMilliseconds(timeleft));
            while (CurrentSlot < slots.Count)
            {
                CurrentSlot++;
                UPNP.Update++;

                await Task.Delay((int)Slot.Duration.TotalMilliseconds);
            }
            if (DateTime.Now.Day == slots[^1].StartTime.Day)
            {
                Random random = new((int)DateTime.Now.Ticks);
                CurrentSlot = random.Next(0, slots.Count);
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
