namespace UProLogParserCUI
{
    using System.Collections.Generic;

    public interface ILogFileSearcher
    {
        IEnumerable<string> EnumerateFiles(string root);
    }
}
