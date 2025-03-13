using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using static PVLib.Settings;
using static PVLib.ISchedule;

namespace PseudoVision
{
    public class Program : PVObject
    {

        static string Public_IP;
        static bool IsGening;
        static UPNP upnp;
        static async Task Main(string[] args)
        {

            MainLog.Cycle(Environment.MachineName);
            TerminateProcess("ffmpeg");
            try
            {
                CurrentSettings = SaveLoad<Settings>.Load(FileSystem.SettingsFile);
            }
            catch
            {
                CurrentSettings = Settings.Default;
                Console.WriteLine("Settings File Not Found. Using Default Settings");
            }
            upnp = CurrentSettings.upnp;
            string localIp = GetLocalIPAddress();
            int prt = CurrentSettings.Port;
            CreateScheds();
            try
            {
                var PIP = await GetExternalIpAddress();
                Public_IP = PIP.ToString();
            }
            catch
            {

            }
            Task.Run(() => StartHttpServer(localIp, prt));
            Thread.Sleep(1000);
            if(CurrentSettings.useUPNP)
            {
                //
                upnp.Start(localIp, prt);
            }
            MainLog.writeMessage("Server is running.");
            await waittilnextday();
        }
        
        static async Task waittilnextday()
        {
            while (AllSchedules.Count>0)
            {
                var waittime = (DateTime.Today.AddDays(1) - DateTime.Now);
                await Task.Delay((int)waittime.TotalMilliseconds);
                CreateScheds();
            }
        }

        static void CreateScheds()
        {
            MainLog.Cycle(Environment.MachineName);
            DirectoryInfo Channels = new(FileSystem.Channels);
            IsGening = true;
            var Da = DateTime.Now; 
            var M = Da.Date.Month;
            var D = Da.Date.Day;
            var Y = Da.Date.Year;
            var CDs = Channels.GetDirectories();
            for (int i = 0; i < CDs.Length; i++)
            {
                if (AllSchedules.ContainsKey(CDs[i].Name)) continue;
                Channel chan = Channel.Load(FileSystem.ChannleChan(CDs[i].Name));
                chan.CreateNewSchedule(DateTime.Now);
                if (chan.CTD.Length > 0)
                {
                    var scdpath = Path.Combine(FileSystem.ChanSchedules(chan.ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}");
                    ISchedule sch = (chan.channel_Type == Channel_Type.TV_Like | chan.channel_Type == Channel_Type.Movies) ?
                       SaveLoad<Schedule>.Load(scdpath) : SaveLoad<ShowList>.Load(scdpath);
                    sch.Name = chan.ChannelName.ToLower();
                    if (chan.Live & CurrentSettings.liveProtocol == LiveProtocol.HLS)
                    {
                        if (chan.channel_Type == Channel_Type.TV_Like)
                        {
                            HLSSchedule schedule = (Schedule)sch;
                            sch = schedule;
                        }
                        else
                        {
                            BingeHLS schedule = new((ShowList)sch);
                            sch = schedule;
                        }
                    }
                    AllSchedules.Add(chan.ChannelName.ToLower(), sch);
                    sch.StartCycle();
                }
            }
            for (int i = 0; i < AllSchedules.Count; i++)
            {
                
                if (AllSchedules.ElementAt(i).Value.ScheduleType == Schedule_Type.TV_Like | AllSchedules.ElementAt(i).Value.ScheduleType == Schedule_Type.LiveStream)
                {
                    Playlist playlist = null;
                    if (AllSchedules.ElementAt(i).Value.ScheduleType == Schedule_Type.TV_Like)
                    {
                        var sch = (Schedule)AllSchedules.ElementAt(i).Value;
                        playlist = new(sch);
                    }
                    else
                    {
                        var sch = (HLSSchedule)AllSchedules.ElementAt(i).Value;
                        playlist = new(sch);
                    }
                    string pth = FileSystem.ArchiveDirectory(Channels.GetDirectories()[i].Name);
                    Directory.CreateDirectory(pth);
                    File.WriteAllText(FileSystem.Archive(Channels.GetDirectories()[i].Name, DateTime.Now), playlist.ToString());
                }
                MainLog.writeMessage(AllSchedules.ElementAt(i).Key);
            }
            IsGening = false;
        }

        static async Task StartHttpServer(string localIp, int port)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://{localIp}:{port}/");
            listener.Start();
            Console.WriteLine($"http://{localIp}:{port}/");
            
            while (true)
            {
                HandleClient(await listener.GetContextAsync(), localIp, port);
                MainLog.Cycle(Environment.MachineName);
            }
        }
        
        static async Task HandleClient(HttpListenerContext context, string ip, int prt)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            

