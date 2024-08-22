using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class Settings
    {
        public PlaylistFormat playlistFormat;
        public int Port;
        public bool useUPNP;
        public string Archive_Output = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Playlists");
        public Uri redirectsite;
        public bool UseLogin;
        public Settings() { }
    }
}
