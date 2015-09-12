using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UProLogParserCUI
{
    public sealed class LogProcessor
    {
        private ILogFileSearcher _fileSearcher;

        public LogProcessor(ILogFileSearcher fileSearcher)
        {
            _fileSearcher = fileSearcher;
        }

        private static readonly string[] _exeptions = new[]
        {
            "Exception: DbEntityValidationException", "Exception: EnrichException", "Exception: InvalidCalculationRequestException"
        };
        private const string _errorDataPresenceMarker = "ErrorData";
        private const string _occuringDatePattern = @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{4})";

        public void Process(IEnumerable<string> paths, string[] productMarkers, string outputFilename = "output")
        {
            var c = paths.
                SelectMany(p => _fileSearcher.EnumerateFiles(p))
                .Select(p => ReadFileByBlock(p))
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithMergeOptions(ParallelMergeOptions.Default)
                .MapReduce(
                    src =>
                        src
                        .Where(x => productMarkers.Any(m => x.Content.Contains(m)) && _exeptions.Any(ex => x.Content.Contains(ex)) && x.Content.Contains(_errorDataPresenceMarker))
                        .Select(block => new { Block = block, Info = GetTextByLine(block.Content).FirstOrDefault(line => line.StartsWith("AdditionalInfo")) })
                        .Select(s => new { Block = s.Block, Info = JsonConvert.DeserializeObject<AdditionalInfo>(CleanJsonObjectString(s.Info)) })
                        .SelectMany(ai => ai.Info.ErrorData.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries), (p, m) => new { Data = m, FileName = p.Block.LogFilePath, Time = p.Block.GetOccurenceDateString(), ExName = p.Block.GetExceptionName() }),
                    s => s.Data,
                    g => new[] { new { Error = g.Key, Count = g.Count(), DataObj = (from obj in g orderby DateTime.Parse(obj.Time) descending select obj).First() } })
                .Where(s => !string.IsNullOrWhiteSpace(s.Error))
                .OrderByDescending(s => s.Count);

            using (var fs = new FileStream(".\\" + outputFilename + ".txt", FileMode.Create))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                foreach (var item in c)
                {
                    sw.WriteLine("{0};{1};{2};{3};{4}", item.Error.Trim(), item.Count, item.DataObj.Time, item.DataObj.FileName, item.DataObj.ExName);
                }
            }
        }

        private static IEnumerable<string> GetTextByLine(string textBlock)
        {
            if (textBlock == null)
                return Enumerable.Empty<string>();

            return GetTextByLineImpl(textBlock);
        }

        private static IEnumerable<string> GetTextByLineImpl(string textBlock)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(textBlock)))
            using (var sr = new StreamReader(ms, true))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        private static string CleanJsonObjectString(string dirtyString)
        {
            int indexOfFirstCurlyBracket = dirtyString.IndexOf('{');
            return dirtyString.Substring(indexOfFirstCurlyBracket);
        }

        private static IEnumerable<LogEntryBlock> ReadFileByBlock(string path)
        {
            var e = File.ReadLines(path).GetEnumerator();
            if (!e.MoveNext()) return Enumerable.Empty<LogEntryBlock>();
            return ReadFileByBlockImpl(e, path);
        }

        private static IEnumerable<LogEntryBlock> ReadFileByBlockImpl(IEnumerator<string> linesEnum, string path)
        {
            var e = linesEnum;
            var block = new StringBuilder();
            string content;

            while (true)
            {
                string line = e.Current;
                if (Regex.Matches(line, _occuringDatePattern).Count == 0)
                {
                    block.AppendLine(line);
                }
                else
                {
                    content = block.ToString();
                    block = block.Clear();
                    if (!String.IsNullOrWhiteSpace(content))
                    {
                        block.AppendLine(line);
                        yield return new LogEntryBlock(content, path);
                    }
                    else
                    {
                        block.AppendLine(line);
                    }
                }
                if (e.MoveNext())
                {
                    continue;
                }
                else
                {
                    content = block.ToString();
                    if (!String.IsNullOrWhiteSpace(content))
                    {
                        yield return new LogEntryBlock(content, path);
                        break;
                    }
                }
            }
        }
    }
}
