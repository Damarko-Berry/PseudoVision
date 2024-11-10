using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Xml;
using System.Xml.Serialization;

namespace PVLib
{
    public abstract class Channel
    {
        public const double FULLDAY = 23.9;
        public string ChannelName => new DirectoryInfo(HomeDirectory).Name;
        public string HomeDirectory;
        public string ShowDirectory => Path.Combine(HomeDirectory,"Shows");
        public abstract Channel_Type channel_Type {get;}
        public ContentDirectory[] CTD
        {
            get
            {
                DirectoryInfo info = new(ShowDirectory);
                var allS = info.GetFiles();
                ContentDirectory[] S = new ContentDirectory[allS.Length];
                for (int i = 0; i < S.Length; i++)
                {
                    S[i] = ContentDirectory.Load(allS[i].FullName);
                }
                return S;
            }
        }
        public void AddShow(Show newShow)
        {
            if(channel_Type == Channel_Type.Movies) return;
            var shw = newShow.Clone();
            shw.Reset();
            SaveLoad<Show>.Save(shw, Path.Combine(ShowDirectory, new FileInfo(shw.HomeDirectory).Name+"."+FileSystem.ShowEXT));
        }
        public abstract void CreateNewSchedule(DateTime today);
        public virtual void Cancel(string name)
        {
            File.Delete(Path.Combine(ShowDirectory,name+"."+FileSystem.ShowEXT));
        }
        public abstract bool isSupported(DirectoryType type);
        public Channel() { }

        public static Channel Load(string path)
        {
            var doc = File.ReadAllText(path);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(doc);
            XmlNodeList c = xmlDoc.GetElementsByTagName("Channel_Type");
            var CType = EnumTranslator<Channel_Type>.fromString(c[0].InnerText);
            XmlSerializer serializer = CType switch
            {
                Channel_Type.Binge_Like => new XmlSerializer(typeof(Binge_LikeChannel)),
                Channel_Type.TV_Like => new XmlSerializer(typeof(TV_LikeChannel)),
                _=> new XmlSerializer(typeof(MovieChannel))
            };
            StreamReader sr = new StreamReader(path);
            
            Channel channel = CType switch
            {
                Channel_Type.TV_Like => (TV_LikeChannel)serializer.Deserialize(sr),
                Channel_Type.Binge_Like => (Binge_LikeChannel)serializer.Deserialize(sr),
                _=> (MovieChannel)serializer.Deserialize(sr)
            };
            sr.Close();
            return channel;
        }
        public bool ScheduleExists(DateTime today)
        {
            var M = today.Date.Month;
            var D = today.Date.Day;
            var Y = today.Date.Year;
            return File.Exists(Path.Combine(FileSystem.ChanSchedules(ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}"));
        }
        public void SaveSchedule<T>(T schedule, DateTime today)
        {
            var M = today.Date.Month;
            var D = today.Date.Day;
            var Y = today.Date.Year;
            Directory.CreateDirectory(FileSystem.ChanSchedules(ChannelName));
            SaveLoad<T>.Save(schedule, Path.Combine(FileSystem.ChanSchedules(ChannelName), $"{M}.{D}.{Y}.{FileSystem.ScheduleEXT}"));
        }
    }
}
