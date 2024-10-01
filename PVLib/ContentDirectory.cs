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
        protected static string[] ValidExtentions => new[] { ".mp4", ".avi", ".mov", ".mkv", ".flv", ".wmv", ".webm", ".mpeg", ".mpg", ".m4v" };
        protected static bool isValid(FileInfo file) => ValidExtentions.Contains(file.Extension);
        public string HomeDirectory;
        protected DirectoryInfo HomeDirectoryInfo=> new DirectoryInfo(HomeDirectory);
        public abstract DirectoryType dirtype { get; }
        public abstract FileInfo[] Content {  get; }
        public virtual int Length => Content.Length;
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
