using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBRepacker.extractPAC
{
    class NTP3 : Internals
    {
        short numberofDDS = 0, widthResolution = 0, heightResolution = 0;
        int ddsDataChunkSize = 0, NTP3HeaderChunkSize = 0, FHMOffset = 0, DDSFileNumber = 1;
        string compressionType = string.Empty, DDSFileName = string.Empty;
        bool isCompressed = false, byteReversed = false;
        byte[] remainderNTP3Chunk = new byte[0]; // 0 for initialization purpose only.
        byte[] GIDXChunk = new byte[0];
        byte[] DDSHeaderChunk = new byte[0];
        byte[] DDSDataChunk = new byte[0];

        public static Dictionary<int, string> DDSCompressionType = new Dictionary<int, string>
            {
                { 0x0002, "DXT5" },
                { 0x0000, "DXT1"},
                { 0x000E, "No Compression byteReversed"},
                { 0x0007, "No Compression"}
            };

        public NTP3(FileStream PAC, int FHMOffset) : base(PAC)
        {
            this.FHMOffset = FHMOffset;
        }

        public void extract()
        {
            if(checkLinked())
                parseNTP3();
        }

        private bool checkLinked()
        {
            /* What this does is to check if the current FHMOffset that extract() is called has already been extracted. 
             * For FHM, there will be a number of linked (duplicate) offset that points to the same NTP3 header. 
             * The duplicate offset is actually pointing to a group of NTP3, but it is impossible to tell.
             * Hence, what we do in this class is to check if the NTP3 is 'multiple' type, and extract them all at once. 
             * Hence, if the next file is pointing to the same NTP3, we will extract the same dds images again, and this is wrong. 
             */
            if (NTP3LinkedOffset.Contains(FHMOffset))
            {
                return false;
            }
            else
            {
                NTP3LinkedOffset.Add(FHMOffset);
                return true;
            }
        }

        private void parseNTP3()
        {
            readNumberofFiles();

            while(DDSFileNumber <= numberofDDS)
            {
                readMetadata();
                parseRemainderNTP3Chunks();
                createDDSHeader();
                parseDDSDataChunk();

                List<byte[]> buffer = new List<byte[]>();
                buffer.Add(DDSHeaderChunk);
                buffer.Add(DDSDataChunk);
                byte[] DDSBuffer = buffer.SelectMany(b => b).ToArray();

                createFile("dds", DDSBuffer, createDDSExtractFilePath(fileNumber));

                DDSFileNumber++;
            }

            DDSFileNumber = 1;
        }

        private void readNumberofFiles()
        {
            PAC.Seek(0x02, SeekOrigin.Current);
            numberofDDS = readShort(PAC.Position, true);
            appendPACInfo("Number of Files: " + numberofDDS.ToString());
        }

        private void readMetadata()
        {
            int seekRange = DDSFileNumber > 1 ? 0x08 : 0x10; // Multiple NTP3 file has none of the 0x10 NTP3 header chunk, so we only have to skip half of it (0x08).
            PAC.Seek(seekRange, SeekOrigin.Current);
            ddsDataChunkSize = readIntBigEndian(PAC.Position);
            NTP3HeaderChunkSize = readShort(PAC.Position, true);
            PAC.Seek(0x04, SeekOrigin.Current);
            compressionType = identifyCompressionType(readShort(PAC.Position, true));
            byteReversed = compressionType.Contains("byteReversed") ? true : false;

            if (!compressionType.Contains("No Compression"))
                isCompressed = true;

            widthResolution = readShort(PAC.Position, true);
            heightResolution = readShort(PAC.Position, true);
        }

        private void parseRemainderNTP3Chunks()
        {
            long NTP3RemainderSize = NTP3HeaderChunkSize - 0x28; // NTP3 header chunk (useful) metadata is always 0x28 in size, and subtracting the metadata size with whole NTP3 header chunk size will get the remainder size. 
            remainderNTP3Chunk = extractChunk(PAC.Position, NTP3RemainderSize); // Extracting the remainder NTP3 Chunks to be used for repacking. The chunk meaning is not known yet, so copy & paste is the only option.
            GIDXChunk = extractChunk(PAC.Position, 0x10); // GIDX Chunk have fixed 0x10 size, and is technically outside NTP3 header Chunk. (This is technically counted as DDS file data).
            DDSFileName = readIntBigEndian(PAC.Position - 0x08).ToString("X4"); // Read the DDS File Name in the GIDX Chunk.
            PAC.Seek(0x04, SeekOrigin.Current);
            appendPACInfo("#DDS: " + DDSFileNumber);
            appendPACInfo("Name: " + DDSFileName);
            appendPACInfo("remainderNTP3Chunk: " + Encoding.Default.GetString(remainderNTP3Chunk));
            appendPACInfo("GIDXChunk: " + Encoding.Default.GetString(GIDXChunk));
        }

        private void createDDSHeader()
        {
            if (DDSHeaderChunk.Length != 0)
                DDSHeaderChunk = new byte[0];

            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x44445320, true); // Fixed DDS Header
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x0000007C, false); // Fixed 7C 

            uint compressFlag = isCompressed ? (uint)0x07100800 : (uint)0x0F100000; // Set compressFlag.
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, compressFlag, true);

            // Set width and height resolution info.
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, (uint)heightResolution, false); // width and height resolution is little endian int32.
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, (uint)widthResolution, false);
            
            
            // Set ddsSize info
            uint ddsSize = isCompressed ? (uint)ddsDataChunkSize : (uint)widthResolution * 4; // For uncompressed the size is always widthResolution * 4, no idea why.
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, ddsSize, false);

            // Set 0x34 sized null
            DDSHeaderChunk = appendZeroByteStream(DDSHeaderChunk, 0x34);
            // Set fixed 0x20 
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00000020, false);

            // Write end flags depending on compressed method. (Alot of them are fixed)
            if (isCompressed)
            {
                writeDDSHeaderEndCompressed();
            }
            else
            {
                if(byteReversed)
                {
                    writeDDSHeaderEndUncompressedbyteReversed();
                }
                else
                {
                    writeDDSHeaderEndUncompressed();
                }
                
            }

            // 0x10 zero chunks
            DDSHeaderChunk = appendZeroByteStream(DDSHeaderChunk, 0x10);
        }

        // https://docs.microsoft.com/en-us/windows/win32/direct3ddds/dds-pixelformat
        // I have no idea how NTP3 stores these infos that can be converted to DDS, but here's the three only examples I found that has different End Bytes. 

        private void writeDDSHeaderEndUncompressedbyteReversed()
        {
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00000041, false);
            DDSHeaderChunk = appendZeroByteStream(DDSHeaderChunk, 0x04);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00000020, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00FF0000, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x0000FF00, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x000000FF, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0xFF000000, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00001000, false);
        }

        private void writeDDSHeaderEndUncompressed()
        {
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00000040, false);
            DDSHeaderChunk = appendZeroByteStream(DDSHeaderChunk, 0x04);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00000010, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x0000F800, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x000007E0, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x0000001F, false);
            DDSHeaderChunk = appendZeroByteStream(DDSHeaderChunk, 0x04);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00001000, false);
        }

        private void writeDDSHeaderEndCompressed()
        {
            uint compressedTypeFlag = compressionType == "DXT5" ? (uint)0x44585435 : (uint)0x44585431;
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00000004, false);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, compressedTypeFlag, true);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x40E7A100, true);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x2368AB80, true);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0xFEFFFFFF, true);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0xFC9BA100, true);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0xE517A100, true);
            DDSHeaderChunk = appendIntByteStream(DDSHeaderChunk, 0x00001000, false);
        }

        private void parseDDSDataChunk()
        {
            DDSDataChunk = extractChunk(PAC.Position, ddsDataChunkSize);
            if(byteReversed)
                DDSDataChunk = reverseEndianess(DDSDataChunk, 4);
        }

        private string createDDSExtractFilePath(int fileNumber)
        {
            string filePath = currDirectory + @"\" + fileNumber.ToString("000");
            if (numberofDDS > 1)
            {
                filePath += ("-" + DDSFileNumber.ToString("000") + " (" + DDSFileName + ")");
            }
            return filePath;
        }

        // Utilities
        private string identifyCompressionType(short magic)
        {
            if (DDSCompressionType.ContainsKey(magic))
            {
                return DDSCompressionType[magic];
            }
            else
            {
                throw new Exception("Compression magic unidentified.");
            }
        }
    }
}
