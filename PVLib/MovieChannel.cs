using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class MovieChannel : Channel
    {
        Channel_Type type;
        public override Channel_Type channel_Type => type;

        public override void CreateNewSchedule(DateTime today)
        {
            throw new NotImplementedException();
        }
    }
}
