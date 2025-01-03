using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static System.Reflection.Metadata.BlobBuilder;
using System.Diagnostics;

namespace PVLib
{
    public class BingeHLS : ISchedule
    {
        public Schedule_Type ScheduleType => Schedule_Type.Binge_Like;
        public List<string> Shows = new();
        TimeSlot CurrentlyPlaying = new();
        int CurrentSlot = 0;
        HLS CurrentSate = null;
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
        [XmlIgnore]
        public Dictionary<string, ISchedule> AllSchedules { get; set; }
        public void selectMedia()
        {
            if (File.Exists(LastPLayed))
            {
                CurrentlyPlaying = SaveLoad<TimeSlot>.Load(LastPLayed);
            }
            else
            {
                UPNP.Update++;
                Directory.CreateDirectory(Path.Combine(FileSystem.ChanSchedules(Name), "Last Played"));
            }
            var P = GetPlaylist;
            Random rnd = new Random();
            int shw = rnd.Next(Shows.Count);
            if (DateTime.Now.AddMinutes(1.3) > CurrentlyPlaying.EndTime)
            {
                Show show = SaveLoad<Show>.Load(Shows[shw]);
                CurrentlyPlaying = new TimeSlot(show.NextEpisode());
                P.Add(CurrentlyPlaying);
                File.WriteAllText(FileSystem.Archive(Name, DateTime.Now), P.ToString());
                UPNP.Update++;
                SaveLoad<Show>.Save(show, Shows[shw]);
            }
            SaveLoad<TimeSlot>.Save(CurrentlyPlaying, LastPLayed);
        }
        CancellationTokenSource cts = new();
        public async Task SendMedia(HttpListenerContext client)
        {
            if (CurrentSegment == null)
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
                fs.Close();
                Console.WriteLine(ex.ToString());
            }
            client.Response.Close();
            fs.Close();
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
            client.OutputStream.Close();
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

        public string GetContent(int s, string ip, int prt)
        {
            return $@"<item id=""{s}"" parentID=""0"" restricted=""false"">
                        <dc:title>{Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem.videoItem</upnp:class>
                        <res protocolInfo=""http-get:*:video/{info.Extension}:*"" resolution=""1920x1080"">http://{ip}:{prt}/live/{Name}</res>
                    </item>";
        }
        public async Task StartCycle()
        {
            TimeSpan FiveMin = new(0, 5, 0);
            CleanUp();
            selectMedia();
            SegCyc();
        StartUp:
            await Task.Delay(TimeLeftInDay.Subtract(FiveMin));
            DateTime tmrw = DateTime.Now.Date.AddDays(1);
            var chan = Channel.Load(FileSystem.ChanSchedules(Name));
            chan.CreateNewSchedule(tmrw);
            await Task.Delay(TimeLeftInDay);
            if (chan.ScheduleExists(tmrw))
            {
                var scdpath = Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{tmrw.Month}.{tmrw.Day}.{tmrw.Year}.{FileSystem.ScheduleEXT}");
                Shows = SaveLoad<ShowList>.Load(scdpath).Shows;
                goto StartUp;
            }
            AllSchedules.Remove(Name);
        }
        TimeSpan TimeLeftInDay
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
                TimeSpan timeLeft = endOfDay - now;
                return timeLeft;
            }
        }


