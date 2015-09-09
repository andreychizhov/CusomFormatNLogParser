using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UProLogParserCUI
{
    class Program
    {
        private static readonly string[] exeptions = new[] { "Exception: DbEntityValidationException", "Exception: EnrichException" };
        private const string errorDataPresenceMarker = "ErrorData";
        static void Main(string[] args)
        {
            string[] paths = args.Any() ? args : new[] { "d:\\files" };
            var c = paths.
                SelectMany(p => Directory.EnumerateFiles(p, "*.log"))
                .AsParallel()
                .MapReduce(
                    path => Regex.Split(ReadFile(path), @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{4})")
                        .Where(x => exeptions.Any(ex => x.Contains(ex)) && x.Contains(errorDataPresenceMarker))
                        .Select(block => GetTextByLine(block).FirstOrDefault(line => line.StartsWith("AdditionalInfo")))
                        .Select(s => JsonConvert.DeserializeObject<AdditionalInfo>(CleanJsonObjectString(s))),
                    i => i.ErrorData,
                    g => new[] { new { Data = g.Key, Count = g.Count() } })
                .Where(s => !string.IsNullOrWhiteSpace(s.Data))
                .OrderByDescending(s => s.Count);

            Console.WriteLine("Exeptions occuriong freqency statistics calculation started...");

            using (var sw = new StreamWriter(@"d:\files\output.txt"))
            {
                foreach (var item in c)
                {
                    sw.WriteLine("Error '{0}' occured in the log {1} {2}", item.Data.Trim(), item.Count, item.Count == 1 ? "time" : "times");
                }
            }

            Console.WriteLine("Completed. Please, check the output folder.");

            Console.ReadLine();
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

        private static string ReadFile(string path)
        {
            Char[] buffer;

            using (var sr = new StreamReader(path))
            {
                buffer = new Char[(int)sr.BaseStream.Length];
                sr.Read(buffer, 0, (int)sr.BaseStream.Length);
            }
            return new StringBuilder().Append(buffer).ToString();
        }
    }

    static class PLINQExtensions
    {
        public static ParallelQuery<TResult> MapReduce<TSource, TMapped, TKey, TResult>(
            this ParallelQuery<TSource> source,
            Func<TSource, IEnumerable<TMapped>> map,
            Func<TMapped, TKey> keySelector,
            Func<IGrouping<TKey, TMapped>, IEnumerable<TResult>> reduce)
        {
            return source.SelectMany(map)
            .GroupBy(keySelector)
            .SelectMany(reduce);
        }
    }
}
