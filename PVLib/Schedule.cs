using System;
using System.Net;
using System.Net.Sockets;


namespace PVLib
{
    public class Schedule : ISchedule
    {
        public readonly List<TimeSlot> slots = new();
        public bool isLive = false;
        List<segment> Segment = new();
        int CurrentSegment;
        Dictionary<string, int> ClientProgress= new();
        public string Name { get; set; }
        string liveOutputDirectory => Path.Combine("output", Name, "segments");
        public string Manifest => File.ReadAllText(Path.Combine("output", Name, "index.m3u8"));
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
            
            FileStream fs = new(Slot.Media, FileMode.Open, FileAccess.Read);
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
        public async Task SendMedia(HttpListenerContext client)
        {
            ClientProgress.TryAdd(client.Request.UserAgent, CurrentSegment);
            FileStream fs = new(Segment[ClientProgress[client.Request.UserAgent]].path, FileMode.Open, FileAccess.Read);
            
            try
            {
                client.Response.ContentType = $"video/{info.Extension}";
                client.Response.ContentLength64 = fs.Length;
                await fs.CopyToAsync(client.Response.OutputStream);
                int CI = ClientProgress[client.Request.UserAgent]+1;
                if(CI > Segment.Count)
                {
                    CI = 0;
                }
                ClientProgress[client.Request.UserAgent] = CI + 1;
            }
            catch (Exception ex)
            {
                fs.Close();
                Console.WriteLine(ex.ToString());
            }
            client.Response.Close();
            fs.Close();
        }

        public async void StartCycle()
        {
            isLive = false;
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
            if (isLive)
            {
                var age = DateTime.Now-(DateTime)Slot.StartTime;
                await SegCyc(Slot,age);
            }
            else
            {
                await Task.Delay(TimeSpan.FromMilliseconds(timeleft));
            }
            while (CurrentSlot < slots.Count)
            {
                CurrentSlot++;
                UPNP.Update++;
                if (isLive)
                {
                    
                    await SegCyc(Slot);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(timeleft));
                }
            }
            Random random = new((int)DateTime.Now.Ticks);
            CurrentSlot = random.Next(0, slots.Count);
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

        #region live
        async Task SegCyc(TimeSlot slot, TimeSpan sinceOGStart = new())
        {
            ProcessVideo(slot.Media);
            
            await Task.Delay(10*1000);
            int SS = 0;
            if (sinceOGStart.TotalSeconds > 20)
            {
                while (Directory.GetFiles(liveOutputDirectory).Length <= 0)
                {
                    await Task.Delay(1000);
                }
                TimeSpan TSA= new TimeSpan();
                for (SS = 0; SS < Segment.Count; SS++)
                {
                    TSA.Add(Segment[SS].duration);
                    if(TSA> sinceOGStart)
                    {
                        break;
                    }
                }
            }
            Console.WriteLine("Ready to stream");
            for ( CurrentSegment= SS;  CurrentSegment< Directory.GetFiles(liveOutputDirectory).Length; CurrentSegment++)
            {
                var m3u = File.ReadAllLines(Path.Combine("output", Name, "index.m3u8"));
                Segment.Clear();
                for (int i = 0; i < m3u.Length; i++)
                {
                    if (m3u[i].Contains("#EXTINF:"))
                    {
                        var timeSegment = m3u[i].Split(':')[1];
                        var second = int.Parse(timeSegment.Split('.')[0]);
                        var millisecons = int.Parse(timeSegment.Split('.')[1].Replace(",", string.Empty).Replace(".", string.Empty));
                        i++;
                        Segment.Add(new(Path.Combine(liveOutputDirectory,m3u[i]),new TimeSpan(0, 0, 0,  second, 0, millisecons)));
                    }
                }
                int time = (int)Segment[CurrentSegment].duration.TotalMilliseconds-100;
                await Task.Delay(time);
            }
        }
        async Task ProcessVideo(string filePath)
        {
            
            string playlist = Path.Combine("output",Name);
            Directory.CreateDirectory(liveOutputDirectory);
            filePath = "\""+filePath+"\"";
            string ffmpegArgs = $"-i {filePath} -c:v libx264 -c:a aac -strict -2 -f hls -hls_time 10 -hls_list_size 0 -hls_segment_filename {liveOutputDirectory}/[{Name}]seg%d.ts {playlist}/index.m3u8";
            await RunFFmpeg(ffmpegArgs); 
        }
        async Task RunFFmpeg(string arguments) 
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = @"ffmpeg\ffmpeg.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    await process.WaitForExitAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
            Console.WriteLine("Processed");
        }
        #endregion
    }
    struct segment
    {
        public string path;
        public TimeSpan duration;
        public segment(string path, TimeSpan duration)
        {
            this.path = path;
            this.duration = duration;
        }
    }
}
