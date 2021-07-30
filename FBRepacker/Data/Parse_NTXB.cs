using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data
{
    class Parse_NTXB : Internals
    {
        public int schema_version = 1;

        public void readNTXB()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.inputNTXBBinaryPath);
            uint magic = readUIntBigEndian(fs);

            if (magic != 0x4E545842)
                throw new Exception("File Magic is not NTXB!");

            ushort unk_0x4 = readUShort(fs, true);

            if (unk_0x4 != 0x0002)
                throw new Exception("Version is not supported!");

            ushort version = readUShort(fs, true);

            uint fileSize = readUIntBigEndian(fs);
            uint headerChunkSize = readUIntBigEndian(fs);

            if (headerChunkSize != 0x30)
                throw new Exception("Header chunk size is not 0x30!");

            uint stringInfoPointer = readUIntBigEndian(fs);
            uint nameInfoPointer = readUIntBigEndian(fs);
            uint unk_ChunkPointer = readUIntBigEndian(fs);
            uint stringDataPointer = readUIntBigEndian(fs);

            if(stringDataPointer - unk_ChunkPointer != 0x10)
                throw new Exception("unk_Chunk size is not 0x10!");

            fs.Seek(nameInfoPointer, SeekOrigin.Begin);

            uint nameInfoStringCount = readUIntBigEndian(fs);

            List<name_Str_Info> all_name_string_infos = new List<name_Str_Info>();
            for (int i = 0; i < nameInfoStringCount; i++)
            {
                name_Str_Info name_Str_Info = new name_Str_Info();
                ushort unk_ushort = readUShort(fs, true);
                name_Str_Info.name_Str_Crc16X25_Checksum = unk_ushort;

                ushort name_Str_Length = readUShort(fs, true);
                name_Str_Info.name_Str_Length = name_Str_Length;

                uint offset = readUIntBigEndian(fs);
                name_Str_Info.string_Info_Offset = offset;

                all_name_string_infos.Add(name_Str_Info);
            }

            // stringInfoPointer
            fs.Seek(stringInfoPointer, SeekOrigin.Begin);

            uint stringCount = readUIntBigEndian(fs);

            if (stringCount != nameInfoStringCount)
                throw new Exception("Never seen case of stringCount != nameInfoStringCount!");
            
            NTXB ntxb = new NTXB();

            ntxb.schema_version = 1;
            ntxb.NTXB_Version = version;

            List<NTXBInfo> all_string_infos = ntxb.NTXBInfo;

            for(int i = 0; i < stringCount; i++)
            {
                NTXBInfo string_info = new NTXBInfo();

                uint strOffset = readUIntBigEndian(fs);

                uint nameStrOffsetPosition = (uint)fs.Position - 0x4 - headerChunkSize;

                uint nameStrOffset = readUIntBigEndian(fs);
                uint unkStrOffset = readUIntBigEndian(fs);
                
                ushort unk_0xC = readUShort(fs, true);
                ushort unk_0xE = readUShort(fs, true);

                if (unk_0xC != 0xFFFF)
                    throw new Exception("string Info's unk_0xC is not 0xFFFF");

                string_info.str_Info.str_info_unk_0xC = unk_0xC;
                string_info.str_Info.str_info_unk_0xE = unk_0xE;

                long returnPos = fs.Position;

                fs.Seek(stringDataPointer + strOffset, SeekOrigin.Begin);
                ushort unk_str_ushort = readUShort(fs, true);

                string_info.string_Data.unk_str_ushort = unk_str_ushort;

                ushort str_length = readUShort(fs, true);
                string str = readString(fs, '\0');

                string_info.str = str;

                
                name_Str_Info name_Str_Info = all_name_string_infos.FirstOrDefault(x => x.string_Info_Offset == nameStrOffsetPosition);

                if (name_Str_Info == null)
                    throw new Exception("Cannot find corresponding name Str Offset!");

                fs.Seek(stringDataPointer + nameStrOffset, SeekOrigin.Begin);

                string name_Str = readString(fs, name_Str_Info.name_Str_Length);

                string_info.nameStr = name_Str;
                string_info.name_Info.str_Name_Crc16X25_Checksum = name_Str_Info.name_Str_Crc16X25_Checksum;


                fs.Seek(stringDataPointer + unkStrOffset, SeekOrigin.Begin);
                uint unk_Str = readUIntBigEndian(fs);

                if (unk_Str != 0)
                    throw new Exception("unk_Str not 0!");

                string_info.string_Data.unk_Str = unk_Str.ToString();

                all_string_infos.Add(string_info);

                fs.Seek(returnPos, SeekOrigin.Begin);
            }

            /*
            List<NTXBInfo> orderedNTXBInfo = new List<NTXBInfo>();

            orderedNTXBInfo = ntxb.NTXBInfo.OrderBy(p => p.name_Info.str_Name_Crc16X25_Checksum).ToList();

            ntxb.NTXBInfo = orderedNTXBInfo;
            */

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputNTXBBinaryPath);

            string JSON = JsonConvert.SerializeObject(ntxb, Formatting.Indented);

            StreamWriter sw = File.CreateText(Properties.Settings.Default.outputNTXBJSONPath + @"\" + fileName + " - NTXB.json");
            sw.Write(JSON);

            sw.Close();
        }

        public void writeNTXB()
        {
            StreamReader sR = File.OpenText(Properties.Settings.Default.inputNTXBJSONPath);
            string JSON = sR.ReadToEnd();
            sR.Close();
            NTXB NTXB = JsonConvert.DeserializeObject<NTXB>(JSON);

            MemoryStream headerMS = new MemoryStream();
            MemoryStream strInfoMS = new MemoryStream();
            MemoryStream nameInfoMS = new MemoryStream();
            MemoryStream unkChunkMS = new MemoryStream();
            MemoryStream strDataMS = new MemoryStream();

            List<NTXBInfo> NTXBInfos = NTXB.NTXBInfo;

            Dictionary<string, uint> strPointers = new Dictionary<string, uint>();
            Dictionary<string, uint> nameStrPointers = new Dictionary<string, uint>();
            List<uint> unkStrPointers = new List<uint>();

            // STR DATA
            appendZeroMemoryStream(strDataMS, 4); // Reserve 4 bytes for size.
            for (int i = 0; i < NTXBInfos.Count; i++)
            {
                NTXBInfo NTXBInfo = NTXBInfos[i];

                ushort strLength = (ushort)NTXBInfo.str.Length;

                switch (NTXB.NTXB_Version)
                {
                    // they use the same pointer instead of writing it again and again
                    case 0x100:
                        if (!strPointers.ContainsKey(NTXBInfo.str))
                        {
                            strPointers[NTXBInfo.str] = ((uint)strDataMS.Position);

                            appendUShortMemoryStream(strDataMS, NTXBInfo.string_Data.unk_str_ushort, true);

                            appendUShortMemoryStream(strDataMS, strLength, true);
                            appendPaddedStringMemoryStream(strDataMS, NTXBInfo.str, Encoding.UTF8, 0);
                        }

                        if (!nameStrPointers.ContainsKey(NTXBInfo.nameStr))
                        {
                            nameStrPointers[NTXBInfo.nameStr] = ((uint)strDataMS.Position);
                            appendPaddedStringMemoryStream(strDataMS, NTXBInfo.nameStr, Encoding.UTF8, 1);
                        }

                        if (NTXBInfo.string_Data.unk_Str != "0")
                            throw new Exception("unk_Str is not 0!");

                        if (unkStrPointers.Count == 0)
                        {
                            unkStrPointers.Add((uint)strDataMS.Position);
                            appendUIntMemoryStream(strDataMS, 0, true);
                        }
                        else
                        {
                            unkStrPointers.Add(unkStrPointers.First());
                        }
                        break;

                    case 0x200:
                        strPointers[NTXBInfo.str] = ((uint)strDataMS.Position);
                        appendUShortMemoryStream(strDataMS, NTXBInfo.string_Data.unk_str_ushort, true);

                        appendUShortMemoryStream(strDataMS, strLength, true);
                        appendPaddedStringMemoryStream(strDataMS, NTXBInfo.str, Encoding.UTF8, 0);

                        nameStrPointers[NTXBInfo.nameStr] = ((uint)strDataMS.Position);
                        appendPaddedStringMemoryStream(strDataMS, NTXBInfo.nameStr, Encoding.UTF8, 1);

                        if (NTXBInfo.string_Data.unk_Str != "0")
                            throw new Exception("unk_Str is not 0!");

                        unkStrPointers.Add((uint)strDataMS.Position);
                        appendUIntMemoryStream(strDataMS, 0, true);
                        break;
                }
            }

            uint strDataChunkSize = (uint)strDataMS.Length - 0x4;
            uint strDataChunkSizeBE = BinaryPrimitives.ReverseEndianness(strDataChunkSize);
            byte[] sizeByte = BitConverter.GetBytes(strDataChunkSizeBE);

            strDataMS.Seek(0, SeekOrigin.Begin);
            strDataMS.Write(sizeByte, 0, 0x4);

            // Pad 0xFF until align
            addPaddingStream(strDataMS, 0xFF);


            // STR INFO
            Dictionary<string, uint> strInfoNameStrOffset = new Dictionary<string, uint>();
            appendUIntMemoryStream(strInfoMS, (uint)NTXB.NTXBInfo.Count, true);
            for (int i = 0; i < NTXBInfos.Count; i++)
            {
                NTXBInfo NTXBInfo = NTXBInfos[i];
                appendUIntMemoryStream(strInfoMS, strPointers[NTXBInfo.str], true);

                strInfoNameStrOffset[NTXBInfo.nameStr] = ((uint)strInfoMS.Position - 0x4);

                appendUIntMemoryStream(strInfoMS, nameStrPointers[NTXBInfo.nameStr], true);
                appendUIntMemoryStream(strInfoMS, unkStrPointers[i], true);
                appendUShortMemoryStream(strInfoMS, NTXBInfo.str_Info.str_info_unk_0xC, true);
                appendUShortMemoryStream(strInfoMS, NTXBInfo.str_Info.str_info_unk_0xE, true);


                // calculate crc16 checksum for name str info
                // Without this checksum the string won't work
                // http://www.metools.info/code/c15.html
                // https://github.com/invertedtomato/crc
                var crc = InvertedTomato.IO.CrcAlgorithm.CreateCrc16X25();
                crc.Append(Encoding.UTF8.GetBytes(NTXBInfo.nameStr));
                UInt64 checkSum = crc.ToUInt64();
                NTXBInfo.name_Info.str_Name_Crc16X25_Checksum = (ushort)checkSum;
            }

            // Pad 0xFF until align
            addPaddingStream(strInfoMS, 0xFF);


            // NAME STR INFO
            appendUIntMemoryStream(nameInfoMS, (uint)NTXB.NTXBInfo.Count, true);

            // Order this list based on checksum from small to large.
            List<NTXBInfo> NTXBInfoSortedbyName = NTXBInfos.OrderBy(p => p.name_Info.str_Name_Crc16X25_Checksum).ToList();
            for (int i = 0; i < NTXBInfoSortedbyName.Count; i++)
            {
                NTXBInfo NTXBInfo = NTXBInfoSortedbyName[i];
                appendUShortMemoryStream(nameInfoMS, NTXBInfo.name_Info.str_Name_Crc16X25_Checksum, true);

                ushort nameStrLength = (ushort)NTXBInfo.nameStr.Length;
                appendUShortMemoryStream(nameInfoMS, nameStrLength, true);

                uint strInfoOffset = strInfoNameStrOffset[NTXBInfo.nameStr];
                appendUIntMemoryStream(nameInfoMS, strInfoOffset, true);
            }

            addPaddingStream(nameInfoMS, 0xFF);

            // unknown chunk, always 00 00 00 00 FF FF FF FF FF FF FF FF FF FF FF FF
            appendUIntMemoryStream(unkChunkMS, 0, true);
            appendUIntMemoryStream(unkChunkMS, 0xFFFFFFFF, true);
            appendUIntMemoryStream(unkChunkMS, 0xFFFFFFFF, true);
            appendUIntMemoryStream(unkChunkMS, 0xFFFFFFFF, true);


            // Header
            // Magic
            appendUIntMemoryStream(headerMS, 0x4E545842, true);

            appendUShortMemoryStream(headerMS, 0x0002, true);

            if (NTXB.NTXB_Version != 0x100 && NTXB.NTXB_Version != 0x200)
                throw new Exception("NTXB version not supported!");

            appendUShortMemoryStream(headerMS, (ushort)NTXB.NTXB_Version, true);

            uint totalFileSize = 0x30 + (uint)strInfoMS.Length + (uint)nameInfoMS.Length + (uint)unkChunkMS.Length + (uint)strDataMS.Length;

            appendUIntMemoryStream(headerMS, totalFileSize, true);
            appendUIntMemoryStream(headerMS, 0x30, true); // Will always be the same header size
            appendUIntMemoryStream(headerMS, 0x30, true); // Starting pointer for strInfoMs
            appendUIntMemoryStream(headerMS, 0x30 + (uint)strInfoMS.Length, true); // Starting pointer for nameInfoMS
            appendUIntMemoryStream(headerMS, 0x30 + (uint)strInfoMS.Length + (uint)nameInfoMS.Length, true); // Starting pointer for unkChunkMS
            appendUIntMemoryStream(headerMS, 0x30 + (uint)strInfoMS.Length + (uint)nameInfoMS.Length + (uint)unkChunkMS.Length, true); // Starting pointer for strDataMS

            // Pad the next 0x10 with 0xFFFFFFFF;
            appendUIntMemoryStream(headerMS, 0xFFFFFFFF, true);
            appendUIntMemoryStream(headerMS, 0xFFFFFFFF, true);
            appendUIntMemoryStream(headerMS, 0xFFFFFFFF, true);
            appendUIntMemoryStream(headerMS, 0xFFFFFFFF, true);

            headerMS.Seek(0, SeekOrigin.Begin);
            strInfoMS.Seek(0, SeekOrigin.Begin);
            nameInfoMS.Seek(0, SeekOrigin.Begin);
            unkChunkMS.Seek(0, SeekOrigin.Begin);
            strDataMS.Seek(0, SeekOrigin.Begin);

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputNTXBJSONPath);
            FileStream NTXBMS = File.Create(Properties.Settings.Default.outputNTXBBinaryPath + @"\" + fileName + ".bin");

            headerMS.CopyTo(NTXBMS);
            strInfoMS.CopyTo(NTXBMS);
            nameInfoMS.CopyTo(NTXBMS);
            unkChunkMS.CopyTo(NTXBMS);
            strDataMS.CopyTo(NTXBMS);

            NTXBMS.Close();

            // Save JSON
            FileStream JSfs = File.OpenRead(Properties.Settings.Default.inputNTXBJSONPath);
            string JSfsPath = Path.GetDirectoryName(Properties.Settings.Default.inputNTXBJSONPath);
            
            FileStream JSfsBackup = File.Create(JSfsPath + @"\" + fileName + @" - Backup.json");
            JSfs.CopyTo(JSfsBackup);
            JSfsBackup.Close();
            JSfs.Close();

            string JSONnew = JsonConvert.SerializeObject(NTXB, Formatting.Indented);

            StreamWriter sw = File.CreateText(Properties.Settings.Default.inputNTXBJSONPath);
            sw.Write(JSONnew);

            sw.Close();
        }
    }

    class name_Str_Info
    {
        public ushort name_Str_Crc16X25_Checksum { get; set; }
        public ushort name_Str_Length { get; set; }
        public uint string_Info_Offset { get; set; }
    }
}
