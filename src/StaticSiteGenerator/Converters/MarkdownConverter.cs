using System.IO;
using Markdig;

namespace StaticSiteGenerator.Converters
{
    public static class MarkdownConverter
    {
        public static void Convert(string inputFolder, string outputFolder)
        {
            var mdFiles = Directory.GetFiles(inputFolder, "*.md");

            if (mdFiles.Length == 0)
            {
                return;
            }

            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

            foreach (var mdFile in mdFiles)
            {
                var fileContent = File.ReadAllText(mdFile);
                var result = Markdown.ToHtml(fileContent, pipeline);

                var fileName = Path.GetFileNameWithoutExtension(mdFile);

                File.WriteAllText(Path.Combine(outputFolder, $"{fileName}.html"), result);
            }
        }
    }
}