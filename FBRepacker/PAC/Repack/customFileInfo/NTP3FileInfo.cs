using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    class NTP3FileInfo
    {
        public int fileNo;
        public int DDSDataChunkSize;
        public int beforeCompressionShort;
        public int NTP3HeaderChunkSize;
        public int widthReso;
        public int heightReso;
        public byte[] hexName;
        public byte[] remainderNTP3Chunk;
        public byte[] GIDXChunk;
        public string CompressionType;
    }
}
