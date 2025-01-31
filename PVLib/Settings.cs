using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class Settings
    {
        public PlaylistFormat playlistFormat = PlaylistFormat.m3u;
        public int Port;
        public string IP = string.Empty;
        public bool useUPNP;
        public UPNP upnp = UPNP.Default;
        public string Archive_Output;
        public string redirectsite;
        public string VideoExtensions = string.Empty;
        public string ffmpegCache = Directory.GetCurrentDirectory();
        public bool Portable;

        public LiveHandling liveHandling = LiveHandling.Storage_Saver;
        public string[] GetVideoExtensions
        {
            get
            {
                if (VideoExtensions == string.Empty) 
                    return null;
                return VideoExtensions.Split(',');
            }
        }
        public SecurityApplication securityLevel = SecurityApplication.Never;
        public Settings() { }
        public static Settings CurrentSettings { get; set; }
        public static Settings Default
        {
            get
            {
                return new Settings()
                {
                    Port = 6589,
                    upnp = UPNP.Default,
                    useUPNP = true,
                    Archive_Output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Playlists"),
                    redirectsite = "https://cartoonnetwork.com",
                    securityLevel = SecurityApplication.Never,
                    playlistFormat = PlaylistFormat.m3u,
                    VideoExtensions= string.Empty,
                    IP = GetLocalIPAddress(),
                    ffmpegCache = Directory.GetCurrentDirectory()
                };
                string GetLocalIPAddress()
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
            }
        }
    }
}
