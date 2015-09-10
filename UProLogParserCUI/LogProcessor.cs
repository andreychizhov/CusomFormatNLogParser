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
        private static readonly string[] _exeptions = new[]
        {
            "Exception: DbEntityValidationException", "Exception: EnrichException", "Exception: InvalidCalculationRequestException"
        };
        private const string errorDataPresenceMarker = "ErrorData";

        public void Process(string[] paths, string[] productMarkers, string outputFilename = "output")
        {
            var c = paths.
                SelectMany(p => Directory.EnumerateFiles(p, "*.log"))
                .Select(p => ReadFileByBlock(p))
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .WithMergeOptions(ParallelMergeOptions.Default)
                .MapReduce(
                    src =>
                        src
                        .Where(x => productMarkers.Any(m => x.Contains(m)) && _exeptions.Any(ex => x.Contains(ex)) && x.Contains(errorDataPresenceMarker))
                        .Select(block => GetTextByLine(block).FirstOrDefault(line => line.StartsWith("AdditionalInfo")))
                        .Select(s => JsonConvert.DeserializeObject<AdditionalInfo>(CleanJsonObjectString(s)))
                        .SelectMany(ai => ai.ErrorData.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)),
                    s => s,
                    g => new[] { new { Data = g.Key, Count = g.Count() } })
                .Where(s => !string.IsNullOrWhiteSpace(s.Data))
                .OrderByDescending(s => s.Count);

            using (var fs = new FileStream(".\\" + outputFilename + ".txt", FileMode.Create))
            using (var sw = new StreamWriter(fs, Encoding.UTF8))
            {
                foreach (var item in c)
                {
                    sw.WriteLine("{0};{1}", item.Data.Trim(), item.Count);
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

        private static IEnumerable<string> ReadFileByBlock(string path)
        {
            var e = File.ReadLines(path).GetEnumerator();
            if (!e.MoveNext()) return Enumerable.Empty<string>();
            return ReadFileByBlockImpl(e);
        }

        private static IEnumerable<string> ReadFileByBlockImpl(IEnumerator<string> linesEnum)
        {
            var e = linesEnum;
            var block = new StringBuilder();
            string content;

            while (true)
            {
                string line = e.Current;
                if (Regex.Matches(line, @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{4})").Count == 0)
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
                        yield return content;
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
                        yield return content;
                        break;
                    }
                }
            }
        }
    }
}
