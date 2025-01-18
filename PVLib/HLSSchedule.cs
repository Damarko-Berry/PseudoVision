using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.Xml.Serialization;
using static PVLib.ISchedule;

namespace PVLib
{
    public class HLSSchedule : PVObject, ISchedule
    {
        public readonly List<TimeSlot> slots = new();
        public HLSSchedule() { }
        segment CurrentSegment = null;
        int CurrentSlot;
        bool processing;
        
        public TimeSlot Slot => slots[CurrentSlot];
        
        int SlotsAvalible 
        { 
            get
            {
                int G = 0;
                var HL = Directory.GetFiles(ManifestOutputDirectory,"index(*).m3u8");
                for (int i = 0; i < HL.Length; i++)
                {
                    var H = HLS.Load(File.ReadAllText(HL[i]));
                    if (File.Exists(Path.Combine(liveOutputDirectory, H.Body[^1].path)))
                    {
                        G++;
                    }
                }
                return G;
            } 
        }
        public FileInfo info => new FileInfo(Slot.Media);

        public string Name { get; set; }
        string liveOutputDirectory => Path.Combine("output", Name, "segments");
        string ManifestOutputDirectory => Path.Combine("output", Name);
        HLS CurrentSate=null;
        public Schedule_Type ScheduleType => Schedule_Type.LiveStream;

        public string GetContent(int index, string ip, int prt)
        {
            return $@"<item id=""{index+1}"" parentID=""0"" restricted=""false"">
                        <dc:title>{Name}</dc:title>
                        <dc:creator>My boy</dc:creator>
                        <upnp:class>object.item.videoItem</upnp:class>
                        <res protocolInfo=""http-get:*:application/vnd.apple.mpegurl:*"" resolution=""1920x1080"">http://{ip}:{prt}/live/{Name}{info.Extension}</res>
                    </item>";
        }

        public async Task SendMedia(HttpListenerContext client)
        
        {
            if(CurrentSegment == null)
            {
                client.Response.OutputStream.Close();
                return;
            }
            if (!client.Request.Url.AbsolutePath.Contains("["))
            {
                SendManifest(client);
                return;
            }
            var seg = client.Request.Url.AbsolutePath.Split("/");
            var pth = Path.Combine(liveOutputDirectory, seg[^1].Replace("/", string.Empty));
            if (!File.Exists(pth))
            {
                SendManifest(client);
                return;
            }
            FileStream fs = new(pth, FileMode.Open, FileAccess.Read);

            try
            {
                client.Response.ContentType = $"video/{info.Extension}";
                client.Response.ContentLength64 = fs.Length;
                await fs.CopyToAsync(client.Response.OutputStream);
            }
            catch (Exception ex)
            {
                //fs.Close();
                Console.WriteLine(ex.ToString());
            }
            //client.Response.Close();
            fs.Close();
        }
        
        
        public async Task SendMedia(string Request, NetworkStream stream)
        {
            if (CurrentSegment == null) return;
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };
            if (!Request.Contains("["))
            {
                HLS hLS = CurrentSate;
                hLS.SetOffset(CurrentSegment.path);
                var des = hLS.ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(des);
                writer.WriteLine("HTTP/1.1 200 OK"); 
                writer.WriteLine("Content-Type: application/vnd.apple.mpegurl");
                writer.WriteLine($"Content-Length: {buffer.Length}");
                writer.WriteLine();
                writer.Flush();
                await stream.WriteAsync(buffer);
                return;
            }
            var seg = Request.Split("/");
            byte[] fileBytes = await File.ReadAllBytesAsync(Path.Combine(liveOutputDirectory, seg[^1].Replace("/", string.Empty)));
            writer.WriteLine("HTTP/1.1 200 OK"); 
            writer.WriteLine("Content-Type: video/mp4");
            writer.WriteLine($"Content-Length: {fileBytes.Length}");
            writer.WriteLine();
            writer.Flush();
            await stream.WriteAsync(fileBytes);
        }

