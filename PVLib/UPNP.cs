﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PVLib
{
    public class UPNP
    {
        public static UPNP Default => new UPNP()
        {
            DeviceName = "PseudoVision",
            Manufacturer = "YAKWII",
            ModelName = "model 1",
            ModelNumber = 0,
            UniqueID = "45da98e2-34af-4f39-a620-3bb14e942f22",
            Major = 0, Minor = 1,
        };
        public int Major, Minor;
        public string UniqueID;
        public string DeviceName ;
        public string ModelName;
        public double ModelNumber;
        public string Manufacturer;
        static public int Update {  get; set; }
        List<ServiceSchema> ServiceList => new List<ServiceSchema>()
        {
            new ServiceSchema()
            {
                ServiceType = ServiceType.ContentDirectory,
                SCPDURL ="cds.xml",
                controlURL = "media",
                eventSubURL = "events"
            },
            new ServiceSchema()
            {
                ServiceType = ServiceType.ConnectionManager,
                SCPDURL ="cms.xml",
                controlURL = "control",
                eventSubURL = "events"
            },
        };
        [XmlIgnore]
        public List<ISchedule> ScheduleList = new();
       
        string ServiceListSchemas
        {
            get
            {
                if(ServiceList.Count == 0) return string.Empty;
                var Sch = "<serviceList>\n  ";
                for (int i = 0; i < ServiceList.Count; i++)
                {
                    Sch += ServiceList[i].ToString()+"\n    ";
                }
                Sch += @"</serviceList>"; 
                return Sch;
            }
        }
        public override string ToString()
        {
            return $@"<?xml version='1.0'?>
<root xmlns='urn:schemas-upnp-org:device-1-0'>
  <specVersion>
    <major>{Major}</major>
    <minor>{Minor}</minor>
  </specVersion>
  <device>
    <deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>
    <friendlyName>{DeviceName}</friendlyName>
    <manufacturer>{Manufacturer}</manufacturer>
    <modelName>{ModelName}</modelName>
    <modelNumber>{ModelNumber}</modelNumber>
    <UDN>uuid:{UniqueID}</UDN>
    <iconList>
      <icon>
        <mimetype>image/png</mimetype>
        <width>48</width>
        <height>48</height>
        <depth>24</depth>
        <url>/image/tv.png</url>
      </icon>
    </iconList>
    {ServiceListSchemas}
  </device>
</root>";
        }
        
        public async void Start(string localIp, int port)
        {
            await Task.Delay(1000);
            Task.Run(()=>ListenForSsdpRequests(localIp, port));
            Task.Run(()=>SendSsdpAnnouncements(localIp, port));
        }
        public string Media(ISchedule[] Medias, string IP, int port)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ChannelList));
            StringWriter sw = new StringWriter();
            ChannelList list = new();
            for (int i = 0; i < Medias.Length; i++)
            {
                list.Add($"http://{IP}:{port}/live/{Medias[i].Name}");
            }
            xmlSerializer.Serialize(sw, list);
            return sw.ToString();
        }
        async Task SendSsdpAnnouncements(string localIp, int port)
        {
            string ssdpNotifyTemplate = "NOTIFY * HTTP/1.1\r\n" +
                                        "HOST: 239.255.255.250:1900\r\n" +
                                        "CACHE-CONTROL: max-age=1800\r\n" +
                                        $"LOCATION: http://{localIp}:{port}/description.xml\r\n" +
                                        $"CHANNELS: http://{localIp}:{port}/media\r\n" +
                                        "NT: urn:PseudoVision:schemas-upnp-org:MediaServer:1\r\n" +
                                        "NTS: ssdp:all\r\n" +
                                        "SERVER: Custom/1.0 UPnP/1.0 DLNADOC/1.50\r\n" +
                                        $"USN: uuid:{UniqueID}::urn:schemas-upnp-org:device:MediaServer:1\r\n" +
                                        "\r\n";

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
            UdpClient client = new UdpClient();
            byte[] buffer = Encoding.UTF8.GetBytes(ssdpNotifyTemplate);

            while (true)
            {
                Console.WriteLine("ssdp message sent");
                client.Send(buffer, buffer.Length, endPoint);
                await Task.Delay(1000 * 30); // Send every 30 seconds
            }
        }

        async Task ListenForSsdpRequests(string localIp, int port)
        {
            UdpClient client = new UdpClient();
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 1900);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;
            client.Client.Bind(localEndPoint);
            client.JoinMulticastGroup(IPAddress.Parse("239.255.255.250"));

            while (true)
            {
                UdpReceiveResult result = await client.ReceiveAsync();
                string request = Encoding.UTF8.GetString(result.Buffer);

                if (request.Contains("M-SEARCH") && request.Contains("ssdp:discover"))
                {
                    string responseTemplate = !request.Contains("upnp:rootdevice") ? $"HTTP/1.1 200 OK\r\n" +
                                              "CACHE-CONTROL: max-age=1800\r\n" +
                                              $"DATE: {DateTime.UtcNow.ToString("r")}\r\n" +
                                              "EXT:\r\n" +
                                              $"LOCATION: http://{localIp}:{port}/description.xml\r\n" +
                                              $"CHANNELS: http://{localIp}:{port}/media\r\n" +
                                              "SERVER: Custom/1.0 UPnP/1.0 DLNADOC/1.50\r\n" +
                                              "ST: urn:PseudoVision:device:MediaServer:1\r\n" +
                                              $"USN: uuid:{UniqueID}::urn:schemas-upnp-org:device:MediaServer:1\r\n" +
                                              "\r\n" :
                                              $"HTTP/1.1 200 OK\r\n" +
                                              "CACHE-CONTROL: max-age=1800\r\n" +
                                              $"DATE: {DateTime.UtcNow.ToString("r")}\r\n" +
                                              "EXT:\r\n" +
                                              $"LOCATION: http://{localIp}:{port}/description.xml\r\n" +
                                              "SERVER: Custom/1.0 UPnP/1.0 DLNADOC/1.50\r\n" +
                                              "ST: upnp:rootdevice\r\n" +
                                              $"USN: uuid:{UniqueID}::upnp:rootdevice\r\n" +
                                              "\r\n";
                    if (request.Contains("upnp:rootdevice"))
                    {
                        Console.WriteLine("ROOT");
                    }
                    byte[] responseData = Encoding.UTF8.GetBytes(responseTemplate);
                    await client.SendAsync(responseData, responseData.Length, result.RemoteEndPoint);
                }
            }
        }
        UPNP() { }
    }
    public struct ServiceSchema
    {
        public ServiceType ServiceType;
        public string SCPDURL;
        public string controlURL;
        public string eventSubURL;

        public override string ToString()
        {
            return @$"<service>
        <serviceType>urn:schemas-upnp-org:service:{ServiceType}:1</serviceType>
        <serviceId>urn:upnp-org:serviceId:{ServiceType}</serviceId>
        <SCPDURL>/{SCPDURL}</SCPDURL>
        <controlURL>/{controlURL}</controlURL>
        <eventSubURL>/{eventSubURL}</eventSubURL>
    </service>";
        }
    }
   
}
