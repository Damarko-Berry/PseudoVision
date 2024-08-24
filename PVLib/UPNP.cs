using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
        public List<ISchedule> ScheduleList = new();
        public string contentDirectory()
        {
            var Srt = "<?xml version=\"1.0\"?>";
             
            return Srt;
        }
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
        public static string CDS_XML => @"<?xml version=""1.0""?>
<scpd xmlns=""urn:schemas-upnp-org:service-1-0"">
  <specVersion>
    <major>1</major>
    <minor>0</minor>
  </specVersion>
  <actionList>
    <action>
      <name>Browse</name>
      <argumentList>
        <argument>
          <name>ObjectID</name>
          <direction>in</direction>
          <relatedStateVariable>A_ARG_TYPE_ObjectID</relatedStateVariable>
        </argument>
        <argument>
          <name>BrowseFlag</name>
          <direction>in</direction>
          <relatedStateVariable>A_ARG_TYPE_BrowseFlag</relatedStateVariable>
        </argument>
        <argument>
          <name>Filter</name>
          <direction>in</direction>
          <relatedStateVariable>A_ARG_TYPE_Filter</relatedStateVariable>
        </argument>
        <argument>
          <name>StartingIndex</name>
          <direction>in</direction>
          <relatedStateVariable>A_ARG_TYPE_Index</relatedStateVariable>
        </argument>
        <argument>
          <name>RequestedCount</name>
          <direction>in</direction>
          <relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable>
        </argument>
        <argument>
          <name>SortCriteria</name>
          <direction>in</direction>
          <relatedStateVariable>A_ARG_TYPE_SortCriteria</relatedStateVariable>
        </argument>
        <argument>
          <name>Result</name>
          <direction>out</direction>
          <relatedStateVariable>A_ARG_TYPE_Result</relatedStateVariable>
        </argument>
        <argument>
          <name>NumberReturned</name>
          <direction>out</direction>
          <relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable>
        </argument>
        <argument>
          <name>TotalMatches</name>
          <direction>out</direction>
          <relatedStateVariable>A_ARG_TYPE_Count</relatedStateVariable>
        </argument>
        <argument>
          <name>UpdateID</name>
          <direction>out</direction>
          <relatedStateVariable>A_ARG_TYPE_UpdateID</relatedStateVariable>
        </argument>
      </argumentList>
    </action>
  </actionList>
  <serviceStateTable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_ObjectID</name>
      <dataType>string</dataType>
    </stateVariable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_BrowseFlag</name>
      <dataType>string</dataType>
      <allowedValueList>
        <allowedValue>BrowseMetadata</allowedValue>
        <allowedValue>BrowseDirectChildren</allowedValue>
      </allowedValueList>
    </stateVariable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_Filter</name>
      <dataType>string</dataType>
    </stateVariable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_Index</name>
      <dataType>ui4</dataType>
    </stateVariable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_Count</name>
      <dataType>ui4</dataType>
    </stateVariable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_SortCriteria</name>
      <dataType>string</dataType>
    </stateVariable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_Result</name>
      <dataType>string</dataType>
    </stateVariable>
    <stateVariable sendEvents=""no"">
      <name>A_ARG_TYPE_UpdateID</name>
      <dataType>ui4</dataType>
    </stateVariable>
  </serviceStateTable>
</scpd>
";
        public async void Start(string localIp, int port)
        {
            await Task.Delay(1000);
            Task.Run(()=>SendSsdpAnnouncements(localIp, port));
            Task.Run(()=>ListenForSsdpRequests(localIp, port));
        }
        async Task SendSsdpAnnouncements(string localIp, int port)
        {
            string ssdpNotifyTemplate = "NOTIFY * HTTP/1.1\r\n" +
                                        "HOST: 239.255.255.250:1900\r\n" +
                                        "CACHE-CONTROL: max-age=1800\r\n" +
                                        $"LOCATION: http://{localIp}:{port}/description.xml\r\n" +
                                        "NT: urn:schemas-upnp-org:device:MediaServer:1\r\n" +
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
                                              "SERVER: Custom/1.0 UPnP/1.0 DLNADOC/1.50\r\n" +
                                              "ST: urn:schemas-upnp-org:device:MediaServer:1\r\n" +
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

                    byte[] responseData = Encoding.UTF8.GetBytes(responseTemplate);
                    await client.SendAsync(responseData, responseData.Length, result.RemoteEndPoint);
                }
            }
        }
    }
    internal struct ServiceSchema
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
    public enum DeviceType { MediaServer, InternetGatewayDevice, lighting, reminder }
    public enum ServiceType { ContentDirectory, ConnectionManager }
}
