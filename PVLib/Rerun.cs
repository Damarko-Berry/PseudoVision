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
        public TimeSpan Duration => MediaLength;
        public Rerun(TimeSlot slot)
        {
            Media = slot.Media;
            MediaLength = slot.Duration;
        }
        public static implicit operator Rerun(TimeSlot timeSlot)
        {
            return new Rerun(timeSlot);
        }
    }
}
