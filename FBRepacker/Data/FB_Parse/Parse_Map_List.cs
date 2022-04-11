using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using Newtonsoft.Json;

namespace FBRepacker.Data.FB_Parse
{
    internal class Parse_Map_List : Internals
    {
        public Parse_Map_List()
        {

        }

        public void deserialize_map_list()
        {
            Map_List map_List = parse_map_list(Properties.Settings.Default.inputMapListBinaryPath);
            string JSON = JsonConvert.SerializeObject(map_List, Formatting.Indented);

            StreamWriter sw = File.CreateText(Properties.Settings.Default.outputMapListJSONPath + @"\map_list.JSON");
            sw.WriteLine(JSON);
            sw.Close();
        }

        public Map_List parse_map_list(string input)
        {
            FileStream fs = File.OpenRead(input);

            Map_List map_List = new Map_List();

            map_List.version = 1;

            uint sstageliststring_pointer = readUIntBigEndian(fs);

            long returnPos = fs.Position;
            fs.Seek(sstageliststring_pointer, SeekOrigin.Begin);

            map_List.SStageListString = readString(fs);

            fs.Seek(returnPos, SeekOrigin.Begin);
            ushort number_of_maps = readUShort(fs, true);

            // this is always 0000
            fs.Seek(0x2, SeekOrigin.Current);

            for(int i = 0; i < number_of_maps; i++)
            {
                Map_List_Properties map_list_properties = new Map_List_Properties();
                map_list_properties.index = (byte)fs.ReadByte();
                map_list_properties.series_index = (byte)fs.ReadByte();
                
                uint check_0xFFFF = readUShort(fs, true);

                if (check_0xFFFF != 0xFFFF)
                    throw new Exception();

                uint release_string_pointer = readUIntBigEndian(fs);

                returnPos = fs.Position;
                fs.Seek(release_string_pointer, SeekOrigin.Begin);

                map_list_properties.release_string = readString(fs);

                fs.Seek(returnPos, SeekOrigin.Begin);

                uint stage_string_pointer = readUIntBigEndian(fs);

                returnPos = fs.Position;
                fs.Seek(stage_string_pointer, SeekOrigin.Begin);

                map_list_properties.stage_string = readString(fs);

                fs.Seek(returnPos, SeekOrigin.Begin);

                map_list_properties.map_hash = readUIntBigEndian(fs);
                map_list_properties.map_select_Flags = (map_select_Flag)fs.ReadByte();

                byte check_0xFF = (byte)fs.ReadByte();

                if (check_0xFF != 0xFF)
                    throw new Exception();

                check_0xFFFF = readUShort(fs, true);

                if (check_0xFFFF != 0xFFFF)
                    throw new Exception();

                map_list_properties.map_sprite_hash = readUIntBigEndian(fs);
                map_list_properties.select_order = readUIntBigEndian(fs);
                map_list_properties.image_sprite_index = (byte)fs.ReadByte();

                check_0xFF = (byte)fs.ReadByte();

                if (check_0xFF != 0xFF)
                    throw new Exception();

                check_0xFFFF = readUShort(fs, true);

                if (check_0xFFFF != 0xFFFF)
                    throw new Exception();

                map_list_properties.unk_0x20 = readUIntBigEndian(fs);

                map_List.map_list_properties.Add(map_list_properties);
            }

            fs.Close();

            map_List.map_list_properties = map_List.map_list_properties.OrderBy(x => x.select_order).ToList();

            return map_List;
        }

        public void serialize_map_list()
        {
            StreamReader sr = File.OpenText(Properties.Settings.Default.inputMapListJSONPath);
            string JSON = sr.ReadToEnd();
            sr.Close();

            Map_List map_List = JsonConvert.DeserializeObject<Map_List>(JSON);

            MemoryStream oms = write_map_list(map_List);

            FileStream ofs = File.Create(Properties.Settings.Default.outputMapListBinaryPath + @"\map_List.bin");
            oms.Seek(0, SeekOrigin.Begin);
            oms.CopyTo(ofs);

            ofs.Close();
        }

