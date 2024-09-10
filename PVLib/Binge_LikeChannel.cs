using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class Binge_LikeChannel : Channel
    {
        public bool SendToNextChanWhenFinished;
        public string NextChan;
        void OnFinished(Show show)
        {
            Cancel(new FileInfo(show.HomeDirectory).Name);
            if(SendToNextChanWhenFinished)
            {
                Channel NC = Load(NextChan);
                NC.AddShow(show);
            }
        }
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
            CheckForFin();
            if (shows.Length <= 0) return;
            Console.WriteLine($"Scheduling process started: {DateTime.Now}");
            ShowList showList = new(new(ShowDirectory));
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName));
            SaveLoad<ShowList>.Save(showList, Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName, $"{M}.{D}.{Y}.scd"));
            Console.WriteLine($"Scheduling process ended: {DateTime.Now}");
        }
        void CheckForFin()
        {
            var s = shows;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].Status == ShowStatus.Complete) OnFinished(s[i]);
            }
        }
    }
}
