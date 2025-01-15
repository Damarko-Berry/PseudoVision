using System.Xml.Serialization;
using WMPLib;

namespace PVLib
{
    public class Show : ContentDirectory
    {
        public int CurrentEpisodeNumber;
        public int MovieNo;
        public int EpisodesSinceLastMovie;
        int EpisodesPerMovie
        {
            get
            {
                if(MovieProgress == ShowStatus.Complete)return 0;
                if(EpisodeProgress == ShowStatus.Complete)return -1;
                if(TotalEpsisodes == 0)return -1;
                return TotalEpsisodes / TotalMovies;
            }
        }
        DirectoryInfo MovieDirectory
        {
            get
            {
                if(Directory.Exists(Path.Combine(HomeDirectory, "Movies")))
                {
                    return new(Path.Combine(HomeDirectory, "Movies"));
                }
                if(Directory.Exists(Path.Combine(HomeDirectory, "movies")))
                {
                    return new(Path.Combine(HomeDirectory, "movies"));
                }
                return null;
            }
        }
        int TotalMovies
        {
            get
            {
                if(MovieDirectory == null)
                {
                    return 0;
                }
                return MovieDirectory.GetFiles().Length;
            }
        }
        int TotalEpsisodes => Length;
        public override string NextEpisode()
        {
            if(Status == ShowStatus.Complete)
            {
                return GetRerun();
            }
            var ep = string.Empty;
            if (EpisodesSinceLastMovie>EpisodesPerMovie)
            {
                EpisodesSinceLastMovie = 0;
                ep = MovieDirectory.GetFiles()[MovieNo].FullName;
                MovieNo++;
            }
            else{
               ep = Content[CurrentEpisodeNumber].FullName;
                CurrentEpisodeNumber++;
                if(MovieProgress != ShowStatus.Complete)
                {
                    EpisodesSinceLastMovie++;
                }
            }
            return ep;
        }
        string GetRerun()
        {
            Random rnd = new Random();
            int ep = rnd.Next(TotalEpsisodes);
            return Content[ep].FullName;
        }
        public ShowStatus Status
        {
            get
            {
                var S = ShowStatus.New;
                switch (EpisodeProgress)
                {
                    case ShowStatus.New:
                        S = MovieProgress switch
                        {
                            ShowStatus.New => ShowStatus.New,
                            _ => ShowStatus.Ongoing
                        };
                        break;
                    case ShowStatus.Ongoing:
                        S = ShowStatus.Ongoing;
                        break;
                    case ShowStatus.Complete:
                        S = MovieProgress switch
                        {
                            ShowStatus.Complete => ShowStatus.Complete,
                            _=> ShowStatus.Ongoing,
                        };
                        break;
                }
                return S;
            }
        }
        ShowStatus EpisodeProgress
        {
            get
            {
                if (CurrentEpisodeNumber >= TotalEpsisodes) return ShowStatus.Complete;
                else if (CurrentEpisodeNumber > 0&CurrentEpisodeNumber<TotalEpsisodes) return ShowStatus.Ongoing;
                return ShowStatus.New;
            }
        }
        ShowStatus MovieProgress
        {
            get
            {
                if (MovieNo >= TotalMovies) return ShowStatus.Complete;
                else if (MovieNo > 0 & MovieNo<TotalMovies) return ShowStatus.Ongoing;
                return ShowStatus.New;
            }
        }
        
        internal override FileInfo[] Content
        {
            get
            {
                List<FileInfo> VF = new();
                
                var td = new List<DirectoryInfo>(HomeDirectoryInfo.GetDirectories());
                for (int i = 0; i < td.Count; i++)
                {
                    if (td[i].Name.ToLower().Trim() == "movies" | td[i].Name.ToLower().Trim() == "specials"| td[i].Name.ToLower().Trim() == "shorts")
                        td.RemoveAt(i);
                }
                for (int i = 0; i < td.Count; i++)
                    for (int j = 0; j < ValidExtensions.Length; j++)
                        VF.AddRange(td[i].GetFiles("*" + ValidExtensions[j]));
                return [.. VF];
            }
        }
        public Rerun[] shorts
        {
            get
            {
                DirectoryInfo shortsdirectory = null;
                if(HomeDirectoryInfo.GetDirectories("shorts").Length>0)
                {
                    shortsdirectory = HomeDirectoryInfo.GetDirectories("shorts")[0];
                }
                else if(HomeDirectoryInfo.GetDirectories("Shorts").Length>0)
                {
                    shortsdirectory = HomeDirectoryInfo.GetDirectories("Shorts")[0];
                }
                if (shortsdirectory == null) return [];

                List<Rerun> reruns = new List<Rerun>();
                List<FileInfo> mPaths = new();
                for (int i = 0; i < ValidExtensions.Length; i++)
                {
                    mPaths.AddRange(shortsdirectory.GetFiles("*"+ValidExtensions[i], SearchOption.AllDirectories));
                }
                for (int i = 0; i < mPaths.Count; i++)
                {
                    reruns.Add(new TimeSlot(mPaths[i].FullName));
                }
                return [.. reruns];
            }
        }

        public override TimeSpan Duration
        {
            get
            {
                TimeSpan durr = new();
                var player = new WindowsMediaPlayer();
                string durs = string.Empty;
                for (int i = 0; i < Content.Length; i++)
                {
                    var clip = player.newMedia(Content[i].FullName);
                    // Add milliseconds for better accuracy
                    int hours = (int)(clip.duration / 3600);
                    int minutes = (int)((clip.duration % 3600) / 60);
                    int seconds = (int)(clip.duration % 60);
                    int milliseconds = (int)((clip.duration - Math.Floor(clip.duration)) * 1000);
                    var MT = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                    durs +=$"Ep{i+1}= Hours:{hours} Minutes:{minutes} Seconds:{seconds} Milliseconds:{milliseconds}\n";
                    durr += MT;
                }
                
                return durr;
            }
        }


        public void Reset()
        {
            CurrentEpisodeNumber = 0;
            MovieNo = 0;
            EpisodesSinceLastMovie = 0;
        }

        public Show Clone()
        {
            return (Show)MemberwiseClone();
        }
        public Show() { }
    }
}
