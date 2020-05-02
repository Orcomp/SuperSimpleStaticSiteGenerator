using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Markdig;
using NUglify;
using NUglify.Html;
using HtmlParser = AngleSharp.Parser.Html.HtmlParser;

namespace StaticSiteGenerator
{
    internal class Program
    {
        //==============================================
        // Config

        private static bool _copyJsFolder = true;
        private static bool _copyImageFolder = false;
        private static bool _copyCssFolder = true;
            
        private static bool _minifyFiles = true;
            
        private static string _rootFolder = @"C:\Source\SuperSimpleStaticSiteGenerator\data";

        private static string _inputFolder = Path.Combine(_rootFolder, "content");
        private static string _htmlFolder = Path.Combine(_rootFolder, "html");
        private static string _outputFolder = Path.Combine(_rootFolder, "output");
        private static string _templateFolder = Path.Combine(_rootFolder, "templates");
        private static string _cssFolder = Path.Combine(_rootFolder, "css");
        private static string _jsFolder = Path.Combine(_rootFolder, "js");
        private static string _imgFolder = Path.Combine(_rootFolder, "img");
        
        public static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            var contentByToken = new Dictionary<string, string>();
            var proc = new Process();

            //-------------------------------------
            // Copy js and css files to output folder

            if (_copyCssFolder)
            {
                var cssTargetFolder = Path.Combine(_outputFolder, "css");

                if (Directory.Exists(cssTargetFolder))
                {
                    Directory.Delete(cssTargetFolder, true);
                }

                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = "robocopy";
                // we want to exclude files that start with a dot or start with the letters "style"
                proc.StartInfo.Arguments = $"{_cssFolder} {cssTargetFolder} /S /XF .* style* ";
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                proc.WaitForExit();
            }

            //-------------------------------------
            // Copy img folder

            if (_copyImageFolder)
            {
                var imgTargetFolder = Path.Combine(_outputFolder, "img");

                if (Directory.Exists(imgTargetFolder))
                {
                    Directory.Delete(imgTargetFolder, true);
                }

                proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = "robocopy";
                // we want to exclude files that start with a dot or start with the letters "style"
                proc.StartInfo.Arguments = $"{_imgFolder} {imgTargetFolder} /S /XF .* style* ";
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                proc.WaitForExit();
            }

            //-------------------------------------
            // Copy JS folder

            // NOTE: Need to copy sw.js manually to root directory

            if (_copyJsFolder)
            {
                var jsTargetFolder = Path.Combine(_outputFolder, "js");

                if (Directory.Exists(jsTargetFolder))
                {
                    Directory.Delete(jsTargetFolder, true);
                }

                proc = new Process();
                proc.StartInfo.UseShellExecute = true;
                proc.StartInfo.FileName = "robocopy";
                // Do not copy service worker. It must be in the root directory
                proc.StartInfo.Arguments = $"{_jsFolder} {jsTargetFolder} /S /XF sw.js *.txt";
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                proc.WaitForExit();
            }

            //==============================================
            // File conversion to HTML
            
            //----------------------------
            // Convert MD files
            
            ConvertMarkDownFiles(_inputFolder, _htmlFolder);

            //----------------------------
            // Convert AsciiDoc files

            var startAsciiDocConversion = sw.ElapsedMilliseconds;
            
            proc = new Process();
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.FileName = "asciidoctor";
            proc.StartInfo.WorkingDirectory = _inputFolder;
            proc.StartInfo.Arguments = $"-r asciidoctor-html5s -b html5s -R {_inputFolder} -D {_htmlFolder} '**/*.adoc'";
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            proc.WaitForExit();
            
            var endAsciiDocConversion = sw.ElapsedMilliseconds;

            Console.WriteLine($"Convert files to AsciiDoc: {(endAsciiDocConversion - startAsciiDocConversion) / 1000d} secs");

            //==============================================
            // Get all the templates and look at their tokens

            var templateByFileName = GetTemplates(_templateFolder);

            foreach (var templateInFileName in templateByFileName)
            {
                contentByToken.Clear();
                    
                var fileName = templateInFileName.Key;
                var templateSource = templateInFileName.Value;
                
                var template = new Template(templateSource, _rootFolder);
                var tokensByFileName = GetTokensByFile(template.Tokens);

                if (template.Files.Any())
                {
                    foreach (var file in template.Files)
                    {
                        SetContentByToken(tokensByFileName, contentByToken, file);
                        RenderTemplate(template, contentByToken, file);
                    }
                }
                else
                {
                    SetContentByToken(tokensByFileName, contentByToken);
                    RenderTemplate(template, contentByToken, fileName);
                }
            }

            sw.Stop();

            Console.WriteLine($"Total elapsed time: {sw.ElapsedMilliseconds / 1000d} sec");
        }
        
