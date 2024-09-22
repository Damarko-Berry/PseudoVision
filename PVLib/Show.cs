namespace PVLib
{
    public class Show
    {
        public string HomeDirectory;
        string dir
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    HomeDirectory = HomeDirectory.Replace("C:\\", "/mnt/c/");
                    return HomeDirectory.Replace('\\', '/');

                }
                return HomeDirectory;
            }
        }
        public int Season;
        public int EpisodeNo;
        public int MovieNo;
        int EpisodesPerMovie
        {
            get
            {
                if(MovieProgress == ShowStatus.Complete)return 0;
                if(EpisodeProgress == ShowStatus.Complete)return -1;
                if(TotalEps == 0)return -1;
                return TotalEps / TotalMovies;
            }
        }
        public int EpisodesSinceLastMovie;
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
                if(Directory.Exists(Path.Combine(HomeDirectory, "Specials")))
                {
                    return new(Path.Combine(HomeDirectory, "Specials"));
                }
                if(Directory.Exists(Path.Combine(HomeDirectory, "specials")))
                {
                    return new(Path.Combine(HomeDirectory, "specials"));
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
        DirectoryInfo[] SeasonDirectories
        {
            get
            {
                DirectoryInfo se = new(dir);
                var td = new List<DirectoryInfo>(se.GetDirectories());
                for (int i = 0; i < td.Count; i++)
                {
                    if (td[i].Name.ToLower().Replace(" ", string.Empty) == "movies" | td[i].Name.ToLower().Replace(" ", string.Empty) == "specials")
                        td.RemoveAt(i);
                }
                return td.ToArray();
            }
        }
        int TotalEps
        {
            get
            {
                int e = 0;

                var td = SeasonDirectories;
                for (int i = 0; i < td.Length; i++)
                {
                    e += td[i].GetFiles().Length;
                }
                return e;
            }
        }
        int CuEpNo
        {
            get
            {
                int e = 0;
                int CU = Season;
                
                var td = SeasonDirectories;
                for (int i = 0; i < CU; i++)
                {
                    for (int j = 0; j < td[i].GetFiles().Length; j++)
                    {
                        e++;
                        if(i == Season && j== EpisodeNo)
                        {
                            break;
                        }
                    }
                }

                return e;
            }
        }
        public string NextEpisode()
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
               ep = SeasonDirectories[Season].GetFiles()[EpisodeNo].FullName;
                EpisodeNo++;
                if (EpisodeNo >= SeasonDirectories[Season].GetFiles().Length)
                {
                    Season++;
                    EpisodeNo = 0;
                }
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
            int ep = rnd.Next(TotalEps);
            int r = 0;
            int s = 0; int e = 0;
            var H = SeasonDirectories;
            for (; s < H.Length; s++)
            {
                DirectoryInfo se = new(dir);
                for (; e < se.GetDirectories()[s].GetFiles().Length; e++)
                {
                    r++;
                    if (r >= ep)
                    {
                        return H[s].GetFiles()[e].FullName;
                    }
                }
            }
            return H[s-1].GetFiles()[e-1].FullName;
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
                if (CuEpNo >= TotalEps) return ShowStatus.Complete;
                else if (CuEpNo > 0&CuEpNo<TotalEps) return ShowStatus.Ongoing;
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
        public void Reset()
        {
            Season = 0;
            EpisodeNo = 0;
            MovieNo = 0;
            EpisodesSinceLastMovie = 0;
        }

        public Show Clone()
        {
            return (Show)MemberwiseClone();
        }
    }
}
