using FBRepacker.Data.FB_Parse.DataTypes;
using FBRepacker.PAC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FBRepacker.Data.FB_Parse
{
    class Parse_Series_Info : Internals
    {
        public Parse_Series_Info()
        {

        }

        public void readFBSeriesInfoList()
        {
            string path = Properties.Settings.Default.inputFBSeriesInfoListBinary;

            FileStream fs = File.OpenRead(path);

            List<Series_Info_List> series_Info_Lists = readInfoList(fs);

            JsonSerializerOptions json_options = new JsonSerializerOptions();
            json_options.WriteIndented = true;
            string JSON = JsonSerializer.Serialize<List<Series_Info_List>>(series_Info_Lists, json_options);
            string opath = Properties.Settings.Default.outputFBSeriesInfoListJSONFolder + @"\Series List.json";

            StreamWriter sw = File.CreateText(opath);
            sw.Write(JSON);

            sw.Close();
            fs.Close();
        }

        public void writeFBSeriesInfoList()
        {
            string JSONIPath = Properties.Settings.Default.inputFBSeriesInfoListJSON;

            string JSON = File.OpenText(JSONIPath).ReadToEnd();
            List<Series_Info_List> Series_Info_Lists = JsonSerializer.Deserialize<List<Series_Info_List>>(JSON);

            writeInfoList(Series_Info_Lists);
        }

        public List<Series_Info_List> readInfoList(FileStream fs)
        {
            uint SSeriesListstr_ptr = readUIntBigEndian(fs);

            string SCharacterListstr = readString(fs, SSeriesListstr_ptr, true);

            if (SCharacterListstr != "SSeriesList")
                throw new Exception("Cannot find SSeriesList!");

            ushort series_count = readUShort(fs, true);
            ushort unk_0x6 = readUShort(fs, true);

            if (unk_0x6 != 0)
                throw new Exception("unk_0x6 not 0!");

            List<Series_Info_List> Series_Info_Lists = new List<Series_Info_List>();

            for (int i = 0; i < series_count; i++)
            {
                Series_Info_List Series_Info_List = new Series_Info_List();

                byte series_index = (byte)fs.ReadByte();
                byte series_index_2 = (byte)fs.ReadByte();

                Series_Info_List.series_index = series_index;
                Series_Info_List.series_index_2 = series_index_2;

                byte unk_0x3 = (byte)fs.ReadByte();
                if (unk_0x3 != 0x80 && unk_0x3 != 0)
                    throw new Exception("unk_0x3 not 0x80 or 0!");
                Series_Info_List.unk_0x3 = unk_0x3;

                byte unk_0x4 = (byte)fs.ReadByte();
                if (unk_0x4 != 0xFF)
                    throw new Exception("unk_0x4 not 0xFF");
                Series_Info_List.unk_0x4 = unk_0x4;

                uint release_str_ptr = readUIntBigEndian(fs);
                string release_str = readString(fs, release_str_ptr, true);
                Series_Info_List.release_string = release_str;

                byte series_placement_order = (byte)fs.ReadByte();
                Series_Info_List.series_placement_order = series_placement_order;

                byte series_sprite_index = (byte)fs.ReadByte();
                Series_Info_List.series_sprite_index = series_sprite_index;

                byte series_sprite_index_2 = (byte)fs.ReadByte();
                Series_Info_List.series_sprite_index_2 = series_sprite_index_2;

                byte unk_0xB = (byte)fs.ReadByte();
                if (unk_0xB != 0xFF)
                    throw new Exception("unk_0xB not 0xFF");
                Series_Info_List.unk_0xB = unk_0xB;

                uint series_movie_hash = readUIntBigEndian(fs);
                Series_Info_List.series_movie_hash = series_movie_hash;

                if (Properties.Settings.Default.allSeriesInfoListInputMBON)
                    fs.Seek(0x14, SeekOrigin.Current);

                Series_Info_Lists.Add(Series_Info_List);
            }

            return Series_Info_Lists;
        }

        public void writeInfoList(List<Series_Info_List> Series_Info_Lists)
        {
            MemoryStream InfoMS = new MemoryStream();
            MemoryStream StrMS = new MemoryStream();

            uint InfoMSSize = (uint)(Series_Info_Lists.Count() * 0x10) + 0x8; // 0x8 for header 0x8 length

            appendUIntMemoryStream(InfoMS, InfoMSSize, true);

            appendStringMemoryStream(StrMS, "SSeriesList\0", Encoding.Default);
            // Release keyword after SSSeriesList
            uint release_pointer = (uint)(InfoMSSize + StrMS.Position);

            appendStringMemoryStream(StrMS, "リリース\0", Encoding.UTF8);

            appendUShortMemoryStream(InfoMS, (ushort)Series_Info_Lists.Count(), true);
            appendUShortMemoryStream(InfoMS, 0, true);

            uint zero_pointer = InfoMSSize;
            for (int i = 0; i < Series_Info_Lists.Count(); i++)
            {
                Series_Info_List Series_Info_List = Series_Info_Lists[i];

                InfoMS.WriteByte(Series_Info_List.series_index);
                InfoMS.WriteByte(Series_Info_List.series_index_2);
                InfoMS.WriteByte(Series_Info_List.unk_0x3);

                if (Series_Info_List.unk_0x4 != 0xFF)
                    throw new Exception("unk_0x4 != 0xFF!");

                InfoMS.WriteByte(Series_Info_List.unk_0x4);

                appendUIntMemoryStream(InfoMS, release_pointer, true);

                InfoMS.WriteByte(Series_Info_List.series_placement_order);
                InfoMS.WriteByte(Series_Info_List.series_sprite_index);
                InfoMS.WriteByte(Series_Info_List.series_sprite_index_2);

                if (Series_Info_List.unk_0xB != 0xFF)
                    throw new Exception("unk_0xB != 0xFF!");

                InfoMS.WriteByte(Series_Info_List.unk_0xB);

                appendUIntMemoryStream(InfoMS, Series_Info_List.series_movie_hash, true);
            }

            string OutputPath = Properties.Settings.Default.outputFBSeriesInfoListBinaryFolder;
            FileStream ofs = File.Create(OutputPath + @"\Series List.bin");

            InfoMS.Seek(0, SeekOrigin.Begin);
            StrMS.Seek(0, SeekOrigin.Begin);

            InfoMS.CopyTo(ofs);
            StrMS.CopyTo(ofs);

            ofs.Close();
        }
    }


}