        public async Task StartCycle()
        {
            ConsoleLog.Cycle(Name);
            CleanUp();
            await Task.Delay(200);
            while (!Slot.Durring(DateTime.Now))
            {
                Increment();
            }

            await SegCyc();
        }
        #region live
        async void UpdateCurrentState(CancellationToken Toolong)
        {
            while(AllSchedules.ContainsKey(Name)&!Toolong.IsCancellationRequested)
            {
                
                var Hs = new HLS();
                var manifests = Directory.GetFiles(ManifestOutputDirectory, @"index(*).m3u8");
                for (int i = 0; i < manifests.Length; i++)
                {
                    Hs += HLS.Load(File.ReadAllText(manifests[i]));
                }
                CurrentSate = Hs;
                if (CurrentSegment == null)
                {
                    await Task.Delay(500);
                }
                else
                {
                    await Task.Delay(CurrentSegment.duration);
                }
            }
            Console.WriteLine("UpdateCurrentState has ended");
        }

        async Task SegCyc()
        {

            ProcessVideo(Slot.Media, CurrentSlot);
            await Task.Delay(5*1000);
            CancellationTokenSource cts = new();
            var Toolong = cts.Token;
            UpdateCurrentState(Toolong);
        Oops:
            DateTime StartTime = Slot.StartTime;
            var TargetOffset = DateTime.Now.Subtract(StartTime);
            var pLength = CurrentSate.Length;
            while (TargetOffset > pLength)
            {
                await Task.Delay(500);
                TargetOffset = DateTime.Now.Subtract(StartTime);
                pLength =CurrentSate.Length;
                if (!Slot.Durring(DateTime.Now))
                {
                    cts.Cancel();
                    CleanUp();
                    StartCycle();
                    return;
                }
            }
            CurrentSegment = CurrentSate.GetSegment(DateTime.Now.Subtract(StartTime));
            ProcessNext();
            
            
            while(CurrentSegment != null)
            {
                ConsoleLog.writeMessage("current Segment: "+CurrentSegment.path);
                await Task.Delay(CurrentSegment.duration);
                if (CurrentSegment.path.Contains("seg0"))
                {
                    Increment();
                    CleanUp(CurrentSlot - 1, 30);
                }
                if (CurrentSegment.path.Contains("seg3"))
                {
                    CleanUp(CurrentSlot - 1, 0);
                }
                ProcessNext();
                CurrentSegment = CurrentSate.NextSegment(CurrentSegment);
                
            }
            if (processing) goto Oops;
            await Task.Delay(TimeLeftInDay);
            cts.Cancel();
            AllSchedules.Remove(Name);
            CleanUp(CurrentSlot, 0);
        }
        async Task TestSegCyc()
        {

            ProcessVideo(Slot.Media, CurrentSlot);
            CancellationTokenSource cts = new();
            var Toolong = cts.Token;
            UpdateCurrentState(Toolong);
            while (CurrentSate.Length.TotalMinutes < 1)
            {
                await Task.Delay(500);
            }
            CurrentSegment = CurrentSate.Body[0];
            ProcessNext();

        Oops:
            while (CurrentSegment != null)
            {
                Console.WriteLine("current Segment: "+CurrentSegment.path);
                await Task.Delay((int)CurrentSegment.duration.TotalMilliseconds);
                if (!processing)
                {
                    if (CurrentSegment.path.Contains("seg0"))
                    {
                        Increment();
                        CleanUp(CurrentSlot - 1, 30);
                    }
                    if (CurrentSegment.path.Contains("seg3"))
                    {
                        CleanUp(CurrentSlot - 1, 0);
                    }
                }
                ProcessNext();
                CurrentSegment = CurrentSate.NextSegment(CurrentSegment);

            }
            if (processing) goto Oops;
            cts.Cancel();
        }
        
