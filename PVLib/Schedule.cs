using System;
using System.Net;
using System.Net.Sockets;
using System.Xml.Serialization;


namespace PVLib
{
    public class Schedule : ISchedule
    {
        public readonly List<TimeSlot> slots = new();
        
        public string Name { get; set; }
        int CurrentSlot;
        public Schedule_Type ScheduleType => Schedule_Type.TV_Like;
        public TimeSlot Slot 
        { 
            get 
            {
                try
                {
                    return slots[CurrentSlot];
                }
                catch
                {
                    Random random = new((int)DateTime.Now.Ticks);
                    int i = random.Next(0, slots.Count);
                    return slots[i];
                } 
            } 
        }
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
        [XmlIgnore]
        public Dictionary<string, ISchedule> AllSchedules { get; set; }

        public async Task SendMedia(HttpListenerContext client)
        {
            
            FileStream fs = new(Slot.Media, FileMode.Open, FileAccess.Read);
            try
            {
                client.Response.ContentType = $"video/{info.Extension}";
                client.Response.ContentLength64 = fs.Length;
                await fs.CopyToAsync(client.Response.OutputStream);
            }
            catch (Exception ex)
            {
                fs.Close();
                Console.WriteLine(ex.ToString());
            }
            client.Response.Close();
            fs.Close();
        }

        public async Task SendMedia(string Request, NetworkStream stream)
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(Slot.Media);
            try
            {
                StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
                writer.WriteLine("HTTP/1.1 200 OK"); writer.WriteLine("Content-Type: video/mp4");
                writer.WriteLine($"Content-Length: {fileBytes.Length}");
                writer.WriteLine();
                writer.Flush();
                await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }catch (Exception ex)
            {
                stream.Close();
                Console.WriteLine(ex.ToString());
            }
            stream.Close();
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
                if(CurrentSlot+1 == slots.Count)
                {
                    DateTime tmrw = DateTime.Now.AddDays(1);
                    var chan = Channel.Load(FileSystem.ChanSchedules(Name));
                    chan.CreateNewSchedule(tmrw);
                    if(chan.ScheduleExists(tmrw))
                    {
                        var scdpath = Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{tmrw.Month}.{tmrw.Day}.{tmrw.Year}.{FileSystem.ScheduleEXT}");
                        Schedule sch = SaveLoad<Schedule>.Load(scdpath);
                        slots.AddRange(sch.slots);
                    }
                }
                await Task.Delay(TimeSpan.FromMilliseconds(timeleft));
            }
            Random random = new((int)DateTime.Now.Ticks);
            CurrentSlot = random.Next(0, slots.Count);
            AllSchedules.Remove(Name);
        }

        public string GetContent(int index, string ip, int prt)
        {
            return $@"<item id=""{index}"" parentID=""0"" restricted=""false"">
                        <dc:title>{info.Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem</upnp:class>
                        <res protocolInfo=""http-get:*:video/{info.Extension}:*"" resolution=""1920x1080"">http://{ip}:{prt}/live/{Name}</res>
                        <upnp:albumArtURI>http://{ip}:{prt}/thumbnails/{Name}.png</upnp:albumArtURI>
                    </item>";
        }

        public Schedule()
        {

        }
        public Schedule(HLSSchedule schedule)
        {
            slots = schedule.slots;
            Name = schedule.Name;
        }
        public static implicit operator Schedule(HLSSchedule schedule)
        {
            return new(schedule);
        }
       
        public static implicit operator HLSSchedule(Schedule schedule)
        {
            return new(schedule);
        }
    }
   
}
