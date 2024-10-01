using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class MovieChannel : Channel
    {
        public Channel_Type type = Channel_Type.Movies;
        public override Channel_Type channel_Type => type;
        public MovieDirectory[] Shows
        {
            get
            {
                var s = CTD;
                List<MovieDirectory> ret = new List<MovieDirectory>();
                for (int i = 0; i < s.Length; i++)
                    if (s[i].dirtype == DirectoryType.Movie) 
                        ret.Add((MovieDirectory)s[i]);
                return ret.ToArray();
            }
        }
        public override void CreateNewSchedule(DateTime today)
        {
            throw new NotImplementedException();
        }
    }
}