        void Increment()
        {
            UPNP.Update++;
            CurrentSlot++;
            if (CurrentSlot + 1 == slots.Count)
            {
                DateTime tmrw = DateTime.Now.AddDays(1);
                var chan = Channel.Load(FileSystem.ChannleChan(Name));
                chan.CreateNewSchedule(tmrw);
                if (chan.ScheduleExists(tmrw))
                {
                    var scdpath = Path.Combine(FileSystem.ChannleChan(chan.ChannelName), $"{tmrw.Month}.{tmrw.Day}.{tmrw.Year}.{FileSystem.ScheduleEXT}");
                    Schedule sch = SaveLoad<Schedule>.Load(scdpath);
                    if (sch.slots[0] == slots[^1])
                    {
                        sch.slots.RemoveAt(0);
                    }
                    slots.AddRange(sch.slots);
                }
            }
        }
        void ProcessNext()
        {           
            if(!processing & SlotsAvalible < 2 & CurrentSlot+1 < slots.Count)
            {
                ProcessVideo(slots[CurrentSlot + 1].Media, CurrentSlot + 1);
            }
        }

        async Task ProcessVideo(string filePath, int SlotNo)
        {
            if (processing) return;
            processing = true;
            Directory.CreateDirectory(liveOutputDirectory);
            filePath = "\"" + filePath + "\"";
            string ffmpegArgs;

            ffmpegArgs = $"-i {filePath} -c:v libx264 -c:a aac -strict -2 -f hls -hls_time 10 -hls_list_size 0 -hls_segment_filename {liveOutputDirectory}/(Slot{SlotNo})[{Name}]seg%d.ts -metadata title=\"{Name}\" -t {slots[SlotNo].Duration.TotalSeconds} {ManifestOutputDirectory}/index({SlotNo}).m3u8";

            await RunFFmpeg(ffmpegArgs);
            processing = false;
        }
        async Task RunFFmpeg(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = FileSystem.FFMPEG,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = Process.Start(startInfo);
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine("Processed");
        }



        async void SendManifest(HttpListenerContext Context)
        {
            HLS hLS = new();
            string des = "";
            hLS = CurrentSate;

            hLS.SetOffset(CurrentSegment.path);
            des = hLS.ToString(CurrentSegment);

            byte[] buffer = Encoding.UTF8.GetBytes(des);
            HttpListenerResponse client = Context.Response;
            client.ContentLength64 = buffer.Length;
            client.ContentType = "application/vnd.apple.mpegurl";
            await client.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            //client.OutputStream.Close();
        }
        #endregion
        async void CleanUp(int slotNum, int offset)
        {
            if(!File.Exists(Path.Combine(ManifestOutputDirectory, $"index({slotNum}).m3u8"))) return;
            
            var HLSO = HLS.Load(File.ReadAllText(Path.Combine(ManifestOutputDirectory, $"index({slotNum}).m3u8")));
            segment[] files = HLSO.Body.ToArray(); 
            for (int i = 0; i < files.Length-offset; i++)
            {
                if(File.Exists(Path.Combine(liveOutputDirectory, files[i].path))){
                    try
                    {
                        File.Delete(Path.Combine(liveOutputDirectory, files[i].path));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    } 
                    await Task.Delay(1);
                }
                
            }
            var vttfiles = Directory.GetFiles(ManifestOutputDirectory, "*.vtt");
            
            for (int i = 0; i < vttfiles.Length; i++)
            {
                File.Delete(vttfiles[i]);
            }
            if(vttfiles.Length>0)
            File.Delete(Path.Combine(ManifestOutputDirectory,$"index({slotNum})_vtt.m3u8"));
        }
        void CleanUp()
        {
           
            if (Directory.Exists(liveOutputDirectory))
            {
                Directory.Delete(ManifestOutputDirectory,true);
            }
        }
       

        public TimeSpan TimeLeftInDay
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
                TimeSpan timeLeft = endOfDay - now;
                return timeLeft;
            }
        }

        public HLSSchedule(Schedule schedule)
        {
            slots = schedule.slots;
            Name = schedule.Name;
        }
    }


}
