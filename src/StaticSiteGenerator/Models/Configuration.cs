using System.IO;

namespace StaticSiteGenerator
{
    public class Configuration
    {
        public bool CopyJsFolder { get; }
        public bool CopyImageFolder { get; }
        public bool CopyCssFolder { get; }
        public bool MinifyFiles { get; }
        public string RootFolder { get; }
        public string InputFolder { get; }
        public string HtmlFolder { get; }
        public string OutputFolder { get; }
        public string TemplateFolder { get; }
        public string CssFolder { get; }
        public string JsFolder { get; }
        public string ImgFolder { get; }

        public Configuration(string rootFolder)
        {
            RootFolder = rootFolder;
            
            InputFolder = Path.Combine(RootFolder, "content");
            HtmlFolder = Path.Combine(RootFolder, "html");
            OutputFolder = Path.Combine(RootFolder, "output");
            TemplateFolder = Path.Combine(RootFolder, "templates");
            CssFolder = Path.Combine(RootFolder, "css");
            JsFolder = Path.Combine(RootFolder, "js");
            ImgFolder = Path.Combine(RootFolder, "img");

            CopyJsFolder = true;
            CopyImageFolder = false;
            CopyCssFolder = true;
            MinifyFiles = true;
        }
    }
}