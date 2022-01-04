using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.Psarc.V2
{
    internal class TOCFileInfo
    {
        public uint totalFileEntries { get; set; }

        public List<PACFileInfoV2> allFiles { get; set; }

        public TOCFileInfo()
        {
            allFiles = new List<PACFileInfoV2>();
        }
    }
}
