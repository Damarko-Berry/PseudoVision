using System.Net;
using System.Net.Sockets;
using static PVLib.ISchedule;

namespace PVLib
{
    public class ShowList : PVObject, ISchedule
    {
        public Schedule_Type ScheduleType => Schedule_Type.Binge_Like;
        public List<string> Shows = new();
        int RotationPos = 0;
        TimeSlot CurrentlyPlaying = new();
        public bool live = false;
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
        void Itterate()
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

            if (DateTime.Now > CurrentlyPlaying.EndTime)
            {
                Show show = SaveLoad<Show>.Load(Shows[RotationPos]);
                CurrentlyPlaying = new TimeSlot(show.NextEpisode());
                P.Add(CurrentlyPlaying);
                File.WriteAllText(FileSystem.Archive(Name, DateTime.Now), P.ToString());
                UPNP.Update++;
                SaveLoad<Show>.Save(show, Shows[RotationPos]);
                RotationPos++;
                if (RotationPos >= Shows.Count) RotationPos = 0;
                SaveLoad<TimeSlot>.Save(CurrentlyPlaying, LastPLayed);
            }
        }
        public async Task SendMedia(HttpListenerContext client)
        {
            Itterate();

            if (live)
            {
                var startTime = (int)(DateTime.Now - (DateTime)CurrentlyPlaying.StartTime).TotalSeconds;
                if (startTime < 30) startTime = 0;
                await SendMedia(client, startTime);
                return;
            }
            client.Response.Headers.Add("Accept-Ranges", "bytes");
            FileStream fs = new FileStream(CurrentlyPlaying.Media, FileMode.Open, FileAccess.Read);
            try
            {
                client.Response.ContentType = $"video/{info.Extension}";
                client.Response.ContentLength64 = fs.Length;

                if (client.Request.Headers["Range"] != null)
                {
                    var range = client.Request.Headers["Range"];
                    var bytesRange = range.Replace("bytes=", "").Split('-');
                    var from = long.Parse(bytesRange[0]);
                    var to = bytesRange.Length > 1 && !string.IsNullOrEmpty(bytesRange[1]) ? long.Parse(bytesRange[1]) : fs.Length - 1;

                    client.Response.StatusCode = 206;
                    client.Response.Headers.Add("Content-Range", $"bytes {from}-{to}/{fs.Length}");
                    client.Response.ContentLength64 = to - from + 1;

                    fs.Seek(from, SeekOrigin.Begin);
                    await fs.CopyToAsync(client.Response.OutputStream, (int)(to - from + 1));
                }
                else
                {
                    await fs.CopyToAsync(client.Response.OutputStream);
                }
                fs.Close();
            }
            catch (Exception ex)
            {
                ConsoleLog.writeError(ex.ToString());
            }
            finally
            {
                client.Response.Close();
            }
        }
        public async Task SendMedia(HttpListenerContext client, int startTimeSeconds = 0)
        {
            client.Response.Headers.Add("Accept-Ranges", "bytes");
            using FileStream fs = new(CurrentlyPlaying.Media, FileMode.Open, FileAccess.Read);
            try
            {
                client.Response.ContentType = $"video/{info.Extension}";
                client.Response.ContentLength64 = fs.Length;

                long startByte = 0;
                if (startTimeSeconds > 0)
                {
                    long bitrate = 500_000; // Approximate bytes per second (adjust based on actual file)
                    startByte = startTimeSeconds * bitrate;
                    startByte = Math.Min(startByte, fs.Length - 1); // Prevent seeking past the file
                }

                client.Response.StatusCode = 206;
                client.Response.Headers.Add("Content-Range", $"bytes {startByte}-{fs.Length - 1}/{fs.Length}");
                client.Response.ContentLength64 = fs.Length - startByte;

                fs.Seek(startByte, SeekOrigin.Begin);
                await fs.CopyToAsync(client.Response.OutputStream);

                fs.Close();
            }
            catch (Exception ex)
            {
                ConsoleLog.writeError(ex.ToString());
            }
            finally
            {
                client.Response.Close();
            }
        }

        public async Task SendMedia(string request, NetworkStream stream)
        {
            Itterate();
            try
            {
                string filePath = CurrentlyPlaying.Media;
                long fileLength = info.Length;

                StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

                if (request.Contains("Range"))
                {
                    string rangeHeader = request.Substring(request.IndexOf("Range"));
                    string range = rangeHeader.Split('=')[1].Split('-')[0];
                    long start = long.Parse(range);
                    long end = fileLength - 1;

                    writer.WriteLine("HTTP/1.1 206 Partial Content");
                    writer.WriteLine("Accept-Ranges: bytes");
                    writer.WriteLine($"Content-Range: bytes {start}-{end}/{fileLength}");
                    writer.WriteLine($"Content-Length: {end - start + 1}");
                    writer.WriteLine($"Content-Type: video/{info.Extension}");
                    writer.WriteLine();
                    writer.Flush();

                    using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
                    fs.Seek(start, SeekOrigin.Begin);

                    byte[] buffer = new byte[64 * 1024];
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0 && start <= end)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                        start += bytesRead;
                    }
                }
                else
                {
                    writer.WriteLine("HTTP/1.1 200 OK");
                    writer.WriteLine("Accept-Ranges: bytes");
                    writer.WriteLine($"Content-Length: {fileLength}");
                    writer.WriteLine($"Content-Type: video/{info}");
                    writer.WriteLine();
                    writer.Flush();

                    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                    await stream.WriteAsync(fileBytes, 0, fileBytes.Length);
                }
            }
            catch (Exception ex)
            {
                ConsoleLog.writeError(ex.ToString());
            }
            finally
            {
                stream.Close();
            }
        }
        public string GetContent(int s, string ip, int prt)
        {
            return $@"<item id=""{s + 1}"" parentID=""0"" restricted=""false"">
                        <dc:title>{Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem.videoItem</upnp:class>
                        <res protocolInfo=""http-get:*:video/.mp4:*"" resolution=""1920x1080"">http://{ip}:{prt}/live/{Name}.mp4</res>
                    </item>";
        }
        public async Task StartCycle()
        {
            TimeSpan FiveMin = new(0, 5, 0);
        StartUp:
            await Task.Delay(TimeLeftInDay.Subtract(FiveMin));
            DateTime tmrw = DateTime.Now.AddDays(1);
            var chan = Channel.Load(FileSystem.ChannleChan(Name));
            chan.CreateNewSchedule(tmrw);
            await Task.Delay(TimeLeftInDay);
            if (chan.ScheduleExists(tmrw))
            {
                var scdpath = Path.Combine(FileSystem.ChannleChan(chan.ChannelName), $"{tmrw.Month}.{tmrw.Day}.{tmrw.Year}.{FileSystem.ScheduleEXT}");
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
        public ShowList() { }
        public ShowList(DirectoryInfo ShowDirectory)
        {
            var files = ShowDirectory.GetFiles();
            Shuffle(files);
            for (int i = 0; i < files.Length; i++)
            {
                Shows.Add(files[i].FullName);
            }
        }

        static void Shuffle<T>(T[] array)
        {
            Random random = new Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }
    }
}
