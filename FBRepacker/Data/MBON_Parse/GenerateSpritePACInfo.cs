using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    internal class GenerateSpritePACInfo : Internals
    {
        public GenerateSpritePACInfo()
        {
            
        }

        public string writeSpritePACInfo(string input, int startFileNo, int FHMFileNo)
        {
            StringBuilder info = new StringBuilder();
            List<string> SpriteFiles = Directory.GetFiles(input).ToList();
            SpriteFiles = SpriteFiles.Where(s => Path.GetExtension(s).Equals(".dds")).ToList();

            SpriteFiles = SpriteFiles.OrderBy(s =>
            {
                string fileName = Path.GetFileNameWithoutExtension(s);
                string[] spilt = fileName.Split('_');
                uint res = 0;
                if (spilt.Count() >= 3)
                {
                    string fileNo = spilt[2];
                    uint.TryParse(fileNo, out res);
                }
                return res;
            }
            ).ToList();

            for (int i = 0; i < SpriteFiles.Count(); i++)
            {
                string fileName = Path.GetFileName(SpriteFiles[i]);

                FileStream fs = File.OpenRead(SpriteFiles[i]);
                fs.Seek(0xc, SeekOrigin.Begin);

                uint height = readUIntSmallEndian(fs);
                uint width = readUIntSmallEndian(fs);

                fs.Seek(0x54, SeekOrigin.Begin);

                string compressiontype = readString(fs, 4);

                if (compressiontype == "")
                    compressiontype = "No Compression";


                info.AppendLine("--" + (startFileNo + i) + "--");
                /*
                    FHMOffset: 273712
                    Size: 2144
                    FHMAssetLoadEnum: 1
                    FHMunkEnum: 0
                    FHMFileNo: 3
                    Header: NTP3
                    Number of Files: 1
                    #DDS: 1
                    Name: 0000
                    DDS Data Chunk Size: 2048
                    NTP3 Header Chunk Size: 80
                    numberofMipmaps: 1
                    Width Resolution: 100
                    Height Resolution: 100
                    Compression Type: DXT5
                    eXtChunk: ZVh0AAAAACAAAAAQAAAAAA==
                    GIDXChunk: R0lEWAAAABAAAAAAAAAAAA==
                    fileName: 008.dds 
                 */
                info.AppendLine("FHMOffset: 0");
                info.AppendLine("Size: 0");
                info.AppendLine("FHMAssetLoadEnum: 1");
                info.AppendLine("FHMunkEnum: 0");
                info.AppendLine("FHMFileNo: " + FHMFileNo);
                info.AppendLine("Header: NTP3");
                info.AppendLine("Number of Files: 1");
                info.AppendLine("#DDS: 1");
                info.AppendLine("Name: 0000");
                info.AppendLine("DDS Data Chunk Size: 0");
                info.AppendLine("NTP3 Header Chunk Size: 0");
                info.AppendLine("numberofMipmaps: 1");
                info.AppendLine("Width Resolution: " + width);
                info.AppendLine("Height Resolution: " + height);
                info.AppendLine("Compression Type: " + compressiontype);
                if (compressiontype == "No Compression")
                    info.AppendLine("pixelFormat: AbgrExt");
                info.AppendLine("eXtChunk: ZVh0AAAAACAAAAAQAAAAAA==");
                info.AppendLine("GIDXChunk: R0lEWAAAABAAAAAAAAAAAA==");
                info.AppendLine("fileName: " + fileName);
                info.AppendLine("");
                info.AppendLine("");
                info.AppendLine(@"//");
            }

            return info.ToString();
        }
    }
}
