using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public struct Season
    {
        public DateTime Start;
        public DateTime End;
        public bool Durring(DateTime time)
        {
            DateTime st = Start;
            DateTime et = End;
            return ((time >= st) && (time < et));
        }
        public List<string> SeasonSpecials;

        public Season() { }
    }
}