        private static string RenderTemplate(Template template, Dictionary<string, string> contentByToken)
        {
            var result = template.Render(contentByToken);

            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            var htmlSettings = new HtmlSettings();
            htmlSettings.RemoveComments = true;
            htmlSettings.CollapseWhitespaces = true;
            htmlSettings.RemoveEmptyAttributes = true;
            htmlSettings.RemoveOptionalTags = false;

            if (_minifyFiles)
            {
                var resultUglify = Uglify.Html(result, htmlSettings);

                if (!resultUglify.HasErrors)
                {
                    result = resultUglify.ToString();
                }
            }

            return result;
        }

        private static void RenderTemplate(Template template, Dictionary<string, string> contentByToken, string fileName)
        {
            var result = RenderTemplate(template, contentByToken);

            var outputFilePath = Path.Combine(_outputFolder, fileName);
            var outputFolder = _outputFolder;

            if (fileName.EndsWith("template"))
            {
                if (!string.IsNullOrEmpty(template.OutputDirectory))
                {
                    outputFolder = Path.Combine(_outputFolder, template.OutputDirectory);
                }
                
                outputFilePath = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(fileName) + ".html");
            }
            
            var fileInfo = new FileInfo(outputFilePath);
            fileInfo.Directory?.Create();

            File.WriteAllText( outputFilePath, result);
        }

        private static void SetContentByToken(Dictionary<string, List<string>> tokensByFileName, Dictionary<string, string> contentByToken, string relativeFilePath = null)
        {
            // From the tokens find which files to load

            var isContentByTokenPopulated = contentByToken.Count > 0;

            foreach (var tokensInFileName in tokensByFileName)
            {
                var tokenFilePath = tokensInFileName.Key;
                var tokenValues = tokensInFileName.Value;

                if (string.IsNullOrEmpty(tokenFilePath))
                {
                    continue;
                }

                if (isContentByTokenPopulated && !tokenFilePath.Equals("self"))
                {
                    // We already have the content
                    continue;
                }
                
                // Get the file path
                var filePath = Path.Combine(_rootFolder, tokenFilePath);
                
                if (tokenFilePath.Equals("self") && relativeFilePath != null)
                {
                    filePath = Path.Combine(_htmlFolder, relativeFilePath);
                }

                if (!File.Exists(filePath))
                {
                    continue;
                }

                if (tokenValues.Any())
                {
                    var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    
                    var parser = new HtmlParser();
                    var document = parser.Parse(fileStream);

                    foreach (var tokenValue in tokenValues)
                    {
                        var results = document.QuerySelectorAll(tokenValue);

                        if (results.Any())
                        {
                            var html = results[0].OuterHtml;
                            contentByToken[$"{tokenFilePath}:{tokenValue}"] = html;
                        }
                    }

                    fileStream.Dispose();
                }
                else
                {
                    var contents = File.ReadAllText(filePath);
                    
                    var fileInfo = new FileInfo(filePath);
                    
                    if (fileInfo.Extension.Equals(".template"))
                    {
                        // We have a template that we need to populate
                        
                        var innerContentByToken = new Dictionary<string, string>();
                        
                        var template = new Template(contents, _rootFolder);
                        var innerTokensByFileName = GetTokensByFile(template.Tokens);
                        
                        SetContentByToken(innerTokensByFileName, innerContentByToken);
                        contents = RenderTemplate(template, innerContentByToken);
                    }
                    else if (_minifyFiles && tokenFilePath.StartsWith("css"))
                    {
                        var uglify = Uglify.Css(contents);

                        if (!uglify.HasErrors)
                        {
                            contents = uglify.ToString();
                        }
                    }
                    else if (_minifyFiles && tokenFilePath.StartsWith("js"))
                    {
                        var uglify = Uglify.Js(contents);
                        
                        if (!uglify.HasErrors)
                        {
                            contents = uglify.ToString();
                        }
                    }
                    
                    contentByToken.Add($"{tokenFilePath}", contents);
                }
            }
        }

        private static Dictionary<string, string> GetTemplates(string directory)
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

        private static void MinifyDirectory(string directory)
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

        private static void ConvertMarkDownFiles(string inputFolder, string outputFolder)
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

        private static Dictionary<string, List<string>> GetTokensByFile(IEnumerable<string> tokens)
        {
            var tokensByFileName = new Dictionary<string, List<string>>();

            tokensByFileName.Add(string.Empty, new List<string>());

            foreach (var token in tokens)
            {
                var splits = token.Split(':');

                if (splits.Length == 1)
                {
                    tokensByFileName.Add(splits[0], new List<string>());
                }
                else if (splits.Length == 2)
                {
                    var fileName = splits[0];

                    if (!tokensByFileName.ContainsKey(fileName)) tokensByFileName.Add(fileName, new List<string>());

                    tokensByFileName[fileName].Add(splits[1]);
                }
            }

            return tokensByFileName;
        }
    }
}