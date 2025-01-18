using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    public class Log
    {
        TimeOnly TimeStarted;
        List<LogEntry> logs = new();
        bool logging;
        public async void Cycle(string name)
        {
            if (logging) return;
            logging = true;
            Directory.CreateDirectory(Path.Combine(FileSystem.Root, "Logs",name, "Errors"));
            Directory.CreateDirectory(Path.Combine(FileSystem.Root, "Logs",name, "Message"));
            while (logging)
            {
                TimeStarted = TimeOnly.FromDateTime(DateTime.Now);
                //await Task.Delay(1000);
                await Task.Delay((1000 * 60) * 2);
                if (logs.Count > 0)
                {
                    var errors = logs.Where(x => x.messageType == MessageType.Error);
                    var messages = logs.Where(x => x.messageType == MessageType.Normal);
                    if (errors.Any())
                    {
                        var erors = string.Empty;
                        foreach ( var error in errors)
                        {
                            erors += error.message+"\n\n-----\n\n";
                        }
                        File.WriteAllText(Path.Combine(FileSystem.Root, "Logs", name, "Errors", $"{TimeStarted.Hour}.{TimeStarted.Minute}.{TimeStarted.Second}.log"), erors);
                    }
                    if (messages.Any())
                    {
                        var mesag = string.Empty;
                        foreach (var message in messages)
                        {
                            mesag += message.message + "\n\n-----\n\n";
                        }
                        File.WriteAllText(Path.Combine(FileSystem.Root, "Logs", name, "Message", $"{TimeStarted.Hour}.{TimeStarted.Minute}.{TimeStarted.Second}.log"), mesag);
                    }
                    logs.Clear();
                }
                else
                {
                    logging = false;
                    break;
                }
                
            }
        }

        public void writeMessage(string message)
        {
            if(!logging) return;
            logs.Add(new(message, MessageType.Normal));
        }
        
        public void writeError(string message)
        {
            if(!logging) return;
            logs.Add(new(message, MessageType.Error));
        }
        
        public void writeError(Exception message)
        {
            if(!logging) return;
            logs.Add(new(message.ToString(), MessageType.Error));
        }
    }
    struct LogEntry
    {
        public string message => $"{timeOnly}:\n{Message}";
        public MessageType messageType;
        TimeOnly timeOnly;
        string Message;
        public LogEntry(string message, MessageType messageTypes)
        {
            Message = message;
            messageType = messageTypes;
            timeOnly = TimeOnly.FromDateTime(DateTime.Now);
            Console.WriteLine(message);
        }
    }

    public class PVObject
    {
        public Log ConsoleLog = new Log();
        public static Log MainLog = new Log();
        public static TimeSpan GetMediaDuration(string media)
        {
            var ffmpegPath = FileSystem.FFMPEG;
            var startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{media}\"",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();

                var durationMatch = System.Text.RegularExpressions.Regex.Match(output, @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                if (durationMatch.Success)
                {
                    int hours = int.Parse(durationMatch.Groups[1].Value);
                    int minutes = int.Parse(durationMatch.Groups[2].Value);
                    int seconds = int.Parse(durationMatch.Groups[3].Value);
                    int milliseconds = int.Parse(durationMatch.Groups[4].Value) * 10;

                    return new TimeSpan(0, hours, minutes, seconds, milliseconds);
                }
                else
                {
                    throw new Exception("Could not determine media duration.");
                }
            }
        }

    }
}
