﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoVision
{
    internal static class UPNP
    {
        public static DeviceType DeviceType = DeviceType.MediaServer;
        public static string DeviceTypeSchema => $"<deviceType>urn:schemas-upnp-org:device:{DeviceType}:1</deviceType>";
        public static int Major=1, Minor=5;
        static public string UniqueID = "45da98e2-34af-4f39-a620-3bb14e942f22";
        static public string DeviceName = "PseudoVision";
        static public string ModelName= "model 1";
        static public double ModelNumber= 1.12;
        static public string Manufacturer = "YAKWII";
        public static List<ServiceSchema> ServiceList = new List<ServiceSchema>()
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
        public static string ServiceListSchemas
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
        static public string ToString()
        {
            return $@"<?xml version='1.0'?>
<root xmlns='urn:schemas-upnp-org:device-1-0'>
  <specVersion>
    <major>{Major}</major>
    <minor>{Minor}</minor>
  </specVersion>
  <device>
    {DeviceTypeSchema}
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
        public const string CDS_XML = @"<?xml version=""1.0""?>
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
    }
    internal class ServiceSchema
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
