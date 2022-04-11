using FBRepacker.PAC;
using FBRepacker.Data.FB_Parse.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace FBRepacker.Data.FB_Parse
{
    class Parse_Unit_Info_List : Internals
    {
        public Parse_Unit_Info_List()
        {

        }

        public void readFBUnitInfoList()
        {
            string path = Properties.Settings.Default.inputFBUnitInfoListBinary;
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\002.bin";
            FileStream fs = File.OpenRead(path);

            List<Unit_Info_List> unit_Info_Lists = readInfoList(fs);

            JsonSerializerOptions json_options = new JsonSerializerOptions();
            json_options.WriteIndented = true;
            string JSON = JsonSerializer.Serialize<List<Unit_Info_List>>(unit_Info_Lists, json_options);
            string opath = Properties.Settings.Default.outputFBUnitInfoListJSONFolder + @"\Unit List.json";
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\Original.json";

            StreamWriter sw = File.CreateText(opath);
            sw.Write(JSON);

            sw.Close();
            fs.Close();
        }

        public void writeFBUnitInfoList()
        {
            string JSONIPath = Properties.Settings.Default.inputFBUnitInfoListJSON;
                //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\Unit List.json";
            string JSON = File.OpenText(JSONIPath).ReadToEnd();
            List<Unit_Info_List> unit_Info_Lists = JsonSerializer.Deserialize<List<Unit_Info_List>>(JSON);

            writeInfoList(unit_Info_Lists);
        }

        public List<Unit_Info_List> readInfoList(FileStream fs)
        {
            // First unit will contain some metadata.
            uint SCharacterListstr_ptr = readUIntBigEndian(fs);

            string SCharacterListstr = readString(fs, SCharacterListstr_ptr, true);

            if (SCharacterListstr != "SCharacterList")
                throw new Exception("Cannot find SCharacterList!");

            ushort unit_count = readUShort(fs, true);
            ushort unk_0x6 = readUShort(fs, true);

            if (unk_0x6 != 0)
                throw new Exception("unk_0x6 not 0!");

            List<Unit_Info_List> unit_Info_Lists = new List<Unit_Info_List>();

            for (int i = 0; i < unit_count; i++)
            {
                Unit_Info_List unit_Info_List = new Unit_Info_List();
                /*
                if (i != 0)
                {
                    uint start_ptr = readUIntBigEndian(fs);
                    string startstr = readString(fs, start_ptr, true);

                    unit_Info_List.start_string = startstr;

                    ushort _null = readUShort(fs, true);
                    unk_0x6 = readUShort(fs, true);

                    if (_null != 0)
                        throw new Exception("null not 0!");

                    unit_Info_List.number_of_selectable_units = _null;
                    unit_Info_List.unk_0x6 = unk_0x6;
                }
                else
                {
                    unit_Info_List.start_string = SCharacterListstr;
                    unit_Info_List.number_of_selectable_units = unit_count;
                    unit_Info_List.unk_0x6 = unk_0x6;
                }
                */

                byte unit_index = (byte)fs.ReadByte();
                byte series_index = (byte)fs.ReadByte();

                unit_Info_List.unit_index = unit_index;
                unit_Info_List.series_index = series_index;

                ushort unk_0x2 = readUShort(fs, true);
                if (unk_0x2 != 0xFFFF)
                    throw new Exception("unk_0x2 not 0xFFFF!");
                unit_Info_List.unk_0x2 = unk_0x2;

                uint unit_ID = readUIntBigEndian(fs);
                unit_Info_List.unit_ID = unit_ID;

                uint release_str_ptr = readUIntBigEndian(fs);
                string release_str = readString(fs, release_str_ptr, true);
                unit_Info_List.release_string = release_str;

                uint F_str_ptr = readUIntBigEndian(fs);
                string F_str = readString(fs, F_str_ptr, true);
                unit_Info_List.F_string = F_str;

                uint F_out_str_ptr = readUIntBigEndian(fs);
                string F_out_str = readString(fs, F_out_str_ptr, true);
                unit_Info_List.F_out_string = F_out_str;

                uint P_str_ptr = readUIntBigEndian(fs);
                string P_str = readString(fs, P_str_ptr, true);
                unit_Info_List.P_string = P_str;

                byte internal_index = (byte)fs.ReadByte();
                byte arcade_small_sprite_index = (byte)fs.ReadByte();
                byte arcade_small_sprite_index_2 = (byte)fs.ReadByte();
                byte unk_0x1B = (byte)fs.ReadByte();

                if (arcade_small_sprite_index_2 != arcade_small_sprite_index)
                    throw new Exception("arcade small sprite index 1 and 2 not same!");

                if (unk_0x1B != 0xFF)
                    throw new Exception("unk_0x1B not 0xFF!");

                unit_Info_List.internal_index = internal_index;
                unit_Info_List.arcade_small_sprite_index = arcade_small_sprite_index;
                unit_Info_List.arcade_unit_name_sprite = arcade_small_sprite_index_2;
                unit_Info_List.unk_0x1B = unk_0x1B;

                uint arcade_selection_sprite_costume_1_hash = readUIntBigEndian(fs);
                unit_Info_List.arcade_selection_sprite_costume_1_hash = arcade_selection_sprite_costume_1_hash;

                uint arcade_selection_sprite_costume_2_hash = readUIntBigEndian(fs);
                unit_Info_List.arcade_selection_sprite_costume_2_hash = arcade_selection_sprite_costume_2_hash;

                uint arcade_selection_sprite_costume_3_hash = readUIntBigEndian(fs);
                unit_Info_List.arcade_selection_sprite_costume_3_hash = arcade_selection_sprite_costume_3_hash;

                uint loading_ally_sprite_costume_1_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_ally_sprite_costume_1_hash = loading_ally_sprite_costume_1_hash;

                uint loading_ally_sprite_costume_2_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_ally_sprite_costume_2_hash = loading_ally_sprite_costume_2_hash;

                uint loading_ally_sprite_costume_3_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_ally_sprite_costume_3_hash = loading_ally_sprite_costume_3_hash;

                uint loading_enemy_sprite_costume_1_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_enemy_sprite_costume_1_hash = loading_enemy_sprite_costume_1_hash;

                uint loading_enemy_sprite_costume_2_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_enemy_sprite_costume_2_hash = loading_enemy_sprite_costume_2_hash;

                uint loading_enemy_sprite_costume_3_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_enemy_sprite_costume_3_hash = loading_enemy_sprite_costume_3_hash;

                uint free_battle_selection_sprite_costume_1_hash = readUIntBigEndian(fs);
                unit_Info_List.free_battle_selection_sprite_costume_1_hash = free_battle_selection_sprite_costume_1_hash;

                uint free_battle_selection_sprite_costume_2_hash = readUIntBigEndian(fs);
                unit_Info_List.free_battle_selection_sprite_costume_2_hash = free_battle_selection_sprite_costume_2_hash;

                uint free_battle_selection_sprite_costume_3_hash = readUIntBigEndian(fs);
                unit_Info_List.free_battle_selection_sprite_costume_3_hash = free_battle_selection_sprite_costume_3_hash;

                uint loading_enemy_target_unit_sprite_costume_1_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_enemy_target_unit_sprite_costume_1_hash = loading_enemy_target_unit_sprite_costume_1_hash;

                uint loading_enemy_target_pilot_sprite_costume_1_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_enemy_target_pilot_sprite_costume_1_hash = loading_enemy_target_pilot_sprite_costume_1_hash;

                uint loading_enemy_target_pilot_sprite_costume_2_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_enemy_target_pilot_sprite_costume_2_hash = loading_enemy_target_pilot_sprite_costume_2_hash;

                uint loading_enemy_target_pilot_sprite_costume_3_hash = readUIntBigEndian(fs);
                unit_Info_List.loading_enemy_target_pilot_sprite_costume_3_hash = loading_enemy_target_pilot_sprite_costume_3_hash;

                uint in_game_sortie_and_awakening_sprite_costume_1_hash = readUIntBigEndian(fs);
                unit_Info_List.in_game_sortie_and_awakening_sprite_costume_1_hash = in_game_sortie_and_awakening_sprite_costume_1_hash;

                uint in_game_sortie_and_awakening_sprite_costume_2_hash = readUIntBigEndian(fs);
                unit_Info_List.in_game_sortie_and_awakening_sprite_costume_2_hash = in_game_sortie_and_awakening_sprite_costume_2_hash;

                uint in_game_sortie_and_awakening_sprite_costume_3_hash = readUIntBigEndian(fs);
                unit_Info_List.in_game_sortie_and_awakening_sprite_costume_3_hash = in_game_sortie_and_awakening_sprite_costume_3_hash;

                uint KPKP_hash = readUIntBigEndian(fs);
                unit_Info_List.KPKP_hash = KPKP_hash;

                uint result_small_sprite_hash = readUIntBigEndian(fs);
                unit_Info_List.result_small_sprite_hash = result_small_sprite_hash;

                byte unk_0x70 = (byte)fs.ReadByte();
                if (unk_0x70 != 0)
                    throw new Exception("unk_0x70 not 0!");
                unit_Info_List.unk_0x70 = unk_0x70;

                byte figurine_sprite_index = (byte)fs.ReadByte();
                unit_Info_List.figurine_sprite_index = figurine_sprite_index;

                ushort unk_0x72 = readUShort(fs, true);
                if (unk_0x72 != 0xFFFF)
                    throw new Exception("unk_0x72 not 0xFFFF!");
                unit_Info_List.unk_0x72 = unk_0x72;

                uint figurine_sprite_hash = readUIntBigEndian(fs);
                unit_Info_List.figurine_sprite_hash = figurine_sprite_hash;

                uint unused_MBON_style_sprite_hash = readUIntBigEndian(fs);
                unit_Info_List.target_small_sprite_hash = unused_MBON_style_sprite_hash;

                uint unk_0x7C = readUIntBigEndian(fs);
                unit_Info_List.unk_0x7C = unk_0x7C;

                uint unk_0x80 = readUIntBigEndian(fs);
                unit_Info_List.unk_0x80 = unk_0x80;

                uint catalog_pilot_costume_2_sprite_hash = readUIntBigEndian(fs);
                unit_Info_List.catalog_pilot_costume_2_sprite_hash = catalog_pilot_costume_2_sprite_hash;

                uint IS_Costume_T_string_ptr = readUIntBigEndian(fs);
                string IS_Costume_T_str = readString(fs, IS_Costume_T_string_ptr, true);
                unit_Info_List.IS_Costume_T_costume_2_string = IS_Costume_T_str;

                uint IS_Costume_string_ptr = readUIntBigEndian(fs);
                string IS_Costume_str = readString(fs, IS_Costume_string_ptr, true);
                unit_Info_List.IS_Costume_costume_2_string = IS_Costume_str;

                uint catalog_pilot_costume_3_sprite_hash = readUIntBigEndian(fs);
                unit_Info_List.catalog_pilot_costume_3_sprite_hash = catalog_pilot_costume_3_sprite_hash;

                uint IS_Costume_T_costume_3_str_ptr = readUIntBigEndian(fs);
                string IS_Costume_T_costume_3_str = readString(fs, IS_Costume_T_costume_3_str_ptr, true);
                unit_Info_List.IS_Costume_T_costume_3_string = IS_Costume_T_costume_3_str;

                uint IS_Costume_costume_3_str_ptr = readUIntBigEndian(fs);
                string IS_Costume_costume_3_str = readString(fs, IS_Costume_costume_3_str_ptr, true);
                unit_Info_List.IS_Costume_costume_3_string = IS_Costume_costume_3_str;

                uint unk_0x9C = readUIntBigEndian(fs);
                unit_Info_List.unk_0x9C = unk_0x9C;

                unit_Info_Lists.Add(unit_Info_List);
            }

            return unit_Info_Lists;
        }

        public void writeInfoList(List<Unit_Info_List> unit_Info_Lists)
        {
            MemoryStream InfoMS = new MemoryStream();
            MemoryStream StrMS = new MemoryStream();

            uint InfoMSSize = (uint)(unit_Info_Lists.Count() * 0xA0) + 0x8; // 0x8 for header 0x8 length

            appendUIntMemoryStream(InfoMS, InfoMSSize, true);

            appendStringMemoryStream(StrMS, "SCharacterList\0", Encoding.Default);
            // Release keyword after SCharacterList
            uint release_pointer = (uint)(InfoMSSize + StrMS.Position);

            appendStringMemoryStream(StrMS, "リリース\0", Encoding.UTF8);

            appendUShortMemoryStream(InfoMS, (ushort)unit_Info_Lists.Count(), true);
            appendUShortMemoryStream(InfoMS, 0, true);

            uint zero_pointer = InfoMSSize;
            for (int i = 0; i < unit_Info_Lists.Count(); i++)
            {
                Unit_Info_List unit_Info_List = unit_Info_Lists[i];
                /*
                if(i == 0)
                {
                    // Only need to append these strings once.
                    appendUIntMemoryStream(InfoMS, start_pointer, true);
                    appendStringMemoryStream(StrMS, "SCharacterList\0", Encoding.Default);
                    start_pointer += (uint)StrMS.Position;

                    appendStringMemoryStream(StrMS, unit_Info_List.release_string + "\0", Encoding.UTF8);

                    appendUShortMemoryStream(InfoMS, (ushort)unit_Info_Lists.Count(), true);
                    appendUShortMemoryStream(InfoMS, 0, true);
                }
                else
                {
                    appendUIntMemoryStream(InfoMS, end_pointer, true);
                    appendUShortMemoryStream(InfoMS, unit_Info_List.number_of_selectable_units, true); // should be 0 for all cases
                    appendUShortMemoryStream(InfoMS, unit_Info_List.unk_0x6, true);
                }
                */

                InfoMS.WriteByte(unit_Info_List.unit_index);
                InfoMS.WriteByte(unit_Info_List.series_index);
                appendUShortMemoryStream(InfoMS, unit_Info_List.unk_0x2, true); // 0xFFFF
                appendUIntMemoryStream(InfoMS, unit_Info_List.unit_ID, true);
                appendUIntMemoryStream(InfoMS, release_pointer, true);

                appendUIntMemoryStream(InfoMS, (uint)(InfoMSSize + StrMS.Position), true);
                appendStringMemoryStream(StrMS, unit_Info_List.F_string + "\0", Encoding.Default);

                appendUIntMemoryStream(InfoMS, (uint)(InfoMSSize + StrMS.Position), true);
                appendStringMemoryStream(StrMS, unit_Info_List.F_out_string + "\0", Encoding.Default);

                appendUIntMemoryStream(InfoMS, (uint)(InfoMSSize + StrMS.Position), true);
                appendStringMemoryStream(StrMS, unit_Info_List.P_string + "\0", Encoding.Default);

                InfoMS.WriteByte(unit_Info_List.internal_index);
                InfoMS.WriteByte(unit_Info_List.arcade_small_sprite_index);
                InfoMS.WriteByte(unit_Info_List.arcade_unit_name_sprite);
                InfoMS.WriteByte(unit_Info_List.unk_0x1B);

                appendUIntMemoryStream(InfoMS, unit_Info_List.arcade_selection_sprite_costume_1_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.arcade_selection_sprite_costume_2_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.arcade_selection_sprite_costume_3_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_ally_sprite_costume_1_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_ally_sprite_costume_2_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_ally_sprite_costume_3_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_enemy_sprite_costume_1_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_enemy_sprite_costume_2_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_enemy_sprite_costume_3_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.free_battle_selection_sprite_costume_1_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.free_battle_selection_sprite_costume_2_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.free_battle_selection_sprite_costume_3_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_enemy_target_unit_sprite_costume_1_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_enemy_target_pilot_sprite_costume_1_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_enemy_target_pilot_sprite_costume_2_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.loading_enemy_target_pilot_sprite_costume_3_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.in_game_sortie_and_awakening_sprite_costume_1_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.in_game_sortie_and_awakening_sprite_costume_2_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.in_game_sortie_and_awakening_sprite_costume_3_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.KPKP_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.result_small_sprite_hash, true);

                InfoMS.WriteByte(unit_Info_List.unk_0x70);
                InfoMS.WriteByte(unit_Info_List.figurine_sprite_index);

                appendUShortMemoryStream(InfoMS, unit_Info_List.unk_0x72, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.figurine_sprite_hash, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.target_small_sprite_hash, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.unk_0x7C, true);
                appendUIntMemoryStream(InfoMS, unit_Info_List.unk_0x80, true);

                appendUIntMemoryStream(InfoMS, unit_Info_List.catalog_pilot_costume_2_sprite_hash, true);

                if (i == 0)
                {
                    uint count = 0;
                    if(unit_Info_List.IS_Costume_T_costume_2_string != "0.0")
                        count += (uint)unit_Info_List.IS_Costume_T_costume_2_string.ToArray().Length + 1;
                    if (unit_Info_List.IS_Costume_T_costume_3_string != "0.0")
                        count += (uint)unit_Info_List.IS_Costume_T_costume_3_string.ToArray().Length + 1;
                    if (unit_Info_List.IS_Costume_costume_2_string != "0.0")
                        count += (uint)unit_Info_List.IS_Costume_costume_2_string.ToArray().Length + 1;
                    if (unit_Info_List.IS_Costume_costume_3_string != "0.0")
                        count += (uint)unit_Info_List.IS_Costume_costume_3_string.ToArray().Length + 1;

                    zero_pointer += (uint)StrMS.Position + count;
                }

                if (unit_Info_List.IS_Costume_T_costume_2_string != "0.0")
                {
                    appendUIntMemoryStream(InfoMS, (uint)(InfoMSSize + StrMS.Position), true);
                    appendStringMemoryStream(StrMS, unit_Info_List.IS_Costume_T_costume_2_string + "\0", Encoding.Default);
                }
                else
                {
                    appendUIntMemoryStream(InfoMS, zero_pointer, true);
                }

                if (unit_Info_List.IS_Costume_costume_2_string != "0.0")
                {
                    appendUIntMemoryStream(InfoMS, (uint)(InfoMSSize + StrMS.Position), true);
                    appendStringMemoryStream(StrMS, unit_Info_List.IS_Costume_costume_2_string + "\0", Encoding.Default);
                }
                else
                {
                    appendUIntMemoryStream(InfoMS, zero_pointer, true);
                }

                appendUIntMemoryStream(InfoMS, unit_Info_List.catalog_pilot_costume_3_sprite_hash, true);

                if (unit_Info_List.IS_Costume_T_costume_3_string != "0.0")
                {
                    appendUIntMemoryStream(InfoMS, (uint)(InfoMSSize + StrMS.Position), true);
                    appendStringMemoryStream(StrMS, unit_Info_List.IS_Costume_T_costume_3_string + "\0", Encoding.Default);
                }
                else
                {
                    appendUIntMemoryStream(InfoMS, zero_pointer, true);
                }


                if (unit_Info_List.IS_Costume_costume_3_string != "0.0")
                {
                    appendUIntMemoryStream(InfoMS, (uint)(InfoMSSize + StrMS.Position), true);
                    appendStringMemoryStream(StrMS, unit_Info_List.IS_Costume_costume_3_string + "\0", Encoding.Default);
                }
                else
                {
                    appendUIntMemoryStream(InfoMS, zero_pointer, true);
                }

                if (i == 0)
                    appendStringMemoryStream(StrMS, "0\x2E" + "0\0", Encoding.Default); // For nil IS_string.

                appendUIntMemoryStream(InfoMS, unit_Info_List.unk_0x9C, true);
            }

            string OutputPath = Properties.Settings.Default.outputFBUnitInfoListBinaryFolder;
            //@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Common FB JSON\";
            FileStream ofs = File.Create(OutputPath + @"\Unit List.bin");

            InfoMS.Seek(0, SeekOrigin.Begin);
            StrMS.Seek(0, SeekOrigin.Begin);

            InfoMS.CopyTo(ofs);
            StrMS.CopyTo(ofs);

            ofs.Close();
        }

        //loading_enemy_target_unit_sprite_costume_1_hash
        //loading_enemy_target_pilot_sprite_costume_1_hash
        //Awakening_sprites
        //KPKP
        //Trophy Sprite (Individual)
        //unused MBON style small image sprites
    }
}
