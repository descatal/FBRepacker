using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class Parse_LMB : Internals
    {
        public Parse_LMB()
        {
            FileStream fs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Sprites Factory\Awakening Repack Combined - DFA5898F\Awakening Cut In Costume 1 Sprite - DFA5898F\001-MBON\002.LMB");
            string outputPath = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\MBON Units\Bael\Converted from MBON\Awakening.LMB";

            // MBON version:
            uint magic = readUIntBigEndian(fs);

            if (magic != 0x4C4D4200)
                throw new Exception("not .LMB header!");

            byte[] header_arr = new byte[0x3C];
            fs.Read(header_arr, 0, 0x3c);
            MemoryStream header = new MemoryStream(header_arr);
            MemoryStream header_rev = reverseEndianess(header, 4);

            uint unk_0xF001 = readUIntSmallEndian(fs);
            if (unk_0xF001 != 0xF001)
                throw new Exception("0x40 is not 0xF001!");

            int unk_0xF001_Size = (int)readUIntSmallEndian(fs);
            uint unk_temp_0xF001_Size = (uint)unk_0xF001_Size;
            uint unk_0xF001_0x8 = readUIntSmallEndian(fs);
            uint unk_0xF001_0xC = readUIntSmallEndian(fs);
            uint unk_0xF001_0x10 = readUIntSmallEndian(fs);

            unk_0xF001_Size -= 0x3;

            List<string> unk_0xF001_str = new List<string>();
            do
            {
                uint unk_0xF001_str_length = readUIntSmallEndian(fs);
                uint append_size = 0x4 - (unk_0xF001_str_length % 0x4);
                uint true_length = (append_size != 0 ? append_size : 0x4) + unk_0xF001_str_length;
                string str = readString(fs, (int)unk_0xF001_str_length);
                fs.Seek(append_size, SeekOrigin.Current);
                unk_0xF001_str.Add(str);
                unk_0xF001_Size -= (int)(true_length / 0x4 + 1);
            } while (unk_0xF001_Size > 0);

            uint unk_0xF002 = readUIntSmallEndian(fs);
            if (unk_0xF002 != 0xF002)
                throw new Exception("0xF002 not found!");

            long data_size = fs.Length - fs.Position;
            byte[] data = new byte[data_size];
            fs.Read(data, 0, (int)data_size);
            MemoryStream data_mem = new MemoryStream(data);
            MemoryStream data_rev = reverseEndianess(data_mem, 4);

            // Write
            MemoryStream LMB = new MemoryStream();

            appendUIntMemoryStream(LMB, 0x4C4D4200, true);

            header_rev.Seek(0, SeekOrigin.Begin);
            header_rev.CopyTo(LMB);

            // F001
            appendUIntMemoryStream(LMB, 0xF001, true);
            // Should count the size instead of using original
            // TODO for proper implementations
            appendUIntMemoryStream(LMB, (uint)unk_temp_0xF001_Size, true);
            appendUIntMemoryStream(LMB, unk_0xF001_0x8, true);
            appendUIntMemoryStream(LMB, unk_0xF001_0xC, true);
            appendUIntMemoryStream(LMB, unk_0xF001_0x10, true);
        
            for(int i = 0; i < unk_0xF001_str.Count(); i++)
            {
                string str = unk_0xF001_str[i];
                byte[] str_enc = Encoding.Default.GetBytes(str);
                uint unk_0xF001_str_length = (uint)str_enc.Length;
                uint append_size = 0x4 - (unk_0xF001_str_length % 0x4);
                uint true_length = (append_size != 0 ? append_size : 0x4);
                appendUIntMemoryStream(LMB, unk_0xF001_str_length, true);
                appendStringMemoryStream(LMB, str, Encoding.Default);
                appendZeroMemoryStream(LMB, (int)true_length);
            }

            appendUIntMemoryStream(LMB, 0xF002, true);

            data_rev.Seek(0, SeekOrigin.Begin);
            data_rev.CopyTo(LMB);

            FileStream ofs = File.Create(outputPath);
            LMB.Seek(0, SeekOrigin.Begin);
            LMB.CopyTo(ofs);
            ofs.Close();
        }
    }
}
