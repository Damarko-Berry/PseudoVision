using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class ShowRef
    {
        public string Directory;
        public string name => new DirectoryInfo(Directory).Name;
        public DayOfWeek DayToPlay;
        public ShowRef(string directory, DayOfWeek dayToPlay)
        {
            Directory = directory;
            DayToPlay = dayToPlay;
        }
        public ShowRef() { }
    }
}
