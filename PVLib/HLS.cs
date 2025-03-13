using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class HLS: PVObject
    {
        TimeSpan offset;
        public bool isfinised;
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
        int staringIndex = 0;
        public int targetDuration
        {
            get
            {
                double[] TD= new double[Body.Count];
                for (int i = 0; i < Body.Count; i++)
                {
                    TD[i] = Body[i].duration.TotalSeconds;
                }
                return (int)TD.Average();
            }
        }
        public string Header => $"#EXTM3U\n#EXT-X-VERSION:3\n#EXT-X-START:TIME-OFFSET={offset.TotalSeconds},PRECISE=YES\n#EXT-X-TARGETDURATION:{targetDuration}";
        public static string Footer => "#EXT-X-ENDLIST";
        string FullBody
        {
            get
            {
                
                string body = string.Empty;
                for (int i = 0; i < Body.Count; i++)
                {
                    if (Body[i].path.Contains("seg0"))
                    {
                        body += $"#EXT-X-MEDIA-SEQUENCE:{staringIndex}\n";
                    }
                        
                    body += $"{Body[i]}\n";
                }
                return body;
            }
        }

        // for finished manifest
        public string AsMediaSegment(int MS)
        {
            string body = string.Empty;
            if (MS > 0)
            {
                body += "#EXT-X-DISCONTINUITY\n";
            }
            body += $"#EXT-X-MEDIA-SEQUENCE:{MS}\n";
            for (int i = 0; i < Body.Count; i++)
            {
                body += $"{Body[i]}\n";
            }
            return body;
        }
        // for the current manifest
        public string AsMediaSegment(int MS, segment segment)
        {
            bool SegmentFound = false;
            TimeSpan Buffer = new();
            string body = string.Empty;
            if (MS > 0)
            {
                body += "#EXT-X-DISCONTINUITY\n";
            }
            body += $"#EXT-X-MEDIA-SEQUENCE:{MS}\n";
            for (int i = 0; i < Body.Count; i++)
            {
                if (Body[i].Equals(segment))
                {
                    segment = Body[i];
                    SegmentFound = true;
                }
                if (SegmentFound)
                {
                    Buffer += Body[i].duration;
                    if (Buffer.TotalSeconds > 30)
                    {
                        Console.WriteLine($"Buffer is {Buffer.TotalSeconds}");
                        break;
                    }
                }
                body += $"{Body[i]}\n";
            }
            return body;
        }

        public static HLS operator +(HLS A, HLS B)
        {
            foreach (var segment in B.Body)
            {
                A.Body.Add(segment);
                A.PruneSegments();
            }
            return A;
        }
        public void merge(segment currentsegment, HLS B)
        {
            foreach (var segment in B.Body)
            {
                Body.Add(segment);
                PruneSegments(CurrentPlaybackPosition());
            }
            int CurrentPlaybackPosition()
            {
                if (currentsegment == null) return 10000; // Fallback limit if no playback info

                int currentIndex = Body.FindIndex(seg => seg.path == currentsegment.path);
                return Math.Max(currentIndex - 50, 500); // Keep 50 segments before and 500 after
            }
        }

        public void PruneSegments(int maxSegments = 5000)
        {
            while (Body.Count > maxSegments)
            {
                int removeCount =0;
                for (int i = 1; i < Body.Count; i++)
                {
                    if (Body[i].path.Contains("seg0"))
                    {
                        removeCount = i;
                        break;
                    }
                }
                staringIndex += removeCount;
                Body.RemoveRange(0, removeCount); // Remove oldest segments
                ConsoleLog.writeMessage($"Pruned {removeCount} old segments, keeping {maxSegments}.");
            }
        }

        public static HLS Parse(string text)
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
            A.isfinised = text.Contains("#EXT-X-ENDLIST");
            return A;
        }

        public void SetOffset(string segmentName)
        {
            TimeSpan timeSpan = new();
            for (int i = 0; i < Body.Count; i++)
            {
                if (Body[i].path == segmentName)
                {
                    
                    timeSpan -= Body[i].duration;
                    break;
                }
                timeSpan+=Body[i].duration;
            }
            offset = timeSpan;
            
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
            
            return $"{Header}\n{FullBody}\n{Footer}";
        }
        public string ToString(segment segment)
        {
            
            string body = string.Empty;
            bool SegmentFound = false;
            TimeSpan Buffer = new();
            
            int i = 0;
            for (i = 0; i < Body.Count; i++)
            {
                if (Body[i].Equals(segment))
                {
                    segment = Body[i];
                    SegmentFound = true;
                }
                if (SegmentFound)
                {
                    Buffer += Body[i].duration;
                    if (Buffer.TotalSeconds > 30)
                    {
                        Console.WriteLine($"Buffer is {Buffer.TotalSeconds}");
                        break;
                    }
                }
                if (Body[i].path.Contains("seg0"))
                {
                    if (i > 0)
                    {
                        body += "#EXT-X-DISCONTINUITY\n";
                    }

                    body += $"#EXT-X-MEDIA-SEQUENCE:{staringIndex+i}\n";
                }
                
                body += $"{Body[i]}\n";
            }
            return (i<Body.Count)?$"{Header}\n{body}\n": ToString();
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
