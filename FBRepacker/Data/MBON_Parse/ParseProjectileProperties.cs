using FBRepacker.PAC;
using FBRepacker.Data.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.MBON_Parse
{
    class ParseProjectileProperties : Internals
    {
        Dictionary<uint, uint> convertAssistProjectileType = new Dictionary<uint, uint>() 
        {
            { 0x3, 0x2 }, // Missiles
            { 0x2A, 0x6 }, // Gerobi
            { 0xC3ECE, 0x52DA }, // Infinite Justice Boomerang
            { 0xC3ED8, 0x52E4 }, // Infinite Justice Anchor
            { 0xC3EC4, 0x52D0 }, // Infinite Justice Backpack Sub
            { 0xC3EE2, 0x5302 }, // Infinite Justice Assist
        };

        public ParseProjectileProperties()
        {
            FileStream fs = File.OpenRead(@"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice Boss METEOR\Extract MBON\Data - EBCEFEC7\001-MBON\002-FHM\010.bin");
            changeStreamFile(fs);

            string outputPath = @"G:\Games\PS3\EXVSFB JPN\Pkg research\FB Repacker\Repack\PAC\Input\MBON Reimport Project\Infinite Justice Boss METEOR\Converted from MBON\Projectile_Properties.bin";

            uint magic = readUIntBigEndian();
            uint hash = readUIntBigEndian();
            uint unitID = readUIntBigEndian();

            Dictionary<uint, Projectile_Properties> projectiles = new Dictionary<uint, Projectile_Properties>();
            uint properties_pointer = readUIntBigEndian();
            uint set_count = readUIntBigEndian();

            for (int i = 0; i < set_count; i++)
            {
                uint projectile_hash = readUIntBigEndian();
                uint returnAddress = (uint)Stream.Position;

                Stream.Seek(properties_pointer + (i * 0x128), SeekOrigin.Begin);
                Projectile_Properties proj = new Projectile_Properties();
                proj.projectile_Type = readUIntBigEndian();
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
                proj.max_travel_distance = readUIntBigEndian();
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
                proj.gerobi_wiggle = readUIntBigEndian();
                proj.effect_conductivity = readUIntBigEndian();
                proj.unk_0xE4 = readUIntBigEndian();
                proj.unk_0xE8 = readUIntBigEndian();
                proj.unk_0xEC = readUIntBigEndian();
                proj.unk_0xF0 = readUIntBigEndian();
                proj.unk_0xF4 = readUIntBigEndian();
                proj.unk_0xF8 = readUIntBigEndian();
                proj.unk_0xFC = readUIntBigEndian();
                proj.unk_0x100 = readUIntBigEndian();
                proj.unk_0x104 = readUIntBigEndian();
                proj.unk_0x108 = readUIntBigEndian();
                proj.unk_0x10C = readUIntBigEndian();
                proj.unk_0x110 = readUIntBigEndian();
                proj.unk_0x114 = readUIntBigEndian();
                proj.unk_0x118 = readUIntBigEndian();
                proj.unk_0x11C = readUIntBigEndian();
                proj.unk_0x120 = readUIntBigEndian();
                proj.unk_0x124 = readUIntBigEndian();

                projectiles[projectile_hash] = proj;

                Stream.Seek(returnAddress, SeekOrigin.Begin);
            }

            fs.Close();

            MemoryStream output_projectile = new MemoryStream();
            List<uint> projectile_hashes = projectiles.Keys.ToList();

            appendUIntMemoryStream(output_projectile, magic, true);
            appendUIntMemoryStream(output_projectile, hash, true);
            appendUIntMemoryStream(output_projectile, unitID, true);
            appendUIntMemoryStream(output_projectile, (0x10 + 0x4 + (uint)((projectile_hashes.Count) * 4)), true); // pointer

            appendUIntMemoryStream(output_projectile, (uint)projectile_hashes.Count, true);
            for (int i = 0; i < projectile_hashes.Count; i++)
            {
                appendUIntMemoryStream(output_projectile, projectile_hashes[i], true);
            }

            for(int i = 0; i < projectiles.Count; i++)
            {
                Projectile_Properties proj = projectiles[projectile_hashes[i]];

                if (convertAssistProjectileType.ContainsKey(proj.projectile_Type))
                    proj.projectile_Type = convertAssistProjectileType[proj.projectile_Type];

                if(proj.projectile_Type > 0x6)
                {

                }

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
                appendUIntMemoryStream(output_projectile, proj.max_travel_distance, true);
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
                appendUIntMemoryStream(output_projectile, proj.gerobi_wiggle, true);
                appendUIntMemoryStream(output_projectile, proj.effect_conductivity, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xE4, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xE8, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xEC, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xF0, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xF4, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xF8, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0xFC, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x100, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x104, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x108, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x10C, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x110, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x114, true);

                /*
                appendUIntMemoryStream(output_projectile, proj.unk_0x118, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x11C, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x120, true);
                appendUIntMemoryStream(output_projectile, proj.unk_0x124, true);
                */
            }

            FileStream ofs = File.Create(outputPath);
            output_projectile.Seek(0, SeekOrigin.Begin);
            output_projectile.CopyTo(ofs);
            ofs.Close();
        }
    }
}
