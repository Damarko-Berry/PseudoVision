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
        public MovieMode movieMode;
        public override Channel_Type channel_Type => Channel_Type;
        string SeasonsDirectory => Path.Combine(HomeDirectory, "Seasons");
        public string RerunDirectory => Path.Combine(HomeDirectory, "Reruns");
        Season[] seasons
        {
            get
            {
                var seasons = new List<Season>();
                Directory.CreateDirectory(SeasonsDirectory);
                UpdateSeasons();
                var SD = new DirectoryInfo(SeasonsDirectory).GetFiles();
                for (int i = 0; i < SD.Length; i++)
                {
                    seasons.Add(SaveLoad<Season>.Load(SD[i].FullName));
                }
                return seasons.ToArray();
            }
        }
        Season CurrentSeason()
        {
            var seas = new List<Season>(seasons);
            for (int i = 0; i < seas.Count; i++)
            {
                if (!seas[i].Durring(DateTime.Now))
                {
                    seas.RemoveAt(i);
                    i--;
                }
            }
            if (seas.Count == 0) return null;
            return seas[new Random().Next(0, seas.Count)];
        }
        TimeSpan Reruntime
        {
            get
            {
                var Rs = reruns;
                TimeSpan time = new TimeSpan();
                for (int i = 0; i < reruns.Length; i++)
                {
                    time += Rs[i].Duration;
                }
                return time;
            }
        }
        double RerunTimeThreshhold
        {
            get
            {

                double time = 0.0;
                int sl = CTD.Length;
                for (int i = 0; i < sl; i++)
                {
                    time += (AVERAGEEPISODETIME * 2);
                }

                return time > FULLDAY ? FULLDAY : time;
            }
        }
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
                return [.. list];
            }
        }
        public MovieDirectory MovieDirectory
        {
            get
            {
                List<MovieDirectory> list = new List<MovieDirectory>();
                var cd = CTD;
                for (int i = 0; i < cd.Length; i++)
                {
                    if (cd[i].dirtype == DirectoryType.Movie) list.Add((MovieDirectory)cd[i]);
                }
                if (list.Count > 0)
                {
                    return list[new Random().Next(list.Count)];
                }
                return null;
            }
        }
        public Time PrimeTime = new Time()
        {
            Hour = 8
        };
        public Rerun[] reruns
        {
            get
            {
                var rrs = new List<Rerun>();
                Directory.CreateDirectory(RerunDirectory);
                var rrfls = new DirectoryInfo(RerunDirectory).GetFiles("*.rrn");
                for (int i = 0; i < rrfls.Length; i++)
                {
                    rrs.Add(SaveLoad<Rerun>.Load(rrfls[i].FullName));
                }
                return [.. rrs];
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
            Console.WriteLine($"Scheduling process started: {DateTime.Now}");
            Schedule schedule = new Schedule();
            if (Reruntime.TotalHours < RerunTimeThreshhold)
            {
                rotation.CreateNewRotation(Shows);
            }
            var RR = new List<Rerun>(reruns);
            Action[] rerunAlgs = [GetRerun, getSpecial, PlayMovie];
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
                        rerunAlgs[new Random().Next(rerunAlgs.Length)].Invoke();
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
                        rerunAlgs[new Random().Next(rerunAlgs.Length)].Invoke();
                    }
                }
                else
                {
                    rerunAlgs[new Random().Next(rerunAlgs.Length)].Invoke();
                }
            }
            void GetRerun()
            {
                Random rnd = new();
                if (RR.Count == 0)
                {
                    RR.AddRange(reruns);
                }
                ChoooseR:
                var i = rnd.Next(RR.Count);
                if (!File.Exists(RR[i].Media))
                {
                    OnMissingRerun(RR[i].Media);
                    RR.Remove(RR[i]);
                    goto ChoooseR;
                }
                Rerun M = new Rerun();
                if(movieMode == MovieMode.WithReruns)
                {
                    if (MovieDirectory != null)
                    {
                        M = new Rerun(new TimeSlot(MovieDirectory.NextEpisode()));
                        if (new Random().Next(100) > 50)
                        {
                            schedule.slots.Add(new(M, schedule.slots));
                            return;
                        }
                    }
                }
                schedule.slots.Add(new(RR[i], schedule.slots));
                RR.RemoveAt(i);
            }
            void Newep(string e)
            {
                schedule.slots.Add(new(e, schedule.slots));

                Directory.CreateDirectory(RerunDirectory);
                FileInfo file = new FileInfo(schedule.slots[^1].Media);
                SaveLoad<Rerun>.Save(new(schedule.slots[^1]), Path.Combine(RerunDirectory, $"{file.Name}.rrn"));
            }
            void getSpecial()
            {
                var s = CurrentSeason();
                if (s == null)
                {
                    GetRerun();
                    return;
                }
                int TR = (int)(s.SpecialThreshold * 100);
                if (s.Specials.Count > 0)
                {
                    schedule.slots.Add(new(s.Something, schedule.slots));
                }
                else
                {
                    GetRerun();
                }
            }
            void PlayMovie()
            {
                if (movieMode != MovieMode.WithReruns)
                {
                    var m = MovieDirectory;
                    if (m != null & today.DayOfWeek.ToString().ToLower() == movieMode.ToString().ToLower())
                    {
                        if (m.Length > 0)
                        {
                            schedule.slots.Add(new(m.NextEpisode(), schedule.slots));
                            return;
                        }
                    }
                }
                GetRerun();
            }
            
            Directory.CreateDirectory(FileSystem.ChanSchedules(ChannelName));
            SaveLoad<Schedule>.Save(schedule, Path.Combine(FileSystem.ChanSchedules(ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}"));
            SaveLoad<TV_LikeChannel>.Save(this, FileSystem.ChannleChan(ChannelName));
            Console.WriteLine($"Scheduling process ended: {DateTime.Now}");
        }

        void OnMissingRerun(string epname)
        {
            var fi = new FileInfo(epname);
            File.Delete(Path.Combine(RerunDirectory, $"{fi.Name}.rrn"));
        }
        public override void Cancel(string name)
        {
            base.Cancel(name);
            rotation.Cancel(name, (Shows));
            var doa = Directory.GetFiles(RerunDirectory);
            for (int i = 0; i < doa.Length; i++)
            {
                if (doa[i].Contains(name))
                {
                    File.Delete(doa[i]);
                    i--;
                }
            }
        }
        void UpdateSeasons()
        {
            var SeD = new DirectoryInfo(SeasonsDirectory).GetFiles();
            for (int i = 0; i < SeD.Length; i++)
            {
                var seas = SaveLoad<Season>.Load(SeD[i].FullName);
                if (DateTime.Now > seas.End)
                {
                    seas.Start.Year++;
                    seas.End.Year++;
                }
                SaveLoad<Season>.Save(seas, SeD[i].FullName);
            }
        }

        public override bool isSupported(DirectoryType type) => type == DirectoryType.Movie | type == DirectoryType.Show;
    }
}
