using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    public class GeneralFileInfo : FHMFileInfo
    {
        public int fileSize;
        public int fileNo;
        public string header;
        public int FHMOffset;
        public int FHMAssetLoadEnum;
        public int FHMunkEnum;
        public string fileName;

        public int FHMFileNumber;
        public string FHMFileName;

        public bool isLinked = false;
        public int linkedFileNo;
        public string linkedFileName;

        // not used
        public int fileNoinFHM;

        public GeneralFileInfo()
        {

        }
    }
}
