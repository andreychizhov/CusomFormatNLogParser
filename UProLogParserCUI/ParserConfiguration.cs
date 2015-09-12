using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UProLogParserCUI
{
    public class ParserConfiguration
    {
        private char[] _delimeters = new[] { ';' };

        public IEnumerable<string> CollectedExceptions
        {
            get
            {
                return from x in ReadSection("CollectedExeptions").Split(_delimeters)
                       select x.Trim();
            }
        }

        public IEnumerable<string> IgnoreErrorsThatStartsWith
        {
            get
            {
                string sectionText = ReadSection("IgnoreErrorsThatStartsWith");
                var result = !String.IsNullOrEmpty(sectionText) ? 
                    sectionText.Split(_delimeters).Select(x => x.Trim()) : Enumerable.Empty<string>();
                return result;
            }
        }

        private string ReadSection(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key];
                return result;
            }
            catch (ConfigurationErrorsException)
            {
                throw new ConfigurationErrorsException(string.Format("Unable to read section {0}", key));
            }
        }
    }
}
