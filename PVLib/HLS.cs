using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class HLS
    {
        public TimeSpan offset;
        public List<segment> Body = new();

        public static HLS operator +(HLS A, HLS B)
        {
            A.Body.AddRange(B.Body);
            return A;
        }

        public static HLS Load(string text)
        {
            HLS A = new HLS();
            var m3u = text.Split("\r\n");
            for (int i = 0; i < m3u.Length; i++)
            {
                if (m3u[i].Contains("#EXTINF:"))
                {
                    var timeSegment = m3u[i].Split(':')[1];
                    var second = int.Parse(timeSegment.Split('.')[0]);
                    var millisecons = int.Parse(timeSegment.Split('.')[1].Replace(",", string.Empty).Replace(".", string.Empty));
                    i++;
                    A.Body.Add(new(m3u[i], new(0, 0, 0, second, 0, millisecons)));
                }
                else if (m3u[i].Contains("#EXT-X-START:TIMEOFFSET"))
                {
                    var time = m3u[i].Split('=')[1];
                    var second = int.Parse(time.Split('.')[0]);
                    var millisecons = int.Parse(time.Split('.')[1].Replace(",", string.Empty).Replace(".", string.Empty));
                    A.offset = new(0, 0, 0, second, 0, millisecons);
                }
            }
            return A;
        }

        public void SetOffset(string segmentName)
        {
            TimeSpan timeSpan = new();
            for (int i = 0; i < Body.Count; i++)
            {
                if (Body[i].path == segmentName)
                {
                    break;
                }
                timeSpan.Add(Body[i].duration);
            }
            offset = timeSpan;
        }

        public override string ToString()
        {
            string header = $"#EXTM3U\r\n#EXT-X-START:TIMEOFFSET={offset.TotalSeconds}\r\n#EXT-X-VERSION:3\r\n#EXT-X-TARGETDURATION:12\r\n#EXT-X-MEDIA-SEQUENCE:0";
            string body = string.Empty;
            for (int i = 0; i < Body.Count; i++)
            {
                body += $"{Body[i]}\n\r";
            }
            return $"{header}\r\n{body}";
        }
    }

    public struct segment
    {
        public string path;
        public TimeSpan duration;
        public segment(string path, TimeSpan duration)
        {
            this.path = path;
            this.duration = duration;
        }
        public override string ToString()
        {
            return $"#EXTINF:{duration.TotalSeconds}\n\r{path}";
        }
    }
}
