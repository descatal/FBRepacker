using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack
{
    class STREAM : Internals
    {
        int audioEntries = 0, STREAMPosition = 0, STREAMHeaderChunkSize = 0, STREAMDataChunkSize = 0, audioTotalFileSize = 0, sampleRate = 0, audioDataSize = 0, audioFileNumber = 1;

        public STREAM(FileStream PAC, int FHMOffset) : base()
        {
            changeStreamFile(PAC);
            STREAMPosition = FHMOffset;
        }

        public void repack()
        {
            //string audioEntriesStr = getFileInfoProperties("--STREAM--", "Number of audio files");
            //int.TryParse(audioEntriesStr, out int audioEntries);
            
        }
    }
}
