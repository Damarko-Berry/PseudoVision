using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static PVLib.Settings;

namespace PseudoVision
{
    internal class Program
    {

        static Dictionary<string,ISchedule> Schedules = new Dictionary<string,ISchedule>();
        static string Public_IP;
        static bool IsGening;
        static UPNP upnp => CurrentSettings.upnp;
        static async Task Main(string[] args)
        {
            TerminateProcess("ffmpeg");
            try
            {
                CurrentSettings = SaveLoad<Settings>.Load(FileSystem.SettingsFile);
            }
            catch
            {
                CurrentSettings = Settings.Default;
            }
            string localIp = GetLocalIPAddress();
            int prt = CurrentSettings.Port;
            CreateScheds();
            var PIP = await GetExternalIpAddress();
            Public_IP = PIP.ToString();
            Task.Run(() => StartHttpServer(localIp, prt));
            Thread.Sleep(1000);
            if(CurrentSettings.useUPNP)
            {
                //
                upnp.Start(localIp, prt);
            }
            waittilnextday();
            Console.WriteLine("Server is running. Press Enter to exit...");
            Console.ReadLine();
        }
        
        static async Task waittilnextday()
        {
            while (Schedules.Count>0)
            {
                var waittime = (DateTime.Today.AddDays(1) - DateTime.Now);
                await Task.Delay((int)waittime.TotalMilliseconds);
            }
        }

        static void CreateScheds()
        {
            DirectoryInfo Channels = new(FileSystem.Channels);
            IsGening = true;
            var Da = DateTime.Now; 
            var M = Da.Date.Month;
            var D = Da.Date.Day;
            var Y = Da.Date.Year;
            var CDs = Channels.GetDirectories();
            for (int i = 0; i < CDs.Length; i++)
            {
                Channel chan = Channel.Load(FileSystem.ChannleChan(CDs[i].Name));
                chan.CreateNewSchedule(DateTime.Now);
                if (chan.CTD.Length > 0)
                {
                    var scdpath = Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}");
                    bool live= false;
                    if(chan.channel_Type == Channel_Type.TV_Like)
                    {
                        var TV = (TV_LikeChannel)chan;
                        live = TV.Live;
                    }
                    ISchedule sch = (chan.channel_Type == Channel_Type.TV_Like | chan.channel_Type == Channel_Type.Movies) ?
                       SaveLoad<Schedule>.Load(scdpath) : SaveLoad<ShowList>.Load(scdpath);
                    sch.Name = chan.ChannelName.ToLower();
                    if (live)
                    {
                        HLSSchedule schedule = (Schedule)sch;
                        sch = schedule;
                    }
                    sch.AllSchedules = Schedules;
                    sch.StartCycle();
                    Schedules.Add(chan.ChannelName.ToLower(), sch);
                }
            }
            for (int i = 0; i < Schedules.Count; i++)
            {
                
                if (Schedules.ElementAt(i).Value.ScheduleType == Schedule_Type.TV_Like | Schedules.ElementAt(i).Value.ScheduleType == Schedule_Type.LiveStream)
                {
                    Playlist playlist = null;
                    if (Schedules.ElementAt(i).Value.ScheduleType == Schedule_Type.TV_Like)
                    {
                        var sch = (Schedule)Schedules.ElementAt(i).Value;
                        playlist = new(sch);
                    }
                    else
                    {
                        var sch = (HLSSchedule)Schedules.ElementAt(i).Value;
                        playlist = new(sch);
                    }
                    string pth = FileSystem.ArchiveDirectory(Channels.GetDirectories()[i].Name);
                    Directory.CreateDirectory(pth);
                    File.WriteAllText(FileSystem.Archive(Channels.GetDirectories()[i].Name, DateTime.Now), playlist.ToString());
                }
                Console.WriteLine(Schedules.ElementAt(i).Key);
            }
            IsGening = false;
        }

        static async Task StartHttpServer(string localIp, int port)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://{localIp}:{port}/");
            listener.Start();
            Console.WriteLine($"HTTP Server started at http://{localIp}:{port}/");
            
