using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace PseudoVision
{
    internal class Program
    {

        static Dictionary<string,ISchedule> Schedules = new Dictionary<string,ISchedule>();
        static string Public_IP;
        static bool IsGening;
        static UPNP upnp => Settings.CurrentSettings.upnp;
        static async Task Main(string[] args)
        {
            
            try
            {
                Settings.CurrentSettings = SaveLoad<Settings>.Load(FileSystem.SettingsFile);
            }
            catch
            {
                Settings.CurrentSettings = Settings.Default;
            }
            string localIp = GetLocalIPAddress();
            int prt = Settings.CurrentSettings.Port;
            CreateScheds();
            var PIP = await GetExternalIpAddress();
            Public_IP = PIP.ToString();
            Task.Run(() => StartHttpServer(localIp, Settings.CurrentSettings.Port));
            Thread.Sleep(1000);
            if(Settings.CurrentSettings.useUPNP)
            {
                //
                upnp.Start(localIp, Settings.CurrentSettings.Port);
            }
            waittilnextday();
            Console.WriteLine("Server is running. Press Enter to exit...");
            Console.ReadLine();
        }
        
        static async void waittilnextday()
        {
            while (true)
            {
                var waittime = (DateTime.Today.AddDays(1) - DateTime.Now);
                await Task.Delay((int)waittime.TotalMilliseconds);
                CreateScheds();
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
            Schedules.Clear();
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
                        (live)?SaveLoad<HLSSchedule>.Load(scdpath): SaveLoad<Schedule>.Load(scdpath) :
                        SaveLoad<ShowList>.Load(scdpath);
                    sch.Name = chan.ChannelName.ToLower();
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
                        sch.StartCycle();
                        playlist = new(sch);
                    }
                    else
                    {
                        var sch = (HLSSchedule)Schedules.ElementAt(i).Value;
                        sch.StartCycle();
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

                    bool isPrivate = userip.Contains("192");
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
            }
            
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
