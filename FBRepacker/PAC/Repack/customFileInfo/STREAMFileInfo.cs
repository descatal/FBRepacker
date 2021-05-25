using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    public class STREAMFileInfo
    {
        public uint codec { get; set; }
        public uint subheader_size { get; set; }
        public uint loop_start { get; set; }
        public uint loop_length { get; set; }
        public uint loop_flag { get; set; }
        public float loop_float { get; set; }
        public float loop_float_2 { get; set; }
        public float var_0x50 { get; set; }
        public uint var_0x54 { get; set; }
        public uint var_0x60 { get; set; }
        public float var_0x6C { get; set; }
        public uint var_0x70 { get; set; }
        public uint var_0x9C { get; set; }
        public uint var_0xAC { get; set; }
        public string file_Name { get; set; }
    }
}
