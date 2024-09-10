using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Xml;
using System.Xml.Serialization;

namespace PVLib
{
    public abstract class Channel
    {
        public string ChannelName => new DirectoryInfo(HomeDirectory).Name;
        public string HomeDirectory;
        string dir
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    HomeDirectory = HomeDirectory.Replace("C:\\", "/mnt/c/");
                    return HomeDirectory.Replace('\\', '/');

                }
                return HomeDirectory;
            }
        }
        public string ShowDirectory => Path.Combine(dir,"Shows");
        public abstract Channel_Type channel_Type {get;}
        public Show[] shows
        {
            get
            {
                DirectoryInfo info = new(ShowDirectory);
                var allS = info.GetFiles();
                Show[] S = new Show[allS.Length];
                for (int i = 0; i < S.Length; i++)
                {
                    S[i] = SaveLoad<Show>.Load(allS[i].FullName);
                }
                return S;
            }
        }
        public void AddShow(Show newShow)
        {
            var shw = newShow.Clone();
            shw.Reset();
            SaveLoad<Show>.Save(shw, Path.Combine(ShowDirectory, new FileInfo(shw.HomeDirectory).Name+".shw"));
        }
        public abstract void CreateNewSchedule(DateTime today);
        public virtual void Cancel(string name)
        {
            File.Delete(Path.Combine(ShowDirectory,name+".shw"));
        }
        
        public Channel() { }

        public static Channel Load(string path)
        {
            var doc = File.ReadAllText(path);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(doc);
            XmlNodeList c = xmlDoc.GetElementsByTagName("Channel_Type");
            var CType = EnumTranslator.CT_fromString(c[0].InnerText);
            XmlSerializer serializer = (CType == Channel_Type.Binge_Like) ? new XmlSerializer(typeof(Binge_LikeChannel)) : new XmlSerializer(typeof(TV_LikeChannel));
            StreamReader sr = new StreamReader(path);
            Channel channel = (CType == Channel_Type.Binge_Like) ? (Binge_LikeChannel)serializer.Deserialize(sr):(TV_LikeChannel)serializer.Deserialize(sr);
            sr.Close();
            return channel;
        }
    }
}
