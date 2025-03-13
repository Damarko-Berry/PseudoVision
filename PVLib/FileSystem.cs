using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public static class FileSystem
    {
        //public static string Root = Directory.GetCurrentDirectory();
        public static string FFMPEG => Path.Combine("ffmpeg","ffmpeg.exe");
        public static string Channels => Path.Combine("Channels");
        public static string Schedules => Path.Combine("Schedules");
        public static string SettingsFile => Path.Combine("settings");
        public static string ChannleChan(string cname)=> Path.Combine(Channels, cname, "Channel.chan");
        public static string Seasons(string cname)=> Path.Combine(Channels, cname, "Seasons");
        public static string ChanSchedules(string cname)=> Path.Combine(Schedules, cname);
        public const string ShowEXT = "shw";
        public const string ScheduleEXT = "scd";
        public static string ArchiveDirectory(string chaname) => Path.Combine(Settings.CurrentSettings.Archive_Output, "PV-Archive", chaname);
        public static string Archive(string chaname, DateTime date) => Path.Combine(ArchiveDirectory(chaname), $"{date.Month}.{date.Day}.{date.Year}.{Settings.CurrentSettings.playlistFormat}");
    }
}
