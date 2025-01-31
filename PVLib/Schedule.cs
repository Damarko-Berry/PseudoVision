using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;
using static PVLib.ISchedule;


namespace PVLib
{
    public class Schedule : PVObject, ISchedule
    {
        public List<TimeSlot> slots = new();
        
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

        public async Task SendMedia(HttpListenerContext client)
        {
            client.Response.Headers.Add("Accept-Ranges", "bytes");
            using FileStream fs = new(Slot.Media, FileMode.Open, FileAccess.Read);
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
            try
            {
                string filePath = Slot.Media;
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

        public async Task StartCycle()
        {
           
            while (!Slot.Durring(DateTime.Now))
            {
                itterate();
            }

            var timeleft = (Slot.EndTime - DateTime.Now);
            UPNP.Update++;
            await Task.Delay(timeleft);
            
            while (CurrentSlot < slots.Count)
            {
                itterate();
                await Task.Delay(Slot.Duration);
            }
            Random random = new((int)DateTime.Now.Ticks);
            CurrentSlot = random.Next(0, slots.Count);
            AllSchedules.Remove(Name);
        }
        void itterate()
        {
            CurrentSlot++;
            UPNP.Update++;
            try
            {
                var NS = CurrentSlot + 1;
                if (NS >= slots.Count)
                {
                    DateTime tmrw = DateTime.Now.AddDays(1);
                    var chan = Channel.Load(FileSystem.ChannleChan(Name));
                    chan.CreateNewSchedule(tmrw);
                    if (chan.ScheduleExists(tmrw))
                    {
                        var scdpath = Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{tmrw.Month}.{tmrw.Day}.{tmrw.Year}.{FileSystem.ScheduleEXT}");
                        Schedule sch = SaveLoad<Schedule>.Load(scdpath);
                        if (sch.slots[0] == slots[^1])
                        {
                            sch.slots.RemoveAt(0);
                        }
                        slots.AddRange(sch.slots);
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLog.writeError(ex.ToString());
            }
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Age.TotalDays > 1)
                {
                    slots.RemoveAt(i);
                    CurrentSlot--;
                    i--;
                }
            }
        }
        public string GetContent(int index, string ip, int prt)
        {
            return $@"<item id=""{index+1}"" parentID=""0"" restricted=""false"">
                        <dc:title>{Name}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem</upnp:class>
                        <res protocolInfo=""http-get:*:video/{info.Extension}:*"" resolution=""1920x1080"">http://{ip}:{prt}/live/{Name}{info.Extension}</res>
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
