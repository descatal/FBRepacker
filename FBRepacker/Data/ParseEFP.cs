using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data
{
    class ParseEFP : Internals
    {
        public ParseEFP()
        {
            
            /*
            EFP MBON_EFP = readEFP(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common MBON\Common EFP - 34F85A51\001-FHM\002.bin");
            
            List<EFP_Properties> MBON_Prop = MBON_EFP.EFP_Properties;
            List<EFP_Properties> FB_Prop = FB_EFP.EFP_Properties;

            List<uint> EFPMBON = MBON_Prop.Select(x => x.EFP_hash).ToList();
            List<uint> EFPFB = FB_Prop.Select(x => x.EFP_hash).ToList();

            List<uint> exception = EFPFB.Except(EFPMBON).ToList();

            //
            Dictionary<uint, List<EFP_Properties>> result = MBON_Prop.Concat(FB_Prop)
                .GroupBy(x => x.EFP_hash)
                .ToDictionary(g => g.Key, g => g.ToList());
            */

            /*
            var results = MBON_Prop.Join(FB_Prop, l1 => l1.EFP_hash, l2 => l2.EFP_hash,
                (lhs, rhs) => new { ID = lhs.EFP_hash, Name1 = lhs, Name2 = rhs }
            ).ToList();
            //

            for (int i = 0; i < exception.Count; i++)
            {
                EFP_Properties EFPHash = FB_Prop.FirstOrDefault(x => x.EFP_hash == exception[i]);
                MBON_Prop.Add(EFPHash);
            }
            */

            //writeEFP(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common MBON\Common EFP - 34F85A51\001-FHM\test.bin", MBON_EFP);
        }

        public void parseEFP()
        {
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB Extract\1.09 EFP - PATCH34F85A51\001-FHM\002.bin");

            EFP FB_EFP = readEFP(Properties.Settings.Default.inputEFPBinary);
            string JSON = JsonConvert.SerializeObject(FB_EFP, Formatting.Indented);

            string JSONPath = Properties.Settings.Default.outputEFPJSONPath + @"/" + Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputEFPBinary) + ".json";

            StreamWriter JSONsw = File.CreateText(JSONPath);
            JSONsw.Write(JSON);
            JSONsw.Close();
        }

        public void serializeEFP()
        {
            StreamReader streamReader = File.OpenText(Properties.Settings.Default.inputEFPJSON);
            string JSON = streamReader.ReadToEnd();
            streamReader.Close();

            string binaryPath = Properties.Settings.Default.outputEFPBinaryPath + @"/" + Path.GetFileNameWithoutExtension(Properties.Settings.Default.inputEFPJSON) + ".bin";

            EFP efp = JsonConvert.DeserializeObject<EFP>(JSON);
            writeEFP(binaryPath, efp);
        }

        public EFP readEFP(string EFPBinaryPath)
        {
            FileStream fs = File.OpenRead(EFPBinaryPath);

            uint EFP_Magic = readUIntBigEndian(fs);
            if (EFP_Magic != 0x45465020)
                throw new Exception("The file is not EFP!");

            uint version = readUIntBigEndian(fs);
            if (version != 0x3)
                throw new Exception("Version is not 3!");

            uint EFP_Count = readUIntBigEndian(fs);
            uint unk_0xC = readUIntBigEndian(fs); // Always 0

            EFP EFP = new EFP();
            EFP.schemaVersion = 1;

            for(int i = 0; i < EFP_Count; i++)
            {
                EFP_Properties EFP_Properties = new EFP_Properties();

                uint EFP_hash = readUIntBigEndian(fs);
                EFP_Properties.EFP_hash = EFP_hash;

                uint EFP_Pointer = readUIntBigEndian(fs);
                long returnPos = fs.Position;

                uint nextPointer = 0;
                if(i != EFP_Count - 1)
                {
                    fs.Seek(0x4, SeekOrigin.Current);
                    nextPointer = readUIntBigEndian(fs);
                }
                else
                {
                    nextPointer = 0x10 + EFP_Count * 0x8;
                }

                fs.Seek(EFP_Pointer, SeekOrigin.Begin);

                uint EFP_Type = readUIntBigEndian(fs);
                EFP_Properties.EFP_Type = EFP_Type;

                uint ALEO_Hash = readUIntBigEndian(fs);
                EFP_Properties.ALEO_Hash = ALEO_Hash;

                uint Model_Hash = readUIntBigEndian(fs);
                EFP_Properties.Model_Hash = Model_Hash;

                uint unk_Enum = readUIntBigEndian(fs);
                EFP_Properties.unk_Enum = unk_Enum;

                float translate_X_Offset = readFloat(fs, true);
                EFP_Properties.translate_X_Offset = translate_X_Offset;

                float translate_Y_Offset = readFloat(fs, true);
                EFP_Properties.translate_Y_Offset = translate_Y_Offset;

                float translate_Z_Offset = readFloat(fs, true);
                EFP_Properties.translate_Z_Offset = translate_Z_Offset;

                float rotate_X_Offset = readFloat(fs, true);
                EFP_Properties.rotate_X_Offset = rotate_X_Offset;

                float rotate_Y_Offset = readFloat(fs, true);
                EFP_Properties.rotate_Y_Offset = rotate_Y_Offset;

                float rotate_Z_Offset = readFloat(fs, true);
                EFP_Properties.rotate_Z_Offset = rotate_Z_Offset;

                float size = readFloat(fs, true);
                EFP_Properties.size = size;

                while(fs.Position < nextPointer)
                {
                    uint extra_Info = readUIntBigEndian(fs);
                    EFP_Properties.extra_Info.Add(extra_Info);
                }

                fs.Seek(returnPos, SeekOrigin.Begin);

                EFP.EFP_Properties.Add(EFP_Properties);
            }

            fs.Close();

            return EFP;
        }

        public void writeEFP(string outputBinaryPath, EFP EFP)
        {
            MemoryStream EFPMS = new MemoryStream();
            MemoryStream EFPHashList = new MemoryStream();
            MemoryStream EFPpropertiesData = new MemoryStream();

            List<EFP_Properties> EFP_Properties = EFP.EFP_Properties;

            EFP_Properties = EFP_Properties.OrderBy(p => p.EFP_hash).ToList();

            List<EFP_Properties> EFP_Properties_0x80_Start = EFP_Properties.Where(x => x.EFP_hash >= 0x80000000).ToList();
            List<EFP_Properties> EFP_Properties_0_Start = EFP.EFP_Properties.Where(x => (x.EFP_hash > 0 && x.EFP_hash < 0x80000000)).ToList();

            appendUIntMemoryStream(EFPHashList, 0x45465020, true);
            appendUIntMemoryStream(EFPHashList, 0x3, true);
            appendUIntMemoryStream(EFPHashList, (uint)EFP_Properties.Count, true);
            appendUIntMemoryStream(EFPHashList, 0, true);

            uint EFPHashListSize = 0x10 + (uint)EFP_Properties.Count * 0x8;

            for (int i = 0; i < EFP_Properties_0x80_Start.Count; i++)
            {
                EFP_Properties EFP_Property = EFP_Properties_0x80_Start[i];
                appendUIntMemoryStream(EFPHashList, EFP_Property.EFP_hash, true);

                uint offset = EFPHashListSize + (uint)EFPpropertiesData.Length;
                appendUIntMemoryStream(EFPHashList, offset, true);

                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.EFP_Type, true);
                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.ALEO_Hash, true);
                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.Model_Hash, true);
                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.unk_Enum, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.translate_X_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.translate_Y_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.translate_Z_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.rotate_X_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.rotate_Y_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.rotate_Z_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.size, true);

                foreach(uint data in EFP_Property.extra_Info)
                {
                    appendUIntMemoryStream(EFPpropertiesData, data, true);
                }
            }

            for (int i = 0; i < EFP_Properties_0_Start.Count; i++)
            {
                EFP_Properties EFP_Property = EFP_Properties_0_Start[i];
                appendUIntMemoryStream(EFPHashList, EFP_Property.EFP_hash, true);

                uint offset = EFPHashListSize + (uint)EFPpropertiesData.Length;
                appendUIntMemoryStream(EFPHashList, offset, true);

                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.EFP_Type, true);
                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.ALEO_Hash, true);
                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.Model_Hash, true);
                appendUIntMemoryStream(EFPpropertiesData, EFP_Property.unk_Enum, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.translate_X_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.translate_Y_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.translate_Z_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.rotate_X_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.rotate_Y_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.rotate_Z_Offset, true);
                appendFloatMemoryStream(EFPpropertiesData, EFP_Property.size, true);

                foreach (uint data in EFP_Property.extra_Info)
                {
                    appendUIntMemoryStream(EFPpropertiesData, data, true);
                }
            }

            EFPHashList.Seek(0, SeekOrigin.Begin);
            EFPpropertiesData.Seek(0, SeekOrigin.Begin);

            EFPHashList.CopyTo(EFPMS);
            EFPpropertiesData.CopyTo(EFPMS);
            EFPMS.Seek(0, SeekOrigin.Begin);

            FileStream ofs = File.Create(outputBinaryPath);
            EFPMS.CopyTo(ofs);
            ofs.Close();
        }
    }
}
