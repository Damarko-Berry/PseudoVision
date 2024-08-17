namespace PVLib
{
    public class Show
    {
        public int Season;
        public int EpisodeNo;
        int TotalEps
        {
            get
            {
                int e = 0;
                DirectoryInfo se= new(HomeDirectory);
                var td = se.GetDirectories().Length;
                for (int i = 0; i < td; i++)
                {
                    e += se.GetDirectories()[i].GetFiles().Length;
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
                DirectoryInfo se= new(HomeDirectory);
                for (int i = 0; i < CU; i++)
                {
                    for (int j = 0; j < se.GetDirectories()[i].GetFiles().Length; j++)
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
        public string HomeDirectory;
        public string NextEpisode()
        {

            DirectoryInfo H = new DirectoryInfo(HomeDirectory);
            if(Status== ShowStatus.Complete)
            {
                return GetRerun();
            }
            var ep = H.GetDirectories()[Season].GetFiles()[EpisodeNo].FullName;
            EpisodeNo++;
            if (EpisodeNo>=H.GetDirectories()[Season].GetFiles().Length)
            {
                Season++;
                EpisodeNo = 0;
            }
            return ep;
        }
        string GetRerun()
        {
            Random rnd = new Random();
            int ep = rnd.Next(TotalEps);
            int r = 0;
            int s = 0; int e = 0;
            DirectoryInfo H = new DirectoryInfo(HomeDirectory);
            for (; s < H.GetDirectories().Length; s++)
            {
                DirectoryInfo se = new(HomeDirectory);
                for (; e < se.GetDirectories()[s].GetFiles().Length; e++)
                {
                    r++;
                    if (r >= ep)
                    {
                        return H.GetDirectories()[s].GetFiles()[e].FullName;
                    }
                }
            }
            return H.GetDirectories()[s-1].GetFiles()[e-1].FullName;
        }
        public ShowStatus Status
        {
            get
            {

                if (CuEpNo >= TotalEps) return ShowStatus.Complete;
                else if (CuEpNo > 0&CuEpNo<TotalEps) return ShowStatus.Ongoing;
                return ShowStatus.New;
            }
        }

    }
}