            var userip = context.Request.RemoteEndPoint.Address.ToString();
            bool isPrivate = userip.StartsWith("192")| userip.StartsWith("172");
            MainLog.writeMessage($"{request.Url.AbsolutePath}: {DateTime.Now}");
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
                //response.OutputStream.Close();
            }
            else if (request.Url.AbsolutePath.Contains("["))
            {
                string channame = request.Url.AbsolutePath.Replace("/live/", string.Empty);
                channame = request.Url.AbsolutePath.Split('[')[1].Split(']')[0].Replace("]",string.Empty).Trim();
                var Sched = AllSchedules[channame.ToLower()];
                context.Response.Headers.Add("Connection", "keep-alive");
                await Sched.SendMedia(context);
            }
            else if (request.Url.AbsolutePath.Contains("/live/"))
            {
                string channame = request.Url.AbsolutePath.Replace("/live/", string.Empty);
                if (channame.Contains("."))
                {
                    channame = channame.Split(".")[0];
                }
                MainLog.writeMessage($"Connecting {userip} to {channame}");
                var Sched = AllSchedules[channame.ToLower()];
                context.Response.Headers.Add("Connection", "keep-alive");
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
                MainLog.writeMessage($"Connecting {context.Request.UserAgent} to {ppname}");
                pP.SendMedia(response);
            }
            else if (UserAuthenticator.Auth(request,Settings.CurrentSettings.securityLevel))
            {    
                if (request.Url.AbsolutePath == "/media")
                {
                    var sc = AllSchedules.Values.ToArray();
                    var Re = upnp.Media(sc, ip, prt);
                    byte[] buffer = Encoding.UTF8.GetBytes(Re);
                    response.ContentLength64 = buffer.Length;
                    response.ContentType = Text.Xml;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    //response.OutputStream.Close();
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
                    //response.OutputStream.Close();
                }
                else if (request.HttpMethod == "POST" && request.Headers["SOAPAction"] != null)
                {
                    string soapAction = request.Headers["SOAPAction"].Trim('"');
                    MainLog.writeMessage($"SOAPAction: {soapAction}");

                    if (soapAction == "urn:schemas-upnp-org:service:ContentDirectory:1#Browse")
                    {
                        // Handle the Browse action
                        string contentDirectoryResponse = $@"<?xml version=""1.0"" ?>

<SOAP-ENV:Envelope SOAP-ENV:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/"" xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"">
    <SOAP-ENV:Body>
        <m:BrowseResponse xmlns:m=""urn:schemas-upnp-org:service:ContentDirectory:1"">
          <Result dt:dt=""string"" xmlns:dt=""urn:schemas-microsoft-com:datatypes"">
{GenerateContentDirectoryResponse()}
          </Result>
            <NumberReturned dt:dt=""ui4"" xmlns:dt=""urn:schemas-microsoft-com:datatypes"">{AllSchedules.Count}</NumberReturned>
            <TotalMatches dt:dt=""ui4"" xmlns:dt=""urn:schemas-microsoft-com:datatypes"">{AllSchedules.Count}</TotalMatches>
            <UpdateID dt:dt=""ui4"" xmlns:dt=""urn:schemas-microsoft-com:datatypes"">{UPNP.Update}</UpdateID>
        </m:BrowseResponse>
    </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

                        byte[] buffer = Encoding.UTF8.GetBytes(contentDirectoryResponse);
                        response.ContentLength64 = buffer.Length;
                        response.ContentType = "text/xml; charset=utf-8";
                        response.Headers.Add("Server", "Custom-UPnP/1.0 UPnP/1.0 DLNADOC/1.50");
                        response.Headers.Add("Cache-Control", "no-cache");
                        response.Headers.Add("Connection", "close");
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        response.OutputStream.Close();
                        return;
                    }
                }
            }
            

        } 


        static string CDS(ISchedule[] schedules, bool isprivate)
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
            sb.AppendLine(@"<DIDL-Lite xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"" xmlns=""urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"">");
            sb.AppendLine($@"<container id=""1"" parentID=""0"" restricted=""false"" updateID=""{UPNP.Update}"">");
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
        static string GenerateContentDirectoryResponse()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<DIDL-Lite xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"" xmlns=""urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/"">");
            sb.AppendLine($@"<container id=""0"" parentID=""-1"" restricted=""false"" childCount=""{AllSchedules.Count}"">");
            sb.AppendLine($@"<dc:title>Media Server</dc:title>");
            sb.AppendLine($@"<upnp:class>object.container</upnp:class>");
            for (int i = 0; i < AllSchedules.Count; i++)
            {
                try
                {
                    sb.AppendLine(AllSchedules.ElementAt(i).Value.GetContent(i, CurrentSettings.IP, CurrentSettings.Port));
                }
                catch (Exception e)
                {
                    MainLog.writeError(e.ToString());
                }
            }
            sb.AppendLine($@"</container>");
            sb.AppendLine("</DIDL-Lite>");
            return sb.ToString();
        }
        static string GetLocalIPAddress()
        {
            if(CurrentSettings.IP != string.Empty)
            {
                return CurrentSettings.IP;
            }
            Console.WriteLine("Getting Local IP Address");
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
                        MainLog.writeMessage($"Terminated process {processName} with PID {process.Id}");
                    }
                    catch (Exception ex)
                    {
                        MainLog.writeError($"Error terminating process {processName}: {ex.Message}");
                    }
                }
            }

        }
        
        
        static string webPlayer(string localip, int port)
        {
            var idx = (Path.Combine(Directory.GetCurrentDirectory(), "Index.html"));
            bool Indexu = File.Exists(idx);

            var WP = File.ReadAllText("Index.html");
            var arr = "[";
            for (int i = 0; i < AllSchedules.Count; i++)
            {
                var lnk = $"http://{localip}:{port}/live/{AllSchedules.ElementAt(i).Key}";
                arr += $@"""{lnk}"",";
            }
            arr += "];";
            return WP.Replace("REPLACEME", arr).Replace("REPLCACESRC", $"http://{localip}:{port}/live/{AllSchedules.ElementAt(0).Key}");
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
