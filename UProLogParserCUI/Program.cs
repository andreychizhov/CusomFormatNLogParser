using System;
using System.Collections.Generic;
using System.Linq;

namespace UProLogParserCUI
{
    class Program
    {
        static LogProcessor _processor;
        const string _recursiveSubdidectoriesSearchKey = "-r";

        static void Main(string[] args)
        {
            var cla = new List<string>(args);

            if (!cla.Any())
            {
                Console.WriteLine("Target dir is not specified. Use format: parser.exe [path1] [path 2] ... [path n]");
                Console.WriteLine(string.Format("Use {0} key to include subdirectories.", _recursiveSubdidectoriesSearchKey));
                return;
            }
            if (cla.Contains(_recursiveSubdidectoriesSearchKey))
            {
                _processor = new LogProcessor(new RecursiveFileSearcher());
                cla.Remove(_recursiveSubdidectoriesSearchKey);
            }
            else
            {
                _processor = new LogProcessor(new DefaultFileSearcher());
            }

            Console.WriteLine("Exeptions occuring frequency statistics calculation started...");

            _processor.Process(cla, new[] { "api/ContractDTO" }, "output_kasko");
            _processor.Process(cla, new[] { "api/OSAGO", "api/integration/RSA" }, "output_osago");

            Console.WriteLine("Completed. Please, check the output folder.");

            Console.ReadLine();
        }
    }
}
