using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static PVLib.ISchedule;

namespace PVLib
{
    internal class Procedural : PVObject ,ISchedule
    {
        public string Name { get; set; }
        TimeSlot CurrentlyPlaying;
        FileInfo info => new FileInfo(CurrentlyPlaying.Media);
        public string LastPLayed => Path.Combine(FileSystem.ChanSchedules(Name), "Last Played", $"LastPLayed.lsp");
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

            if (DateTime.Now > CurrentlyPlaying.EndTime)
            {
                var channel = SaveLoad<Channel>.Load(FileSystem.ChannleChan(Name));
                CurrentlyPlaying = channel.CreateSlot(DateTime.Now);
                var P = GetPlaylist;
                P.Add(CurrentlyPlaying);
                File.WriteAllText(FileSystem.Archive(Name, DateTime.Now), P.ToString());
                UPNP.Update++;
                SaveLoad<TimeSlot>.Save(CurrentlyPlaying, LastPLayed);
                SaveLoad<Channel>.Save(channel, FileSystem.ChannleChan(Name));
            }
        }
        public async Task StartCycle()
        {
            TimeSpan FiveMin = new(0, 5, 0);
            Channel cn = SaveLoad<Channel>.Load(FileSystem.ChannleChan(Name));
            while (cn.CTD.Length>0) {
                await Task.Delay(TimeLeftInDay.Subtract(FiveMin));
                cn = SaveLoad<Channel>.Load(FileSystem.ChannleChan(Name));
            }
            AllSchedules.Remove(Name);
        }

        public async Task SendMedia(HttpListenerContext client)
        {
            Itterate();

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

        public Schedule_Type ScheduleType =>  Schedule_Type.PerRequest;

        public string GetContent(int index, string ip, int port)
        {
            throw new NotImplementedException();
        }

        public Procedural(string name)
        {
            Name = name;
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
    }
}
