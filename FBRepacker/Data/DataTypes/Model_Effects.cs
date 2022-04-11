using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    internal class Model_Effects
    {
        public uint version { get; set; }
        public uint ID_Hash { get; set; }
        public uint unit_ID { get; set; }
        public Dictionary<uint, List<Model_Bone_Effect_Dataset>> model_hashes_and_data { get; set; }

        public Model_Effects()
        {
            model_hashes_and_data = new Dictionary<uint, List<Model_Bone_Effect_Dataset>>();
        }
    }

    internal class Model_Bone_Effect_Dataset
    {
        public uint unk_bone_dataset_enum { get; set; }

        public List<Model_Bone_Effects_Data> dataset { get; set; }

        public Model_Bone_Effect_Dataset()
        {
            dataset = new List<Model_Bone_Effects_Data>();
        }
    }

    internal class Model_Bone_Effects_Data
    {
        public uint bone_index { get; set; }

        // Theoretically all 0x50 size?
        public uint ALEO_Hash_0x0 { get; set; }
        public uint unk_0x4 { get; set; }
        public uint ALEO_Hash_0x8 { get; set; }
        public uint unk_0xc { get; set; }
        public uint unk_0x10 { get; set; }
        public uint unk_0x14 { get; set; }
        public uint unk_0x18 { get; set; }
        public uint unk_0x1c { get; set; }
        public uint unk_0x20 { get; set; }
        public uint unk_0x24 { get; set; }
        public uint unk_0x28 { get; set; }
        public uint unk_0x2c { get; set; }
        public uint unk_0x30 { get; set; }
        public uint unk_0x34 { get; set; }
        public float unk_0x38 { get; set; }
        public uint unk_0x3c { get; set; }
        public float unk_0x40 { get; set; }
        public uint unk_0x44 { get; set; }
        public uint unk_0x48 { get; set; }
        public uint unk_0x4c { get; set; }
    }
}
