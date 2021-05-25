using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    class Projectile_Properties
    {
        public uint projectile_Type { get; set; }
        public uint hit_properties_hash { get; set; }
        public uint model_nud_hash { get; set; }
        public uint model_vbn_index { get; set; }
        public uint aim_enum { get; set; }
        public float translate_Y { get; set; }
        public float translate_Z { get; set; }
        public float translate_X { get; set; }
        public float rotate_X_angle { get; set; } // radian
        public float rotate_Z_angle { get; set; } // radian
        public uint cosmetic_hash { get; set; }
        public uint unk_0x2C { get; set; }
        public uint unk_0x30 { get; set; }
        public uint unk_0x34 { get; set; }
        public uint unk_0x38 { get; set; }
        public uint ammo_reduce_amount { get; set; }
        public uint duration_frame { get; set; }
        public uint max_travel_distance { get; set; }
        public float initial_speed { get; set; }
        public float acceleration { get; set; }
        public uint unk_0x50 { get; set; }
        public uint unk_0x54 { get; set; }
        public float max_speed { get; set; }
        public uint unk_0x5C { get; set; }
        public uint unk_0x60 { get; set; }
        public uint unk_0x64 { get; set; }
        public uint unk_0x68 { get; set; }
        public uint unk_0x6C { get; set; }
        public uint unk_0x70 { get; set; }
        public uint unk_0x74 { get; set; } // Always 2
        public float horizontal_guidance_amount { get; set; }
        public float horizontal_guidance_angle { get; set; } // radian
        public float vertical_guidance_amount { get; set; }
        public float vertical_guidance_angle { get; set; } // radian
        public uint unk_0x88 { get; set; }
        public uint unk_0x8C { get; set; }
        public float unk_0x90 { get; set; }
        public float unk_0x94 { get; set; }
        public float unk_0x98 { get; set; }
        public float unk_0x9C { get; set; }
        public uint unk_0xA0 { get; set; }
        public uint unk_0xA4 { get; set; }
        public uint unk_0xA8 { get; set; }
        public float gerobi_length { get; set; }
        public float size { get; set; }
        public uint penetrate_target { get; set; }
        public uint unk_0xB8 { get; set; }
        public uint sound_effect_hash { get; set; } // vag
        public uint unk_0xC0 { get; set; }
        public uint unk_0xC4 { get; set; }
        public uint continue_projectile_hash { get; set; } // Bafuku / explode
        public uint unk_0xCC { get; set; }
        public uint unk_0xD0 { get; set; }
        public uint unk_0xD4 { get; set; }
        public uint unk_0xD8 { get; set; }
        public uint gerobi_wiggle { get; set; }
        public uint effect_conductivity { get; set; }
        public uint unk_0xE4 { get; set; }
        public uint unk_0xE8 { get; set; }
        public uint unk_0xEC { get; set; }
        public uint unk_0xF0 { get; set; }
        public uint unk_0xF4 { get; set; }
        public uint unk_0xF8 { get; set; }
        public uint unk_0xFC { get; set; }
        public uint unk_0x100 { get; set; }
        public uint unk_0x104 { get; set; }
        public uint unk_0x108 { get; set; }
        public uint unk_0x10C { get; set; }
        public uint unk_0x110 { get; set; }
        public uint unk_0x114 { get; set; }
        public uint unk_0x118 { get; set; }
        public uint unk_0x11C { get; set; }
        public uint unk_0x120 { get; set; }
        public uint unk_0x124 { get; set; }

        public Projectile_Properties()
        {

        }
    }
}
