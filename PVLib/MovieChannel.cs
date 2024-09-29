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
        public MovieDirectory[] Shows => (MovieDirectory[])shows;
        public override void CreateNewSchedule(DateTime today)
        {
            throw new NotImplementedException();
        }
    }
}
