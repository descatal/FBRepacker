using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    public class EIDXFileInfo
    {
        public enum fileType
        {
            ALEO = 0x01,
            NUT = 0x02,
            NUD = 0x03
        }

        public int file_Index;
        public string file_Hash;
        public fileType file_Header;

    }
}
