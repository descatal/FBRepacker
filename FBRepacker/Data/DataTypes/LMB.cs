using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    internal class LMB
    {
        public uint schemaVersion { get; set; }
        public List<LMB_Properties> LMB_Properties { get; set; }

        public LMB()
        {
            LMB_Properties = new List<LMB_Properties>();
        }
    }

    class LMB_Properties
    {
        public List<string> lmb_0xF001_string_list {  get; set; } // 0xF001

        public List<short> lmb_0xF002 { get; set; } // 1 set = 4 shorts

        public List<float> lmb_0xF003 { get; set; } // 1 set = 6 floats

        public List<float> lmb_0xF004 { get; set; } // 1 set = 3 floats

        public List<byte> lmb_0xF005 { get; set; } // set length is dependent on the enum

        public List<byte> lmb_0xF007 { get; set; } // 1 set = 4 floats, resolution of images.

        public uint lmb_0xF008 { get; set; } // this should only have 1 int

        public uint lmb_0xF009 { get; set; } // similiar with 0xF008

        public uint lmb_0xF00A { get; set; } // similiar with 0xF008

        public uint lmb_0xF00B { get; set; } // similiar with 0xF008, although the code does read the length

        public List<dynamic> lmb_0xF00C {  get; set; } 

        public List<float> lmb_0xF103 { get; set; } // 1 set = 2 floats

        public List<uint> lmb_0xF022 {  get; set; }



        public LMB_Properties()
        {
            lmb_0xF001_string_list = new List<string>();
        }
    }
}
