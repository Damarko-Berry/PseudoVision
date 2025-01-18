using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PVLib
{
    public interface ISchedule
    {
        string Name { get; set; }
        [XmlIgnore]
        public static Dictionary<string, ISchedule> AllSchedules = new();
        public async Task SendMedia(HttpListenerContext client) { }
        public async Task SendMedia(string Request, NetworkStream stream) { }
        public async Task StartCycle() { }
        public Schedule_Type ScheduleType { get; }
        public string GetContent(int index, string ip, int port);
        public static List<SubtitleStream> GetSubtitleStreams(string filePath)
        {
            List<SubtitleStream> subtitleStreams = new();

            var ffprobePath = @"ffmpeg/ffprobe"; // Path to ffprobe executable
            var arguments = $"-v error -select_streams s -show_entries stream=index,codec_name:stream_tags=language -of json \"{filePath}\"";

            ProcessStartInfo startInfo = new()
            {
                FileName = ffprobePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            using var reader = process.StandardOutput;

            var output = reader.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                // Parse the JSON output
                var jsonDoc = JsonDocument.Parse(output);
                foreach (var stream in jsonDoc.RootElement.GetProperty("streams").EnumerateArray())
                {
                    subtitleStreams.Add(new SubtitleStream
                    {
                        Index = stream.GetProperty("index").GetInt32(),
                        CodecName = stream.GetProperty("codec_name").GetString(),
                        Language = stream.TryGetProperty("tags", out var tags) && tags.TryGetProperty("language", out var lang)
                            ? lang.GetString()
                            : "und" // "und" for undefined
                    });
                }
            }

            return subtitleStreams;
        }
    }
}
