using System.Xml.Serialization;

namespace PVLib
{
    public class ChannelList
    {
        public List<ChannelRef> Channels= new();
        public int length => Channels.Count;
        public ChannelRef this[int index] => Channels[index];
        public void Add(Channel chan)
        {
            ChannelRef channle = new(chan);
            if (Channels == null) Channels = new();
            Channels.Add(channle);
        }  
        public void Add(ChannelRef chan)
        {
            if (Channels == null) Channels = new();
            Channels.Add(chan);
        } 
        public static ChannelList operator +(ChannelList a, ChannelList b)
        {
            for (int i = 0; i < b.length; i++)
            {
                a.Channels.Add(b[i]);
            }
            return a;
        }
        public ChannelList(string channles)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ChannelList));
            StringReader stringReader = new StringReader(channles);
            var C = (ChannelList)serializer.Deserialize(stringReader);
            stringReader.Close();
            this.Channels.AddRange(C.Channels);
        }
        public ChannelList() { }
    }

    public struct ChannelRef
    {
        public string name;
        public string IP;
        public int Port;

        public ChannelRef(Channel chan)
        {
            name = chan.ChannelName;
            IP = Settings.CurrentSettings.IP;
            Port = Settings.CurrentSettings.Port;
        }

        public ChannelRef(string name, string IP, int Port)
        {
            this.name = name;
            this.IP = IP;
            this.Port = Port;
        }

        public string Link => $"http://{IP}:{Port}/live/{name}";
        
        public Channel_Type type;
    }
}
