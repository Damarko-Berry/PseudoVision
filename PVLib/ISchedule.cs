﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public interface ISchedule
    {
        string Name { get; set; }
        public async Task SendMedia(HttpListenerContext client) { }
        public async Task SendMedia(string Request, NetworkStream stream) { }
        public Schedule_Type ScheduleType { get; }
        public string GetContent(int index, string ip, int port);
    }
}
