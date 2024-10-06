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
        public Show[] Shows
        {
            get
            {
                List<Show> list = new List<Show>();
                var cd = CTD;
                for (int i = 0; i < cd.Length; i++)
                {
                    if (cd[i].dirtype == DirectoryType.Show) list.Add((Show)cd[i]);
                }
                return list.ToArray();
            }
        }
        public string NextChan;
        public Channel_Type Channel_Type = Channel_Type.Binge_Like;
        public override Channel_Type channel_Type => Channel_Type;
        public override void CreateNewSchedule(DateTime today)
        {
            var M = today.Date.Month;
            var D = today.Date.Day;
            var Y = today.Date.Year;
            if (File.Exists(Path.Combine(FileSystem.ChanSchedules(ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}")))
            {
                Console.WriteLine("Shedeule already exist for today");
                return;
            }
            CheckForFin();
            if (CTD.Length <= 0) return;
            Console.WriteLine($"Scheduling process for {ChannelName} started {DateTime.Now}");
            ShowList showList = new(new(ShowDirectory));
            Directory.CreateDirectory(FileSystem.ChanSchedules(ChannelName));
            SaveLoad<ShowList>.Save(showList, Path.Combine(FileSystem.ChanSchedules(ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}"));
            Console.WriteLine($"Scheduling process for {ChannelName} ended: {DateTime.Now}");
        }
        void CheckForFin()
        {
            var s = CTD;
            for (int i = 0; i < s.Length; i++)
            {
                var sh = (Show)s[i];
                if (sh.Status == ShowStatus.Complete) OnFinished(sh);
            }
        }
        void OnFinished(Show show)
        {
            Cancel(new FileInfo(show.HomeDirectory).Name);
            if(SendToNextChanWhenFinished)
            {
                Channel NC = Load(NextChan);
                NC.AddShow(show);
            }
        }

        public override bool isSupported(DirectoryType type) => type == DirectoryType.Show;
        
    }
}
