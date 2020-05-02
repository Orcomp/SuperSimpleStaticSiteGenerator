using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUglify.JavaScript.Syntax;

namespace StaticSiteGenerator
{
    public class Template
    {
        public Template(string source, string rootFolder)
        {
            Tokens = new List<string>();
            Files = new List<string>();
            MatchByToken = new Dictionary<string, Match>();

            Source = source;
            RootFolder = rootFolder;

            Parse();
        }
        
        public string RootFolder { get; private set; }
        public string OutputDirectory { get; private set; }
        public List<string> Tokens { get; private set; }
        
        /// <summary>
        /// These are the text files that will use this template
        /// </summary>
        public List<string> Files { get; private set; }
        public Dictionary<string, Match> MatchByToken { get; private set; }
        public string Source { get; private set; }

        public string Render(Dictionary<string, string> contentByToken)
        {
            var substringStart = 0;
            var sb = new StringBuilder();
            
            foreach (var matchInToken in MatchByToken)
            {
                var token = matchInToken.Key;
                var match = matchInToken.Value;

                var substring = string.Empty;

                if (match.Index > substringStart)
                {
                    var length = match.Index - substringStart;
                    substring = Source.Substring(substringStart, length);
                }
                
                sb.Append(substring);
                    
                if (contentByToken.TryGetValue(token, out var matchedTokenContent))
                {
                    sb.Append(matchedTokenContent);
                }
                
                substringStart = match.Index + match.Length;
            }

            if (substringStart < Source.Length)
            {
                var length = Source.Length - substringStart;
                var substring = Source.Substring(substringStart, length);
                
                sb.Append(substring);
            }

            return sb.ToString();
        }
        
        private void Parse()
        {
            var matches = Regex.Matches(Source, "{{([^{]+)}}");

            foreach (Match match in matches)
            {
                var token = match.Groups[1].Value;

                if (token.StartsWith("!"))
                {
                    // We have a file statement
                    GetFiles(token.Substring(1, token.Length - 1));
                }
                Tokens.Add(token);
                MatchByToken.Add(token, match);
            }
        }

        private void GetFiles(string fileStatement)
        {
            var fileRules = GetFileRules(fileStatement);
            var files = new List<string>();

            var htmlFolder = Path.Combine(RootFolder, "html\\");

            if (fileRules.TryGetValue("DIRECTORY", out var matchedDirectories))
            {
                foreach (var matchedDirectory in matchedDirectories)
                {
                    var directory = Path.Combine(htmlFolder, matchedDirectory);
                    
                    if (Directory.Exists(directory))
                    {
                        files.AddRange(Directory.GetFiles(directory, "*.html"));
                    }
                }
            }
            
            if (fileRules.TryGetValue("FILES", out var matchedFilePaths))
            {
                foreach (var filePath in matchedFilePaths)
                {
                    if(File.Exists(filePath))
                    {
                        files.Add(filePath);
                    }
                }
            }
            
            if (fileRules.TryGetValue("EXCLUDE", out var matchedExcludePatterns))
            {
                foreach (var excludePattern in matchedExcludePatterns)
                {

                }
            }
            
            if (fileRules.TryGetValue("OUTPUT", out var matchedOutputDirectory))
            {
                OutputDirectory = matchedOutputDirectory.FirstOrDefault();
            }
            
            // We want relative path
            if (string.IsNullOrEmpty(OutputDirectory))
            {
                Files.AddRange(files.Select(x => x.Replace(htmlFolder, string.Empty)));
            }
            else
            {
                Files.AddRange(files.Select(x => new FileInfo(x)).Select(x => Path.Combine(OutputDirectory, x.Name)));
            }
        }

        private Dictionary<string, List<string>> GetFileRules(string fileStatement)
        {
            var fileRules = new Dictionary<string, List<string>>();
            
            var rules = fileStatement.Split(';');

            foreach (var rule in rules)
            {
                var splitRule = rule.Split(':');
                var key = splitRule[0];
                var value = splitRule[1];
                
                fileRules.Add(key.Trim().ToUpper(), value.Split(',').Select(x => x.Trim()).ToList());
            }

            return fileRules;
        }
    }
}