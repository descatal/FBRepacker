using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    class GeneralFileInfo
    {
        public int fileSize;
        public int fileNo;
        public string header;
        public int FHMOffset;
        public int fileNoinFHM;
        public bool isLinked;
        public int linkFileNumber;

        public GeneralFileInfo()
        {

        }
    }
}
