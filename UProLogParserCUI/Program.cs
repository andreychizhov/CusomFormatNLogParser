using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace UProLogParserCUI
{
    class Program
    {
        static LogProcessor _processor = new LogProcessor();

        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("Target dir is not specified. Use format: parser.exe [path1] [path 2] ... [path n]");
                return;
            }

            string[] paths = args;

            Console.WriteLine("Exeptions occuring frequency statistics calculation started...");

            _processor.Process(paths, new[] { "api/ContractDTO" }, "output_kasko");
            _processor.Process(paths, new[] { "api/OSAGO", "api/integration/RSA" }, "output_osago");

            Console.WriteLine("Completed. Please, check the output folder.");

            Console.ReadLine();
        }

        
    }
}
