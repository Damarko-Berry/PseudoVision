using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class TV_LikeChannel : Channel
    {
        public bool Live;
        
        const double AVERAGEEPISODETIME = .35;
        
        public Rotation rotation = new Rotation();
        
        public Channel_Type Channel_Type = Channel_Type.TV_Like;
        
        public MovieMode movieMode;
        

        public bool FillTime;
        
        public override Channel_Type channel_Type => Channel_Type;
        
        public string SeasonsDirectory => Path.Combine(HomeDirectory, "Seasons");
        
        public string RerunDirectory => Path.Combine(HomeDirectory, "Reruns");
        
        public string ShortsDirectory => Path.Combine(HomeDirectory, "Shorts");
        
        public Season[] Seasons
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
        
        Season CurrentSeason(Season[] seasons)
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
            if (seas.Count == 0)
            {
                return null;
            }
            return seas[new Random().Next(0, seas.Count)];
        }
        
        TimeSpan Reruntime(Rerun[]Rs)
        {
                TimeSpan time = new TimeSpan();
                for (int i = 0; i < Rs.Length; i++)
                {
                    time += Rs[i].Duration;
                }
                return time;
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
        
        MovieDirectory Shorts
        {
            get
            {
                DirectoryInfo info = new(ShortsDirectory);
                var allS = info.GetFiles();
                MovieDirectory[] S = new MovieDirectory[allS.Length];
                for (int i = 0; i < S.Length; i++)
                {
                    S[i] = SaveLoad<MovieDirectory>.Load(allS[i].FullName);
                }
                return S.Length > 0 ? S[new Random().Next(S.Length)] : null;
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
            if (ScheduleExists(today)) return;
            if (CTD.Length <= 0) return;
            if (Shows.Length <= 0) return;
            DateTime Start= DateTime.Now;
            var schedule = new Schedule();
            var seas = Seasons;
            var StaticRRs = reruns;
            List<Rerun> allR = new List<Rerun>();
            if (FillTime)
            {
                var s = Shows;
                for (int i = 0; i < s.Length; i++)
                {
                    allR.AddRange(s[i].shorts);
                }
            }
            if (Reruntime(StaticRRs).TotalHours < RerunTimeThreshhold)
            {
                rotation.CreateNewRotation(Shows);
            }
            var RR = new List<Rerun>(StaticRRs);
            Action[] rerunAlgs = [GetRerun, getSpecial, PlayMovie];
            while (schedule.ScheduleDuration.TotalHours < FULLDAY)
            {
                if (Reruntime(StaticRRs).TotalHours < RerunTimeThreshhold)
                {
                    var ep = rotation.GetNextEp(this);
                    if (ep != string.Empty)
                    {
                        Newep(ep);
                        StaticRRs = reruns;
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
                if (FillTime)
                {
                    var endT = schedule.slots[^1].EndTime;

                    var next30minmark = new DateTime();
                    try
                    {
                        next30minmark= (endT.Minute < 30) ? new DateTime(Y, M, D, endT.Hour, 30, 0) : new DateTime(Y, M, D, endT.Hour + 1, 0, 0);
                    }
                    catch
                    {
                        next30minmark = endT.Date;
                        next30minmark = next30minmark.AddDays(1);
                    }

                    TimeSpan duration = next30minmark - endT;
                    if (duration.Minutes < 21)
                    {
                        var siller = getFill(duration);
                        for (int i = 0; i < siller.Length; i++)
                        {
                            schedule.slots.Add(new(siller[i],schedule.slots));
                        }
                    }
                }
            }
            void Newep(string e)
            {
                schedule.slots.Add(new(e, schedule.slots));

                Directory.CreateDirectory(RerunDirectory);
                FileInfo file = new FileInfo(schedule.slots[^1].Media);
                SaveLoad<Rerun>.Save(new(schedule.slots[^1]), Path.Combine(RerunDirectory, $"{file.Name}.rrn"));
            }
            void GetRerun()
            {
                Random rnd = new();
                if (RR.Count == 0)
                {
                    RR.AddRange(StaticRRs);
                }
                ChoooseR:
                var i = rnd.Next(RR.Count);
                if (!File.Exists(RR[i].Media))
                {
                    OnMissingRerun(RR[i].Media);
                    RR.Remove(RR[i]);
                    goto ChoooseR;
                }
                Rerun M = new();
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
            void getSpecial()
            {
                var s = CurrentSeason(seas);
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
            Rerun[] getFill(TimeSpan time)
            {
                if(allR.Count < 1 & Shorts == null) return new Rerun[0];
                List<Rerun> list = new List<Rerun>();
                while (TotalTime()<time)
                {
                    Random random = new Random();
                    if (random.Next(10) > 5 & allR.Count > 0)
                    {
                        list.Add(allR[random.Next(allR.Count)]);
                    }
                    else if(Shorts!= null)
                    {
                        list.Add(new TimeSlot(Shorts.NextEpisode()));
                    }
                }
                return [.. list];
                TimeSpan TotalTime()
                {
                    TimeSpan span = TimeSpan.Zero;
                    for (int i = 0; i < list.Count; i++)
                    {
                        span+=list[i].Duration;
                    }
                    return span;
                }
            }
            SaveSchedule(schedule,today);
            SaveLoad<TV_LikeChannel>.Save(this, FileSystem.ChannleChan(ChannelName));
            DateTime endtime = DateTime.Now;
            Console.WriteLine($"{ChannelName}: {(endtime-Start).TotalSeconds} seconds");
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
