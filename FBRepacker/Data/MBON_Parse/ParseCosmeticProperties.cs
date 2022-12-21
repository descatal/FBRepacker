using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class ParseCosmeticProperties : Internals
    {
        public ParseCosmeticProperties()
        {
            
        }

        public void MBONConvert()
        {
            FileStream FBfs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB Extract\1.08 Latest Cosmetic ID List - PATCHCCABBB3B\001-FHM\002.bin");
            FileStream MBONfs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common MBON\Cosmetic List - CCABBB3B\001-FHM\002.bin");

            CosmeticList FBCosmeticList = new CosmeticList();
            CosmeticList MBONCosmeticList = new CosmeticList();
            CosmeticList nMBONCosmeticList = new CosmeticList();

            FBCosmeticList = parseCosmeticList(FBfs);
            MBONCosmeticList = parseCosmeticList(MBONfs);
            nMBONCosmeticList.properties = new Dictionary<uint, CosmeticProperties>(MBONCosmeticList.properties);

            foreach (var cos in MBONCosmeticList.properties)
            {
                if (FBCosmeticList.properties.ContainsKey(cos.Key))
                {
                    nMBONCosmeticList.properties[cos.Key].body_ALEO_hash = FBCosmeticList.properties[cos.Key].body_ALEO_hash;
                }
            }

            MBONCosmeticList.properties = nMBONCosmeticList.properties;

            // MBON will have more Cosmetic
            // Filter out cosmetic removed in MBON
            Dictionary<uint, CosmeticProperties> removed_cosmetic = FBCosmeticList.properties.Where(s => !MBONCosmeticList.properties.ContainsKey(s.Key)).ToDictionary(entry => entry.Key, entry => entry.Value);

            // Append the removed stuff into MBON
            foreach (var cosmetic in removed_cosmetic)
            {
                MBONCosmeticList.properties[cosmetic.Key] = cosmetic.Value;
            }

            MemoryStream oMBONfs = new MemoryStream();
            appendUIntMemoryStream(oMBONfs, 0x23A56922, true);
            appendUIntMemoryStream(oMBONfs, 0x2, true);
            appendUIntMemoryStream(oMBONfs, 0, true);
            appendUIntMemoryStream(oMBONfs, 0, true);

            appendUIntMemoryStream(oMBONfs, (uint)(MBONCosmeticList.properties.Count), true);
            foreach (var MBON in MBONCosmeticList.properties)
            {
                appendUIntMemoryStream(oMBONfs, MBON.Key, true);
            }
            foreach (var MBON in MBONCosmeticList.properties)
            {
                CosmeticProperties cosmetic = MBON.Value;
                appendUIntMemoryStream(oMBONfs, cosmetic.cosmetic_enum, true);
                appendFloatMemoryStream(oMBONfs, cosmetic.size, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.body_ALEO_hash, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_ALEO_hash, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_enum_0x10, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.muzzle_ALEO_hash, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.collision_ALEO_hash, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x1C, true);
                appendFloatMemoryStream(oMBONfs, cosmetic.linger_time, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_enum2_0x24, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x28, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x2C, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x30, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x34, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x38, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x3C, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x40, true);
                appendUIntMemoryStream(oMBONfs, cosmetic.unk_0x44, true);
            }

            oMBONfs.Seek(0, SeekOrigin.Begin);

            string outputPath = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice METEOR\Converted from MBON\Cosmetic_Properties.bin";
            FileStream ofs = File.Create(outputPath);
            oMBONfs.CopyTo(ofs);

            FBfs.Close();
            MBONfs.Close();
            ofs.Close();
        }

        public void serializeCosmeticList()
        {
            using (var json = File.OpenText(Properties.Settings.Default.inputCosmeticListJSONPath))
            {
                var cosmeticList = JsonConvert.DeserializeObject<CosmeticList>(json.ReadToEnd());

                MemoryStream ms = new MemoryStream();

                appendUIntMemoryStream(ms, cosmeticList.magic, true);
                appendUIntMemoryStream(ms, cosmeticList.version, true);
                appendUIntMemoryStream(ms, cosmeticList.unk_0x8, true);
                appendUIntMemoryStream(ms, cosmeticList.unk_0xC, true);

                appendUIntMemoryStream(ms, (uint)cosmeticList.properties.Count(), true);

                MemoryStream hash_ms = new MemoryStream();
                MemoryStream properties_ms = new MemoryStream();
                foreach(var properties in cosmeticList.properties)
                {
                    appendUIntMemoryStream(hash_ms, properties.Key, true);

                    var cosmetic_properties = properties.Value;
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.cosmetic_enum, true);
                    appendFloatMemoryStream(properties_ms, cosmetic_properties.size, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.body_ALEO_hash, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_ALEO_hash, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_enum_0x10, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.muzzle_ALEO_hash, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.collision_ALEO_hash, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x1C, true);
                    appendFloatMemoryStream(properties_ms, cosmetic_properties.linger_time, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_enum2_0x24, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x28, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x2C, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x30, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x34, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x38, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x3C, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x40, true);
                    appendUIntMemoryStream(properties_ms, cosmetic_properties.unk_0x44, true);
                }

                hash_ms.Seek(0, SeekOrigin.Begin);
                properties_ms.Seek(0, SeekOrigin.Begin);

                hash_ms.CopyTo(ms);
                properties_ms.CopyTo(ms);

                ms.Seek(0, SeekOrigin.Begin);

                using (var ofs = File.Create(Properties.Settings.Default.outputCosmeticListBinaryPath + @"\CosmeticList.bin"))
                {
                    ms.CopyTo(ofs);
                }
            }
        }

        public void deserializeCosmeticList()
        {
            var fs = File.OpenRead(Properties.Settings.Default.inputCosmeticListBinaryPath);
            var cosmeticList = parseCosmeticList(fs);

            var json = JsonConvert.SerializeObject(cosmeticList);
            var ofs = File.CreateText(Properties.Settings.Default.outputCosmeticListJSONPath + @"\CosmeticList.json");

            ofs.Write(json);
            ofs.Close();
        }

        public CosmeticList parseCosmeticList(FileStream fs)
        {
            CosmeticList CosmeticList = new CosmeticList();

            CosmeticList.properties = new Dictionary<uint, CosmeticProperties>();
            CosmeticList.magic = readUIntBigEndian(fs);
            CosmeticList.version = readUIntBigEndian(fs);
            CosmeticList.unk_0x8 = readUIntBigEndian(fs);
            CosmeticList.unk_0xC = readUIntBigEndian(fs);

            uint count = readUIntBigEndian(fs);

            List<uint> hashes = new List<uint>();
            for(int i = 0; i < count; i++)
            {
                uint hash = readUIntBigEndian(fs);
                hashes.Add(hash);
            }

            foreach(var hash in hashes)
            {
                CosmeticProperties cosmetic = new CosmeticProperties();
                cosmetic.cosmetic_enum = readUIntBigEndian(fs);
                cosmetic.size = readFloat(fs, true);
                cosmetic.body_ALEO_hash = readUIntBigEndian(fs);
                cosmetic.unk_ALEO_hash = readUIntBigEndian(fs);
                cosmetic.unk_enum_0x10 = readUIntBigEndian(fs);
                cosmetic.muzzle_ALEO_hash = readUIntBigEndian(fs);
                cosmetic.collision_ALEO_hash = readUIntBigEndian(fs);  // Collision to ground / building effect
                cosmetic.unk_0x1C = readUIntBigEndian(fs);
                cosmetic.linger_time = readFloat(fs, true);  // 438C00000
                cosmetic.unk_enum2_0x24 = readUIntBigEndian(fs);  // 0309
                cosmetic.unk_0x28 = readUIntBigEndian(fs);
                cosmetic.unk_0x2C = readUIntBigEndian(fs);
                cosmetic.unk_0x30 = readUIntBigEndian(fs);
                cosmetic.unk_0x34 = readUIntBigEndian(fs);
                cosmetic.unk_0x38 = readUIntBigEndian(fs);
                cosmetic.unk_0x3C = readUIntBigEndian(fs);
                cosmetic.unk_0x40 = readUIntBigEndian(fs);
                cosmetic.unk_0x44 = readUIntBigEndian(fs);

                CosmeticList.properties[hash] = cosmetic;
            }

            return CosmeticList;
        }
    }
}
