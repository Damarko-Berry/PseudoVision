using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;

namespace PVLib
{
    public abstract class ContentDirectory
    {
        public string HomeDirectory;
        public abstract DirectoryType dirtype {  get; }
        internal string dir
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
        public abstract string NextEpisode();
        public static ContentDirectory Load(string path)
        {
            var doc = File.ReadAllText(path);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(doc);
            XmlNodeList c = xmlDoc.GetElementsByTagName("directoryType");
            var CType = EnumTranslator<DirectoryType>.fromString(c[0].InnerText);
            XmlSerializer serializer = (CType == DirectoryType.Movie) ? new XmlSerializer(typeof(MovieDirectory)) : new XmlSerializer(typeof(Show));
            StreamReader sr = new StreamReader(path);
            ContentDirectory channel = (CType == DirectoryType.Movie) ? (MovieDirectory)serializer.Deserialize(sr) : (Show)serializer.Deserialize(sr);
            sr.Close();
            return channel;
        }
    }
}
