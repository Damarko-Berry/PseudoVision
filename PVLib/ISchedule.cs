using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public interface ISchedule
    {
        string Name { get; set; }
        public async void SendMedia(HttpListenerResponse client) { }
        public Channel_Type ScheduleType { get; }
        public string GetContent(int index, string ip, int port);
    }
}
