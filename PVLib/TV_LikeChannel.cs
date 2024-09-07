using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class TV_LikeChannel : Channel
    {
        const double FULLDAY = 23.9;
        const double AVERAGEEPISODETIME = .35;
        public Rotation rotation = new Rotation();
        public Channel_Type Channel_Type = Channel_Type.TV_Like;
        public override Channel_Type channel_Type => Channel_Type;
        TimeSpan Reruntime
        {
            get
            {
                TimeSpan time = new TimeSpan();
                for (int i = 0; i < reruns.Count; i++)
                {
                    time += reruns[i].Duration;
                }
                return time;
            }
        }
        double RerunTimeThreshhold
        {
            get
            {

                double time = 0.0;
                int sl = shows.Length;
                for (int i = 0; i < sl; i++)
                {
                    time += (AVERAGEEPISODETIME * 2);
                }

                return time > FULLDAY ? FULLDAY : time;
            }
        }
        public Time PrimeTime = new Time()
        {
            Hour = 8
        };
        public List<Rerun> reruns = new List<Rerun>();
        public override void CreateNewSchedule(DateTime today)
        {
            var M = today.Date.Month;
            var D = today.Date.Day;
            var Y = today.Date.Year;
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName, $"{M}.{D}.{Y}.scd")))
            {
                return;
            }
            Console.WriteLine($"Scheduling process started: {DateTime.Now}");
            Schedule schedule = new Schedule();
            if (Reruntime.TotalHours < RerunTimeThreshhold)
            {
                rotation.CreateNewRotation(shows);
            }
            var RR = new List<Rerun>(reruns);
            while (schedule.ScheduleDuration.TotalHours < FULLDAY)
            {
                if (Reruntime.TotalHours < RerunTimeThreshhold)
                {
                    var ep = rotation.GetNextEp(this);
                    if (ep != string.Empty)
                    {
                        Newep(ep);
                    }
                    else
                    {
                        Rerun();
                    }
                }
                else if (schedule.ScheduleDuration.TotalHours >= PrimeTime.Hour & schedule.ScheduleDuration.TotalHours <= PrimeTime.Hour + (AVERAGEEPISODETIME * 2))
                {
                    var ep = rotation.GetNextEp(today.DayOfWeek, this);
                    if (ep != string.Empty)
                    {
                        Newep(ep);
                    }
                    else
                    {
                        Rerun();
                    }
                }
                else
                {
                    Rerun();
                }
            }
            void Rerun()
            {
                Random rnd = new Random();
                if (RR.Count == 0) RR.AddRange(reruns);
                ChoooseR:
                var i = rnd.Next(RR.Count);
                if (!File.Exists(RR[i].Media))
                {
                    OnMissingRerun(RR[i].Media);
                    RR.Remove(RR[i]);
                    goto ChoooseR;
                }
                schedule.slots.Add(new(RR[i], schedule.slots));
                RR.RemoveAt(i);
            }
            void Newep(string e)
            {
                schedule.slots.Add(new(e, schedule.slots));
                for (int i = 0; i < reruns.Count; i++)
                {
                    if (schedule.slots[^1].Media == reruns[i].Media)
                    {
                        reruns.RemoveAt(i);
                        break;
                    }
                }
                reruns.Add(new(schedule.slots[^1]));
            }
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName));
            SaveLoad<Schedule>.Save(schedule, Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName, $"{M}.{D}.{Y}.scd"));
            SaveLoad<Channel>.Save(this, Path.Combine(HomeDirectory, $"Channel.chan"));
            Console.WriteLine($"Scheduling process ended: {DateTime.Now}");
        }
        void OnMissingRerun(string epname)
        {
            for (int i = 0; i < reruns.Count; i++)
            {
                if (reruns[i].Media == epname)
                {
                    reruns.RemoveAt(i);
                    break;
                }
            }
        }
        public override void Cancel(string name)
        {
            base.Cancel(name);
            rotation.Cancel(name, shows);
            for (int i = 0; i < reruns.Count; i++)
            {
                if (reruns[i].Media.Contains(name))
                {
                    reruns.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
