using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AngleSharp.Parser.Html;
using ManyConsole;
using NUglify;
using StaticSiteGenerator.Converters;

namespace StaticSiteGenerator.Commands
{
    public class GeneratorCommand : ConsoleCommand
    {
        public string DataDirectory;

        public GeneratorCommand()
        {
            IsCommand("Generator", "Create a static website.");
            HasOption("directory|d=", "Data directory", v => DataDirectory = v);
        }
        
        public override int Run(string[] remainingArguments)
        {
            var config = new Configuration(DataDirectory);
            
            var sw = Stopwatch.StartNew();

            var contentByToken = new Dictionary<string, string>();
            var proc = new Process();
            
            var folderCopier = new FolderCopier();

            //-------------------------------------
            // Copy js and css files to output folder

            if (config.CopyCssFolder)
            {
                var cssTargetFolder = Path.Combine(config.OutputFolder, "css");
                folderCopier.Copy(config.CssFolder, cssTargetFolder);
            }
            
            //-------------------------------------
            // Copy img folder

            if (config.CopyImageFolder)
            {
                var imgTargetFolder = Path.Combine(config.OutputFolder, "img");
                folderCopier.Copy(config.ImgFolder, imgTargetFolder);
            }

            //-------------------------------------
            // Copy JS folder

            // NOTE: Need to copy sw.js manually to root directory

            if (config.CopyJsFolder)
            {
                var jsTargetFolder = Path.Combine(config.OutputFolder, "js");
                folderCopier.Copy(config.JsFolder, jsTargetFolder);
            }

            //==============================================
            // File conversion to HTML
            
            //----------------------------
            // Convert MD files
            
            MarkdownConverter.Convert(config.InputFolder, config.HtmlFolder);

            //----------------------------
            // Convert AsciiDoc files

            AsciiDocConverter.Convert(config.InputFolder, config.HtmlFolder);

            //==============================================
            // Get all the templates and look at their tokens
            
            var templateRetriever = new TemplateRetriever();
            var templateByFileName = templateRetriever.Fetch(config.TemplateFolder);
            
            var tokenInterpreter = new TokenInterpreter();

            foreach (var templateInFileName in templateByFileName)
            {
                contentByToken.Clear();
                    
                var fileName = templateInFileName.Key;
                var templateSource = templateInFileName.Value;
                
                var template = new Template(templateSource, config.RootFolder);
                var tokensByFileName = tokenInterpreter.Translate(template.Tokens);
                
                var templateRenderer = new TemplateRenderer(template, config);

                if (template.Files.Any())
                {
                    foreach (var file in template.Files)
                    {
                        SetContentByToken(config, tokensByFileName, contentByToken, file);
                        templateRenderer.Render(template, contentByToken, file);
                    }
                }
                else
                {
                    SetContentByToken(config, tokensByFileName, contentByToken);
                    templateRenderer.Render(template, contentByToken, fileName);
                }
            }

            sw.Stop();

            Console.WriteLine($"Total elapsed time: {sw.ElapsedMilliseconds / 1000d} sec");
            
            return 0;
        }
        
        private void SetContentByToken(Configuration configuration, Dictionary<string, List<string>> tokensByFileName, Dictionary<string, string> contentByToken, string relativeFilePath = null)
        {
            // From the tokens find which files to load

            var isContentByTokenPopulated = contentByToken.Count > 0;
            
            var tokenInterpreter = new TokenInterpreter();

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
                var filePath = Path.Combine(configuration.RootFolder, tokenFilePath);
                
                if (tokenFilePath.Equals("self") && relativeFilePath != null)
                {
                    filePath = Path.Combine(configuration.HtmlFolder, relativeFilePath);
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
                        
                        var template = new Template(contents, configuration.RootFolder);
                        var innerTokensByFileName = tokenInterpreter.Translate(template.Tokens);
                        
                        SetContentByToken(configuration, innerTokensByFileName, innerContentByToken);
                        
                        var templateRenderer = new TemplateRenderer(template, configuration);
                        contents = templateRenderer.Render(template, innerContentByToken);
                    }
                    else if (configuration.MinifyFiles && tokenFilePath.StartsWith("css"))
                    {
                        var uglify = Uglify.Css(contents);

                        if (!uglify.HasErrors)
                        {
                            contents = uglify.ToString();
                        }
                    }
                    else if (configuration.MinifyFiles && tokenFilePath.StartsWith("js"))
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
    }
}