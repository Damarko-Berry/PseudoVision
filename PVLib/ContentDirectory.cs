using System.Xml;
using System.Xml.Serialization;


namespace PVLib
{
    public abstract class ContentDirectory : PVObject
    {
        protected static string[] ValidExtensions
        {
            get
            {
                List<string> list = new List<string>([".mp4", ".avi", ".mov", ".mkv", ".flv", ".wmv", ".webm", ".mpeg", ".mpg", ".m4v"]);
                if (Settings.CurrentSettings.GetVideoExtensions != null)
                {
                    var SE = Settings.CurrentSettings.GetVideoExtensions;
                    for (int i = 0; i < SE.Length; i++)
                    {
                        list.Add(SE[i].Trim());
                    }
                }
                return [.. list];
            }
        }
        public static DirectoryType DDetector(DirectoryInfo directory)
        {
            List<FileInfo> surfacefiles = new List<FileInfo>();
            for (int i = 0; i < ValidExtensions.Length; i++)
            {
                surfacefiles.AddRange(directory.GetFiles("*" + ValidExtensions[i], SearchOption.TopDirectoryOnly));
            }
            if (surfacefiles.Count > 0)
            {
                return DirectoryType.Movie;
            }
            List<FileInfo> deepfiles = new List<FileInfo>();
            for (int i = 0; i < ValidExtensions.Length; i++)
            {
                deepfiles.AddRange(directory.GetFiles("*" + ValidExtensions[i], SearchOption.AllDirectories));
            }
            if (deepfiles.Count > 0 & directory.GetDirectories().Length > 0)
            {
                return DirectoryType.Show;
            }
            throw new Exception("Not a valid Directory");
        }

        public string HomeDirectory;
        public string GetHomeDirectory=> Settings.CurrentSettings.Portable? HomeDirectory.Replace(Directory.GetDirectoryRoot(HomeDirectory), Directory.GetDirectoryRoot(Directory.GetCurrentDirectory())):HomeDirectory;
        protected DirectoryInfo HomeDirectoryInfo => new DirectoryInfo(GetHomeDirectory);
        public DirectoryType dirtype => DDetector(HomeDirectoryInfo);
        internal abstract FileInfo[] Content { get; }
        public virtual int Length => Content.Length;
        public abstract TimeSpan Duration { get; }
        public abstract string NextEpisode();
        public static ContentDirectory Load(string path)
        {
            var doc = File.ReadAllText(path);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(doc);
            XmlNodeList c = xmlDoc.GetElementsByTagName("HomeDirectory");
            var CType = DDetector(new(c[0].InnerText));
            int atmpts = 0;
        Ser:
            try
            {
                XmlSerializer serializer = (CType == DirectoryType.Movie) ? new XmlSerializer(typeof(MovieDirectory)) : new XmlSerializer(typeof(Show));
                StreamReader sr = new StreamReader(path);
                ContentDirectory channel = (CType == DirectoryType.Movie) ? (MovieDirectory)serializer.Deserialize(sr) : (Show)serializer.Deserialize(sr);
                sr.Close();
                return channel;
            }
            catch (Exception ex)
            {
                CType = (CType == DirectoryType.Movie) ? DirectoryType.Movie : DirectoryType.Movie;
                atmpts++;
                if (atmpts <= 3)
                    goto Ser;
            }

            throw new Exception("OOOOOOOOOPS");
        }
    }
}
