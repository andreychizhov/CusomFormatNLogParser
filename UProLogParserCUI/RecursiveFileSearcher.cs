using System;
using System.Collections.Generic;
using System.IO;

namespace UProLogParserCUI
{
    class RecursiveFileSearcher : ILogFileSearcher
    {
        public IEnumerable<string> EnumerateFiles(string rootDir)
        {
            return Directory.EnumerateFiles(rootDir, "*.log", SearchOption.AllDirectories);
        }
    }
}
