using System.Collections.Generic;
using System.IO;
using NUglify;
using NUglify.Html;

namespace StaticSiteGenerator
{
    public class TemplateRenderer
    {
        public TemplateRenderer(Template template, Configuration configuration)
        {
            Template = template;
            Configuration = configuration;
        }
        
        private Template Template { get; }
        private Configuration Configuration { get; }
        
        public void Render(Template template, Dictionary<string, string> contentByToken, string fileName)
        {
            var result = Render(template, contentByToken);

            var outputFilePath = Path.Combine(Configuration.OutputFolder, fileName);
            var outputFolder = Configuration.OutputFolder;

            if (fileName.EndsWith("template"))
            {
                if (!string.IsNullOrEmpty(template.OutputDirectory))
                {
                    outputFolder = Path.Combine(outputFolder, template.OutputDirectory);
                }
                
                outputFilePath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(fileName) + ".html");
            }
            
            var fileInfo = new FileInfo(outputFilePath);
            fileInfo.Directory?.Create();

            File.WriteAllText( outputFilePath, result);
        }
        
        internal string Render(Template template, Dictionary<string, string> contentByToken)
        {
            var result = template.Render(contentByToken);

            if (!Directory.Exists(Configuration.OutputFolder))
            {
                Directory.CreateDirectory(Configuration.OutputFolder);
            }

            var htmlSettings = new HtmlSettings();
            htmlSettings.RemoveComments = true;
            htmlSettings.CollapseWhitespaces = true;
            htmlSettings.RemoveEmptyAttributes = true;
            htmlSettings.RemoveOptionalTags = false;

            if (Configuration.MinifyFiles)
            {
                var resultUglify = Uglify.Html(result, htmlSettings);

                if (!resultUglify.HasErrors)
                {
                    result = resultUglify.ToString();
                }
            }

            return result;
        }
    }
}