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
        static UPNP upnp = new UPNP();
        static async Task Main(string[] args)
        {
            
            string localIp = GetLocalIPAddress();
            int prt = 6589;
            CreateScheds();
            var PIP = await GetExternalIpAddress();
            Public_IP = PIP.ToString();
            Task.Run(() => StartHttpServer(localIp, prt));
            Thread.Sleep(1000);
            if(Settings.useUPNP)
            {
                //

            }
            upnp = UPNP.Default;
            upnp.Start(localIp, prt);
            Console.WriteLine("DLNA Server is running. Press Enter to exit...");
            waittilnextday();
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
                Channel chan = SaveLoad<Channel>.Load(Path.Combine(Channels.GetDirectories()[i].FullName, "Channel.chan"));
                chan.CreateNewSchedule(DateTime.Now);
                ISchedule sch = (chan.Channel_Type == Channel_Type.TV_Like) ? 
                    SaveLoad<Schedule>.Load(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", chan.ChannelName, $"{M}.{D}.{Y}.scd")):
                    SaveLoad<ShowList>.Load(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", chan.ChannelName, $"{M}.{D}.{Y}.scd"));
                Schedules.Add(chan.ChannelName.ToLower() , sch);
            }
            for (int i = 0; i < Schedules.Count; i++)
            {
                if (Schedules.ElementAt(i).Value.ScheduleType == Channel_Type.TV_Like)
                {
                    var sch = (Schedule)Schedules.ElementAt(i).Value;
                    sch.StartCycle();
                    Playlist playlist = new((Schedule)Schedules.ElementAt(i).Value);
                    string pth = Path.Combine(Settings.Archive_Output, "PV-Archives", Channels.GetDirectories()[i].Name);
                    Directory.CreateDirectory(pth);
                    File.WriteAllText(Path.Combine(pth, $"{M}.{D}.{Y}.m3u"), playlist.ToM3u());
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
            //Console.WriteLine($"HTTP Server started at http://{Public_IP}:{prt}/");
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
            else if (request.Url.AbsolutePath == "/media")
            {
                var Re = $@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
  <s:Body>
    <u:BrowseResponse xmlns:u=""urn:schemas-upnp-org:service:ContentDirectory:1"">
        <DIDL-Lite xmlns=""urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"" xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"">
        {result(Schedules.ElementAt(0).Key)}
    </DIDL-Lite>
      <NumberReturned>1</NumberReturned>
      <TotalMatches>1</TotalMatches>
      <UpdateID>0</UpdateID>
    </u:BrowseResponse>
  </s:Body>
</s:Envelope>
";
                byte[] buffer = Encoding.UTF8.GetBytes(Re);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/xml; charset=utf-8";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if(request.Url.AbsolutePath == "/cds.xml")
            {
                byte[] buffer = Encoding.UTF8.GetBytes(UPNP.CDS_XML);
                response.ContentLength64 = buffer.Length;
                response.ContentType = Application.Xml;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.Url.AbsolutePath.Contains("/watch/"))
            {
                
                bool isPrivate = userip.Contains("192");
                string channame = request.Url.AbsolutePath.Replace("/watch/", string.Empty);
                string ChosenIP = isPrivate switch
                {
                    false => ip,
                    _ => Public_IP
                };
                byte[] buffer = Encoding.UTF8.GetBytes(webPlayer(ChosenIP, prt, channame.ToLower()));
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            else if (request.Url.AbsolutePath.Contains("/live/"))
            {
                string channame = request.Url.AbsolutePath.Replace("/live/", string.Empty);
                Console.WriteLine($"Connecting {userip} to {channame}");
                Schedules[channame.ToLower()].SendMedia(response);
            }
            else
            {
                response.Redirect("https://cartoonnetwork.com");
            }
        } 

        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
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
       
        static string result(string chan)
        {
            
            return $@"<item id=""1"" parentID=""0"" restricted=""false"">
                        <dc:title>{chan}</dc:title>
                        <dc:creator>Unknown</dc:creator>
                        <upnp:class>object.item.videoItem.movie</upnp:class>
                        <res protocolInfo=""http-get:*:video/mp4:*"">http://192.168.0.40:8080/live/{chan}</res>
                    </item>";
        }
       
    }
}
