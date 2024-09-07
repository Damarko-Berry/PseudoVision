using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class Binge_LikeChannel : Channel
    {
        public Channel_Type Channel_Type = Channel_Type.Binge_Like;
        public override Channel_Type channel_Type => Channel_Type;
        public override void CreateNewSchedule(DateTime today)
        {
            var M = today.Date.Month;
            var D = today.Date.Day;
            var Y = today.Date.Year;
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName, $"{M}.{D}.{Y}.scd")))
            {
                Console.WriteLine("Shedeule already exist for today");
                return;
            }
            Console.WriteLine($"Scheduling process started: {DateTime.Now}");
            
            ShowList showList = new(new(ShowDirectory));
            if (showList.Shows.Count <= 0) return;
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName));
            SaveLoad<ShowList>.Save(showList, Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName, $"{M}.{D}.{Y}.scd"));
            Console.WriteLine($"Scheduling process ended: {DateTime.Now}");

        }
    }
}
