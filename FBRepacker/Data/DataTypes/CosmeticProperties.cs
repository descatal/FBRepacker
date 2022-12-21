using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    class CosmeticList
    {
        public uint magic { get; set; }
        public uint version { get; set; }
        public uint unk_0x8 { get; set; }
        public uint unk_0xC { get; set; }
        public Dictionary<uint, CosmeticProperties> properties { get; set; } = new Dictionary<uint, CosmeticProperties>();
    }

    class CosmeticProperties
    {
        // 0x48 in length
        public uint cosmetic_enum { get; set; }
        public float size { get; set; }
        public uint body_ALEO_hash { get; set; }
        public uint unk_ALEO_hash { get; set; }
        public uint unk_enum_0x10 { get; set; } // 0309
        public uint muzzle_ALEO_hash { get; set; }
        public uint collision_ALEO_hash { get; set; } // Collision to ground / building effect
        public uint unk_0x1C { get; set; }
        public float linger_time { get; set; } // 438C00000
        public uint unk_enum2_0x24 { get; set; } // 0309
        public uint unk_0x28 { get; set; }
        public uint unk_0x2C { get; set; }
        public uint unk_0x30 { get; set; }
        public uint unk_0x34 { get; set; }
        public uint unk_0x38 { get; set; }
        public uint unk_0x3C { get; set; }
        public uint unk_0x40 { get; set; }
        public uint unk_0x44 { get; set; }
    }
}
