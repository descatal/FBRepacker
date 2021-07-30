using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using FBRepacker.PAC;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Psarc
{
    class RepackPsarc : Internals
    {
        public Toc TBL = new Toc();
        FileStream TBLS { get; set; }

        public string TBLPath { get; set; }

        public RepackPsarc(string PsarcFolderPath)
        {
            if (!Directory.Exists(PsarcFolderPath))
                throw new Exception("Psarc Folder Path not valid!");

            // Try to find PATCH.TBL inside the Psarc Folder
            string[] TBLPaths = Directory.GetFiles(PsarcFolderPath, "PATCH.TBL");

            if (TBLPaths.Count() <= 0)
                throw new Exception("Cannot find PATCH.TBL in Psarc Folder");

            TBLPath = TBLPaths[0];

            TBLS = new FileStream(TBLPath, FileMode.Open);

            TBL.parseToc(TBLS, PsarcFolderPath);

            TBLS.Close();
        }

        public void exportToc()
        {
            FileStream backUp = File.Create(Directory.GetCurrentDirectory() + (@"\temp\PATCH(BACKUP).TBL"));
            TBLS = new FileStream(TBLPath, FileMode.Open);
            TBLS.CopyTo(backUp);
            backUp.Close();
            TBLS.Close();

            MemoryStream newTBLMS = TBL.writeToc();
            FileStream TBLFS = File.Create(TBLPath);
            TBLFS.Write(newTBLMS.ToArray(), 0, (int)newTBLMS.Length);
            newTBLMS.Flush();
            TBLFS.Flush();
            TBLFS.Close();
        }

        public void repackPsarc(string outputFileName)
        {
            string repackPath = Properties.Settings.Default.PsarcRepackFolder; 
            string psarcexeSource = Path.Combine(Directory.GetCurrentDirectory(), @"3rd Party\Psarc\psarc.exe"), repackFilesUriArgs = string.Empty;
            string[] files = Directory.GetFiles(repackPath, "*", SearchOption.AllDirectories);

            foreach (var s in files)
            {
                Uri repackPathUri = new Uri(repackPath + @"\");
                Uri repackFilePathUri = new Uri(s);
                Uri repackFileRelativeUri = repackPathUri.MakeRelativeUri(repackFilePathUri);
                repackFilesUriArgs += " " + repackFileRelativeUri.OriginalString;
                Console.WriteLine(repackFilesUriArgs);
            }

            FileStream fs = File.OpenRead(psarcexeSource);
            FileStream exeFs = File.Create(Path.Combine(repackPath, "psarc.exe"));

            fs.CopyTo(exeFs);
            exeFs.Close();
            fs.Close();

            //File.Copy(psarcexeSource, Path.Combine(repackPath, "psarc.exe"), true);

            using (Process psarc = new Process())
            {
                psarc.StartInfo.WorkingDirectory = repackPath;
                psarc.StartInfo.FileName = "psarc.exe";
                psarc.StartInfo.UseShellExecute = false;
                psarc.StartInfo.RedirectStandardOutput = true;
                psarc.StartInfo.CreateNoWindow = true;
                psarc.StartInfo.Arguments = "create -y -v -oOutput.psarc" + repackFilesUriArgs;
                psarc.Start();
                Console.WriteLine(psarc.StandardOutput.ReadToEnd());
                psarc.WaitForExit();
            }

            File.Copy(Path.Combine(repackPath, "Output.psarc"), Path.Combine(Properties.Settings.Default.OutputRepackPsarc, outputFileName + ".psarc"), true);

            try
            {
                File.Delete(Path.Combine(repackPath, "Output.psarc"));
                File.Delete(Path.Combine(repackPath, "psarc.exe"));
            }
            catch (Exception e)
            {
                throw new Exception("Cannot delete. Error: " + e);
            }
        }
    }
}
