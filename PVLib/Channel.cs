using System.Reflection.Metadata.Ecma335;

namespace PVLib
{
    public class Channel
    {
        public string HomeDirectory;
        const double FULLDAY = 23.9;
        const double AVERAGEEPISODETIME = .35;
        public string ShowDirectory => Path.Combine(HomeDirectory,"Shows");
        public Rotation rotation = new Rotation();
        public string ChannelName => new DirectoryInfo(HomeDirectory).Name;
       
        public Time PrimeTime = new Time()
        {
            Hour = 8
        };
        public List<Rerun> reruns = new List<Rerun>();
        public Channel_Type Channel_Type;
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
        public Show[] shows
        {
            get
            {
                DirectoryInfo info = new(ShowDirectory);
                var allS = info.GetFiles();
                Show[] S = new Show[allS.Length];
                for (int i = 0; i < S.Length; i++)
                {
                    S[i] = SaveLoad<Show>.Load(allS[i].FullName);
                }
                return S;
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
                    time += (AVERAGEEPISODETIME*2);
                }

                return time > FULLDAY ? FULLDAY : time;
            }
        }
        public void CreateNewSchedule(DateTime today)
        {
            Console.WriteLine($"Scheduling process started: {DateTime.Now}");
            var M = today.Date.Month;
            var D = today.Date.Day;
            var Y = today.Date.Year;
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName, $"{M}.{D}.{Y}.scd")))
            {
                return;
            }
            if(Channel_Type == Channel_Type.Binge_Like)
            {
                ShowList showList = new(new(ShowDirectory));
                if (showList.Shows.Count <= 0) return;
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName));
                SaveLoad<ShowList>.Save(showList, Path.Combine(Directory.GetCurrentDirectory(), "Schedules", ChannelName, $"{M}.{D}.{Y}.scd"));
                Console.WriteLine($"Scheduling process ended: {DateTime.Now}");
                return;
            }
            Schedule schedule = new Schedule();
            if(Reruntime.TotalHours < RerunTimeThreshhold & Channel_Type == Channel_Type.TV_Like)
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
                else if(schedule.ScheduleDuration.TotalHours>=PrimeTime.Hour & schedule.ScheduleDuration.TotalHours <= PrimeTime.Hour+(AVERAGEEPISODETIME*2))
                {
                    var ep = rotation.GetNextEp(today.DayOfWeek, this);
                    if(ep != string.Empty)
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
        public void Cancel(string name)
        {
            File.Delete(Path.Combine(ShowDirectory,name+".shw"));
            if (Channel_Type == Channel_Type.TV_Like)
            {
                rotation.Cancel(name, shows);
            }
            for (int i = 0; i < reruns.Count; i++)
            {
                if (reruns[i].Media.Contains(name))
                {
                    reruns.RemoveAt(i);
                    i--;
                }
            }
        }
        string ChooseNew()
        {
            Random rnd = new Random();
            var shws = shows;
            int timesTried = 0;
        Here:
            Show show = shws[rnd.Next(shws.Length)];
            if (show.Status == ShowStatus.Complete)
            {
                timesTried++;
                if (timesTried < 10)
                {
                    goto Here;
                }

            }
            string shw = show.NextEpisode();
            var SD = new DirectoryInfo(show.HomeDirectory).Name;
            SaveLoad<Show>.Save(show, Path.Combine(ShowDirectory, $"{SD}.shw"));
            return shw;
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
        public Channel() { }
    }
}
