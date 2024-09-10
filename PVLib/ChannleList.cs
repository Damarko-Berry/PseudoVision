
using System.Xml.Serialization;

namespace PVLib
{
    public struct ChannleList
    {
        public List<string> Channles;
        public int length => Channles.Count;
        public string this[int index] => Channles[index];
        public void Add(string channle)
        {
            Channles.Add(channle);
        } 

        public ChannleList(string channles)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ChannleList));
            StringReader stringReader = new StringReader(channles);
            var C = (ChannleList)serializer.Deserialize(stringReader);
            stringReader.Close();
            Channles = C.Channles;
        }
    }
}
