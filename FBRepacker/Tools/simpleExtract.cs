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
            FileStream fs = File.OpenRead(@"C:\Users\descatal\Desktop\SD G Generation\system.pkd");
            string outputPath = @"C:\Users\descatal\Desktop\SD G Generation\system output";
            string extension = ".hca";

            byte[] seperationMagic = new byte[] { 0x48, 0x43, 0x41, 0x00 };

            MemoryStream fss = new MemoryStream();
            fs.Seek(0, SeekOrigin.Begin);
            fs.CopyTo(fss);
            fss.Seek(0, SeekOrigin.Begin);

            byte[] ba = fss.ToArray();
            List<int> boyerPointers = new BoyerMoore(seperationMagic).Search(ba).ToList();

            for(int i = 0; i < boyerPointers.Count; i++)
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

                /*
                fss.Seek(pointer + 0x4, SeekOrigin.Begin);
                uint fileSize = readUIntSmallEndian(fss);

                

                int a = fileChunk[0x28];
                if(a == 1)
                {
                    fileChunk[0x28] = 0x3;
                }
                */

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
