using System.Xml.Serialization;

namespace PVLib
{
    public struct ChannelList
    {
        public List<string> Channels;
        public int length => Channels.Count;
        public string this[int index] => Channels[index];
        public void Add(string channle)
        {
            if (Channels == null) Channels = new();
            Channels.Add(channle);
        } 
        public static ChannelList operator +(ChannelList a, ChannelList b)
        {
            for (int i = 0; i < b.length; i++)
            {
                a.Add(b[i]);
            }
            return a;
        }
        public ChannelList(string channles)
        {
            Channels = new List<string>();
            XmlSerializer serializer = new XmlSerializer(typeof(ChannelList));
            StringReader stringReader = new StringReader(channles);
            var C = (ChannelList)serializer.Deserialize(stringReader);
            stringReader.Close();
            this += C;
        }
    }
}
