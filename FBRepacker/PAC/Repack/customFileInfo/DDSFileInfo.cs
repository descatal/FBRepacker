using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    class DDSFileInfo
    {
        public int fileNo;
        public int DDSFileChunkSize;
        public int numberofMipmaps;
        public int widthReso;
        public int heightReso;
        public int pixelFormatRGBAByteSize;
        public byte[] hexName;
        public string CompressionType;
        public MemoryStream DDSByteStream;

        public DDSFileInfo()
        {
            DDSByteStream = new MemoryStream();
        }
    }
}
