using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class Settings
    {
        public PlaylistFormat playlistFormat = PlaylistFormat.m3u;
        public int Port;
        public bool useUPNP;
        public UPNP upnp = UPNP.Default;
        public string Archive_Output;
        public string redirectsite;
        public SecurityLevel securityLevel = SecurityLevel.None;
        public Settings() { }
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
                    redirectsite = "http://cartoonnetwork.com",
                    securityLevel = SecurityLevel.None,
                    playlistFormat = PlaylistFormat.m3u
                };
            }
        }
    }
}
