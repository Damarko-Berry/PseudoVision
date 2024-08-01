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
        public async void SendMedia(HttpListenerResponse client) { }
        public Channel_Type ScheduleType { get; }
    }
}
