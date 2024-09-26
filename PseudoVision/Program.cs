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
        static UPNP upnp => Settings.CurrentSettings.upnp;
        static async Task Main(string[] args)
        {
            
            try
            {
                Settings.CurrentSettings = SaveLoad<Settings>.Load(FileSystem.Settings);
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

        public static void CreateScheds()
        {
            DirectoryInfo Channels = new(FileSystem.Channels);
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
                if (chan.shows.Length > 0)
                {
                    ISchedule sch = (chan.channel_Type == Channel_Type.TV_Like) ?
                        SaveLoad<Schedule>.Load(Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}")) :
                        SaveLoad<ShowList>.Load(Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}"));
                    sch.Name = chan.ChannelName.ToLower();
                    Schedules.Add(chan.ChannelName.ToLower(), sch);
                }
            }
            for (int i = 0; i < Schedules.Count; i++)
            {
                if (Schedules.ElementAt(i).Value.ScheduleType == Channel_Type.TV_Like)
                {
                    var sch = (Schedule)Schedules.ElementAt(i).Value;
                    sch.StartCycle();
                    Playlist playlist = new((Schedule)Schedules.ElementAt(i).Value, Settings.CurrentSettings.playlistFormat);
                    string pth = Path.Combine(Settings.CurrentSettings.Archive_Output, "PV-Archives", Channels.GetDirectories()[i].Name);
                    Directory.CreateDirectory(pth);
                    File.WriteAllText(Path.Combine(pth, $"{M}.{D}.{Y}.{Settings.CurrentSettings.playlistFormat}"), playlist.ToString());
                }
                Console.WriteLine(Schedules.ElementAt(i).Key);
            }
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

            if (request.Url.AbsolutePath == "/description.xml")
            {
                var des = upnp.ToString();
                byte[] buffer = Encoding.UTF8.GetBytes(des);
                response.ContentLength64 = buffer.Length;
                response.ContentType = Text.Xml;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.Url.AbsolutePath.Contains("/live/"))
            {
                string channame = request.Url.AbsolutePath.Replace("/live/", string.Empty);
                Console.WriteLine($"Connecting {userip} to {channame}");
                await Schedules[channame.ToLower()].SendMedia(response);
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
