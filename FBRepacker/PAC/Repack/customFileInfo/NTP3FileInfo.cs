using System;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.PAC.Repack.customFileInfo
{
    public class NTP3FileInfo
    {
        public int fileNo;
        public int DDSDataChunkSize;
        public int numberofMipmaps;
        public int NTP3HeaderChunkSize;
        public int widthReso;
        public int heightReso;
        public byte[] hexName;
        public byte[] eXtChunk;
        public byte[] GIDXChunk;
        public string pixelFormat;
        public string CompressionType;
        public string fileName;
        public List<int> mipmapsSizeList = new List<int>();
    }
}
