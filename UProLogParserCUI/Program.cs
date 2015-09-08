using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UProLogParserCUI
{
    class Program
    {
        private static readonly string[] exeptions = new[] { "Exception: DbEntityValidationException", "Exception: EnrichException" };
        private const string errorDataPresenceMarker = "ErrorData";
        static void Main(string[] args)
        {
            var fileContent = ReadFileAsync(@"d:\files\2015-09-05.log").Result;

            var s = Regex.Split(fileContent, @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}.\d{4})")
                .Where(x => exeptions.Any(ex => x.Contains(ex)) && x.Contains(errorDataPresenceMarker));
            var oneFragment = GetTextByLine(s.FirstOrDefault()).FirstOrDefault(line => line.Contains("AdditionalInfo"));
            var cleanStr = CleanJsonObjectString(oneFragment);
            var info = JsonConvert.DeserializeObject<AdditionalInfo>(cleanStr);
            Console.ReadLine();
        }

        private static string CleanJsonObjectString(string dirtyString)
        {
            int indexOfFirstCurlyBracket = dirtyString.IndexOf('{');
            return dirtyString.Substring(indexOfFirstCurlyBracket);
        }

        private static IEnumerable<string> GetTextByLine(string textBlock)
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

        private async static Task<string> ReadFileAsync(string path)
        {
            Char[] buffer;

            using (var sr = new StreamReader(path))
            {
                buffer = new Char[(int)sr.BaseStream.Length];
                await sr.ReadAsync(buffer, 0, (int)sr.BaseStream.Length);
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
