using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public struct Rotation
    {
        public List<ShowRef> ShowList;
        ShowRef this[ShowRef show]
        {
            set 
            {
                for (int i = 0; i < ShowList.Count; i++)
                {
                    if (ShowList[i].DayToPlay == show.DayToPlay)
                    {
                        ShowList[i] = value;
                    }
                }
            }
            get
            {
                for (int i = 0; i < ShowList.Count; i++)
                {
                    if (ShowList[i].DayToPlay == show.DayToPlay)
                    {
                        return ShowList[i];
                    }
                }
                return show; 
            }
        }
        internal void CreateNewRotation(Show[] shows)
        {
            if (ShowList.Count>0) return;
            ShowList = new List<ShowRef>();
            var MAX = shows.Length;
            MAX = Math.Clamp(MAX, 1, 7);
            while(ShowList.Count < MAX)
            {
                choose:
                Random rnd = new Random();
                Show h = shows[rnd.Next(shows.Length)];
                if (AlreadyInRotation(h))
                {
                    goto choose;
                }
                ShowList.Add(new(h.HomeDirectory, (DayOfWeek)(6 - ShowList.Count) ));
            }
        }
        bool AlreadyInRotation(Show show)
        {
            for (int i = 0; i < ShowList.Count; i++)
            {
                if(show.HomeDirectory == ShowList[i].Directory)
                {
                    return true;
                }
            }
            return false;
        }
        internal string GetNextEp(DayOfWeek day, TV_LikeChannel channel)
        {
            if(ShowList.Count == 0) return string.Empty;
            var NE = string.Empty;
            Show v = null;
            ShowRef shwrf = new();
            for (int i = 0; i < ShowList.Count; i++)
            {
                if (ShowList[i].DayToPlay == day) {
                    shwrf = ShowList[i];
                    v = SaveLoad<Show>.Load( Path.Combine(channel.ShowDirectory, ShowList[i].name+".shw"));
                    break;
                }
            }
            if (v == null)
                NE = string.Empty;
            else
            {
                NE = v.NextEpisode();
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
            if(ShowList.Count == 0) return string.Empty;
            Random r = new Random();
            var NE = string.Empty;
            int i = r.Next(ShowList.Count);
            var v = SaveLoad<Show>.Load(Path.Combine(channel.ShowDirectory, ShowList[i].name+".shw"));
            var shwrf = ShowList[i];
            if (v == null)
                NE = string.Empty;
            else
            {
                NE = v.NextEpisode();
                SaveLoad<Show>.Save(v, Path.Combine(channel.ShowDirectory, shwrf.name + ".shw"));
                if (v.Status == ShowStatus.Complete)
                {
                    OnShowComplete(shwrf, channel.Shows);
                }
            }
            return NE;
        }

        void OnShowComplete(ShowRef show, Show[] shows)
        {
            var AllShows = new List<Show>(shows);
            for (int i = 0; i < AllShows.Count; i++)
            {
                if (AllShows[i].Status != ShowStatus.New)
                {
                    AllShows.RemoveAt(i);
                    i--;
                }
            }
            if (AllShows.Count > 0)
            {
                Random random = new Random();
                this[show] = new(AllShows[random.Next(AllShows.Count)]);
            }
            else
            {
                ShowList.Remove(show);
            }
        }

        internal void Cancel(string name, Show[] other_shows)
        {
            for (int i = 0; i < ShowList.Count; i++)
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
