using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Tools
{
    class Reverse32ByteEndian : Internals
    {
        public Reverse32ByteEndian()
        {
            FileStream fs = File.OpenRead(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Small patches\Untitled10.bin");
            changeStreamFile(fs);

            Stream.Seek(0, SeekOrigin.Begin);
            byte[] fsb = new byte[fs.Length];

            fs.Read(fsb, 0, (int)fs.Length);
            byte[] fso = reverseEndianess(fsb, 0x4);

            fs.Close();

            FileStream ofs = File.Create(@"D:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Total MBON Small patches\Untitled11.bin");
            ofs.Write(fso, 0, fso.Length);
            ofs.Close();
        }
    }
}
