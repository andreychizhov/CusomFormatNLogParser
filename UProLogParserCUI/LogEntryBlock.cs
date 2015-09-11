using System;

namespace UProLogParserCUI
{
    public class LogEntryBlock
    {
        public LogEntryBlock(string content, string logFilePath, string lastOccurenceDate)
        {
            Content = content;
            LogFilePath = logFilePath;
            OccurenceTime = lastOccurenceDate;
        }

        public string Content { get; private set; }
        public string LogFilePath { get; private set; }
        public string OccurenceTime { get; private set; }
    }
}