            while (true)
            {
                HandleClient(await listener.GetContextAsync(), localIp, port);
            }
        }
        
        static async Task HandleClient(HttpListenerContext context, string ip, int prt)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            var userip = context.Request.RemoteEndPoint.Address.ToString();
            bool isPrivate = userip.StartsWith("192")| userip.StartsWith("172");
            Console.WriteLine(request.Url.AbsolutePath);
            while(IsGening)
            {
                await Task.Delay(500);
            }
            if (request.Url.AbsolutePath == "/description.xml")
            {
                var des = upnp.ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(des);
                response.ContentLength64 = buffer.Length;
                response.ContentType = Text.Xml;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.Url.AbsolutePath.Contains("["))
            {
                string channame = request.Url.AbsolutePath.Replace("/live/", string.Empty);
                channame = request.Url.AbsolutePath.Split('[')[1].Split(']')[0].Replace("]",string.Empty).Trim();
                var Sched = Schedules[channame.ToLower()];
                await Sched.SendMedia(context);
            }
            else if (request.Url.AbsolutePath.Contains("/live/"))
            {
                string channame = request.Url.AbsolutePath.Replace("/live/", string.Empty);
                Console.WriteLine($"Connecting {userip} to {channame}");
                var Sched = Schedules[channame.ToLower()];
                
                await Sched.SendMedia(context);
            }
            else if (request.Url.AbsolutePath.Contains("/archive/"))
            {
                string[] URL = request.Url.AbsolutePath.Replace("/archive/", string.Empty).Split("/");
                Time time = new()
                {
                    Day = int.Parse(URL[2].Replace("/", string.Empty)),
                    Month = int.Parse(URL[1].Replace("/", string.Empty)),
                    Year = int.Parse(URL[3].Replace("/", string.Empty)),
                };
                if (!File.Exists(FileSystem.Archive(URL[0].Replace("/", string.Empty), time))) return;
                ClientPP pP = null;
                var ppname = ClientPP.potentialfilename(context, URL[0].Replace("/",string.Empty), time);
                var PPfile = Path.Combine(FileSystem.Schedules, "PP", ppname);
                if (File.Exists(PPfile))
                {
                    pP = SaveLoad<ClientPP>.Load(PPfile);
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(FileSystem.Schedules, "PP"));
                    pP = new(context, URL[0].Replace("/", string.Empty), URL[1].Replace("/", string.Empty), URL[2].Replace("/", string.Empty), URL[3].Replace("/", string.Empty));
                }
                Console.WriteLine($"Connecting {context.Request.UserAgent} to {ppname}");
                pP.SendMedia(response);
            }
            else if (UserAuthenticator.Auth(request,Settings.CurrentSettings.securityLevel))
            {    
                if (request.Url.AbsolutePath == "/media")
                {
                    var sc = Schedules.Values.ToArray();
                    var Re = upnp.Media(sc, ip, prt);
                    byte[] buffer = Encoding.UTF8.GetBytes(Re);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = Text.Xml;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else if (request.Url.AbsolutePath.Contains("/watch"))
                {

                    
                    string ChosenIP = isPrivate switch
                    {
                        true => ip,
                        _ => Public_IP
                    };
                    byte[] buffer = Encoding.UTF8.GetBytes(webPlayer(ChosenIP, prt));
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = Text.Html;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else if (request.Url.AbsolutePath.Contains("/stuff")| request.Url.AbsolutePath.Contains("/cds.xml"))
                {
                    
                    var stuff = CDS(Schedules.Values.ToArray(), isPrivate);
                        
                    byte[] buffer = Encoding.UTF8.GetBytes(stuff);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = Text.Xml;

                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.Close();
                    Console.WriteLine("Sent CDS");
                }
            }
            
        } 
        static string CDS(ISchedule[] schedules, bool isprivate)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<DIDL-Lite xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"" xmlns=""urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"">");
            sb.AppendLine($@"<container id=""0"" parentID=""-1"" restricted=""false"" updateID=""{UPNP.Update}"">");
            sb.AppendLine($@"<dc:title>Media Server</dc:title>");
            sb.AppendLine($@"<dc:creator>{upnp.DeviceName}</dc:creator>");
            sb.AppendLine($@"<upnp:class>object.container</upnp:class>");
            for (int i = 0; i < schedules.Length; i++)
            {
                sb.AppendLine(schedules[i].GetContent(i, (isprivate)?CurrentSettings.IP:Public_IP, CurrentSettings.Port));
            }

            sb.AppendLine($@"</container>");
            sb.AppendLine("</DIDL-Lite>");
            return sb.ToString();
        }
        static string GetLocalIPAddress()
        {
            if(Settings.CurrentSettings.IP != string.Empty)
            {
                return Settings.CurrentSettings.IP;
            }
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork & ip.ToString().Contains("192.168"))
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        static void TerminateProcess(string processName)
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
        static IPAddress GetIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        
        static string webPlayer(string localip, int port)
        {
            var idx = (Path.Combine(Directory.GetCurrentDirectory(), "Index.html"));
            bool Indexu = File.Exists(idx);

            var WP = File.ReadAllText("Index.html");
            var arr = "[";
            for (int i = 0; i < Schedules.Count; i++)
            {
                var lnk = $"http://{localip}:{port}/live/{Schedules.ElementAt(i).Key}";
                arr += $@"""{lnk}"",";
            }
            arr += "];";
            return WP.Replace("REPLACEME", arr).Replace("REPLCACESRC", $"http://{localip}:{port}/live/{Schedules.ElementAt(0).Key}");
        }
        
        static async Task<IPAddress?> GetExternalIpAddress()
        {
            var externalIpString = (await new HttpClient().GetStringAsync("http://icanhazip.com"))
                .Replace("\r\n", "").Replace("\n", "").Trim();
            if (!IPAddress.TryParse(externalIpString, out var ipAddress))
                return null;
            return ipAddress;
        }
       
       
    }
}
