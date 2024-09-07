namespace PseudoVision
{
    internal class Playlist
    {
        List<TimeSlot> files;
        PlaylistFormat format;
        public Playlist(Schedule schedule, PlaylistFormat playlistFormat)
        {
            files = new();
            for (int i = 0; i < schedule.slots.Count; i++)
            {
                files.Add(schedule.slots[i]);
            }
            format = playlistFormat;
        }
        string ToM3u()
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

        string ToPls()
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

        public override string ToString()
        {
            return format switch
            {
                PlaylistFormat.m3u => ToM3u(),
                PlaylistFormat.pls => ToPls(),
                _=> throw new Exception("Invalid Selection")
            };
        }
    }
}
