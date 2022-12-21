using FBRepacker.Data.DataTypes;
using FBRepacker.Data.FB_Parse.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.FB_Parse
{
    internal class Parse_Unit_Files_List : Internals
    {
        public Parse_Unit_Files_List() 
        {
            List<Unit_Files_List> FB_List = parse_Unit_Files_List_FB(@"I:\Full Boost\MBON Reimport Project\list_fb.bin");
            List<Unit_Files_List> MBON_List = parse_Unit_Files_List_MBON(@"I:\Full Boost\MBON Reimport Project\big_endian_list.bin");
            List<Unit_Files_List> Combined_List = new List<Unit_Files_List>();

            UnitIDList unitIDList = load_UnitID();
            List<UnitID> unit_ID_List = unitIDList.Unit_ID;

            StringBuilder MBONlog = new StringBuilder();
            MBONlog.AppendLine("MBON PAC Hashes");
            foreach (var MBON in MBON_List)
            {
                if(FB_List.Any(x => x.Unit_ID == MBON.Unit_ID))
                {
                    Unit_Files_List FBUnit = FB_List.FirstOrDefault(x => x.Unit_ID == MBON.Unit_ID);
                    FBUnit.MBONAdded = false;
                    Combined_List.Add(FBUnit);
                }
                else
                {
                    if(unit_ID_List.Any(x => x.id == MBON.Unit_ID))
                    {
                        UnitID unit = unit_ID_List.FirstOrDefault(x => x.id == MBON.Unit_ID);

                        MBONlog.AppendLine(@"//----------------------- " + unit.name_english + @"-----------------------//");

                        // Manually add own made hashes to newly added MBON units
                        Crc32 crc32 = new Crc32();
                        string sound_effect_str = unit.name_english + "_sound_effects";
                        string sound_effect_hash = crc32.Get(Encoding.UTF8.GetBytes(sound_effect_str.ToLower())).ToString("X8");

                        MBONlog.AppendLine(sound_effect_str + " - 0x" + sound_effect_hash);

                        uint result = Convert.ToUInt32(sound_effect_hash, 16);
                        MBON.sound_effect_PAC_hash = result;


                        string global_pilot_voices_str = unit.name_english + "_global_pilot_voices";
                        string global_pilot_voices_hash = crc32.Get(Encoding.UTF8.GetBytes(global_pilot_voices_str.ToLower())).ToString("X8");

                        MBONlog.AppendLine(global_pilot_voices_str + " - 0x" + global_pilot_voices_hash);

                        result = Convert.ToUInt32(global_pilot_voices_hash, 16);
                        MBON.global_pilot_voices_PAC_hash = result;


                        string sortie_and_awakening_sprites_costume_1_str = unit.name_english + "_sortie_and_awakening_sprites_costume_1";
                        string sortie_and_awakening_sprites_costume_1_hash = crc32.Get(Encoding.UTF8.GetBytes(sortie_and_awakening_sprites_costume_1_str.ToLower())).ToString("X8");

                        MBONlog.AppendLine(sortie_and_awakening_sprites_costume_1_str + " - 0x" + sortie_and_awakening_sprites_costume_1_hash);

                        result = Convert.ToUInt32(sortie_and_awakening_sprites_costume_1_hash, 16);
                        MBON.sortie_and_awakening_sprites_PAC_hash = result;


                        string global_pilot_voice_file_list_str = unit.name_english + "_global_pilot_voice_file_list";
                        string global_pilot_voice_file_list_hash = crc32.Get(Encoding.UTF8.GetBytes(global_pilot_voice_file_list_str.ToLower())).ToString("X8");

                        MBONlog.AppendLine(global_pilot_voice_file_list_str + " - 0x" + global_pilot_voice_file_list_hash);

                        result = Convert.ToUInt32(global_pilot_voice_file_list_hash, 16);
                        MBON.voice_file_list_PAC_hash = result;


                        string local_pilot_voices_str = unit.name_english + "_local_pilot_voices";
                        string local_pilot_voices_hash = crc32.Get(Encoding.UTF8.GetBytes(local_pilot_voices_str.ToLower())).ToString("X8");

                        MBONlog.AppendLine(local_pilot_voices_str + " - 0x" + local_pilot_voices_hash);

                        result = Convert.ToUInt32(local_pilot_voices_hash, 16);
                        MBON.local_pilot_voices_STREAM_PAC_hash = result;

                        MBON.MBONAdded = true;

                        Combined_List.Add(MBON);
                    }
                }
            }

            StreamWriter streamWriter = File.CreateText(@"I:\Full Boost\MBON Reimport Project\GeneratedMBONPACHashes.txt");
            streamWriter.Write(MBONlog.ToString());

            streamWriter.Close();

            string json = JsonConvert.SerializeObject(Combined_List, Formatting.Indented);
            StreamWriter jsonSW = File.CreateText(@"I:\Full Boost\MBON Reimport Project\AllUnitsPACHashes.json");
            jsonSW.Write(json);
            jsonSW.Close();
        }

        public List<Unit_Files_List> parse_Unit_Files_List_FB(string path)
        {
            FileStream fs = File.OpenRead(path);

            List<Unit_Files_List> unit_Files_List = new List<Unit_Files_List>();

            while(fs.Position < fs.Length)
            {
                Unit_Files_List unit_files = new Unit_Files_List();
                unit_files.Unit_ID = readUIntBigEndian(fs);
                unit_files.dummy_PAC_hash = readUIntBigEndian(fs);
                unit_files.data_and_script_PAC_hash = readUIntBigEndian(fs);
                unit_files.model_and_texture_PAC_hash = readUIntBigEndian(fs);
                unit_files.animation_OMO_PAC_hash = readUIntBigEndian(fs);
                unit_files.effects_EIDX_PAC_hash = readUIntBigEndian(fs);
                unit_files.sound_effect_PAC_hash = readUIntBigEndian(fs);
                unit_files.global_pilot_voices_PAC_hash = readUIntBigEndian(fs);
                unit_files.weapon_image_DNSO_PAC_hash = readUIntBigEndian(fs);
                unit_files.sortie_and_awakening_sprites_PAC_hash = readUIntBigEndian(fs);
                unit_files.sortie_mouth_anim_enum_KPKP_PAC_hash = readUIntBigEndian(fs);
                unit_files.voice_file_list_PAC_hash = readUIntBigEndian(fs);
                unit_files.local_pilot_voices_STREAM_PAC_hash = readUIntBigEndian(fs);
                unit_Files_List.Add(unit_files);
            }

            fs.Close();

            return unit_Files_List;
        }

        public List<Unit_Files_List> parse_Unit_Files_List_MBON(string path)
        {
            FileStream fs = File.OpenRead(path);

            List<Unit_Files_List> unit_Files_List = new List<Unit_Files_List>();

            while (fs.Position < fs.Length)
            {
                Unit_Files_List unit_files = new Unit_Files_List();
                unit_files.Unit_ID = readUIntBigEndian(fs);
                unit_files.dummy_PAC_hash = readUIntBigEndian(fs);
                unit_files.data_and_script_PAC_hash = readUIntBigEndian(fs);

                uint BABB1 = readUIntBigEndian(fs);
                uint BABB2 = readUIntBigEndian(fs);

                unit_files.model_and_texture_PAC_hash = readUIntBigEndian(fs);
                unit_files.animation_OMO_PAC_hash = readUIntBigEndian(fs);
                unit_files.effects_EIDX_PAC_hash = readUIntBigEndian(fs);

                uint FFFF1 = readUIntBigEndian(fs);
                if (FFFF1 != 0xFFFFFFFF)
                    throw new Exception();

                uint FFFF2 = readUIntBigEndian(fs);
                if (FFFF2 != 0xFFFFFFFF)
                    throw new Exception();

                unit_files.weapon_image_DNSO_PAC_hash = readUIntBigEndian(fs);

                uint unknownHash = readUIntBigEndian(fs);

                unit_files.sortie_mouth_anim_enum_KPKP_PAC_hash = readUIntBigEndian(fs);

                uint FFFF3 = readUIntBigEndian(fs);
                if (FFFF3 != 0xFFFFFFFF)
                    throw new Exception();

                uint FFFF4 = readUIntBigEndian(fs);
                if (FFFF4 != 0xFFFFFFFF)
                    throw new Exception();

                unit_Files_List.Add(unit_files);
            }

            fs.Close(); 

            return unit_Files_List;
        }
    }
}
