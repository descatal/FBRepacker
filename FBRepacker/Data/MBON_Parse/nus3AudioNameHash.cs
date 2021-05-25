using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class nus3AudioNameHash : Internals
    {
        uint STREAM_ID = 0x1E;

        public enum formatEnum
        {
            AT3,
            IS14,
            VAG
        }

        string main_title = "ST_VO_80_P22";//"VO_80_P22"; //"ST_VO_80_P22";

        formatEnum format = formatEnum.VAG;

        Dictionary<formatEnum, string> extension = new Dictionary<formatEnum, string>() { { formatEnum.AT3, ".at3" }, { formatEnum.IS14, ".is14" }, { formatEnum.VAG, ".vag" } };

        public nus3AudioNameHash()
        {
            FileStream fs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice Boss METEOR\Original MBON\SoundEffects.nus3bank");//@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice METEOR\Original MBON\Infinite Justice Local.nus3audio");
            changeStreamFile(fs);

            uint nus3 = readUIntBigEndian();
            if (nus3 != 0x4E555333)
                throw new Exception("Not Nus3!");

            uint size = readUIntSmallEndian();
            uint typeMagic = readUIntBigEndian();

            if (typeMagic == 0x42414E4B) // Bank
            {
                parseNUS3BANK(fs);
            }
            else
            {
                parseNUS3AUDIO(fs);
            }
        }

        public void parseNUS3AUDIO(FileStream fs)
        {
            uint INDX = readUIntBigEndian();
            uint INDX_Size = readUIntSmallEndian();

            uint audio_Entries = readUIntSmallEndian();

            uint TNID = readUIntBigEndian();
            if (TNID != 0x544E4944)
                throw new Exception("Not TNID!");

            uint TNID_Size = readUIntSmallEndian();
            fs.Seek(TNID_Size, SeekOrigin.Current);

            uint NMOF = readUIntBigEndian();
            if (NMOF != 0x4E4D4F46)
                throw new Exception("Not NMOF!");

            uint NMOF_Size = readUIntSmallEndian();

            StringBuilder str = new StringBuilder();
            uint oriPos = (uint)fs.Position;
            List<string> hashList = new List<string>();
            List<byte> newStr = new List<byte>();
            for (int i = 0; i < audio_Entries; i++)
            {
                uint hashPointer = readUIntSmallEndian();
                uint returnAddress = (uint)Stream.Position;

                fs.Seek(hashPointer, SeekOrigin.Begin);

                byte trailingZero = (byte)fs.ReadByte();
                while (trailingZero != 0)
                {
                    newStr.Add(trailingZero);
                    trailingZero = (byte)fs.ReadByte();
                    if (trailingZero == 0)
                    {
                        uint strSize = (uint)fs.Position - oriPos;
                        uint retPos = (uint)fs.Position;
                        Stream.Seek(oriPos, SeekOrigin.Begin);
                        string UpperCase = Encoding.UTF8.GetString(newStr.ToArray()).ToUpper();

                        var crc32 = new Crc32();
                        string UpperHash = crc32.Get(Encoding.UTF8.GetBytes(UpperCase)).ToString("X8");

                        str.Append((i + 1) + " : " + UpperCase + " - " + UpperHash);
                        str.Append(Environment.NewLine);

                        str.Append(Environment.NewLine);

                        hashList.Add(UpperCase);
                        newStr = new List<byte>();
                        Stream.Seek(retPos, SeekOrigin.Begin);
                    }
                }

                fs.Seek(returnAddress, SeekOrigin.Begin);
            }

            writeSoundHash(audio_Entries, hashList);

            StreamWriter txt = File.CreateText(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice METEOR\Converted from MBON\Infinite Justice (Boss) Local Sorted.txt");
            txt.Write(str);

            txt.Close();
        }

        public void parseNUS3BANK(FileStream fs)
        {
            uint true_offset = 0;

            // https://github.com/vgmstream/vgmstream/blob/master/src/meta/nus3bank.c
            uint TOC = readUIntBigEndian();
            uint TOCSize = readUIntSmallEndian(); // Size of the size from this position

            true_offset += TOCSize + 0x14; // 0x14 for the current position.

            uint chunkCount = readUIntSmallEndian();
            if (chunkCount != 7)
                throw new Exception("Chunk Count not 7!");

            uint BINFOffset = 0;
            uint TONEOffset = 0;

            for(int i = 0; i < chunkCount; i++)
            {
                uint chunk_magic = readUIntBigEndian();
                uint chunk_size = readUIntSmallEndian(); 

                if (chunk_magic == 0x42494E46) // BINF
                    BINFOffset = true_offset;

                if (chunk_magic == 0x544F4E45) // TONE
                    TONEOffset = true_offset;

                if (BINFOffset != 0 && TONEOffset != 0)
                    break;

                true_offset += 0x8 + chunk_size; // 0x8 for magic and size
            }

            if (TONEOffset == 0 || BINFOffset == 0)
                throw new Exception("TONE or BINF not found!");

            fs.Seek(BINFOffset, SeekOrigin.Begin);

            uint BINF = readUIntBigEndian();
            if (BINF != 0x42494E46)
                throw new Exception("Not BINF!");

            fs.Seek(0xc, SeekOrigin.Current);

            int main_str_size = fs.ReadByte();
            main_title = readString(fs.Position, main_str_size);

            fs.Seek(TONEOffset, SeekOrigin.Begin);
            uint TONE = readUIntBigEndian();
            if (TONE != 0x544F4E45)
                throw new Exception("Not TONE!");

            uint TONE_size = readUIntSmallEndian();

            uint base_jump_address = (uint)fs.Position;
            uint audio_Entries = readUIntSmallEndian();

            List<string> hashList = new List<string>();

            for (int i = 0; i < audio_Entries; i++)
            {
                uint offset = readUIntSmallEndian();
                uint size = readUIntSmallEndian();
                uint return_address = (uint)fs.Position;

                fs.Seek(base_jump_address + offset, SeekOrigin.Begin);
                fs.Seek(0xC, SeekOrigin.Current);

                int str_size = fs.ReadByte();
                string str = readString(fs.Position, str_size);

                hashList.Add(str);

                fs.Seek(return_address, SeekOrigin.Begin);
            }

            writeSoundHash(audio_Entries, hashList);
        }

        public void writeSoundHash(uint audio_Entries, List<string> hashList)
        {

            MemoryStream soundHash = new MemoryStream();

            appendUIntMemoryStream(soundHash, 0, true);
            appendUIntMemoryStream(soundHash, STREAM_ID, true);
            appendUIntMemoryStream(soundHash, audio_Entries, true);

            // 0x20 = General Header
            // 0x20 = Main Title String (e.g. ST_VO_16_P01)
            // 0x40 = 1 audio Entry string length
            // 0x2 = 1 more 0x40 for string with extension.
            uint fileSize = 0x20 + 0x20 + 0x40 * audio_Entries * 0x2;
            appendUIntMemoryStream(soundHash, fileSize, true);

            appendUIntMemoryStream(soundHash, 0x40, true);
            appendUIntMemoryStream(soundHash, 0x40, true);
            appendZeroMemoryStream(soundHash, 0x08);

            appendStringMemoryStream(soundHash, main_title, Encoding.Default, 0x20);

            string ext = extension[format];

            bool sort = true;
            if (sort)
                hashList.Sort();

            for (int i = 0; i < audio_Entries; i++)
            {
                appendStringMemoryStream(soundHash, hashList[i], Encoding.Default, 0x40);
                appendStringMemoryStream(soundHash, (hashList[i] + ext).ToLower(), Encoding.Default, 0x40);
            }

            FileStream ofs = File.Create(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice Boss METEOR\Converted from MBON\SoundEffects.soundhash");//@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice METEOR\Converted from MBON\Infinite Justice (Boss) Local Sorted.bin");

            soundHash.Seek(0, SeekOrigin.Begin);
            soundHash.CopyTo(ofs);

            ofs.Close();

            /*
            fs.Seek(0xFC8, SeekOrigin.Begin);

            int count = 1;
            while (fs.Position < 0x2425)
            {
                byte by = (byte)fs.ReadByte();
                if(by == 0)
                {
                    uint strSize = (uint)fs.Position - oriPos;
                    uint retPos = (uint)fs.Position;
                    Stream.Seek(oriPos, SeekOrigin.Begin);
                    string UpperCase = Encoding.UTF8.GetString(newStr.ToArray()).ToUpper();
                    //string LowerCase = Encoding.UTF8.GetString(newStr.ToArray()).ToLower();

                    var crc32 = new Crc32();
                    string UpperHash = crc32.Get(Encoding.UTF8.GetBytes(UpperCase)).ToString("X8");
                    //string LowerHash = crc32.Get(Encoding.UTF8.GetBytes(LowerCase)).ToString("X8");

                    str.Append(count + " : " + UpperCase + " - " + UpperHash);
                    str.Append(Environment.NewLine);
                    //str.Append(LowerCase + " - " + LowerHash);
                    //str.Append(Environment.NewLine);

                    str.Append(Environment.NewLine);

                    newStr = new List<byte>();
                    Stream.Seek(retPos, SeekOrigin.Begin);
                    count++;
                }
                else
                {
                    newStr.Add(by);
                }
            }
            */
        }
    }
}
