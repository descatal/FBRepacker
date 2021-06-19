using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data
{
    class Reverse32ByteEndian : Internals
    {
        public Reverse32ByteEndian()
        {
            FileStream fs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Sprites Factory\Awakening Repack Combined - DFA5898F\Awakening Cut In Costume 1 Sprite - DFA5898F\001-MBON\002.LMB");
            changeStreamFile(fs);

            Stream.Seek(0, SeekOrigin.Begin);
            byte[] fsb = new byte[fs.Length];

            fs.Read(fsb, 0, (int)fs.Length);
            byte[] fso = reverseEndianess(fsb, 0x4);

            fs.Close();

            FileStream ofs = File.Create(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Converted from MBON\Awakening.LMB");
            ofs.Write(fso, 0, fso.Length);
            ofs.Close();
        }
    }
}
