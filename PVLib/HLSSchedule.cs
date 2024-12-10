using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PVLib
{
    public class HLSSchedule : ISchedule
    {
        public readonly List<TimeSlot> slots = new();
        public HLSSchedule() { }
        segment CurrentSegment = null;
        int CurrentSlot;
        
        
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

        public string Name { get; set; }
        string liveOutputDirectory => Path.Combine("output", Name, "segments");
        string ManifestOutputDirectory => Path.Combine("output", Name);
        HLS CurrentSate()
        {
            var Hs = new HLS();
            var manifests = Directory.GetFiles(ManifestOutputDirectory, @"index(*).m3u8");
            for (int i = 0; i < manifests.Length; i++)
            {
                Hs += HLS.Load(File.ReadAllText(manifests[i]));
            }
            return Hs;
        }
        public Schedule_Type ScheduleType => Schedule_Type.LiveStream;

        public string GetContent(int index, string ip, int port)
        {
            throw new NotImplementedException();
        }

        public async Task SendMedia(HttpListenerContext client)
        {
            if(CurrentSegment == null) return;
            if (!client.Request.Url.AbsolutePath.Contains("["))
            {
                HLS hLS= CurrentSate();
                hLS.SetOffset(CurrentSegment.path);
                var des = hLS.ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(des);
                client.Response.ContentLength64 = buffer.Length;
                client.Response.ContentType = "application/vnd.apple.mpegurl";
                await client.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                client.Response.OutputStream.Close();
                return;
            }
            var seg = client.Request.Url.AbsolutePath.Split("/");
            FileStream fs = new(Path.Combine(liveOutputDirectory, seg[^1].Replace("/", string.Empty)), FileMode.Open, FileAccess.Read);

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
        public async Task StartCycle()
        {
            CleanUp();
            await Task.Delay(200);
            var ct = DateTime.Now;
            for (CurrentSlot = 0; CurrentSlot<slots.Count; CurrentSlot++)
            {
                if (Slot.Durring(ct))
                {
                    break;
                }
            }
            await SegCyc();
        }
        #region live

        async Task SegCyc()
        {
            Again:
            ProcessVideo(Slot.Media);

            await Task.Delay(5*1000);
            DateTime StartTime = Slot.StartTime;
            var TargetOffset = DateTime.Now.Subtract(StartTime);
            var pLength = CurrentSate().Length;
            while (TargetOffset > pLength)
            {
                await Task.Delay(500);
                TargetOffset = DateTime.Now.Subtract(StartTime);
                HLS G= CurrentSate();
                pLength = G.Length;
                if (!Slot.Durring(DateTime.Now))
                {
                    StartCycle();
                    return;
                }
            }
            CurrentSegment = CurrentSate().GetSegment(DateTime.Now.Subtract(StartTime));
            
            if (timetilEnd().Seconds < 10)
            {
                CurrentSlot++;
                if (CurrentSlot < slots.Count)
                {
                    ProcessVideo(Slot.Media);
                }
            }
            Console.WriteLine($"{Name} is Ready: {CurrentSegment.path}");
            
            while(CurrentSegment != null)
            {
                await Task.Delay((int)CurrentSegment.duration.TotalMilliseconds);
                CurrentSegment = CurrentSate().NextSegment(CurrentSegment);
                if (CurrentSate().Segmentsleft(CurrentSegment) == 16 & timetilEnd().Minutes<1)
                {
                    CurrentSlot++;
                    if (CurrentSlot < slots.Count)
                    {
                        ProcessVideo(Slot.Media);
                    }
                }
                if (CurrentSegment.path.Contains("seg0"))
                {
                    CleanUp(CurrentSlot - 1);
                }
                Console.WriteLine("Segments left: "+CurrentSate().Segmentsleft(CurrentSegment));
            }
            if ((CurrentSlot + 1) < slots.Count)
            {
                StartCycle();
            }
            TimeSpan timetilEnd()
            {
                return Slot.EndTime - DateTime.Now;
            }
        }

        async Task ProcessVideo(string filePath)
        {
            Directory.CreateDirectory(liveOutputDirectory);
            filePath = "\"" + filePath + "\"";
            string ffmpegArgs = $"-i {filePath} -c:v libx264 -c:a aac -strict -2 -f hls -hls_time 10 -hls_list_size 0 -hls_segment_filename {liveOutputDirectory}/(Slot{CurrentSlot})[{Name}]seg%d.ts {ManifestOutputDirectory}/index({CurrentSlot}).m3u8";
            await RunFFmpeg(ffmpegArgs);
        }
        async Task RunFFmpeg(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = @"ffmpeg\ffmpeg.exe",
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

        #endregion
        void CleanUp(int slotNum)
        {
            try
            {
                File.Delete(Path.Combine(ManifestOutputDirectory, $"index({slotNum}).m3u8"));
            }
            catch
            {

            }
            string[] files = Directory.GetFiles(liveOutputDirectory);
            foreach (var item in files)
            {
                if (item.Contains($"(Slot{slotNum})"))
                {
                    try
                    {

                        File.Delete(item);
                    }catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                }
            }
            
        }
        void CleanUp()
        {
            TerminateProcess("ffmpeg");
            if (Directory.Exists(liveOutputDirectory))
            {
                Directory.Delete(ManifestOutputDirectory,true);
            }
        }
       
        void TerminateProcess(string processName)
        {
            
            Process[] processes = Process.GetProcessesByName(processName);

            
            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                        Console.WriteLine($"Terminated process {processName} with PID {process.Id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error terminating process {processName}: {ex.Message}");
                    }
                }
            }
            
        }
        public HLSSchedule(Schedule schedule)
        {
            slots = schedule.slots;
            Name = schedule.Name;
        }
    }


}
