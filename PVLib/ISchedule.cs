using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PVLib
{
    public interface ISchedule
    {
        string Name { get; set; }
        [XmlIgnore]
        public Dictionary<string ,ISchedule> AllSchedules { get; set; }
        public async Task SendMedia(HttpListenerContext client) { }
        public async Task SendMedia(string Request, NetworkStream stream) { }
        public async Task StartCycle() { }
        public Schedule_Type ScheduleType { get; }
        public string GetContent(int index, string ip, int port);
    }
}
