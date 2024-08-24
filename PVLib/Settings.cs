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
        public string Archive_Output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Playlists");
        public Uri redirectsite;
        public SecurityLevel securityLevel = SecurityLevel.None;
        public Settings() { }
    }
}
