using System.Diagnostics;
using System.IO;

namespace StaticSiteGenerator
{
    public class FolderCopier
    {
        public void Copy(string from, string to)
        {
            if (Directory.Exists(to))
            {
                Directory.Delete(to, true);
            }

            var proc = new Process();
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.FileName = "robocopy";
            // we want to exclude files that start with a dot or start with the letters "style"
            proc.StartInfo.Arguments = $"{from} {to} /S /XF .* style* ";
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            proc.WaitForExit();
        }
    }
}