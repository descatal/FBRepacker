using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    class EFP
    {
        public uint schemaVersion { get; set; }
        public List<EFP_Properties> EFP_Properties { get; set; }

        public EFP()
        {
            EFP_Properties = new List<EFP_Properties>();
        }
    }

    class EFP_Properties
    {
        public uint EFP_hash { get; set; }
        public uint EFP_Type { get; set; }
        public uint ALEO_Hash { get; set; }
        public uint Model_Hash { get; set; }
        public uint unk_Enum { get; set; }
        public float translate_X_Offset { get; set; }
        public float translate_Y_Offset { get; set; }
        public float translate_Z_Offset { get; set; }
        public float rotate_X_Offset { get; set; }
        public float rotate_Y_Offset { get; set; }
        public float rotate_Z_Offset { get; set; }
        public float size { get; set; }
        public List<uint> extra_Info { get; set; }
        public EFP_Properties()
        {
            extra_Info = new List<uint>();
        }
    }
}
