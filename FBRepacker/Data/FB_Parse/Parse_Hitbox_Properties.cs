using FBRepacker.Data.DataTypes;
using FBRepacker.PAC;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FBRepacker.Data.FB_Parse
{
    internal class Parse_Hitbox_Properties : Internals
    {
        public Parse_Hitbox_Properties()
        {

        }

        public void serialize_Hitbox_Properties()
        {
            StreamReader sr = File.OpenText(Properties.Settings.Default.inputHitboxPropertiesJSONPath);

            string JSON = sr.ReadToEnd();
            sr.Close();

            Hitbox_Properties hitbox_Properties = JsonConvert.DeserializeObject<Hitbox_Properties>(JSON);

            MemoryStream oms = write_Hitbox_Properties(hitbox_Properties);

            FileStream ofs = File.OpenWrite(Properties.Settings.Default.outputHitboxPropertiesBinaryPath + @"\Hitbox_Properties.bin");

            oms.Seek(0, SeekOrigin.Begin);
            oms.CopyTo(ofs);

            oms.Close();
            ofs.Close();
        }

        public MemoryStream write_Hitbox_Properties(Hitbox_Properties hitbox_Properties)
        {
            MemoryStream hitbox_Properties_MS = new MemoryStream();

            appendUIntMemoryStream(hitbox_Properties_MS, hitbox_Properties.ID_Hash, true);
            appendUIntMemoryStream(hitbox_Properties_MS, hitbox_Properties.unit_ID, true);

            uint melee_Hitbox_Count = (uint)hitbox_Properties.melee_Hitbox_Data.Count();
            uint unk_Hitbox_Count = (uint)hitbox_Properties.unk_Hitbox_Data.Count();
            uint shield_Hitbox_Count = (uint)hitbox_Properties.shield_Hitbox_Data.Count();

            appendUIntMemoryStream(hitbox_Properties_MS, melee_Hitbox_Count, true);
            appendUIntMemoryStream(hitbox_Properties_MS, unk_Hitbox_Count, true);
            appendUIntMemoryStream(hitbox_Properties_MS, shield_Hitbox_Count, true);

            MemoryStream hitbox_Hash_Chunk_MS = new MemoryStream();
            MemoryStream hitbox_Data_Pointer_Chunk_MS = new MemoryStream();
            MemoryStream hitbox_Melee_Hit_Hash_Chunk_MS = new MemoryStream();
            MemoryStream hitbox_Data_Chunk_MS = new MemoryStream();

            long hitbox_Data_Chunk_Global_Pointer =
                hitbox_Properties_MS.Length +  // header
                ((melee_Hitbox_Count + unk_Hitbox_Count + shield_Hitbox_Count) * 0x4 * 0x3); // get the length of these hash and pointer fields

            foreach(var melee_Hitbox in hitbox_Properties.melee_Hitbox_Data)
            {
                appendUIntMemoryStream(hitbox_Hash_Chunk_MS, melee_Hitbox.hitbox_Hash, true);

                appendUIntMemoryStream(hitbox_Data_Pointer_Chunk_MS, (uint)(hitbox_Data_Chunk_Global_Pointer + hitbox_Data_Chunk_MS.Length), true);

                appendUIntMemoryStream(hitbox_Melee_Hit_Hash_Chunk_MS, melee_Hitbox.hitbox_Hit_Hash, true);

                hitbox_Data_Chunk_MS = write_All_Hitbox_Types(hitbox_Data_Chunk_MS, melee_Hitbox.all_Hitbox_Types);
            }

            foreach (var unk_Hitbox in hitbox_Properties.unk_Hitbox_Data)
            {
                appendUIntMemoryStream(hitbox_Hash_Chunk_MS, unk_Hitbox.hitbox_Hash, true);

                appendUIntMemoryStream(hitbox_Data_Pointer_Chunk_MS, (uint)(hitbox_Data_Chunk_Global_Pointer + hitbox_Data_Chunk_MS.Length), true);

                appendUIntMemoryStream(hitbox_Melee_Hit_Hash_Chunk_MS, 0, true);

                hitbox_Data_Chunk_MS = write_All_Hitbox_Types(hitbox_Data_Chunk_MS, unk_Hitbox.all_Hitbox_Types);
            }

            foreach (var shield_Hitbox in hitbox_Properties.shield_Hitbox_Data)
            {
                appendUIntMemoryStream(hitbox_Hash_Chunk_MS, shield_Hitbox.hitbox_Hash, true);

                appendUIntMemoryStream(hitbox_Data_Pointer_Chunk_MS, (uint)(hitbox_Data_Chunk_Global_Pointer + hitbox_Data_Chunk_MS.Length), true);

                appendUIntMemoryStream(hitbox_Melee_Hit_Hash_Chunk_MS, 0, true);

                hitbox_Data_Chunk_MS = write_All_Hitbox_Types(hitbox_Data_Chunk_MS, shield_Hitbox.all_Hitbox_Types);
            }

            hitbox_Hash_Chunk_MS.Seek(0, SeekOrigin.Begin);
            hitbox_Data_Pointer_Chunk_MS.Seek(0, SeekOrigin.Begin);
            hitbox_Melee_Hit_Hash_Chunk_MS.Seek(0, SeekOrigin.Begin);
            hitbox_Data_Chunk_MS.Seek(0, SeekOrigin.Begin);

            hitbox_Hash_Chunk_MS.CopyTo(hitbox_Properties_MS);
            hitbox_Data_Pointer_Chunk_MS.CopyTo(hitbox_Properties_MS);
            hitbox_Melee_Hit_Hash_Chunk_MS.CopyTo(hitbox_Properties_MS);
            hitbox_Data_Chunk_MS.CopyTo(hitbox_Properties_MS);

            return hitbox_Properties_MS;
        }

        public MemoryStream write_All_Hitbox_Types(MemoryStream hitbox_Data_Chunk_MS, all_Hitbox_Types all_Hitbox_Types)
        {
            type_1_Hitbox_Data type_1_Hitbox_Data = all_Hitbox_Types.type_1_Hitboxes;
            type_2_Hitbox_Data type_2_Hitbox_Data = all_Hitbox_Types.type_2_Hitboxes;

            // After investigation there's no type 3 to 5 data, just a constant 0xc 0 padding
            /*
            type_3_Hitbox_Data type_3_Hitbox_Data = all_Hitbox_Types.type_3_Hitboxes;
            type_4_Hitbox_Data type_4_Hitbox_Data = all_Hitbox_Types.type_4_Hitboxes;
            type_5_Hitbox_Data type_5_Hitbox_Data = all_Hitbox_Types.type_5_Hitboxes;
            */

            uint type_1_Hitbox_Data_Count = (uint)type_1_Hitbox_Data.hitbox_Datas.Count();
            uint type_2_Hitbox_Data_Count = (uint)type_2_Hitbox_Data.hitbox_Datas.Count();

            /*
            uint type_3_Hitbox_Data_Count = (uint)type_3_Hitbox_Data.hitbox_Datas.Count();
            uint type_4_Hitbox_Data_Count = (uint)type_4_Hitbox_Data.hitbox_Datas.Count();
            uint type_5_Hitbox_Data_Count = (uint)type_5_Hitbox_Data.hitbox_Datas.Count();
            */

            appendUIntMemoryStream(hitbox_Data_Chunk_MS, type_1_Hitbox_Data_Count, true);
            appendUIntMemoryStream(hitbox_Data_Chunk_MS, type_2_Hitbox_Data_Count, true);

            /*
            appendUIntMemoryStream(hitbox_Data_Chunk_MS, type_3_Hitbox_Data_Count, true);
            appendUIntMemoryStream(hitbox_Data_Chunk_MS, type_4_Hitbox_Data_Count, true);
            appendUIntMemoryStream(hitbox_Data_Chunk_MS, type_5_Hitbox_Data_Count, true);
            */

            // the 0xc 0 padding
            appendUIntMemoryStream(hitbox_Data_Chunk_MS, 0, true);
            appendUIntMemoryStream(hitbox_Data_Chunk_MS, 0, true);
            appendUIntMemoryStream(hitbox_Data_Chunk_MS, 0, true);

            hitbox_Data_Chunk_MS = write_Hitbox_Datas(hitbox_Data_Chunk_MS, type_1_Hitbox_Data.hitbox_Datas);
            hitbox_Data_Chunk_MS = write_Hitbox_Datas(hitbox_Data_Chunk_MS, type_2_Hitbox_Data.hitbox_Datas);

            /*
            hitbox_Data_Chunk_MS = write_Hitbox_Datas(hitbox_Data_Chunk_MS, type_3_Hitbox_Data.hitbox_Datas);
            hitbox_Data_Chunk_MS = write_Hitbox_Datas(hitbox_Data_Chunk_MS, type_4_Hitbox_Data.hitbox_Datas);
            hitbox_Data_Chunk_MS = write_Hitbox_Datas(hitbox_Data_Chunk_MS, type_5_Hitbox_Data.hitbox_Datas);
            */

            return hitbox_Data_Chunk_MS;
        }

        public MemoryStream write_Hitbox_Datas(MemoryStream hitbox_Data_Chunk_MS, List<hitbox_Data> hitbox_Datas)
        {
            uint count = (uint)hitbox_Datas.Count();

            for(int i = 0; i < count; i++)
            {
                hitbox_Data hitbox_Data = hitbox_Datas[i];

                hitbox_Data_Type hitbox_Data_Type = hitbox_Data.hitbox_Type;

                appendUIntMemoryStream(hitbox_Data_Chunk_MS, (uint)hitbox_Data_Type, true);

                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.model_Hash, true);

                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x8, true);

                appendFloatMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.size, true);
                appendFloatMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x10, true);
                appendFloatMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x14, true);

                // These are always 0, never seen case where it is not.
                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x18, true);
                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x1c, true);
                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x20, true);
                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x24, true);
                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x28, true);
                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x2c, true);
                appendUIntMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x30, true);

                appendFloatMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x34, true);
                appendFloatMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.unk_0x38, true);
                appendFloatMemoryStream(hitbox_Data_Chunk_MS, hitbox_Data.direction, true);
            }

            return hitbox_Data_Chunk_MS;
        }

        public void deserialize_Hitbox_Properties()
        {
            Hitbox_Properties hitbox_Properties = parse_Hitbox_Properties(Properties.Settings.Default.inputHitboxPropertiesBinaryPath);
            string JSON = JsonConvert.SerializeObject(hitbox_Properties, Formatting.Indented);
            StreamWriter sw = File.CreateText(Properties.Settings.Default.outputHitboxPropertiesJSONPath + @"\Hitbox_Properties.JSON");
            sw.Write(JSON);
            sw.Close();
        }

        public Hitbox_Properties parse_Hitbox_Properties(string input)
        {
            Hitbox_Properties hitbox_Properties = new Hitbox_Properties();

            FileStream fs = File.OpenRead(input);

            hitbox_Properties.version = 1;
            hitbox_Properties.ID_Hash = readUIntBigEndian(fs);
            hitbox_Properties.unit_ID = readUIntBigEndian(fs);

            uint melee_Hitbox_Count = readUIntBigEndian(fs);
            uint unk_Hitbox_Count = readUIntBigEndian(fs);
            uint shield_Hitbox_Count = readUIntBigEndian(fs);

            uint total_Hitbox_Count = melee_Hitbox_Count + unk_Hitbox_Count + shield_Hitbox_Count;

            long hitbox_Hash_Chunk_Pointer = fs.Position;
            long hitbox_Data_Pointer_Chunk_Pointer = fs.Position + (total_Hitbox_Count * 0x4);
            long hitbox_Melee_Hit_Hash_Chunk_Pointer = fs.Position + (total_Hitbox_Count * 0x4 * 0x2);

            List<melee_Hitbox_Properties> melee_Hitbox_Data = new List<melee_Hitbox_Properties>();
            List<unk_Hitbox_Properties> unk_Hitbox_Data = new List<unk_Hitbox_Properties>();
            List<shield_Hitbox_Properties> shield_Hitbox_Data = new List<shield_Hitbox_Properties>();

            for (int i = 0; i < melee_Hitbox_Count; i++)
            {
                fs.Seek(hitbox_Hash_Chunk_Pointer + (i * 0x4), SeekOrigin.Begin);

                melee_Hitbox_Properties melee_Hitbox_Properties = new melee_Hitbox_Properties();

                uint hitbox_Hash = readUIntBigEndian(fs);

                // Go to pointer
                fs.Seek((hitbox_Data_Pointer_Chunk_Pointer + (i * 0x4)), SeekOrigin.Begin);

                uint hitbox_Data_Pointer = readUIntBigEndian(fs);

                // Go to pointer
                fs.Seek((hitbox_Melee_Hit_Hash_Chunk_Pointer + (i * 0x4)), SeekOrigin.Begin);

                uint hitbox_Melee_Hit_Hash = readUIntBigEndian(fs);

                melee_Hitbox_Properties.hitbox_Hash = hitbox_Hash;
                melee_Hitbox_Properties.hitbox_Hit_Hash = hitbox_Melee_Hit_Hash;

                fs.Seek(hitbox_Data_Pointer, SeekOrigin.Begin);

                melee_Hitbox_Properties.all_Hitbox_Types = read_All_Hitbox_Types(fs);

                melee_Hitbox_Data.Add(melee_Hitbox_Properties);
            }

            long unk_Hitbox_Pointer = hitbox_Hash_Chunk_Pointer + (melee_Hitbox_Count * 0x4);

            for (int i = 0; i < unk_Hitbox_Count; i++)
            {
                fs.Seek((unk_Hitbox_Pointer + (i * 0x4)), SeekOrigin.Begin);

                unk_Hitbox_Properties unk_Hitbox_Properties = new unk_Hitbox_Properties();

                uint hitbox_Hash = readUIntBigEndian(fs);

                // Go to pointer
                fs.Seek((hitbox_Data_Pointer_Chunk_Pointer + (melee_Hitbox_Count * 0x4) + (i * 0x4)), SeekOrigin.Begin);

                uint hitbox_Data_Pointer = readUIntBigEndian(fs);

                // Go to pointer
                fs.Seek((hitbox_Melee_Hit_Hash_Chunk_Pointer + (melee_Hitbox_Count * 0x4) + (i * 0x4)), SeekOrigin.Begin);

                uint hitbox_Melee_Hit_Hash = readUIntBigEndian(fs);

                // Check if not 0
                if (hitbox_Melee_Hit_Hash != 0)
                    throw new Exception();

                unk_Hitbox_Properties.hitbox_Hash = hitbox_Hash;

                fs.Seek(hitbox_Data_Pointer, SeekOrigin.Begin);

                unk_Hitbox_Properties.all_Hitbox_Types = read_All_Hitbox_Types(fs);

                unk_Hitbox_Data.Add(unk_Hitbox_Properties);
            }

            long shield_Hitbox_Pointer = hitbox_Hash_Chunk_Pointer + (melee_Hitbox_Count * 0x4) + (unk_Hitbox_Count * 0x4);

            for (int i = 0; i < shield_Hitbox_Count; i++)
            {
                fs.Seek((shield_Hitbox_Pointer + (i * 0x4)), SeekOrigin.Begin);

                shield_Hitbox_Properties shield_Hitbox_Properties = new shield_Hitbox_Properties();

                uint hitbox_Hash = readUIntBigEndian(fs);

                // Go to pointer
                fs.Seek((hitbox_Data_Pointer_Chunk_Pointer + (melee_Hitbox_Count * 0x4) + (unk_Hitbox_Count * 0x4) + (i * 0x4)), SeekOrigin.Begin);

                uint hitbox_Data_Pointer = readUIntBigEndian(fs);

                // Go to pointer
                fs.Seek((hitbox_Melee_Hit_Hash_Chunk_Pointer + (melee_Hitbox_Count * 0x4) + (unk_Hitbox_Count * 0x4) + (i * 0x4)), SeekOrigin.Begin);

                uint hitbox_Melee_Hit_Hash = readUIntBigEndian(fs);

                // Check if not 0
                if (hitbox_Melee_Hit_Hash != 0)
                    throw new Exception();

                shield_Hitbox_Properties.hitbox_Hash = hitbox_Hash;

                fs.Seek(hitbox_Data_Pointer, SeekOrigin.Begin);

                shield_Hitbox_Properties.all_Hitbox_Types = read_All_Hitbox_Types(fs);

                shield_Hitbox_Data.Add(shield_Hitbox_Properties);
            }

            hitbox_Properties.melee_Hitbox_Data = melee_Hitbox_Data;
            hitbox_Properties.unk_Hitbox_Data = unk_Hitbox_Data;
            hitbox_Properties.shield_Hitbox_Data = shield_Hitbox_Data;

            fs.Close();

            return hitbox_Properties;
        }

        public all_Hitbox_Types read_All_Hitbox_Types(FileStream fs)
        {
            all_Hitbox_Types all_Hitbox_Types = new all_Hitbox_Types();

            type_1_Hitbox_Data type_1_Hitbox_Data = new type_1_Hitbox_Data();
            type_2_Hitbox_Data type_2_Hitbox_Data = new type_2_Hitbox_Data();

            // After investigation there's no type 3 to 5 data, just a constant 0xc 0 padding
            /*
            type_3_Hitbox_Data type_3_Hitbox_Data = new type_3_Hitbox_Data();
            type_4_Hitbox_Data type_4_Hitbox_Data = new type_4_Hitbox_Data();
            type_5_Hitbox_Data type_5_Hitbox_Data = new type_5_Hitbox_Data();
            */

            uint type_1_Hitbox_Data_Count = readUIntBigEndian(fs);
            uint type_2_Hitbox_Data_Count = readUIntBigEndian(fs);

            /*
            uint type_3_Hitbox_Data_Count = readUIntBigEndian(fs);
            uint type_4_Hitbox_Data_Count = readUIntBigEndian(fs);
            uint type_5_Hitbox_Data_Count = readUIntBigEndian(fs);
            */

            uint zeroCheck1 = readUIntBigEndian(fs);
            uint zeroCheck2 = readUIntBigEndian(fs);
            uint zeroCheck3 = readUIntBigEndian(fs);

            if (zeroCheck1 != 0 || zeroCheck2 != 0 || zeroCheck3 != 0)
                throw new Exception();

            type_1_Hitbox_Data.hitbox_Datas = read_Hitbox_Datas(fs, type_1_Hitbox_Data_Count);
            type_2_Hitbox_Data.hitbox_Datas = read_Hitbox_Datas(fs, type_2_Hitbox_Data_Count);

            /*
            type_3_Hitbox_Data.hitbox_Datas = read_Hitbox_Datas(fs, type_3_Hitbox_Data_Count);
            type_4_Hitbox_Data.hitbox_Datas = read_Hitbox_Datas(fs, type_4_Hitbox_Data_Count);
            type_5_Hitbox_Data.hitbox_Datas = read_Hitbox_Datas(fs, type_5_Hitbox_Data_Count);
            */

            all_Hitbox_Types.type_1_Hitboxes = type_1_Hitbox_Data;
            all_Hitbox_Types.type_2_Hitboxes = type_2_Hitbox_Data;

            /*
            all_Hitbox_Types.type_3_Hitboxes = type_3_Hitbox_Data;
            all_Hitbox_Types.type_4_Hitboxes = type_4_Hitbox_Data;
            all_Hitbox_Types.type_5_Hitboxes = type_5_Hitbox_Data;
            */

            return all_Hitbox_Types;
        }

        public List<hitbox_Data> read_Hitbox_Datas(FileStream fs, uint count)
        {
            List<hitbox_Data> hitbox_Datas = new List<hitbox_Data>();

            for (int j = 0; j < count; j++)
            {
                hitbox_Data hitbox_Data = new hitbox_Data();

                uint hitbox_Data_Type = readUIntBigEndian(fs);

                if (hitbox_Data_Type >= 0x4)
                    throw new Exception();

                if (hitbox_Data_Type == 0)
                {

                }

                hitbox_Data.hitbox_Type = (hitbox_Data_Type)hitbox_Data_Type;

                uint model_Hash = readUIntBigEndian(fs);

                hitbox_Data.model_Hash = model_Hash;

                uint unk_0x8 = readUIntBigEndian(fs);

                hitbox_Data.unk_0x8 = unk_0x8;

                float size = readFloat(fs, true);

                hitbox_Data.size = size;

                float unk_0x10 = readFloat(fs, true);

                hitbox_Data.unk_0x10 = unk_0x10;

                float unk_0x14 = readFloat(fs, true);

                hitbox_Data.unk_0x14 = unk_0x14;

                uint unk_0x18 = readUIntBigEndian(fs);

                hitbox_Data.unk_0x18 = unk_0x18;

                uint unk_0x1c = readUIntBigEndian(fs);

                hitbox_Data.unk_0x1c = unk_0x1c;

                uint unk_0x20 = readUIntBigEndian(fs);

                hitbox_Data.unk_0x20 = unk_0x20;

                uint unk_0x24 = readUIntBigEndian(fs);

                hitbox_Data.unk_0x24 = unk_0x24;

                uint unk_0x28 = readUIntBigEndian(fs);

                hitbox_Data.unk_0x28 = unk_0x28;

                uint unk_0x2c = readUIntBigEndian(fs);

                hitbox_Data.unk_0x2c = unk_0x2c;

                uint unk_0x30 = readUIntBigEndian(fs);

                hitbox_Data.unk_0x30 = unk_0x30;

                float unk_0x34 = readFloat(fs, true);

                hitbox_Data.unk_0x34 = unk_0x34;

                float unk_0x38 = readFloat(fs, true);

                hitbox_Data.unk_0x38 = unk_0x38;

                float direction = readFloat(fs, true);

                hitbox_Data.direction = direction;

                hitbox_Datas.Add(hitbox_Data);
            }

            return hitbox_Datas;
        }
    }
}
