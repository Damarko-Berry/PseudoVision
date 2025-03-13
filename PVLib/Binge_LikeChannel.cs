using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class Binge_LikeChannel : Channel
    {
        public bool Boomeranging;
        public Show[] Shows
        {
            get
            {
                List<Show> list = new List<Show>();
                var cd = CTD;
                for (int i = 0; i < cd.Length; i++)
                {
                    if (cd[i].dirtype == DirectoryType.Show) list.Add((Show)cd[i]);
                }
                return list.ToArray();
            }
        }
        public string NextChan;
        public Channel_Type Channel_Type = Channel_Type.Binge_Like;
        public override Channel_Type channel_Type => Channel_Type;
        public override void CreateNewSchedule(DateTime today)
        {
            if (ScheduleExists(today))
            {
                return;
            }
            CheckForFin();
            if (Shows.Length <= 0) return;
            ShowList showList = new(new(ShowDirectory));
            if (Live) showList.live = true;
            SaveSchedule(showList, today);
            
        }
        void CheckForFin()
        {
            var s = CTD;
            for (int i = 0; i < s.Length; i++)
            {
                var sh = (Show)s[i];
                if (sh.Status == ShowStatus.Complete) OnFinished(sh);
            }
        }
        void OnFinished(Show show)
        {
            Cancel(new FileInfo(show.HomeDirectory).Name);
            if(Boomeranging)
            {
                Channel NC = Load(NextChan);
                NC.AddShow(show);
            }
        }

        public override bool isSupported(DirectoryType type) => type == DirectoryType.Show;

        public override TimeSlot CreateSlot(DateTime now)
        {
            throw new NotImplementedException();
        }
    }
}
