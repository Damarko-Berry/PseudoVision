using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace PVLib
{
    public class HLSSchedule : ISchedule
    {
        public readonly List<TimeSlot> slots = new();
        public HLSSchedule() { }
        segment CurrentSegment = new();
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
            var manifests = Directory.GetFiles(ManifestOutputDirectory,"*.m3u8");
            for (int i = 0; i < manifests.Length; i++)
            {
                Hs += HLS.Load(File.ReadAllText(manifests[i]));
            }
            Hs.SetOffset(CurrentSegment.path);
            return Hs;
        }
        public Channel_Type ScheduleType => Channel_Type.TV_Like;

        public string GetContent(int index, string ip, int port)
        {
            throw new NotImplementedException();
        }

        public async Task SendMedia(HttpListenerContext client)
        {
            if (!client.Request.Url.AbsolutePath.Contains("["))
            {
                var des = CurrentSate().ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(des);
                client.Response.ContentLength64 = buffer.Length;
                client.Response.ContentType = "application/vnd.apple.mpegurl";
                await client.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                client.Response.OutputStream.Close();
                return;
            }
            var seg = client.Request.Url.AbsolutePath.Split('/');
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

        #region live

        async Task SegCyc(TimeSlot slot)
        {
            ProcessVideo(slot.Media);
        }
        async Task ProcessVideo(string filePath)
        {

            string playlist = Path.Combine("output", Name);
            Directory.CreateDirectory(liveOutputDirectory);
            filePath = "\"" + filePath + "\"";
            string ffmpegArgs = $"-i {filePath} -c:v libx264 -c:a aac -strict -2 -f hls -hls_time 4 -hls_list_size 0 -hls_segment_filename {liveOutputDirectory}/(Slot{CurrentSlot})[{Name}]seg%d.ts {playlist}/index({CurrentSlot}).m3u8";
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
}
