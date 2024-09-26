using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public static class FileSystem
    {
        static string Root = Directory.GetCurrentDirectory();
        public static string Channels => Path.Combine(Root, "Channels");
        public static string Schedules => Path.Combine(Root, "Schedules");
        public static string Settings => Path.Combine(Root, "Settings");
        public static string ChannleChan(string cname)=> Path.Combine(Channels, cname, "Channel.chan");
        public static string Seasons(string cname)=> Path.Combine(Channels, cname, "Seasons");
        public static string ChanSchedules(string cname)=> Path.Combine(Schedules, cname);
        public const string ShowEXT = "shw";
        public const string ScheduleEXT = "scd";
    }
}
