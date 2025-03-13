using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PVLib
{
    public class Rotation
    {
        public ShowRef[] ShowList= [new() { DayToPlay = DayOfWeek.Sunday },new() { DayToPlay = DayOfWeek.Monday},new() { DayToPlay = DayOfWeek.Tuesday},new() { DayToPlay = DayOfWeek.Wednesday },new() { DayToPlay = DayOfWeek.Thursday},new() { DayToPlay = DayOfWeek.Friday },new() { DayToPlay = DayOfWeek.Saturday},  ];

        int count
        {
            get
            {
                int CountofShows = 0;
                for (int i = 0; i < ShowList.Length; i++)
                {
                    CountofShows += ShowList[i].Directory.Count;
                }
                return CountofShows;
            }
        }
        internal ShowRef this[ShowRef show]
        {
            set
            {
                for (int i = 0; i < ShowList.Length; i++)
                {
                    if (ShowList[i].DayToPlay == show.DayToPlay)
                    {
                        ShowList[i] = value;
                    }
                }
            }
            get
            {
                for (int i = 0; i < ShowList.Length; i++)
                {
                    if (ShowList[i].DayToPlay == show.DayToPlay)
                    {
                        return ShowList[i];
                    }
                }
                return show;
            }
        }
        internal ShowRef this[DayOfWeek day]=> ShowList[(int)day];
        internal Show GetShow(DayOfWeek day, TV_LikeChannel channel)
        {
            try 
            { 
                return SaveLoad<Show>.Load(Path.Combine(channel.ShowDirectory, ShowList[(int)day].name + ".shw"));
            }
            catch
            {
                return null;
            }
        }
        internal void CreateNewRotation(Show[] shows)
        {
            int SD = 6;
            while (count < shows.Length)
            {
            choose:
                Random rnd = new();
                Show h = shows[rnd.Next(shows.Length)];
                if (AlreadyInRotation(h))
                {
                    goto choose;
                }
                ShowList[SD].Directory.Add(h.HomeDirectory);
                SD--;
                if (SD < 0)
                {
                    SD = 6;
                }
            } 
        }
        bool AlreadyInRotation(Show show)
        {
            for (int i = 0; i < ShowList.Length; i++)
            {
                if (ShowList[i].Directory.Contains(show.HomeDirectory))
                {
                    return true;
                }
            }
            return false;
        }
        internal string GetNextEp(DayOfWeek day, TV_LikeChannel channel)
        {
            if (count== 0) return string.Empty;
            var NE = string.Empty;
            if(ShowList[(int)day].Directory.Count == 0)
            {
                OnShowComplete(ShowList[(int)day], channel.Shows);
                if (ShowList[(int)day].Directory.Count == 0)
                {
                    return string.Empty;
                }
            }
            Show v = GetShow(day,channel);
            ShowRef shwrf = new();
           
            if (v == null)
                NE = string.Empty;
            else
            {
                NE = v.NextEpisode(shwrf.Next);
                SaveLoad<Show>.Save(v, Path.Combine(channel.ShowDirectory, shwrf.name + ".shw"));
                if (v.Status == ShowStatus.Complete)
                {
                    OnShowComplete(shwrf, channel.Shows);
                }
            }
            return NE;
        }
        internal string GetNextEp(TV_LikeChannel channel)
        {
            if (ShowList.Length == 0) return string.Empty;
            Random r = new Random();
            var NE = string.Empty;
            int i = r.Next(ShowList.Length);
            var v = SaveLoad<Show>.Load(Path.Combine(channel.ShowDirectory, ShowList[i].name + ".shw"));
            var shwrf = ShowList[i];
            if (v == null)
                NE = string.Empty;
            else
            {
                NE = v.NextEpisode(shwrf.Next);
                SaveLoad<Show>.Save(v, Path.Combine(channel.ShowDirectory, shwrf.name + ".shw"));
                if (v.Status == ShowStatus.Complete)
                {
                    OnShowComplete(shwrf, channel.Shows);
                }
            }
            return NE;
        }
        public void Clear()
        {
            for (int i = 0; i < ShowList.Length; i++)
            {
                ShowList[i].Directory.Clear();
            }
        }
        void OnShowComplete(ShowRef show, Show[] shows)
        {
            
            var AllShows = new List<Show>(shows);
            for (int i = 0; i < AllShows.Count; i++)
            {
                if (AllShows[i].Status == ShowStatus.Complete | AlreadyInRotation(AllShows[i]))
                {
                    AllShows.RemoveAt(i);
                    i--;
                }
            }
            if (AllShows.Count > 0)
            {
                Random random = new Random();
                this[show].Directory.Add(AllShows[random.Next(AllShows.Count)].HomeDirectory);
            }
        }
        
        internal void Cancel(string name, Show[] other_shows)
        {
            for (int i = 0; i < ShowList.Length; i++)
            {
                if (ShowList[i].name == name)
                {
                    OnShowComplete(ShowList[i], other_shows);
                    break;
                }
            }
        }
    }
}
