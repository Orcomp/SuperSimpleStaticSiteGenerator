using System;
using System.Diagnostics;

namespace StaticSiteGenerator.Converters
{
    public static class AsciiDocConverter
    {
        public static void Convert(string inputFolder, string outputFolder)
        {
            // TODO: Cross cutting concern MethodTimer
            
            var sw = Stopwatch.StartNew();
            var startAsciiDocConversion = sw.ElapsedMilliseconds;
            
            var proc = new Process();
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.FileName = "asciidoctor";
            proc.StartInfo.WorkingDirectory = inputFolder;
            proc.StartInfo.Arguments = $"-r asciidoctor-html5s -b html5s -R {inputFolder} -D {outputFolder} '**/*.adoc'";
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            proc.WaitForExit();
            
            var endAsciiDocConversion = sw.ElapsedMilliseconds;

            Console.WriteLine($"Convert files to AsciiDoc: {(endAsciiDocConversion - startAsciiDocConversion) / 1000d} secs");
        }
    }
}