        #region live
        int SlotsAvalible
        {
            get
            {
                int G = 0;
                var HL = Directory.GetFiles(ManifestOutputDirectory, "index(*).m3u8");
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
        segment CurrentSegment = null;
        bool processing = false;

        public BingeHLS(ShowList sch)
        {
            Shows.AddRange(sch.Shows);
            Name = sch.Name;
        }

        string liveOutputDirectory => Path.Combine("output", Name, "segments");
        string ManifestOutputDirectory => Path.Combine("output", Name);


        async void UpdateCurrentState(CancellationToken Toolong)
        {
            CurrentSate = new HLS();
            while (AllSchedules.ContainsKey(Name) & !Toolong.IsCancellationRequested)
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
        bool clean = false;
        async Task SegCyc()
        {
            ProcessVideo(CurrentlyPlaying.Media, CurrentSlot);
            
            Oops:
            UpdateCurrentState(cts.Token);
            while (CurrentSate.Length.TotalMinutes < 1)
            {
                await Task.Delay(500);
            }
            CurrentSegment = CurrentSate.Body[0];
            ProcessNext();

        
            while (CurrentSegment != null & AllSchedules.ContainsKey(Name))
            {
                Console.WriteLine("current Segment: " + CurrentSegment.path);
                await Task.Delay((int)CurrentSegment.duration.TotalMilliseconds);

                CleanUp(CurrentSlot - 1, 0);
                ProcessNext();
                
                CurrentSegment = CurrentSate.NextSegment(CurrentSegment);

            }
            if (processing) goto Oops;
            
            cts.Cancel();
        }

        
        void ProcessNext()
        {
            var timeleftinslot = CurrentlyPlaying.EndTime - DateTime.Now;
            if (!processing & SlotsAvalible < 2 & timeleftinslot.TotalMinutes<1)
            {
                Console.WriteLine("Processing Next");
                CurrentSlot++;
                selectMedia();
                ProcessVideo(CurrentlyPlaying.Media, CurrentSlot);
            }
            else
            {
                Console.WriteLine("\n\nProssesing :"+processing);
                Console.WriteLine("Slots Avalivble :"+SlotsAvalible);
                Console.WriteLine("Clean :"+clean);
                Console.WriteLine("Time Left :"+timeleftinslot+"\n\n");
            }
        }

        async Task ProcessVideo(string filePath, int SlotNo)
        {
            if (processing) return;
            processing = true;
            Directory.CreateDirectory(liveOutputDirectory);
            var cods = ISchedule.GetSubtitleStreams(filePath);
            for (int i = 0; i < cods.Count; i++)
            {
                Console.WriteLine(cods[i].Index);
                Console.WriteLine(cods[i].CodecName);
                Console.WriteLine(cods[i].Language);

            }
            string subtitleCodec = null;

            filePath = "\"" + filePath + "\"";
            try
            {

                subtitleCodec = cods[0].CodecName;
            }
            catch 
            {
                Console.WriteLine("No Subs");
            }
            string subtitleOption = subtitleCodec != null ? $"-c:s {subtitleCodec}" : string.Empty;

            string ffmpegArgs = $"-i {filePath} -c:v libx264 -c:a aac -strict -2 -f hls -hls_time 10 -hls_list_size 0 -hls_segment_filename {liveOutputDirectory}/(Slot{SlotNo})[{Name}]seg%d.ts -metadata title=\"{Name}\" {ManifestOutputDirectory}/index({SlotNo}).m3u8";

            Console.WriteLine(ffmpegArgs);
            await RunFFmpeg(ffmpegArgs);
            clean = false;
            processing = false;
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
        async void CleanUp(int slotNum, int offset)
        {
            if (!File.Exists(Path.Combine(ManifestOutputDirectory, $"index({slotNum}).m3u8"))| DateTime.Now.Subtract(new TimeSpan(0,0,30)) > (DateTime)CurrentlyPlaying.StartTime |clean) return;
            try
            {
                clean = true;
                Console.WriteLine("Cleaning Up");
                var HLSO = HLS.Load(File.ReadAllText(Path.Combine(ManifestOutputDirectory, $"index({slotNum}).m3u8")));
                segment[] files = HLSO.Body.ToArray();
                for (int i = 0; i < files.Length - offset; i++)
                {
                    if (File.Exists(Path.Combine(liveOutputDirectory, files[i].path)))
                    {
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
                if (vttfiles.Length > 0)
                    File.Delete(Path.Combine(ManifestOutputDirectory, $"index({slotNum})_vtt.m3u8"));
                Console.WriteLine("Clean");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        void CleanUp()
        {

            if (Directory.Exists(liveOutputDirectory))
            {
                Directory.Delete(ManifestOutputDirectory, true);
            }
            clean = true;
        }
    }
}
