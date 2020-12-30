using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    public class FHMFileInfo
    {
        // FHM Metadata
        public int totalFileSize;
        public int numberofFiles;
        public int FHMChunkSize;
        public Internals.additionalInfo additionalInfoFlag;

        /*
        public FHMFileInfo(int totalFileSize, int numberofFiles, int FHMChunkSize)
        {
            this.totalFileSize = totalFileSize;
            this.numberofFiles = numberofFiles;
            this.FHMChunkSize = FHMChunkSize;
        }
        */
    }
}
