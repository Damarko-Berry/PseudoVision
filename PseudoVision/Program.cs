using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace PseudoVision
{
    internal class Program
    {
        //static Schedule schedule = null;
        static Dictionary<string,ISchedule> Schedules = new Dictionary<string,ISchedule>();
        static string Public_IP;
        static Settings Settings = new Settings();
        static UPNP upnp => Settings.upnp;
        static async Task Main(string[] args)
        {
            
            try
            {
                Settings = SaveLoad<Settings>.Load("settings");
            }
            catch
            {
                Settings = Settings.Default;
            }
            string localIp = GetLocalIPAddress();
            int prt = Settings.Port;
            CreateScheds();
            var PIP = await GetExternalIpAddress();
            Public_IP = PIP.ToString();
            Task.Run(() => StartHttpServer(localIp, Settings.Port));
            Thread.Sleep(1000);
            if(Settings.useUPNP)
            {
                //
                upnp.Start(localIp, Settings.Port);

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
            DirectoryInfo Channels = new(Path.Combine(Directory.GetCurrentDirectory(), "Channels"));
            var Da = DateTime.Now; 
            var M = Da.Date.Month;
            var D = Da.Date.Day;
            var Y = Da.Date.Year;
            Schedules.Clear();
            for (int i = 0; i < Channels.GetDirectories().Length; i++)
            {
                Channel chan = Channel.Load(Path.Combine(Channels.GetDirectories()[i].FullName, "Channel.chan"));
                chan.CreateNewSchedule(DateTime.Now);
                if (chan.shows.Length > 0)
                {
                    ISchedule sch = (chan.channel_Type == Channel_Type.TV_Like) ?
                        SaveLoad<Schedule>.Load(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", chan.ChannelName, $"{M}.{D}.{Y}.scd")) :
                        SaveLoad<ShowList>.Load(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", chan.ChannelName, $"{M}.{D}.{Y}.scd"));
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
                    Playlist playlist = new((Schedule)Schedules.ElementAt(i).Value, Settings.playlistFormat);
                    string pth = Path.Combine(Settings.Archive_Output, "PV-Archives", Channels.GetDirectories()[i].Name);
                    Directory.CreateDirectory(pth);
                    File.WriteAllText(Path.Combine(pth, $"{M}.{D}.{Y}.{Settings.playlistFormat}"), playlist.ToString());
                    Console.WriteLine(Schedules.ElementAt(i).Key);
                }
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
                response.ContentType = "text/xml";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.Url.AbsolutePath == "/CreateNewUser")
            {

            }
            else if (request.Url.AbsolutePath == "NewPass")
            {

            }
            else if (request.Url.AbsolutePath.Contains("/live/"))
            {
                string channame = request.Url.AbsolutePath.Replace("/live/", string.Empty);
                Console.WriteLine($"Connecting {userip} to {channame}");
                Schedules[channame.ToLower()].SendMedia(response);
            }
            else if(UserAuthenticator.Auth(request,Settings.securityLevel))
            {    
                if (request.Url.AbsolutePath == "/media")
                {
                    var sc = Schedules.Values.ToArray();
                    var Re = upnp.Media(sc, ip, prt);
                    byte[] buffer = Encoding.UTF8.GetBytes(Re);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/xml";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else if (request.Url.AbsolutePath.Contains("/watch/"))
                {

                    bool isPrivate = userip.Contains("192");
                    string channame = request.Url.AbsolutePath.Replace("/watch/", string.Empty);
                    string ChosenIP = isPrivate switch
                    {
                        true => ip,
                        _ => Public_IP
                    };
                    byte[] buffer = Encoding.UTF8.GetBytes(webPlayer(ChosenIP, prt, channame.ToLower()));
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = "text/html";
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
            }
            else
            {
                    response.Redirect("https://cartoonnetwork.com");
            } 
        } 

        static string GetLocalIPAddress()
        {
            if(Settings.IP != string.Empty)
            {
                return Settings.IP;
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
        
        static string webPlayer(string localip, int port, string ch)
        {
            return @$"
<html>
    <style>
        #myVideo {{
             transform: translate(-50%, -50%);
             position: absolute;
             top: 50%;
             left: 50%;
             min-width: 100%;
             max-height: 100%;
             color: black
        }}
    </style>
    <body style=""background-color: black"">
        <video id=""myVideo"" src = ""http://{localip}:{port}/live/{ch}"" controls autoplay type=""video/mp4"">
            Your browser does not support the video tag.
        </video>
    </body>
    <script>
        var video = document.getElementsByTagName('video')[0];
        video.onended = function (e) {{
            start();
        }}
        async function start() {{
        video.src = ""http://{localip}:{port}/live/{ch}"";
            await pause(10);
            video.play();
            console.log(""PLAYING"");
        }}

        async function pause(seconds) {{
            return new Promise(resolve => setTimeout(resolve, seconds * 1000));
        }}
        
    
    </script>
</html>";
        }
        
        static void creatIndex(string ip, int port)
        {
            string[] links;
            for (int i = 0; i < Schedules.Count; i++)
            {
                
            }
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
