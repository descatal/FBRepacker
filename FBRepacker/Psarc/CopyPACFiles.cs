using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FBRepacker.Psarc
{
    class CopyPACFiles
    {
        public CopyPACFiles(string fileListPath, string PsarcPACFolder)
        {
            StreamReader fileList = new StreamReader(fileListPath);
            if (!Directory.Exists(PsarcPACFolder))
                throw new Exception("Psarc PAC Folder is not valid!");

            copyFiles(fileList, PsarcPACFolder);
        }

        public void copyFiles(StreamReader fileList, string PsarcPACFolder)
        {
            string line, input = "";
            int takeIn = 0;
            while ((line = fileList.ReadLine()) != null)
            {
                Match m = Regex.Match(line, @"^[0-9]*$");
                if (m.Success)
                    takeIn = 1;

                if (line != @"//" && takeIn >= 1)
                {
                    switch (takeIn)
                    {
                        case 2:
                            if (!File.Exists(line))
                                throw new Exception();

                            input = line;
                            break;
                        case 3:
                            File.Copy(input, line, true);
                            break;
                        default:
                            break;
                    }
                    takeIn++;
                }
                else
                {
                    takeIn = 0;
                }
            }
        }
    }
}
