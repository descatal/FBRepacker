using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Tools
{
    class simpleExtract : Internals
    {
        public simpleExtract()
        {
            
        }

        public void clearDataUntilHeader()
        {

        }

        public void extractFiles()
        {
            FileStream fs = File.OpenRead(@"D:\Dynasty Warrior Gundam 3 extract\2963.unknown");
            string outputPath = @"D:\Dynasty Warrior Gundam 3 extract\Voice Lines";
            string extension = ".at3";

            byte[] seperationMagic = new byte[] { 0x52, 0x49, 0x46, 0x46 };

            MemoryStream fss = new MemoryStream();
            fs.Seek(0, SeekOrigin.Begin);
            fs.CopyTo(fss);
            fss.Seek(0, SeekOrigin.Begin);

            byte[] ba = fss.ToArray();
            List<int> boyerPointers = new BoyerMoore(seperationMagic).Search(ba).ToList();

            for (int i = 0; i < boyerPointers.Count; i++)
            {
                uint pointer = (uint)boyerPointers[i];
                uint nextpointer = 0;

                if (i == boyerPointers.Count - 1)
                {
                    nextpointer = (uint)fs.Length;
                }
                else
                {
                    nextpointer = (uint)boyerPointers[i + 1];
                }

                byte[] fileChunk = extractChunk(fs, pointer, nextpointer - pointer);


                fss.Seek(pointer + 0x4, SeekOrigin.Begin);
                uint fileSize = readUIntSmallEndian(fss);


                int a = fileChunk[0x28];
                if (a == 1)
                {
                    fileChunk[0x28] = 0x3; // for cases where you need to change the channel to stereo
                }


                MemoryStream ms = new MemoryStream(fileChunk);

                ms.Seek(0, SeekOrigin.Begin);

                FileStream ofs = File.Create(outputPath + @"\" + i + extension);
                ms.CopyTo(ofs);
                ms.Close();

                ofs.Close();
            }

            fs.Close();
        }
    }
}
