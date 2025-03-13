using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class ShowRef
    {
        
        public List<string> Directory = new();
        public int CI;
        public void Next()
        {
            if (SaveLoad<Show>.Load(Directory[CI]).Status == ShowStatus.Complete)
            {
                Remove(Directory[CI]);
            }
            CI++;
            if (CI >= Directory.Count)
            {
                CI = 0;
            }
        }
        public void Remove(string directory)
        {
            int NC = Directory.Count-1;
            Directory.Remove(directory);
            if (CI >= NC)
            {
                CI = 0;
            }
        }
        public string name => new DirectoryInfo(Directory[CI]).Name;
        public DayOfWeek DayToPlay;
        public ShowRef() { }
    }

}
