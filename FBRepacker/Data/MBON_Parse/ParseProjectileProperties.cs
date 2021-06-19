using FBRepacker.PAC;
using FBRepacker.Data.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace FBRepacker.Data.MBON_Parse
{
    class ParseProjectileProperties : Internals
    {
        Dictionary<uint, uint> convertAssistProjectileType = new Dictionary<uint, uint>()
        {
            { 0x3, 0x2 }, // Missiles
            { 0x2A, 0x6 }, // Gerobi
            { 0x3C, 0x46 }, // Bafuku
            { 0xC3ECE, 0x52DA }, // Infinite Justice Boomerang
            { 0xC3ED8, 0x52E4 }, // Infinite Justice Anchor
            { 0xC3EC4, 0x52D0 }, // Infinite Justice Backpack Sub
            { 0xC3EE2, 0x5302 }, // Infinite Justice Assist
        };

        public ParseProjectileProperties()
        {

        }

        public void convertProjectileBintoJSON()
        {
            FileStream fs = File.OpenRead(Properties.Settings.Default.ProjecitleBinaryFilePath);
            changeStreamFile(fs);

            string fileName = Path.GetFileNameWithoutExtension(Properties.Settings.Default.ProjecitleBinaryFilePath);
            string outputPath = Properties.Settings.Default.outputProjectileJSONFolderPath + @"\" + fileName + @"_Projectile.JSON";

            uint magic = readUIntBigEndian();
            uint hash = readUIntBigEndian();
            uint unitID = readUIntBigEndian();

            Projectile_Properties projectile_Properties = new Projectile_Properties();
            projectile_Properties.magic = magic;
            projectile_Properties.unit_ID = unitID;
            projectile_Properties.hash = hash;

            List<Individual_Projectile_Properties> individual_projectiles = new List<Individual_Projectile_Properties>();
            uint properties_pointer = readUIntBigEndian();
            uint set_count = readUIntBigEndian();

            // Determine MBON or FB
            uint fileSize = (uint)fs.Length;
            uint properties_size = fileSize - (0x14 + set_count * 4);
            bool ifMBON = properties_size % (0x128 * set_count) == 0 ? true : false;
            int prop_length = ifMBON ? 0x128 : 0x118;


            for (int i = 0; i < set_count; i++)
            {
                uint projectile_hash = readUIntBigEndian();
                uint returnAddress = (uint)Stream.Position;

                Stream.Seek(properties_pointer + (i * prop_length), SeekOrigin.Begin);
                Individual_Projectile_Properties proj = new Individual_Projectile_Properties();

                proj.hash = projectile_hash;
                proj.projectile_Type = readUIntBigEndian();

                if (convertAssistProjectileType.ContainsKey(proj.projectile_Type) && Properties.Settings.Default.convertMBONProjecitle)
                    proj.projectile_Type = convertAssistProjectileType[proj.projectile_Type];

                proj.hit_properties_hash = readUIntBigEndian();
                proj.model_nud_hash = readUIntBigEndian();
                proj.model_vbn_index = readUIntBigEndian();
                proj.aim_enum = readUIntBigEndian();
                proj.translate_Y = readFloat(true);
                proj.translate_Z = readFloat(true);
                proj.translate_X = readFloat(true);
                proj.rotate_X_angle = readFloat(true);
                proj.rotate_Z_angle = readFloat(true);
                proj.cosmetic_hash = readUIntBigEndian();
                proj.unk_0x2C = readUIntBigEndian();
                proj.unk_0x30 = readUIntBigEndian();
                proj.unk_0x34 = readUIntBigEndian();
                proj.unk_0x38 = readUIntBigEndian();
                proj.ammo_reduce_amount = readUIntBigEndian();
                proj.duration_frame = readUIntBigEndian();
                proj.max_travel_distance = readFloat(true);
                proj.initial_speed = readFloat(true);
                proj.acceleration = readFloat(true);
                proj.unk_0x50 = readUIntBigEndian();
                proj.unk_0x54 = readUIntBigEndian();
                proj.max_speed = readFloat(true);
                proj.unk_0x5C = readUIntBigEndian();
                proj.unk_0x60 = readUIntBigEndian();
                proj.unk_0x64 = readUIntBigEndian();
                proj.unk_0x68 = readUIntBigEndian();
                proj.unk_0x6C = readUIntBigEndian();
                proj.unk_0x70 = readUIntBigEndian();
                proj.unk_0x74 = readUIntBigEndian();
                proj.horizontal_guidance_amount = readFloat(true);
                proj.horizontal_guidance_angle = readFloat(true);
                proj.vertical_guidance_amount = readFloat(true);
                proj.vertical_guidance_angle = readFloat(true);
                proj.unk_0x88 = readUIntBigEndian();
                proj.unk_0x8C = readUIntBigEndian();
                proj.unk_0x90 = readFloat(true);
                proj.unk_0x94 = readFloat(true);
                proj.unk_0x98 = readFloat(true);
                proj.unk_0x9C = readFloat(true);
                proj.unk_0xA0 = readUIntBigEndian();
                proj.unk_0xA4 = readUIntBigEndian();
                proj.unk_0xA8 = readUIntBigEndian();
                proj.gerobi_length = readFloat(true);
                proj.size = readFloat(true);
                proj.penetrate_target = readUIntBigEndian();
                proj.unk_0xB8 = readUIntBigEndian();
                proj.sound_effect_hash = readUIntBigEndian();
                proj.unk_0xC0 = readUIntBigEndian();
                proj.unk_0xC4 = readUIntBigEndian();
                proj.continue_projectile_hash = readUIntBigEndian();
                proj.unk_0xCC = readUIntBigEndian();
                proj.unk_0xD0 = readUIntBigEndian();
                proj.unk_0xD4 = readUIntBigEndian();
                proj.unk_0xD8 = readUIntBigEndian();
                proj.gerobi_wiggle = readFloat(true);
                proj.effect_conductivity = readFloat(true);
                proj.unk_0xE4 = readFloat(true);
                proj.unk_0xE8 = readFloat(true);
                proj.unk_0xEC = readFloat(true);
                proj.unk_0xF0 = readFloat(true);
                proj.unk_0xF4 = readFloat(true);
                proj.unk_0xF8 = readFloat(true);
                proj.unk_0xFC = readFloat(true);
                proj.unk_0x100 = readFloat(true);
                proj.unk_0x104 = readFloat(true);
                proj.unk_0x108 = readFloat(true);
                proj.unk_0x10C = readFloat(true);
                proj.unk_0x110 = readFloat(true);
                proj.unk_0x114 = readFloat(true);

                if (ifMBON)
                {
                    proj.unk_0x118 = readFloat(true);
                    proj.unk_0x11C = readFloat(true);
                    proj.unk_0x120 = readFloat(true);
                    proj.unk_0x124 = readFloat(true);
                }
                else
                {
                    proj.unk_0x118 = 0;
                    proj.unk_0x11C = 0;
                    proj.unk_0x120 = 0;
                    proj.unk_0x124 = 0;
                }

                individual_projectiles.Add(proj);

                Stream.Seek(returnAddress, SeekOrigin.Begin);
            }

            fs.Close();

            projectile_Properties.individual_Projectile_Properties = individual_projectiles;

            string JSON = JsonConvert.SerializeObject(projectile_Properties, Formatting.Indented);
            //JsonSerializerOptions json_options = new JsonSerializerOptions();
            //json_options.WriteIndented = true;
            //string JSON = JsonSerializer.Serialize<Projectile_Properties>(projectile_Properties, json_options);

            StreamWriter fsJSON = File.CreateText(outputPath);
            fsJSON.Write(JSON);
            fsJSON.Close();
        }

        public Projectile_Properties parseProjectileJSON()
        {
            StreamReader fs = File.OpenText(Properties.Settings.Default.ProjecitleJSONFilePath);
            string JSON = fs.ReadToEnd();
            Projectile_Properties projectile_Properties = JsonConvert.DeserializeObject<Projectile_Properties>(JSON);

            return projectile_Properties;
        }

        public void writeProjectileBinary(Projectile_Properties projectile_Properties)
        {
            List<Individual_Projectile_Properties> projectiles = projectile_Properties.individual_Projectile_Properties;
            MemoryStream output_projectile = new MemoryStream();
            List<uint> projectile_hashes = projectiles.Select(x => x.hash).ToList();

            uint magic = projectile_Properties.magic;
            uint hash = projectile_Properties.hash;
            uint unitID = projectile_Properties.unit_ID;

            appendUIntMemoryStream(output_projectile, magic, true);
            appendUIntMemoryStream(output_projectile, hash, true);
            appendUIntMemoryStream(output_projectile, unitID, true);
            appendUIntMemoryStream(output_projectile, (0x10 + 0x4 + (uint)((projectile_hashes.Count) * 4)), true); // pointer

            appendUIntMemoryStream(output_projectile, (uint)projectile_hashes.Count, true);
            for (int i = 0; i < projectile_hashes.Count; i++)
            {
                appendUIntMemoryStream(output_projectile, projectile_hashes[i], true);
            }

            for (int i = 0; i < projectiles.Count; i++)
            {
                Individual_Projectile_Properties proj = projectiles[i];

                appendUIntMemoryStream(output_projectile, proj.projectile_Type, true);
                appendUIntMemoryStream(output_projectile, proj.hit_properties_hash, true);
                appendUIntMemoryStream(output_projectile, proj.model_nud_hash, true);
                appendUIntMemoryStream(output_projectile, proj.model_vbn_index, true);
                appendUIntMemoryStream(output_projectile, proj.aim_enum, true);
                appendFloatMemoryStream(output_projectile, proj.translate_Y, true);
                appendFloatMemoryStream(output_projectile, proj.translate_Z, true);
                appendFloatMemoryStream(output_projectile, proj.translate_X, true);
                appendFloatMemoryStream(output_projectile, proj.rotate_X_angle, true);
                appendFloatMemoryStream(output_projectile, proj.rotate_Z_angle, true);
                appendUIntMemoryStream(output_projectile, proj.cosmetic_hash, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x2C, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x30, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x34, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x38, true);
                appendUIntMemoryStream(output_projectile, proj.ammo_reduce_amount, true);
                appendUIntMemoryStream(output_projectile, proj.duration_frame, true);
                appendFloatMemoryStream(output_projectile, proj.max_travel_distance, true);
                appendFloatMemoryStream(output_projectile, proj.initial_speed, true);
                appendFloatMemoryStream(output_projectile, proj.acceleration, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x50, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x54, true);
                appendFloatMemoryStream(output_projectile, proj.max_speed, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x5C, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x60, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x64, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x68, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x6C, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x70, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x74, true);
                appendFloatMemoryStream(output_projectile, proj.horizontal_guidance_amount, true);
                appendFloatMemoryStream(output_projectile, proj.horizontal_guidance_angle, true);
                appendFloatMemoryStream(output_projectile, proj.vertical_guidance_amount, true);
                appendFloatMemoryStream(output_projectile, proj.vertical_guidance_angle, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x88, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x8C, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x90, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x94, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x98, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x9C, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xA0, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xA4, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xA8, true);
                appendFloatMemoryStream(output_projectile, proj.gerobi_length, true);
                appendFloatMemoryStream(output_projectile, proj.size, true);
                appendUIntMemoryStream(output_projectile, proj.penetrate_target, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xB8, true);
                appendUIntMemoryStream(output_projectile, proj.sound_effect_hash, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xC0, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xC4, true);
                appendUIntMemoryStream(output_projectile, proj.continue_projectile_hash, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xCC, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xD0, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xD4, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xD8, true);
                appendFloatMemoryStream(output_projectile, proj.gerobi_wiggle, true);
                appendFloatMemoryStream(output_projectile, proj.effect_conductivity, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0xE4, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0xE8, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0xEC, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0xF0, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0xF4, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0xF8, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0xFC, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x100, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x104, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x108, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x10C, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x110, true);
                appendFloatMemoryStream(output_projectile, proj.unk_0x114, true);

                if (Properties.Settings.Default.ProjectileBinaryInputGameVer == 1)
                {
                    appendFloatMemoryStream(output_projectile, proj.unk_0x118, true);
                    appendFloatMemoryStream(output_projectile, proj.unk_0x11C, true);
                    appendFloatMemoryStream(output_projectile, proj.unk_0x120, true);
                    appendFloatMemoryStream(output_projectile, proj.unk_0x124, true);
                }

                string fileName = System.IO.Path.GetFileNameWithoutExtension(Properties.Settings.Default.ProjecitleJSONFilePath);
                FileStream ofs = File.Create(Properties.Settings.Default.outputProjectileBinFolderPath + @"\" + fileName + ".bin");
                output_projectile.Seek(0, SeekOrigin.Begin);
                output_projectile.CopyTo(ofs);
                ofs.Close();
            }
        }
    }
}
