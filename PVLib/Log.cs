using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVLib
{
    internal class Log
    {
        public TimeOnly TimeStarted;
        List<LogEntry> logs = new();

        public async void Cycle(string name)
        {
            Directory.CreateDirectory(Path.Combine(FileSystem.Root, "Logs",name, "Errors"));
            Directory.CreateDirectory(Path.Combine(FileSystem.Root, "Logs",name, "Message"));
            while (true)
            {
                TimeStarted = TimeOnly.FromDateTime(DateTime.Now);
                await Task.Delay(1000 * 60 * 5);
                if (logs.Count > 0)
                {
                    var errors = logs.Where(x => x.MessageType == MessageType.Error);
                    var messages = logs.Where(x => x.MessageType == MessageType.Normal);
                    if (errors.Count() > 0)
                    {
                        File.WriteAllLines(Path.Combine(FileSystem.Root, "Logs", name, "Errors", $"{TimeStarted}.log"), errors.Select(x => x.message));
                    }
                    if (messages.Count() > 0)
                    {
                        File.WriteAllLines(Path.Combine(FileSystem.Root, "Logs", name, "Message", $"{TimeStarted}.log"), messages.Select(x => x.message));
                    }
                    logs.Clear();
                }
                
            }
        }
    }
    struct LogEntry
    {
        public string message;
        public MessageType MessageType;
        public LogEntry(string message, MessageType messageTypes)
        {
            this.message = message;
        }
    }
}
