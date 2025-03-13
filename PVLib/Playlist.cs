namespace PVLib
{
    public class Playlist
    {
        List<Rerun> files;
        public Rerun this[int x] => files[x];
        public int Count => files.Count;
        PlaylistFormat format => Settings.CurrentSettings.playlistFormat;
        public Playlist(Schedule schedule)
        {
            files = new();
            for (int i = 0; i < schedule.slots.Count; i++)
            {
                files.Add(schedule.slots[i]);
            }
        }
        public void Add(Rerun rerun)
        {
            files.Add(rerun);
        }
        public Playlist()
        {
            files = new();
        }
        public Playlist(string path)
        {
            files = new();
            FileInfo fileInfo = new FileInfo(path);
            var form = EnumTranslator<PlaylistFormat>.fromString(fileInfo.Extension.Replace(".", string.Empty));
            switch (form)
            {
                case PlaylistFormat.m3u:
                    fromM3u(File.ReadAllText(fileInfo.FullName));
                    break;
                case PlaylistFormat.pls:
                    fromPls(File.ReadAllText(fileInfo.FullName));
                    break;
            }
        }

        public Playlist(string[] paths)
        {
            files = new();
            for (int i = 0; i < paths.Length; i++)
            {
                files.Add(new(paths[i]));
            }
        }
        #region m3u
        void fromM3u(string content)
        {
            var c = content.Split("\n");
            for (int i = 3; i < c.Length; i += 2)
            {
                if (i + 1 == c.Length)
                {
                    break;
                }
                var t = c[i];
                t = t.Replace("#EXTINF:", string.Empty);
                t = t.Split(",")[0];
                var med = c[i + 1];
                Rerun rerun = new Rerun();
                rerun.MediaLength.Second = int.Parse(t.Replace(",", string.Empty));
                rerun.Media = med;
                files.Add(rerun);
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
        #endregion
        #region pls
        void fromPls(string content)
        {
            var c = content.Split("\n");
            for (int i = 2; i < c.Length - 2; i += 3)
            {
                if (i + 2 == c.Length)
                {
                    break;
                }
                var med = c[i].Split("=")[1];
                var t = int.Parse(c[i + 1].Split("=")[1]);
                files.Add(new() { Media = med, MediaLength = new() { Second = t } });
            }
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
        #endregion
        public override string ToString()
        {
            return format switch
            {
                PlaylistFormat.m3u => ToM3u(),
                PlaylistFormat.pls => ToPls(),
                _ => throw new Exception("Invalid Selection")
            };
        }
    }
}
