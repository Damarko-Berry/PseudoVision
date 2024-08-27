using WMPLib;

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
        public TimeSlot(string media, List<TimeSlot> slots)
        {
            Media = media;
            var player = new WindowsMediaPlayer();
            var clip = player.newMedia(media);
            Duration = TimeSpan.FromSeconds(clip.duration);
            if (slots.Count <= 0)
            {
                StartTime = DateTime.Today;
            }
            else
            {
                StartTime = slots[^1].EndTime;
            }
        }
        public TimeSlot(string media)
        {
            Media = media;
            var player = new WindowsMediaPlayer();
            var clip = player.newMedia(media);
            Duration = TimeSpan.FromSeconds(clip.duration);
            StartTime = DateTime.Now;
        }
        public TimeSlot(Rerun media, List<TimeSlot> slots)
        {
            Media = media.Media;

            Duration = media.Duration;
            if (slots.Count <= 0)
            {
                StartTime = DateTime.Today;
            }
            else
            {
                StartTime = slots[^1].EndTime;
            }
        }
    }
}
