

using System.Net.Http.Headers;

namespace PVLib
{
    public struct TimeSlot
    {
        public string Media;
        public TimeSpan Duration;
        public Time StartTime;
        public DateTime EndTime
        {
            get
            {
                if (Media == string.Empty | Media == null)
                {
                    return DateTime.MinValue;
                }
                DateTime ET = StartTime;
                return ET + Duration;
            }
        }
        public bool Durring(DateTime time)
        {
            DateTime st = StartTime;
            DateTime et = EndTime;
            return ((time >= st) && (time < et));
        }

        public override bool Equals(object? obj)
        {
            return obj is TimeSlot slot &&
                   Media == slot.Media &&
                   Duration.Equals(slot.Duration) &&
                   EqualityComparer<Time>.Default.Equals(StartTime, slot.StartTime) &&
                   EndTime == slot.EndTime;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Media, Duration, StartTime, EndTime);
        }

        public TimeSlot(string media, List<TimeSlot> slots, DateTime ST)
        {
            Media = media;
            
            Duration = PVObject.GetMediaDuration(media);
            if (slots.Count <= 0)
            {
                StartTime = ST.Date;
            }
            else
            {
                StartTime = slots[^1].EndTime;
            }
        }
        public TimeSlot(string media)
        {
            Media = media;
            Duration = PVObject.GetMediaDuration(media);
            StartTime = DateTime.Now;
        }
        public TimeSlot(Rerun media, List<TimeSlot> slots, DateTime ST)
        {
            Media = media.Media;

            Duration = media.Duration;
            if (slots.Count <= 0)
            {
                StartTime = ST.Date;
            }
            else
            {
                StartTime = slots[^1].EndTime;
            }
        }
        public TimeSlot(string media, DateTime ST, TimeSpan fixedur)
        {
            Media = media;
            Duration = fixedur;
            StartTime = ST;
        }
        public static bool operator ==(TimeSlot a, TimeSlot b)
        {
            return a.Media == b.Media & a.EndTime == b.EndTime;
        }

        public static bool operator !=(TimeSlot a, TimeSlot b)
        {
            return !(a == b);
        }
    }
}
