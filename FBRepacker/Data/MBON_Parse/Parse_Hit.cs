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
    class Parse_Hit : Internals
    {
        Dictionary<uint, string> hashDic = new Dictionary<uint, string>
            {
                { 0x33EF2CCB, "hit_type" },
                { 0xA964CCA4, "damage" },
                { 0x07431A19, "unk_0x8" },
                { 0x0EDBFE57, "down_value" },
                { 0xD196FC95, "yoruke_value" },
                { 0xD5B8EA1F, "unk_MBON" },
                { 0x54058D5D, "unk_type_0x14" },
                { 0x1A107AB7, "damage_correction" },
                { 0xFAE45595, "special_effect" },
                { 0xBEDC2392, "hit_effect" },
                { 0xEE43A562, "fly_direction_1" },
                { 0x38BEA931, "fly_direction_2" },
                { 0xE392B8D6, "fly_direction_3" },
                { 0xA7C78487, "enemy_camera_shake_multiplier" },
                { 0x7BE01C98, "player_camera_shake_multiplier" },
                { 0x0408DD77, "unk_0x38" },
                { 0x29941888, "knock_up_angle" },
                { 0xC47A5D38, "knock_up_range" },
                { 0x502E9BAF, "unk_0x44" },
                { 0x46CED294, "multiple_hit_interval_frame" },
                { 0xBF000953, "multiple_hit_count" },
                { 0x8B954576, "enemy_stun_duration" },
                { 0x57B2DD69, "player_stun_duration" },
                { 0xE252D228, "hit_visual_effect" },
                { 0xC0EB5412, "hit_visual_effect_size_multiplier" },
                { 0xDDCB9D74, "hit_sound_effect_hash" },
                { 0x8823E502, "unk_0x64" },
                { 0x4F1F46C1, "friendly_damage_flag" },
                { 0xB3419082, "unk_0x6C" },
            };

        List<uint> allKnownHash = new List<uint>
        {
            0x33EF2CCB,
            0xA964CCA4,
            0x07431A19,
            0x0EDBFE57,
            0xD196FC95,
            0xD5B8EA1F,
            0x54058D5D,
            0x1A107AB7,
            0xFAE45595,
            0xBEDC2392,
            0xEE43A562,
            0x38BEA931,
            0xE392B8D6,
            0xA7C78487,
            0x7BE01C98,
            0x0408DD77,
            0x29941888,
            0xC47A5D38,
            0x502E9BAF,
            0x46CED294,
            0xBF000953,
            0x8B954576,
            0x57B2DD69,
            0xE252D228,
            0xC0EB5412,
            0xDDCB9D74,
            0x8823E502,
            0x4F1F46C1,
            0xB3419082,
        };

        public Parse_Hit()
        {

        }

        public void parse_Hit()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.HitBinaryFilePath);

            List<uint> properties_type_hashes = new List<uint>();
            List<HitProperties> hitPropertiesList = new List<HitProperties>();

            Hit hit = new Hit();

            uint magic_hash = readUIntBigEndian(fs);
            uint properties_hash_list_pointer = readUIntBigEndian(fs);
            uint properties_pointer = readUIntBigEndian(fs);
            uint unk_0xC = readUIntBigEndian(fs);

            hit.magic_hash = magic_hash;

            if (unk_0xC != 0)
                throw new Exception("0xc is not 0!");

            uint number_of_properties_type = readUIntBigEndian(fs);

            for(int i = 0; i < number_of_properties_type; i++)
            {
                uint properties_hash = readUIntBigEndian(fs);

                if (!hashDic.Keys.Contains(properties_hash))
                    throw new Exception("new unidentified properties type!");

                properties_type_hashes.Add(properties_hash);
            }

            fs.Seek(properties_hash_list_pointer, SeekOrigin.Begin);

            uint number_of_properties = readUIntBigEndian(fs);

            for (int i = 0; i < number_of_properties; i++)
            {
                HitProperties hitProperties = new HitProperties();
                uint properties_hash = readUIntBigEndian(fs);

                hitProperties.hash = properties_hash;

                uint return_pos = (uint)fs.Position;
                fs.Seek((properties_pointer + i * (number_of_properties_type * 4)), SeekOrigin.Begin);

                if (properties_type_hashes.Contains(allKnownHash[0]))
                    hitProperties.hit_type = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[1]))
                    hitProperties.damage = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[2]))
                    hitProperties.unk_0x8 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[3]))
                    hitProperties.down_value = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[4]))
                    hitProperties.yoruke_value = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[5]))
                    hitProperties.unk_MBON = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[6]))
                    hitProperties.unk_type_0x14 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[7]))
                    hitProperties.damage_correction = readFloat(fs, true);
                if (properties_type_hashes.Contains(allKnownHash[8]))
                    hitProperties.special_effect = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[9]))
                    hitProperties.hit_effect = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[10]))
                    hitProperties.fly_direction_1 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[11]))
                    hitProperties.fly_direction_2 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[12]))
                    hitProperties.fly_direction_3 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[13]))
                    hitProperties.enemy_camera_shake_multiplier = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[14]))
                    hitProperties.player_camera_shake_multiplier = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[15]))
                    hitProperties.unk_0x38 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[16]))
                    hitProperties.knock_up_angle = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[17]))
                    hitProperties.knock_up_range = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[18]))
                    hitProperties.unk_0x44 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[19]))
                    hitProperties.multiple_hit_interval_frame = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[20]))
                    hitProperties.multiple_hit_count = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[21]))
                    hitProperties.enemy_stun_duration = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[22]))
                    hitProperties.player_stun_duration = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[23]))
                    hitProperties.hit_visual_effect = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[24]))
                    hitProperties.hit_visual_effect_size_multiplier = readFloat(fs, true);
                if (properties_type_hashes.Contains(allKnownHash[25]))
                    hitProperties.hit_sound_effect_hash = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[26]))
                    hitProperties.unk_0x64 = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[27]))
                    hitProperties.friendly_damage_flag = readUIntBigEndian(fs);
                if (properties_type_hashes.Contains(allKnownHash[28]))
                    hitProperties.unk_0x6C = readUIntBigEndian(fs);

                hitPropertiesList.Add(hitProperties);

                fs.Seek(return_pos, SeekOrigin.Begin);
            }

            hit.hitProperties = hitPropertiesList;
            hit.hitPropertiesType = properties_type_hashes;

            fs.Close();

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.HitBinaryFilePath);
            string outputPath = Properties.Settings.Default.outputHitJSONFolderPath + @"\" + fileName + @"_Hit.JSON";

            string JSON = JsonConvert.SerializeObject(hit, Formatting.Indented);

            StreamWriter fsJSON = File.CreateText(outputPath);
            fsJSON.Write(JSON);
            fsJSON.Close();
        }

        public void write_Hit_Binary()
        {
            StreamReader fs = File.OpenText(Properties.Settings.Default.HitJSONFilePath);

            string JSON = fs.ReadToEnd();
            Hit hit = JsonConvert.DeserializeObject<Hit>(JSON);

            MemoryStream hitBinary = new MemoryStream();

            appendUIntMemoryStream(hitBinary, hit.magic_hash, true);

            MemoryStream properties_type_hashes = new MemoryStream();
            List<uint> hit_properties_type_hashes = hit.hitPropertiesType;
            appendUIntMemoryStream(properties_type_hashes, (uint)hit_properties_type_hashes.Count(), true);
            for (int i = 0; i < hit_properties_type_hashes.Count(); i++)
            {
                appendUIntMemoryStream(properties_type_hashes, hit_properties_type_hashes[i], true);
            }

            properties_type_hashes.Seek(0, SeekOrigin.Begin);

            MemoryStream properties_hash = new MemoryStream();
            MemoryStream properties = new MemoryStream();
            List<HitProperties> properties_list = hit.hitProperties;
            appendUIntMemoryStream(properties_hash, (uint)properties_list.Count(), true);

            for (int i = 0; i < properties_list.Count(); i++)
            {
                HitProperties hitProperties = properties_list[i];
                appendUIntMemoryStream(properties_hash, hitProperties.hash, true);

                if (hitProperties.damage_correction > 100)
                {

                }

                if(hit_properties_type_hashes.Contains(allKnownHash[0]))
                    appendUIntMemoryStream(properties, hitProperties.hit_type, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[1]))
                    appendUIntMemoryStream(properties, hitProperties.damage, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[2]))
                    appendUIntMemoryStream(properties, hitProperties.unk_0x8, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[3]))
                    appendUIntMemoryStream(properties, hitProperties.down_value, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[4]))
                    appendUIntMemoryStream(properties, hitProperties.yoruke_value, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[5]))
                    appendUIntMemoryStream(properties, hitProperties.unk_MBON, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[6]))
                    appendUIntMemoryStream(properties, hitProperties.unk_type_0x14, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[7]))
                    appendFloatMemoryStream(properties, hitProperties.damage_correction, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[8]))
                    appendUIntMemoryStream(properties, hitProperties.special_effect, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[9]))
                    appendUIntMemoryStream(properties, hitProperties.hit_effect, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[10]))
                    appendUIntMemoryStream(properties, hitProperties.fly_direction_1, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[11]))
                    appendUIntMemoryStream(properties, hitProperties.fly_direction_2, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[12]))
                    appendUIntMemoryStream(properties, hitProperties.fly_direction_3, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[13]))
                    appendUIntMemoryStream(properties, hitProperties.enemy_camera_shake_multiplier, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[14]))
                    appendUIntMemoryStream(properties, hitProperties.player_camera_shake_multiplier, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[15]))
                    appendUIntMemoryStream(properties, hitProperties.unk_0x38, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[16]))
                    appendUIntMemoryStream(properties, hitProperties.knock_up_angle, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[17]))
                    appendUIntMemoryStream(properties, hitProperties.knock_up_range, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[18]))
                    appendUIntMemoryStream(properties, hitProperties.unk_0x44, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[19]))
                    appendUIntMemoryStream(properties, hitProperties.multiple_hit_interval_frame, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[20]))
                    appendUIntMemoryStream(properties, hitProperties.multiple_hit_count, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[21]))
                    appendUIntMemoryStream(properties, hitProperties.enemy_stun_duration, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[22]))
                    appendUIntMemoryStream(properties, hitProperties.player_stun_duration, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[23]))
                    appendUIntMemoryStream(properties, hitProperties.hit_visual_effect, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[24]))
                    appendFloatMemoryStream(properties, hitProperties.hit_visual_effect_size_multiplier, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[25]))
                    appendUIntMemoryStream(properties, hitProperties.hit_sound_effect_hash, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[26]))
                    appendUIntMemoryStream(properties, hitProperties.unk_0x64, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[27]))
                    appendUIntMemoryStream(properties, hitProperties.friendly_damage_flag, true);
                if (hit_properties_type_hashes.Contains(allKnownHash[28]))
                    appendUIntMemoryStream(properties, hitProperties.unk_0x6C, true);
            }

            properties_hash.Seek(0, SeekOrigin.Begin);
            properties.Seek(0, SeekOrigin.Begin);

            uint properties_hash_list_pointer = 0x10 + (uint)properties_type_hashes.Length;
            appendUIntMemoryStream(hitBinary, properties_hash_list_pointer, true);

            uint properties_pointer = properties_hash_list_pointer + (uint)properties_hash.Length;
            appendUIntMemoryStream(hitBinary, properties_pointer, true);

            appendUIntMemoryStream(hitBinary, 0, true);

            properties_type_hashes.CopyTo(hitBinary);
            properties_hash.CopyTo(hitBinary);
            properties.CopyTo(hitBinary);

            FileStream ofs = File.OpenWrite(Properties.Settings.Default.outputHitBinFolderPath + @"\" + Path.GetFileNameWithoutExtension(Properties.Settings.Default.HitJSONFilePath) + ".bin");
            hitBinary.Seek(0, SeekOrigin.Begin);
            hitBinary.CopyTo(ofs);

            ofs.Close();
            fs.Close();

            // Create a backup copy of old JSON.
            string oriJSONFileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.HitJSONFilePath);
            string oriJSONFilePath = Path.GetDirectoryName(Properties.Settings.Default.HitJSONFilePath);
            File.Copy(Properties.Settings.Default.HitJSONFilePath, oriJSONFilePath + @"\" + oriJSONFileName + "_backup.JSON", true);

            string outputPath = Properties.Settings.Default.HitJSONFilePath;

            StreamWriter fsJSON = File.CreateText(outputPath);
            fsJSON.Write(JSON);
            fsJSON.Close();
        }
    }
}
