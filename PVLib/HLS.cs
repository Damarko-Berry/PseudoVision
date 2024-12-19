using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class HLS
    {
        public TimeSpan offset;
        public TimeSpan Length 
        { 
            get
            {
                TimeSpan time = new();
                for (int i = 0; i < Body.Count; i++)
                {
                    time+=Body[i].duration;
                }
                
                return time;
            } 
        }
        public List<segment> Body = new();

        public static HLS operator +(HLS A, HLS B)
        {
            A.Body.AddRange(B.Body);
            return A;
        }

        public static HLS Load(string text)
        {
            HLS A = new HLS();
            var m3u = text.Split("\n");
            for (int i = 0; i < m3u.Length; i++)
            {
                var line = m3u[i];
                if (line.Contains("#EXTINF:"))
                {
                    var timeSegment = line.Split(':')[1];
                    var second = int.Parse(timeSegment.Split('.')[0]);
                    var millisecons = int.Parse(timeSegment.Split('.')[1].Replace(",", string.Empty).Replace(".", string.Empty));
                    i++;
                    A.Body.Add(new(m3u[i], new(0, 0, 0, second, 0, millisecons)));
                }
                else if (line.Contains("#EXT-X-START:TIMEOFFSET"))
                {
                    var time = line.Split('=')[1];
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
                    Console.WriteLine(segmentName);
                    timeSpan -= Body[i].duration;
                    break;
                }
                timeSpan+=Body[i].duration;
            }
            offset = timeSpan;
            Console.WriteLine(offset);
        }
        public segment NextSegment(segment current)
        {
            try
            {
                for (int i = 0; i < Body.Count; i++)
                {
                    if (Body[i].Equals(current))
                    {
                        int j = i+1;
                        var NS = Body[j];
                        return NS;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return null;
        }
        public segment GetSegment(TimeSpan timeSpan)
        {
            int i = 0;
            TimeSpan TP= new();
            for (i = 0; i < Body.Count; i++)
            {
                if(TP>=timeSpan) break;
                TP += Body[i].duration;
            }
            i--;
            if (i < 0) i = 0;
            return Body[i];
        }
        public int Segmentsleft(segment current)
        {
            for (int i = 0; i < Body.Count; i++)
            {
                var same = Body[i].Equals(current);
                if (same)
                {
                    int left = Body.Count - i;
                    return left;
                }
            }
            return 0;
        }
        public override string ToString()
        {
            string header = $"#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-START:TIME-OFFSET={offset.TotalSeconds},PRECISE=YES\n#EXT-X-TARGETDURATION:12\n#EXT-X-MEDIA-SEQUENCE:0";
            string body = string.Empty;
            for (int i = 0; i < Body.Count; i++)
            {
                body += $"{Body[i]}\n";
            }
            return $"{header}\n{body}\n#EXT-X-ENDLIST";
        }
    }

    public class segment
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
            return $"#EXTINF:{duration.TotalSeconds}\n{path}";
        }
        public bool Equals(segment other)
        {
            bool samepath = path == other.path;
            bool sameduration = duration == other.duration;
            return samepath & sameduration;
        }
        
    }
}
