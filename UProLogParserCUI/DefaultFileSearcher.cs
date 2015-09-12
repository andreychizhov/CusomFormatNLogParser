using System.Collections.Generic;
using System.IO;

namespace UProLogParserCUI
{
    class DefaultFileSearcher : ILogFileSearcher
    {
        public IEnumerable<string> EnumerateFiles(string rootDir)
        {
            return Directory.EnumerateFiles(rootDir, "*.log");
        }
    }
}
