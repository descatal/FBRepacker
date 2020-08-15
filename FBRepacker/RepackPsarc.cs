using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker
{
    class RepackPsarc
    {
        public RepackPsarc()
        {
            string repackPath = Properties.Settings.Default.RepackPath, psarcexeSource = Path.Combine(Directory.GetCurrentDirectory(), @"\3rd Party\"), repackFilesUriArgs = string.Empty;
            string[] files = Directory.GetFiles(repackPath, "*", SearchOption.AllDirectories);

            foreach(var s in files)
            {
                Uri repackPathUri = new Uri(repackPath + @"\");
                Uri repackFilePathUri = new Uri(s);
                Uri repackFileRelativeUri = repackPathUri.MakeRelativeUri(repackFilePathUri);
                repackFilesUriArgs += " " + repackFileRelativeUri.OriginalString;
                Console.WriteLine(repackFilesUriArgs);
            }

            File.Copy(psarcexeSource, Path.Combine(repackPath, "psarc.exe"), true);

            using (Process psarc = new Process())
            {
                psarc.StartInfo.WorkingDirectory = repackPath;
                psarc.StartInfo.FileName = "psarc.exe";
                psarc.StartInfo.UseShellExecute = false;
                psarc.StartInfo.RedirectStandardOutput = true;
                psarc.StartInfo.CreateNoWindow = false;
                psarc.StartInfo.Arguments = "create -y -v -oOutput.psarc" + repackFilesUriArgs;
                psarc.Start();
                Console.WriteLine(psarc.StandardOutput.ReadToEnd());
                psarc.WaitForExit();
            }

            try
            {
                File.Delete(Path.Combine(repackPath, "psarc.exe"));
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
