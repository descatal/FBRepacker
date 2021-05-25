using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
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
            FileStream FBfs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB Extract\1.08 Latest Cosmetic ID List - PATCHCCABBB3B\001-FHM\002.bin");
            FileStream MBONfs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common MBON\Cosmetic List - CCABBB3B\001-FHM\002.bin");

            Dictionary<uint, CosmeticProperties> FBCosmetic = parseCosmeticList(FBfs);
            Dictionary<uint, CosmeticProperties> MBONCosmetic = parseCosmeticList(MBONfs);
            Dictionary<uint, CosmeticProperties> nMBONCosmetic = new Dictionary<uint, CosmeticProperties>(MBONCosmetic);

            foreach (var cos in MBONCosmetic)
            {
                if (FBCosmetic.ContainsKey(cos.Key))
                {
                    nMBONCosmetic[cos.Key].body_ALEO_hash = FBCosmetic[cos.Key].body_ALEO_hash;
                }
            }

            MBONCosmetic = nMBONCosmetic;

            // MBON will have more Cosmetic
            // Filter out cosmetic removed in MBON
            Dictionary<uint, CosmeticProperties> removed_cosmetic = FBCosmetic.Where(s => !MBONCosmetic.ContainsKey(s.Key)).ToDictionary(entry => entry.Key, entry => entry.Value);

            // Append the removed stuff into MBON
            foreach(var cosmetic in removed_cosmetic)
            {
                MBONCosmetic[cosmetic.Key] = cosmetic.Value;
            }

            MemoryStream oMBONfs = new MemoryStream();
            appendUIntMemoryStream(oMBONfs, 0x23A56922, true);
            appendUIntMemoryStream(oMBONfs, 0x2, true);
            appendUIntMemoryStream(oMBONfs, 0, true);
            appendUIntMemoryStream(oMBONfs, 0, true);

            appendUIntMemoryStream(oMBONfs, (uint)(MBONCosmetic.Count), true);
            foreach(var MBON in MBONCosmetic)
            {
                appendUIntMemoryStream(oMBONfs, MBON.Key, true);
            }
            foreach (var MBON in MBONCosmetic)
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

        public Dictionary<uint, CosmeticProperties> parseCosmeticList(FileStream fs)
        {
            Dictionary<uint, CosmeticProperties> CosmeticProperties = new Dictionary<uint, CosmeticProperties>();
            uint magic = readUIntBigEndian(fs);
            uint version = readUIntBigEndian(fs); // float
            uint unk_0x8 = readUIntBigEndian(fs);
            uint unk_0xC = readUIntBigEndian(fs);

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

                CosmeticProperties[hash] = cosmetic;
            }

            return CosmeticProperties;
        }
    }
}
