namespace PseudoVision
{
    internal class Playlist
    {
        List<TimeSlot> files;
        public Playlist(Schedule schedule)
        {
            files = new();
            for (int i = 0; i < schedule.slots.Count; i++)
            {
                files.Add(schedule.slots[i]);
            }
        }
        public string ToM3u()
        {
            var Da = DateTime.Now;
            var M = Da.Date.Month;
            var D = Da.Date.Day;
            var Y = Da.Date.Year;
            var sb = $"#EXTM3U\r\n#{M}.{D}.{Y}.m3u8\n\n";
            for (int i = 0; i < files.Count; i++)
            {
                sb += $"#EXTINF:{(int)files[i].Duration.TotalSeconds},{new FileInfo(files[i].Media).Name}\n";
                sb += $"{files[i].Media}\n";
            }
            return sb;
        }

        public string ToPls()
        {
            var sb = "[playlist]\n\n";
            for (int i = 0; i < files.Count; i++)
            {
                sb += $"File{i + 1}={files[i].Media}\n";
                sb += $"Length{i + 1}={(int)files[i].Duration.TotalSeconds}\n\n";
            }
            sb += $"NumberOfEntries={files.Count}\n";
            sb += $"Version=2";

            return sb;
        }
    }
}
