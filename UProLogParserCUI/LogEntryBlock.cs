using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace UProLogParserCUI
{
    public class LogEntryBlock
    {
        private const string _exceptionPrefix = "|Exception: ";
        private const string _occuringDatePattern = @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{4})";

        public LogEntryBlock(string content, string logFilePath)
        {
            Content = content;
            LogFilePath = logFilePath;
        }

        public string Content { get; private set; }
        public string LogFilePath { get; private set; }

        public string GetExceptionName()
        {
            var firstSymbolIndex = Content.IndexOf(_exceptionPrefix) + _exceptionPrefix.Length;
            var afterLastSymbolIndex = Content.IndexOf(';', firstSymbolIndex);
            var name = Content.Substring(firstSymbolIndex, afterLastSymbolIndex - firstSymbolIndex);
            return name;
        }

        public string GetOccurenceDateString()
        {
            var matches = Regex.Matches(Content, _occuringDatePattern);
            if (matches.Count > 0)
            {
                var actualDate = matches.Cast<Match>().First().ToString();
                DateTime time;
                if (!DateTime.TryParse(actualDate, out time))
                {
                    throw new InvalidDataException("Incorect datetime string '" + actualDate + "'");
                }
                return actualDate;
            }
            throw new InvalidDataException(new StringBuilder().Append("Unable to find date and time in block: ").Append(Content).ToString());
        }
    }
}
