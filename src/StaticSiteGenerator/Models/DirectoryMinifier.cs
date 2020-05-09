using System.IO;
using System.Linq;
using NUglify;

namespace StaticSiteGenerator
{
    public class DirectoryMinifier
    {
        public void Minify(string directory)
        {
            // NOTE: Files will be overriden

            var htmlFiles = Directory.GetFiles(directory, "*.html");

            foreach (var htmlFile in htmlFiles.Where(x => !x.Contains("_min")))
            {
                var fileStream = File.ReadAllText(htmlFile);
                var minResult = Uglify.Html(fileStream);

                File.WriteAllText(htmlFile, minResult.Code);
            }

            var cssFiles = Directory.GetFiles(directory, "*.css");

            foreach (var cssFile in cssFiles.Where(x => !x.Contains("_min")))
            {
                var fileStream = File.ReadAllText(cssFile);
                var minResult = Uglify.Css(fileStream);

                File.WriteAllText(cssFile, minResult.Code);
            }

            var jsFiles = Directory.GetFiles(directory, "*.js");

            foreach (var jsFile in jsFiles.Where(x => !x.Contains("_min")))
            {
                var fileStream = File.ReadAllText(jsFile);
                var minResult = Uglify.Js(fileStream);

                File.WriteAllText(jsFile, minResult.Code);
            }
        }
    }
}