        public MemoryStream write_map_list(Map_List map_List)
        {
            MemoryStream map_List_MS = new MemoryStream();

            MemoryStream SStageList_String_MS = new MemoryStream();
            appendStringMemoryStream(SStageList_String_MS, "SStageList", Encoding.Default, true);

            MemoryStream Release_String_MS = new MemoryStream();
            appendStringMemoryStream(Release_String_MS, "ãƒªãƒªãƒ¼ã‚¹", Encoding.Default, true);

            long map_list_param_length = 0x8 + (map_List.map_list_properties.Count() * 0x24); // 0x8 for the header

            long fixed_string_length = SStageList_String_MS.Length + Release_String_MS.Length;

            long SStageList_String_Pointer = map_list_param_length;
            long release_String_Pointer = map_list_param_length + SStageList_String_MS.Length;
            long stage_String_Pointer = map_list_param_length + SStageList_String_MS.Length + Release_String_MS.Length;

            appendUIntMemoryStream(map_List_MS, (uint)SStageList_String_Pointer, true);
            appendUShortMemoryStream(map_List_MS, (ushort)map_List.map_list_properties.Count(), true);
            appendUShortMemoryStream(map_List_MS, 0, true);

            MemoryStream map_List_Properties_MS = new MemoryStream();
            MemoryStream stage_string_MS = new MemoryStream();

            Dictionary<string, uint> stage_string_and_pointers = new Dictionary<string, uint>();

            for (int i = 0; i < map_List.map_list_properties.Count(); i++)
            {
                Map_List_Properties map_List_Properties = map_List.map_list_properties[i];

                map_List_Properties_MS.WriteByte(map_List_Properties.index);
                map_List_Properties_MS.WriteByte(map_List_Properties.series_index);
                appendUShortMemoryStream(map_List_Properties_MS, 0xFFFF, true);

                appendUIntMemoryStream(map_List_Properties_MS, (uint)release_String_Pointer, true);

                if(!stage_string_and_pointers.ContainsKey(map_List_Properties.stage_string))
                {
                    stage_string_and_pointers[map_List_Properties.stage_string] = (uint)(stage_String_Pointer + stage_string_MS.Length);
                    appendStringMemoryStream(stage_string_MS, map_List_Properties.stage_string, Encoding.Default, true);
                }

                appendUIntMemoryStream(map_List_Properties_MS, stage_string_and_pointers[map_List_Properties.stage_string], true);

                appendUIntMemoryStream(map_List_Properties_MS, map_List_Properties.map_hash, true);
                
                map_List_Properties_MS.WriteByte((byte)map_List_Properties.map_select_Flags);
                map_List_Properties_MS.WriteByte(0xFF);
                appendUShortMemoryStream(map_List_Properties_MS, 0xFFFF, true);

                appendUIntMemoryStream(map_List_Properties_MS, map_List_Properties.map_sprite_hash, true);

                appendUIntMemoryStream(map_List_Properties_MS, map_List_Properties.select_order, true);

                map_List_Properties_MS.WriteByte(map_List_Properties.image_sprite_index);
                map_List_Properties_MS.WriteByte(0xFF);
                appendUShortMemoryStream(map_List_Properties_MS, 0xFFFF, true);

                appendUIntMemoryStream(map_List_Properties_MS, map_List_Properties.unk_0x20, true);
            }

            map_List_Properties_MS.Seek(0, SeekOrigin.Begin);
            map_List_Properties_MS.CopyTo(map_List_MS);

            SStageList_String_MS.Seek(0, SeekOrigin.Begin);
            Release_String_MS.Seek(0, SeekOrigin.Begin);
            stage_string_MS.Seek(0, SeekOrigin.Begin);

            SStageList_String_MS.CopyTo(map_List_MS);
            Release_String_MS.CopyTo(map_List_MS);
            stage_string_MS.CopyTo(map_List_MS);

            return map_List_MS;
        }
    }
}
