using System.Collections.Generic;
using System.IO;

namespace StaticSiteGenerator
{
    public class TemplateRetriever
    {
        public Dictionary<string, string> Fetch(string directory)
        {
            var templateFiles = Directory.GetFiles(directory, "*.template");

            var templateFileByName = new Dictionary<string, string>();

            foreach (var templateFile in templateFiles)
            {
                var fileName = Path.GetFileName(templateFile);
                var content = File.ReadAllText(templateFile);

                templateFileByName.Add(fileName, content);
            }

            return templateFileByName;
        }
    }
}