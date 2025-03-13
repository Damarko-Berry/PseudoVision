using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public struct Rerun
    {
        public string Media;
        public Time MediaLength;
        public TimeSpan Duration => (TimeSpan)MediaLength;
        public Rerun(TimeSlot slot)
        {
            Media = slot.Media;
            MediaLength = slot.Duration;
        }
        public Rerun(string media)
        {
            Media = media;
            MediaLength = PVObject.GetMediaDuration(media);
        }
        public static implicit operator Rerun(TimeSlot timeSlot)
        {
            return new Rerun(timeSlot);
        }
    }
}
