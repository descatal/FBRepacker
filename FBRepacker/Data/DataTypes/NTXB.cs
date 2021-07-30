using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Data.DataTypes
{
    class NTXB
    {
        public int schema_version { get; set; }
        public int NTXB_Version { get; set; }
        public List<NTXBInfo> NTXBInfo { get; set; }

        public NTXB()
        {
            NTXBInfo = new List<NTXBInfo>();
        }
    }

    class NTXBInfo
    {
        public string str { get; set; }
        public string nameStr { get; set; }

        public str_Info str_Info { get; set; }
        public name_Info name_Info { get; set; }
        public string_Data_Info string_Data { get; set; }

        public NTXBInfo()
        {
            str_Info = new str_Info();
            name_Info = new name_Info();
            string_Data = new string_Data_Info();
        }
    }

    class str_Info
    {
        public ushort str_info_unk_0xC { get; set; } // Should be always 0xFF
        public ushort str_info_unk_0xE { get; set; } // Could be 0x12 or 0x22 or 0x24
    }

    class name_Info
    {
        public ushort str_Name_Crc16X25_Checksum { get; set; } // Not sure what this is, but is used to determine the order in the name_Info
    }

    class string_Data_Info
    {
        public string unk_Str { get; set; } 
        public ushort unk_str_ushort { get; set; }
    }


}
