using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class MovieChannel : Channel
    {
        public Channel_Type Channel_Type = Channel_Type.Movies;
        public override Channel_Type channel_Type => Channel_Type;
        public MovieDirectory movieDirectory
        {
            get
            {
                var s = CTD;
                List<MovieDirectory> ret = new List<MovieDirectory>();
                for (int i = 0; i < s.Length; i++)
                    if (s[i].dirtype == DirectoryType.Movie) 
                        ret.Add((MovieDirectory)s[i]);
                return ret[new Random().Next(ret.Count)];
            }
        }
        public override void CreateNewSchedule(DateTime today)
        {
            var M = today.Date.Month;
            var D = today.Date.Day;
            var Y = today.Date.Year;
            if (CTD.Length <= 0) return;
            if (File.Exists(Path.Combine(FileSystem.ChanSchedules(ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}")))
            {
                return;
            }
            if (movieDirectory == null) return;
            Schedule schedule = new Schedule();
            while(schedule.ScheduleDuration.TotalHours< FULLDAY)
            {
                TimeSlot timeSlot = new(movieDirectory.NextEpisode(), schedule.slots, today);
                schedule.slots.Add(timeSlot);
            }
            SaveSchedule(schedule,today);
        }

        public override bool isSupported(DirectoryType type)=> type == DirectoryType.Movie;
    }
}
