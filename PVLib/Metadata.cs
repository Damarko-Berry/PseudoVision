using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Text.Json;

namespace PVLib
{
    public struct Metadata
    {
        [XmlElement(ElementName = "Genre")]
        public Genre Genre { get; set; }

        [XmlElement(ElementName = "Style")]
        public Style Style { get; set; }

        [XmlElement(ElementName = "Age")]
        public Demographic TargetAge { get; set; }

        [XmlElement(ElementName = "Gender")]
        public GenderDemographic TargetGender { get; set; }

        [XmlArray(ElementName = "Studios")]
        public string[] Studios { get; set; }

        [XmlArray(ElementName = "Directors")]
        public string[] Directors { get; set; }

        [XmlElement(ElementName = "Universe")]
        public string Universe { get; set; }

        [XmlElement(ElementName = "PremiereDate")]
        public DateTime PremiereDate { get; set; }

        [XmlElement(ElementName = "EpDate")]
        public EpisodeStyle EpStyle { get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        static public Metadata FromJson(string json)
        {
            return JsonSerializer.Deserialize<Metadata>(json);
        }

        public static Metadata DefaultMetadata()
        {
            return new Metadata
            {
                Genre = Genre.Any,
                Style = Style.Any,
                TargetAge = Demographic.Any,
                TargetGender = GenderDemographic.Any,
                Directors = new string[] { "Various" },
                Studios = new string[] { "Various" },
                PremiereDate = DateTime.MinValue,
                EpStyle = EpisodeStyle.Any,
            };
        }
    }
}
